using Digimezzo.Foundation.Core.Logging;
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
        private IAlbumImageRepository albumImageRepository;

        // Factories
        private ISQLiteConnectionFactory factory;
        private IUnitOfWorksFactory unitOfWorksFactory;

        // Watcher
        private FolderWatcherManager watcherManager;

        // Paths

        // Flags
        private bool isIndexing;
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
            get { return isIndexing; }
        }

        public IndexingService(ISQLiteConnectionFactory factory, ICacheService cacheService, IInfoDownloadService infoDownloadService,
            ITrackVRepository trackVRepository, IFolderVRepository folderVRepository, IAlbumArtworkRepository albumArtworkRepository, IAlbumVRepository albumVRepository,
            IUnitOfWorksFactory unitOfWorksFactory, IAlbumImageRepository albumImageRepository)
        {
            this.cacheService = cacheService;
            this.infoDownloadService = infoDownloadService;
            this.trackVRepository = trackVRepository;
            this.albumVRepository = albumVRepository;
            this.folderVRepository = folderVRepository;
            this.albumArtworkRepository = albumArtworkRepository;
            this.factory = factory;
            this.unitOfWorksFactory = unitOfWorksFactory;
            this.albumImageRepository = albumImageRepository;

            watcherManager = new FolderWatcherManager(folderVRepository);

            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += SettingsClient_SettingChanged;
            watcherManager.FoldersChanged += WatcherManager_FoldersChanged;

            isIndexing = false;
        }

        private async void SettingsClient_SettingChanged(object sender, Digimezzo.Foundation.Core.Settings.SettingChangedEventArgs e)
        {
            if (SettingsClient.IsSettingChanged(e, "Indexing", "RefreshCollectionAutomatically"))
            {
                if ((bool)e.Entry.Value)
                {
                    await watcherManager.StartWatchingAsync();
                }
                else
                {
                    await watcherManager.StopWatchingAsync();
                }
            }
        }

        public async void OnFoldersChanged()
        {
            if (SettingsClient.Get<bool>("Indexing", "RefreshCollectionAutomatically"))
            {
                await watcherManager.StartWatchingAsync();
            }
        }

        public async Task RefreshCollectionAsync(bool bForce, bool bReadTags = false)
        {
            await PrivateRefreshCollectionAsync(bReadTags);
        }

        private async Task PrivateRefreshCollectionAsync(bool bReReadTags)
        {
            Debug.Print("ENTERING PrivateRefreshCollectionAsync");
            if (IsIndexing)
            {
                Debug.Print("EXITING PrivateRefreshCollectionAsync (It already works)");
                return;
            }
            isIndexing = true;
            LogClient.Info("+++ STARTED CHECKING COLLECTION +++");

            canIndexArtwork = false;

            // Wait until artwork indexing is stopped
            while (isIndexingArtwork)
            {
                await Task.Delay(100);
            }
            await watcherManager.StopWatchingAsync();
            IndexingStarted(this, new EventArgs());
            await Task.Run(async () =>
            {
                Debug.Print("ENTERING PrivateRefreshCollectionAsync (TASK)");

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

                    FileOperations.GetFiles(folder.Path,
                        async (path) =>
                        {
                            //=== Check the extension
                            if (!FileFormats.SupportedMediaExtensions.Contains(Path.GetExtension(path.ToLower())))
                                return;

                            //=== Check the DB for the path
                            TrackV trackV = trackVRepository.GetTrackWithPath(path);
                            //TrackV trackV = uc.GetTrackWithPath(path);
                            long DateFileModified = FileUtils.DateModifiedTicks(path);
                            if (trackV != null && DateFileModified == trackV.DateFileModified && !bReReadTags)
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
                            FileMetadata fileMetadata = null;
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
                                mediaFileData.Lyrics = string.IsNullOrEmpty(fileMetadata.Lyrics.Value) ? null : new MediaFileDataText() { Text = fileMetadata.Lyrics.Value };
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

                            using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                            {
                                if (trackV == null)
                                {
                                    LogClientA.Info(String.Format("Adding file: {0}", path));
                                    AddMediaFileResult result = uc.AddMediaFile(mediaFileData, folder.Id);
                                    if (result.Success)
                                    {
                                        addedFiles++;
                                        if (fileMetadata != null && result.AlbumId != null)
                                            await addAlbumImageAsync(fileMetadata, (long)result.AlbumId, uc);
                                    }
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
                                    UpdateMediaFileResult result = uc.UpdateMediaFile(trackV, mediaFileData);
                                    if (result.Success)
                                    {
                                        updatedFiles++;
                                        //=== Add Image (if needed)
                                        if (fileMetadata != null && result.AlbumId != null)
                                            await addAlbumImageAsync(fileMetadata, (long)result.AlbumId, uc);

                                    }
                                    else
                                        failedFiles++;

                                }
                            }

                        },
                        () =>
                        {
                            return bContinue;
                        },
                        (ex) =>
                        {
                            LogClientA.Info(String.Format("Exception: {0}", ex.Message));
                        }
                        );
                    bool isTracksChanged = (addedFiles + updatedFiles + removedFiles) > 0;
                    bool isArtworkCleanedUp = false;
                    //=== STEP 3
                    //=== CLEAN UP AlbumImages
                    using (ICleanUpAlbumImagesUnitOfWork cleanUpAlbumImagesUnitOfWork = unitOfWorksFactory.getCleanUpAlbumImages())
                    {
                        isArtworkCleanedUp = cleanUpAlbumImagesUnitOfWork.CleanUp() > 0;
                    }
                    IList<AlbumImage> images = albumImageRepository.GetAlbumImages();
                    long imageDeletions = 0;
                    HashSet<string> imagePaths = new HashSet<string>(images.Select(x => Path.GetFileNameWithoutExtension(cacheService.GetCachedArtworkPath(x.Path))).ToList());
                    FileOperations.GetFiles(cacheService.CoverArtCacheFolderPath,
                        (path) =>
                        {
                            path = path.ToLower();
                            string ext = Path.GetExtension(path);
                            string name = Path.GetFileNameWithoutExtension(path);

                            if (!ext.Equals(".jpg"))
                                return;
                            if (!imagePaths.Contains(name))
                            {
                                imageDeletions++;
                                Debug.Print("Delete unused image?; {0}", path);
                                System.IO.File.Delete(path);
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

                    // Refresh lists
                    // -------------
                    if (isTracksChanged || isArtworkCleanedUp)
                    {
                        LogClient.Info("Sending event to refresh the lists because: isTracksChanged = {0}, isArtworkCleanedUp = {1}", isTracksChanged, isArtworkCleanedUp);
                        RefreshLists(this, new EventArgs());
                    }

                    // Finalize
                    // --------
                    isIndexing = false;
                    IndexingStopped(this, new EventArgs());

                    AddArtworkInBackgroundAsync(false, false);

                    if (SettingsClient.Get<bool>("Indexing", "RefreshCollectionAutomatically"))
                    {
                        await watcherManager.StartWatchingAsync();
                    }

                }
                Debug.Print("EXITING PrivateRefreshCollectionAsync (TASK)");
            });
            Debug.Print("EXITING PrivateRefreshCollectionAsync");
        }


        private async Task addAlbumImageAsync(FileMetadata fileMetadata, long AlbumId, IUpdateCollectionUnitOfWork uc)
        {
            Debug.Print("addAlbumImageAsync for: {0}", fileMetadata.Path);
            byte[] b = IndexerUtils.GetEmbeddedArtwork(fileMetadata);
            if (b != null)
            {
                IList<AlbumImage> images = albumImageRepository.GetAlbumImages(AlbumId);
                string sourceHash = System.Convert.ToBase64String(new System.Security.Cryptography.SHA1Cng().ComputeHash(b));
                bool bAddImage = false;
                if (ListExtensions.IsNullOrEmpty(images))
                    bAddImage = true;
                else
                {
                    AlbumImage imageWithTheSameSourcePath = images.SingleOrDefault(x => x.SourceHash == sourceHash);
                    bAddImage = imageWithTheSameSourcePath == null;
                }
                if (bAddImage)
                {
                    string imagePath = await cacheService.CacheArtworkAsync(b);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        string realPath = cacheService.GetCachedArtworkPath("cache://" + imagePath);
                        long len = (new FileInfo(realPath)).Length;
                        uc.AddAlbumImage(AlbumId, "cache://" + imagePath, len, sourceHash, "[tag]", false);//=== ALEX TODO. Is the fileSize Correct?
                    }
                }

            }
        }


        private async void WatcherManager_FoldersChanged(object sender, EventArgs e)
        {
            await RefreshCollectionAsync(false, false);
        }

        private async Task<string> GetArtworkFromFile(long albumId)
        {
            List<TrackV> tracks = trackVRepository.GetTracksOfAlbums(new List<long>() { albumId });
            if (tracks.Count == 0)
                return null;
            //tracks = tracks.OrderBy(t => t.DateFileModified).ToList();
            tracks.Sort((x, y) => x.DateFileModified.CompareTo(y.DateFileModified));
            return await cacheService.CacheArtworkAsync(IndexerUtils.GetArtwork(albumId, new FileMetadata(tracks.First().Path)));
        }

        private async Task<string> GetArtworkFromInternet(string albumTitle, IList<string> albumArtists, string trackTitle, IList<string> artists)
        {
            string artworkUriString = await infoDownloadService.GetAlbumImageAsync(albumTitle, albumArtists, trackTitle, artists);
            return await cacheService.CacheArtworkAsync(artworkUriString);
        }

        private async void AddArtworkInBackgroundAsync(bool rescanFailed, bool rescanAll)
        {
            // First, add artwork from file.
            await AddArtworkInBackgroundAsync(1, rescanFailed, rescanAll);

            // Next, add artwork from the Internet, if the user has chosen to do so.
            if (SettingsClient.Get<bool>("Covers", "DownloadMissingAlbumCovers"))
            {
                // Add artwork from the Internet.
                await AddArtworkInBackgroundAsync(2, rescanFailed, rescanAll);
            }
        }

        private async Task AddArtworkInBackgroundAsync(int passNumber, bool rescanFailed, bool rescanAll)
        {
            LogClient.Info("+++ STARTED ADDING ARTWORK IN THE BACKGROUND +++");
            canIndexArtwork = true;
            isIndexingArtwork = true;

            DateTime startTime = DateTime.Now;

            await Task.Run(async () =>
            {
                using (SQLiteConnection conn = factory.GetConnection())
                {
                    try
                    {
                        IList<string> albumKeysWithArtwork = new List<string>();

                        IList<AlbumV> albumDatasToIndex = rescanAll ? albumVRepository.GetAlbums() : albumVRepository.GetAlbumsToIndex(rescanFailed);

                        foreach (AlbumV albumDataToIndex in albumDatasToIndex)
                        {
                            // Check if we must cancel artwork indexing
                            if (!canIndexArtwork)
                            {
                                try
                                {
                                    LogClient.Info("+++ ABORTED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
                                    AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { AlbumKeys = albumKeysWithArtwork }); // Update UI
                                }
                                catch (Exception ex)
                                {
                                    LogClient.Error("Failed to commit changes while aborting adding artwork in background. Exception: {0}", ex.Message);
                                }

                                isIndexingArtwork = false;

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
                                    ArtworkID = await GetArtworkFromFile(albumDataToIndex.Id);
                                }
                                else if (passNumber.Equals(2))
                                {
                                    // During the 2nd pass, look for artwork on the Internet and set NeedsAlbumArtworkIndexing = 0.
                                    // We don't want future passes to index for this AlbumKey anymore.
                                    ArtworkID = await GetArtworkFromInternet(
                                        albumDataToIndex.AlbumArtists,
                                        DataUtils.SplitAndTrimColumnMultiValue(albumDataToIndex.AlbumArtists).ToList(),
                                        null, //albumDataToIndex.TrackTitle,
                                        DataUtils.SplitAndTrimColumnMultiValue(albumDataToIndex.Artists).ToList()
                                        );
                                }

                                if (!string.IsNullOrEmpty(ArtworkID))
                                {
                                    albumVRepository.AddImage(albumDataToIndex, ArtworkID, true);
                                }

                                // If artwork was found for 20 albums, trigger a refresh of the UI.
                                if (albumKeysWithArtwork.Count >= 20)
                                {
                                    var eventAlbumKeys = new List<string>(albumKeysWithArtwork);
                                    albumKeysWithArtwork.Clear();
                                    AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { AlbumKeys = eventAlbumKeys }); // Update UI
                                }
                            }
                            catch (Exception ex)
                            {
                                LogClient.Error("There was a problem while updating the cover art for Album {0}/{1}. Exception: {2}", albumDataToIndex.Name, albumDataToIndex.AlbumArtists, ex.Message);
                            }
                        }

                        try
                        {
                            AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { AlbumKeys = albumKeysWithArtwork }); // Update UI
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

            isIndexingArtwork = false;
            LogClient.Error("+++ FINISHED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
        }

        public async void ReScanAlbumArtworkAsync(bool onlyWhenHasNoCover)
        {
            canIndexArtwork = false;

            // Wait until artwork indexing is stopped
            while (isIndexingArtwork)
            {
                await Task.Delay(100);
            }

            //await trackRepository.EnableNeedsAlbumArtworkIndexingForAllTracksAsync(onlyWhenHasNoCover);

            AddArtworkInBackgroundAsync(true, false);
        }

    }
}
