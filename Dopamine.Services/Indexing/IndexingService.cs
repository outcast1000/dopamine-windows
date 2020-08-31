﻿using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Alex; //Digimezzo.Foundation.Core.Settings
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Core.IO;
using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Data.Repositories;
using Dopamine.Data.UnitOfWorks;
using Dopamine.Services.Cache;
using Dopamine.Services.InfoDownload;
using Dopamine.Services.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Dopamine.Services.Indexing
{
    public class IndexingService : IIndexingService
    {
        // Services
        private ICacheService cacheService;
        private IInfoDownloadService infoDownloadService;

        // Repositories
        private ITrackVRepository trackVRepository;
        private IAlbumVRepository albumVRepository;
        private IAlbumArtworkRepository albumArtworkRepository;
        private IFolderVRepository folderVRepository;

        // Factories
        private ISQLiteConnectionFactory factory;
        private IUnitOfWorksFactory unitOfWorksFactory;

        // Watcher
        private FolderWatcherManager watcherManager;

        // Paths
        private List<FolderPathInfo> allDiskPaths;
        private List<FolderPathInfo> newDiskPaths;

        // Cache
        private IndexerCache cache;

        // Flags
        private bool isIndexing;
        private bool isFoldersChanged;
        private bool canIndexArtwork;
        private bool isIndexingArtwork;

        // Events
        public event EventHandler IndexingStopped = delegate { };
        public event EventHandler IndexingStarted = delegate { };
        public event Action<IndexingStatusEventArgs> IndexingStatusChanged = delegate { };
        public event EventHandler RefreshLists = delegate { };
        public event EventHandler RefreshArtwork = delegate { };
        public event AlbumArtworkAddedEventHandler AlbumArtworkAdded = delegate { };

        public bool IsIndexing
        {
            get { return this.isIndexing; }
        }

        public IndexingService(ISQLiteConnectionFactory factory, ICacheService cacheService, IInfoDownloadService infoDownloadService,
            ITrackVRepository trackVRepository, IFolderVRepository folderVRepository, IAlbumArtworkRepository albumArtworkRepository, IAlbumVRepository albumVRepository,
            IUnitOfWorksFactory unitOfWorksFactory)
        {
            this.cacheService = cacheService;
            this.infoDownloadService = infoDownloadService;
            this.trackVRepository = trackVRepository;
            this.albumVRepository = albumVRepository;
            this.folderVRepository = folderVRepository;
            this.albumArtworkRepository = albumArtworkRepository;
            this.factory = factory;
            this.unitOfWorksFactory = unitOfWorksFactory;

            this.watcherManager = new FolderWatcherManager(this.folderVRepository);
            this.cache = new IndexerCache(factory, trackVRepository);

            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += SettingsClient_SettingChanged;
            this.watcherManager.FoldersChanged += WatcherManager_FoldersChanged;

            this.isIndexing = false;
        }

        private async void SettingsClient_SettingChanged(object sender, Digimezzo.Foundation.Core.Settings.SettingChangedEventArgs e)
        {
            if (SettingsClient.IsSettingChanged(e, "Indexing", "RefreshCollectionAutomatically"))
            {
                if ((bool)e.Entry.Value)
                {
                    await this.watcherManager.StartWatchingAsync();
                }
                else
                {
                    await this.watcherManager.StopWatchingAsync();
                }
            }
        }

        public async void OnFoldersChanged()
        {
            this.isFoldersChanged = true;

            if (SettingsClient.Get<bool>("Indexing", "RefreshCollectionAutomatically"))
            {
                await this.watcherManager.StartWatchingAsync();
            }
        }

        public async Task RefreshCollectionAsync(bool bForce, bool bReadTags = false)
        {
            await PrivateRefreshCollectionAsync(bReadTags);
        }

        private async Task PrivateRefreshCollectionAsync(bool bReReadTags)
        {
            if (IsIndexing)
            {
                return;
            }
            this.isIndexing = true;
            LogClient.Info("+++ STARTED CHECKING COLLECTION +++");

            this.canIndexArtwork = false;

            // Wait until artwork indexing is stopped
            while (isIndexingArtwork)
            {
                await Task.Delay(100);
            }
            await this.watcherManager.StopWatchingAsync();
            this.IndexingStarted(this, new EventArgs());
            await Task.Run(async () =>
            {
                long addedFiles = 0;
                long updatedFiles = 0;
                long removedFiles = 0;
                long failedFiles = 0;
                List<FolderV> folders = folderVRepository.GetFolders();
                // Recursively get all the files in the collection folders
                bool bContinue = true;
                foreach (FolderV folder in folders)
                {
                    //=== STEP 1. DELETE ALL THE FILES from the collections that have been deleted from the disk 
                    //=== Get All the paths from the DB (in chunks of 1000)
                    //=== For each one of them
                    //=== if they exist then OK. Otherwise then set the date to "DELETED_AT"
                    if (!SettingsClient.Get<bool>("Indexing", "IgnoreRemovedFiles"))
                    {
                        long offset = 0;
                        const long limit = 1000;
                        while (true)
                        {
                            IList<TrackV> tracks = trackVRepository.GetTracksOfFolders(new List<long>() { folder.Id }, new QueryOptions() { Limit = limit, Offset = offset, WhereIgnored = QueryOptionsBool.Ignore, WhereVisibleFolders = QueryOptionsBool.Ignore });
                            foreach (TrackV track in tracks)
                            {
                                if (!System.IO.File.Exists(track.Path))
                                {
                                    LogClientA.Info(String.Format("File not found: {0}", track.Path));
                                    using (IDeleteMediaFileUnitOfWork uow = unitOfWorksFactory.getDeleteMediaFileUnitOfWork())
                                    {
                                        if (uow.DeleteMediaFile(track.Id))
                                            removedFiles++;
                                    }
                                }
                            }
                            if (tracks.Count < limit)
                                break;
                            offset += limit;
                        }
                    }

                    //=== STEP 2. Add OR Update the files that on disk
                    //=== TODO Use a factory HERE
                    using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                    {
                        FileOperations.GetFiles(folder.Path,
                        (path) =>
                        {
                            //=== Check the extension
                            if (!FileFormats.SupportedMediaExtensions.Contains(Path.GetExtension(path.ToLower())))
                                return;

                            //=== Check the DB for the path
                            TrackV trackV = uc.GetTrackWithPath(path);
                            long DateFileModified = FileUtils.DateModifiedTicks(path);
                            if (trackV != null && DateFileModified <= trackV.DateFileModified && !bReReadTags)
                            {
                                //LogClientA.Info(String.Format("No need to reprocess the file {0}", path));
                                return;
                            }
                            //=== Get File Info
                            MediaFileData mediaFileData = new MediaFileData()
                            {
                                Path = path,
                                Filesize = FileUtils.SizeInBytes(path),
                                Language = null,
                                DateAdded = DateTime.Now.Ticks,
                                Love = null,
                                DateFileCreated = FileUtils.DateCreatedTicks(path),
                                DateFileModified = DateFileModified,
                                DateFileDeleted = null,
                                DateIgnored = null
                            };
                            FileMetadata fileMetadata;
                            try
                            {
                                fileMetadata = new FileMetadata(path);
                                mediaFileData.Name = FormatUtils.TrimValue(fileMetadata.Title.Value);
                                mediaFileData.Bitrate = fileMetadata.BitRate;
                                mediaFileData.Samplerate = fileMetadata.SampleRate;
                                mediaFileData.Duration = Convert.ToInt64(fileMetadata.Duration.TotalMilliseconds);
                                mediaFileData.Year = (string.IsNullOrEmpty(fileMetadata.Year.Value) ? null : (long?)MetadataUtils.SafeConvertToLong(fileMetadata.Year.Value));
                                mediaFileData.Rating = fileMetadata.Rating.Value == 0 ? null : (long?)fileMetadata.Rating.Value;//Should you take it from the file?
                                mediaFileData.TrackNumber = string.IsNullOrEmpty(fileMetadata.TrackNumber.Value) ? null : (long?)MetadataUtils.SafeConvertToLong(fileMetadata.TrackNumber.Value);
                                mediaFileData.TrackCount = string.IsNullOrEmpty(fileMetadata.TrackCount.Value) ? null : (long?)MetadataUtils.SafeConvertToLong(fileMetadata.TrackCount.Value);
                                mediaFileData.DiscNumber = string.IsNullOrEmpty(fileMetadata.DiscNumber.Value) ? null : (long?)MetadataUtils.SafeConvertToLong(fileMetadata.DiscNumber.Value);
                                mediaFileData.DiscCount = string.IsNullOrEmpty(fileMetadata.DiscCount.Value) ? null : (long?)MetadataUtils.SafeConvertToLong(fileMetadata.DiscCount.Value);
                                mediaFileData.Lyrics = fileMetadata.Lyrics.Value;
                                mediaFileData.Artists = fileMetadata.Artists.Values;
                                mediaFileData.Genres = fileMetadata.Genres.Values;
                                mediaFileData.Album = FormatUtils.TrimValue(fileMetadata.Album.Value);
                                mediaFileData.AlbumArtists = fileMetadata.AlbumArtists.Values;
                            }
                            catch (Exception ex)
                            {
                                LogClientA.Info(String.Format("Unable to READ DATA from the file {0} {1}", path, ex.Message));
                                mediaFileData.Name = Path.GetFileNameWithoutExtension(path);
                                //=== TODO. Do something more advanced like getting tags from path
                            }
                            if (string.IsNullOrEmpty(mediaFileData.Name))
                                mediaFileData.Name = Path.GetFileNameWithoutExtension(path);

                            if (trackV == null)
                            {
                                LogClientA.Info(String.Format("Adding file: {0}", path));

                                if (uc.AddMediaFile(mediaFileData, folder.Id))
                                    addedFiles++;
                                else
                                    failedFiles++;
                            }
                            else
                            {
                                LogClientA.Info(String.Format("Updating file: {0}", path));
                                //= If we update the file we do not want to change these Dates
                                mediaFileData.DateAdded = trackV.DateAdded;
                                mediaFileData.DateIgnored = trackV.DateIgnored;
                                //= If the file was previously deleted then now it seem that i re-emerged
                                mediaFileData.DateFileDeleted = null;
                                //== Love is not saved in tags
                                mediaFileData.Love = trackV.Love;
                                mediaFileData.Language = trackV.Language;
                                if (uc.UpdateMediaFile(trackV, mediaFileData))
                                    updatedFiles++;
                                else
                                    failedFiles++;

                            }
                        },
                            () =>
                            {
                                return bContinue;
                            },
                            (ex) =>
                            {
                                LogClientA.Info(String.Format("Exception: {0}", ex.Message));
                            });
                    }
                    //=== STEP 3
                    bool isArtworkCleanedUp = await this.CleanupArtworkAsync();
                    bool isTracksChanged = (addedFiles + updatedFiles + removedFiles) > 0;
                    // Refresh lists
                    // -------------
                    if (isTracksChanged || isArtworkCleanedUp)
                    {
                        LogClient.Info("Sending event to refresh the lists because: isTracksChanged = {0}, isArtworkCleanedUp = {1}", isTracksChanged, isArtworkCleanedUp);
                        this.RefreshLists(this, new EventArgs());
                    }

                    // Finalize
                    // --------
                    this.isIndexing = false;
                    this.IndexingStopped(this, new EventArgs());

                    this.AddArtworkInBackgroundAsync(false, false);

                    if (SettingsClient.Get<bool>("Indexing", "RefreshCollectionAutomatically"))
                    {
                        await this.watcherManager.StartWatchingAsync();
                    }
                }
            });
        }



        private async void WatcherManager_FoldersChanged(object sender, EventArgs e)
        {
            await this.RefreshCollectionAsync(false, false);
        }

        private async Task<long> DeleteUnusedArtworkFromCacheAsync()
        {
            long numberDeleted = 0;

            await Task.Run(async () =>
            {
                string[] artworkFiles = Directory.GetFiles(this.cacheService.CoverArtCacheFolderPath, "album-*.jpg");

                using (SQLiteConnection conn = this.factory.GetConnection())
                {
                    IList<string> artworkIds = await this.albumArtworkRepository.GetArtworkIdsAsync();

                    foreach (string artworkFile in artworkFiles)
                    {
                        if (!artworkIds.Contains(Path.GetFileNameWithoutExtension(artworkFile)))
                        {
                            try
                            {
                                System.IO.File.Delete(artworkFile);
                                numberDeleted += 1;
                            }
                            catch (Exception ex)
                            {
                                LogClient.Error("There was a problem while deleting cached artwork {0}. Exception: {1}", artworkFile, ex.Message);
                            }
                        }
                    }
                }
            });

            return numberDeleted;
        }

        private async Task<bool> CleanupArtworkAsync()
        {
            LogClient.Info("+++ STARTED CLEANING UP ARTWORK +++");

            DateTime startTime = DateTime.Now;
            long numberDeletedFromDatabase = 0;
            long numberDeletedFromDisk = 0;

            try
            {
                // Step 1: delete unused AlbumArtwork from the database (Which isn't mapped to a Track's AlbumKey)
                // -----------------------------------------------------------------------------------------------
                numberDeletedFromDatabase = await this.albumArtworkRepository.DeleteUnusedAlbumArtworkAsync();

                // Step 2: delete unused artwork from the cache
                // --------------------------------------------
                numberDeletedFromDisk = await this.DeleteUnusedArtworkFromCacheAsync();
            }
            catch (Exception ex)
            {
                LogClient.Info("There was a problem while updating the artwork. Exception: {0}", ex.Message);
            }

            LogClient.Info("+++ FINISHED CLEANING UP ARTWORK: Covers deleted from database: {0}. Covers deleted from disk: {1}. Time required: {3} ms +++", numberDeletedFromDatabase, numberDeletedFromDisk, Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));

            return numberDeletedFromDatabase + numberDeletedFromDisk > 0;
        }

        private async Task<string> GetArtworkFromFile(long albumId)
        {
            List<TrackV> tracks = this.trackVRepository.GetTracksOfAlbums(new List<long>() { albumId });
            if (tracks.Count == 0)
                return null;
            //tracks = tracks.OrderBy(t => t.DateFileModified).ToList();
            tracks.Sort((x, y) => x.DateFileModified.CompareTo(y.DateFileModified));
            return await this.cacheService.CacheArtworkAsync(IndexerUtils.GetArtwork(albumId, new FileMetadata(tracks.First().Path)));
        }

        private async Task<string> GetArtworkFromInternet(string albumTitle, IList<string> albumArtists, string trackTitle, IList<string> artists)
        {
            string artworkUriString = await this.infoDownloadService.GetAlbumImageAsync(albumTitle, albumArtists, trackTitle, artists);
            return await this.cacheService.CacheArtworkAsync(artworkUriString);
        }

        private async void AddArtworkInBackgroundAsync(bool rescanFailed, bool rescanAll)
        {
            // First, add artwork from file.
            await this.AddArtworkInBackgroundAsync(1, rescanFailed, rescanAll);

            // Next, add artwork from the Internet, if the user has chosen to do so.
            if (SettingsClient.Get<bool>("Covers", "DownloadMissingAlbumCovers"))
            {
                // Add artwork from the Internet.
                await this.AddArtworkInBackgroundAsync(2, rescanFailed, rescanAll);
            }
        }

        private async Task AddArtworkInBackgroundAsync(int passNumber, bool rescanFailed, bool rescanAll)
        {
            LogClient.Info("+++ STARTED ADDING ARTWORK IN THE BACKGROUND +++");
            this.canIndexArtwork = true;
            this.isIndexingArtwork = true;

            DateTime startTime = DateTime.Now;

            await Task.Run(async () =>
            {
                using (SQLiteConnection conn = this.factory.GetConnection())
                {
                    try
                    {
                        IList<string> albumKeysWithArtwork = new List<string>();

                        IList<AlbumV> albumDatasToIndex = rescanAll ? albumVRepository.GetAlbums() : albumVRepository.GetAlbumsToIndex(rescanFailed);

                        foreach (AlbumV albumDataToIndex in albumDatasToIndex)
                        {
                            // Check if we must cancel artwork indexing
                            if (!this.canIndexArtwork)
                            {
                                try
                                {
                                    LogClient.Info("+++ ABORTED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
                                    this.AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { AlbumKeys = albumKeysWithArtwork }); // Update UI
                                }
                                catch (Exception ex)
                                {
                                    LogClient.Error("Failed to commit changes while aborting adding artwork in background. Exception: {0}", ex.Message);
                                }

                                this.isIndexingArtwork = false;

                                return;
                            }

                            // Start indexing artwork
                            try
                            {
                                // Delete existing AlbumArtwork
                                albumVRepository.DeleteImage(albumDataToIndex);
                                string ArtworkID = null;

                                if (passNumber.Equals(1))
                                {
                                    // During the 1st pass, look for artwork in file(s).
                                    // Only set NeedsAlbumArtworkIndexing = 0 if artwork was found. So when no artwork was found, 
                                    // this gives the 2nd pass a chance to look for artwork on the Internet.
                                    ArtworkID = await this.GetArtworkFromFile(albumDataToIndex.Id);
                                }
                                else if (passNumber.Equals(2))
                                {
                                    // During the 2nd pass, look for artwork on the Internet and set NeedsAlbumArtworkIndexing = 0.
                                    // We don't want future passes to index for this AlbumKey anymore.
                                    ArtworkID = await this.GetArtworkFromInternet(
                                        albumDataToIndex.AlbumArtists,
                                        DataUtils.SplitAndTrimColumnMultiValue(albumDataToIndex.AlbumArtists).ToList(),
                                        null, //albumDataToIndex.TrackTitle,
                                        DataUtils.SplitAndTrimColumnMultiValue(albumDataToIndex.Artists).ToList()
                                        );
                                }

                                if (!string.IsNullOrEmpty(ArtworkID))
                                {
                                    this.albumVRepository.AddImage(albumDataToIndex, ArtworkID, true);
                                }

                                // If artwork was found for 20 albums, trigger a refresh of the UI.
                                if (albumKeysWithArtwork.Count >= 20)
                                {
                                    var eventAlbumKeys = new List<string>(albumKeysWithArtwork);
                                    albumKeysWithArtwork.Clear();
                                    this.AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { AlbumKeys = eventAlbumKeys }); // Update UI
                                }
                            }
                            catch (Exception ex)
                            {
                                LogClient.Error("There was a problem while updating the cover art for Album {0}/{1}. Exception: {2}", albumDataToIndex.Name, albumDataToIndex.AlbumArtists, ex.Message);
                            }
                        }

                        try
                        {
                            this.AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { AlbumKeys = albumKeysWithArtwork }); // Update UI
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Failed to commit changes while finishing adding artwork in background. Exception: {0}", ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("Unexpected error occurred while updating artwork in the background. Exception: {0}", ex.Message);
                    }
                }
            });

            this.isIndexingArtwork = false;
            LogClient.Error("+++ FINISHED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
        }

        public async void ReScanAlbumArtworkAsync(bool onlyWhenHasNoCover)
        {
            this.canIndexArtwork = false;

            // Wait until artwork indexing is stopped
            while (this.isIndexingArtwork)
            {
                await Task.Delay(100);
            }

            //await this.trackRepository.EnableNeedsAlbumArtworkIndexingForAllTracksAsync(onlyWhenHasNoCover);

            this.AddArtworkInBackgroundAsync(true, false);
        }

    }
}
