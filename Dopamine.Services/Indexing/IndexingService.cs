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
using Dopamine.Data.Providers;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using Dopamine.Services.File;

namespace Dopamine.Services.Indexing
{
    public class IndexingService : IIndexingService
    {
        // Services
        private IInfoDownloadService infoDownloadService;

        // Repositories
        private ITrackVRepository trackVRepository;
        private IAlbumVRepository albumVRepository;
        private IArtistVRepository artistVRepository;
        //private IAlbumImageRepository albumArtworkRepository;
        private IFolderVRepository folderVRepository;
        private IAlbumImageRepository albumImageRepository;

        // Factories
        private ISQLiteConnectionFactory sQLiteConnectionFactory;
        private IUnitOfWorksFactory unitOfWorksFactory;
        private IInfoProviderFactory infoProviderFactory;

        // Watcher
        private FolderWatcherManager watcherManager;

        // Paths

        // Flags
        private bool isIndexing;
        private bool canIndexArtwork;
        private bool isIndexingArtwork;
        private bool canIndexArtistImages;
        private bool isIndexingArtistImages;

        // Events
        public event EventHandler IndexingStopped = delegate { };
        public event EventHandler IndexingStarted = delegate { };
        public event Action<IndexingStatusEventArgs> IndexingStatusChanged = delegate { };
        public event EventHandler RefreshLists = delegate { };
        public event EventHandler RefreshArtwork = delegate { };
        public event AlbumArtworkAddedEventHandler AlbumArtworkAdded = delegate { };
        public event ArtistImagesAddedEventHandler ArtistImagesAdded = delegate { };

        

        public bool IsIndexing
        {
            get { return isIndexing; }
        }

        public IndexingService(ISQLiteConnectionFactory sQLiteConnectionFactory, IInfoDownloadService infoDownloadService,
            ITrackVRepository trackVRepository, IFolderVRepository folderVRepository, IAlbumVRepository albumVRepository,
            IUnitOfWorksFactory unitOfWorksFactory, IAlbumImageRepository albumImageRepository, IArtistVRepository artistVRepository, 
            IInfoProviderFactory infoProviderFactory)
        {
            this.infoDownloadService = infoDownloadService;
            this.trackVRepository = trackVRepository;
            this.albumVRepository = albumVRepository;
            this.artistVRepository = artistVRepository;
            this.folderVRepository = folderVRepository;
            this.sQLiteConnectionFactory = sQLiteConnectionFactory;
            this.unitOfWorksFactory = unitOfWorksFactory;
            this.albumImageRepository = albumImageRepository;
            this.infoProviderFactory = infoProviderFactory;

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

        public async Task RefreshCollectionAsync(bool bForce, bool bReReadTags = false)
        {
            Debug.Print("ENTERING RefreshCollectionAsync");
            if (IsIndexing)
            {
                Debug.Print("EXITING PrivateRefreshCollectionAsync (ALREADY IN");
                return;
            }
            isIndexing = true;
            Trace.WriteLine("ENTERING RefreshCollectionAsync");
            LogClient.Info("ENTERING RefreshCollectionAsync");

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
                                        ITrackInfoProvider tag = infoProviderFactory.GetTrackInfoProviderFromTag(fileMetadata);
                                        if (tag.Success)
                                        {
                                            if (result.AlbumId != null)
                                                addAlbumImageFromTag(tag, (long)result.AlbumId, uc);
                                            addTrackLyricsFromTag(tag, (long)result.TrackId, uc);
                                        }
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
                                        ITrackInfoProvider tag = infoProviderFactory.GetTrackInfoProviderFromTag(fileMetadata);
                                        if (tag.Success)
                                        {
                                            if (tag.Success && result.AlbumId != null)
                                                addAlbumImageFromTag(tag, (long)result.AlbumId, uc);
                                            if (tag.Success)
                                                addTrackLyricsFromTag(tag, trackV.Id, uc);
                                        }

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
                    if (!ListExtensions.IsNullOrEmpty(images))
                    {
                        long imageDeletions = 0;
                        IFileStorage fileService = new FileStorage();
                        HashSet<string> imagePaths = new HashSet<string>(images.Select(x => Path.GetFileNameWithoutExtension(fileService.GetRealPath(x.Location))).ToList());
                        FileOperations.GetFiles(fileService.StorageImagePath,
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
                                    //== ALEX TODO. Temporary disabled. System.IO.File.Delete(path);
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
                    AddArtistImagesInBackgroundAsync(false, false);


                    if (SettingsClient.Get<bool>("Indexing", "RefreshCollectionAutomatically"))
                    {
                        await watcherManager.StartWatchingAsync();
                    }

                }
                Debug.Print("EXITING PrivateRefreshCollectionAsync (TASK)");
            });
            Debug.Print("EXITING PrivateRefreshCollectionAsync");
        }

        // This function saves the Image that is stored inside the Tags of the file
        // We should only keep one of these photos for each album (always overwrite the old one)
        private void addAlbumImageFromTag(ITrackInfoProvider tag, long albumId, IUpdateCollectionUnitOfWork uc)
        {

            //=== If there is already a primary image then do not try to replace it
            AlbumImage existingAlbumImage = albumImageRepository.GetPrimaryAlbumImage(albumId);
            if (existingAlbumImage != null)
            {
                Debug.Print("addAlbumImageFromTag Album image already exists. Exit");
                return;
            }
            if (tag.Data == null)
            {
                Debug.Print("addAlbumImageFromTag No image available in this file. Exit");
                return;
            }
            if (tag.Data.Images != null && tag.Data.Images.Length > 0)
            {
                Debug.Print("addAlbumImageFromTag Adding image");
                IFileStorage fileStorage = new FileStorage();
                string location = fileStorage.SaveImage(tag.Data.Images[0]);
                uc.AddAlbumImage(new AlbumImage()
                {
                    AlbumId = albumId,
                    DateAdded = DateTime.Now.Ticks,
                    Source = tag.ProviderName,
                    Location = location,
                    IsPrimary = true
                });
            }
        }

        private void addTrackLyricsFromTag(ITrackInfoProvider tag, long trackId, IUpdateCollectionUnitOfWork uc)
        {
            if (tag.Data == null)
            {
                Debug.Print("addTrackLyricsFromTag No image available in this file. Exit");
                return;
            }
            if (tag.Data.Lyrics != null && tag.Data.Lyrics.Length > 0)
            {
                Debug.Print("addTrackLyricsFromTag Adding lyrics");
                uc.SetLyrics(new TrackLyrics()
                {
                    TrackId = trackId,
                    DateAdded = DateTime.Now.Ticks,
                    Source = tag.ProviderName,
                    Lyrics = tag.Data.Lyrics[0]
                });
            }

        }


        private async void WatcherManager_FoldersChanged(object sender, EventArgs e)
        {
            await RefreshCollectionAsync(false, false);
        }

        private async Task AddArtworkInBackgroundAsync(bool rescanFailed, bool rescanAll)
        {
            if (!SettingsClient.Get<bool>("Covers", "DownloadMissingAlbumCovers"))
                Debug.Print("DownloadMissingAlbumCovers is false.");
            if (isIndexingArtwork)
            {
                Debug.Print("AddArtworkInBackgroundAsync [ALREADY IN]. Exiting...");
                return;
            }
            LogClient.Info("+++ STARTED ADDING ARTWORK IN THE BACKGROUND +++");
            canIndexArtwork = true;
            isIndexingArtwork = true;

            DateTime startTime = DateTime.Now;

            await Task.Run(() =>
            {
                try
                {
                    IList<AlbumV> albumsAdded = new List<AlbumV>();
                    string providerName = new LastFMAlbumInfoProvider(null, null).ProviderName;
                    IList<AlbumV> albumDatasToIndex = rescanAll ? albumVRepository.GetAlbums() : albumVRepository.GetAlbumsToIndexByProvider(providerName, rescanFailed);
                    IFileStorage fileStorage = new FileStorage();

                    foreach (AlbumV albumDataToIndex in albumDatasToIndex)
                    {
                        if (string.IsNullOrEmpty(albumDataToIndex.Name))
                            continue;
                        // Check if we must cancel artwork indexing
                        if (!canIndexArtwork)
                        {
                            try
                            {
                                LogClient.Info("+++ ABORTED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
                                AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { Albums = albumsAdded }); // Update UI
                            }
                            catch (Exception ex)
                            {
                                LogClient.Error("Failed to commit changes while aborting adding artwork in background. Exception: {0}", ex.Message);
                            }

                            isIndexingArtwork = false;

                            return;
                        }


                        using (var conn = this.sQLiteConnectionFactory.GetConnection())
                        {
                            conn.Execute("DELETE FROM AlbumDownloadFailed WHERE album_id=? AND provider=?", albumDataToIndex.Id, providerName);
                        }

                        // During the 2nd pass, look for artwork on the Internet and set NeedsAlbumArtworkIndexing = 0.
                        // We don't want future passes to index for this AlbumKey anymore.

                        //GetArtworkFromInternet(string albumTitle, IList<string> albumArtists, string trackTitle, IList<string> artists)

                        LastFMAlbumInfoProvider lf = new LastFMAlbumInfoProvider(albumDataToIndex.Name, DataUtils.SplitAndTrimColumnMultiValue(albumDataToIndex.AlbumArtists).ToArray());
                       

                        if (lf.Success && lf.Data.Images != null && lf.Data.Images.Length > 0)
                        {
                            using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                            {
                                string cacheId = fileStorage.SaveImage(lf.Data.Images[0]);
                                uc.AddAlbumImage(new AlbumImage()
                                {
                                    AlbumId = albumDataToIndex.Id,
                                    DateAdded = DateTime.Now.Ticks,
                                    Location = cacheId,
                                    IsPrimary = true,
                                    Source = lf.ProviderName
                                });// albumDataToIndex.Id, "cache://" + albumImageName, len, sourceHash, providerName, false);
                            }
                        }
                        else
                        {
                            using (var conn = this.sQLiteConnectionFactory.GetConnection())
                            {
                                conn.Insert(new AlbumDownloadFailed()
                                {
                                    AlbumId = albumDataToIndex.Id,
                                    DateAdded = DateTime.Now.Ticks,
                                    Provider = providerName
                                });
                            }
                        }

                        // If artwork was found for 20 albums, trigger a refresh of the UI.
                        if (albumsAdded.Count >= 20)
                        {
                            IList<AlbumV> eventArgs = albumsAdded.Select(item => item).ToList();
                            albumsAdded.Clear();
                            AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { Albums = eventArgs }); // Update UI
                        }
                    }

                }
                catch (Exception ex)
                {
                    LogClient.Error("Unexpected error occurred while updating artwork in the background. Exception: {0}", ex.Message);
                }
            });

            isIndexingArtwork = false;
            LogClient.Error("+++ FINISHED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
        }

        private async Task AddArtistImagesInBackgroundAsync(bool rescanFailed, bool rescanAll)
        {
            //=== ALEX TODO
            //if (!SettingsClient.Get<bool>("Covers", "DownloadMissingAlbumCovers"))
            //    Debug.Print("DownloadMissingAlbumCovers is false.");
            //=== ALEX TODO END
            if (isIndexingArtistImages)
            {
                Debug.Print("AddArtistImagesInBackgroundAsync [ALREADY IN]. Exiting...");
                return;
            }
            LogClient.Info("+++ STARTED ADDING ARTWORK IN THE BACKGROUND +++");
            canIndexArtistImages = true;
            isIndexingArtistImages = true;

            DateTime startTime = DateTime.Now;

            await Task.Run(() =>
            {
                try
                {
                    IList<ArtistV> artistsAdded = new List<ArtistV>();
                    string providerName = new GoogleArtistInfoProvider(null).ProviderName;
                    IList<ArtistV> artistsToIndex = rescanAll ? artistVRepository.GetArtists() : artistVRepository.GetArtistToIndexByProvider(providerName, rescanFailed);
                    IFileStorage fileStorage = new FileStorage();

                    foreach (ArtistV artist in artistsToIndex)
                    {
                        if (string.IsNullOrEmpty(artist.Name))
                            continue;
                        // Check if we must cancel artwork indexing
                        if (!canIndexArtistImages)
                        {
                            try
                            {
                                LogClient.Info("+++ ABORTED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
                                ArtistImagesAdded(this, new ArtistImagesAddedEventArgs() { Artists = artistsAdded }); // Update UI
                            }
                            catch (Exception ex)
                            {
                                LogClient.Error("Failed to commit changes while aborting adding artwork in background. Exception: {0}", ex.Message);
                            }

                            isIndexingArtwork = false;

                            return;
                        }


                        using (var conn = this.sQLiteConnectionFactory.GetConnection())
                        {
                            conn.Execute("DELETE FROM ArtistDownloadFailed WHERE artist_id=? AND provider=?", artist.Id, providerName);
                        }

                        // During the 2nd pass, look for artwork on the Internet and set NeedsAlbumArtworkIndexing = 0.
                        // We don't want future passes to index for this AlbumKey anymore.

                        //GetArtworkFromInternet(string albumTitle, IList<string> albumArtists, string trackTitle, IList<string> artists)

                        //IArtistInfoProvider ip = new GoogleArtistInfoProvider(artist.Name);
                        IArtistInfoProvider ip = new YoutubeArtistInfoProvider(artist.Name);


                        if (ip.Success && ip.Data.Images != null && ip.Data.Images.Length >= 1)
                        {
                            using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                            {
                                string cacheId = fileStorage.SaveImage(ip.Data.Images[0]);
                                uc.AddArtistImage(new ArtistImage()
                                {
                                    ArtistId = artist.Id,
                                    DateAdded = DateTime.Now.Ticks,
                                    Location = cacheId,
                                    IsPrimary = true,
                                    Source = ip.ProviderName
                                });// albumDataToIndex.Id, "cache://" + albumImageName, len, sourceHash, providerName, false);
                                artistsAdded.Add(artist);
                            }
                        }
                        else
                        {
                            using (var conn = this.sQLiteConnectionFactory.GetConnection())
                            {
                                conn.Insert(new ArtistDownloadFailed()
                                {
                                    ArtistId = artist.Id,
                                    DateAdded = DateTime.Now.Ticks,
                                    Provider = providerName
                                });
                            }
                        }

                        // If artwork was found for 20 albums, trigger a refresh of the UI.
                        if (artistsAdded.Count >= 20)
                        {
                            IList<ArtistV> eventArgs = artistsAdded.Select(item => item).ToList();
                            artistsAdded.Clear();
                            ArtistImagesAdded(this, new ArtistImagesAddedEventArgs() { Artists = eventArgs }); // Update UI
                            //=== TODO ALEX (REMOVE IT. FOR TESTING)
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {
                    LogClient.Error("Unexpected error occurred while updating artwork in the background. Exception: {0}", ex.Message);
                }
            });

            isIndexingArtwork = false;
            LogClient.Error("+++ FINISHED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
        }



        public async Task ReScanAlbumArtworkAsync(bool onlyWhenHasNoCover)
        {
            canIndexArtwork = false;

            // Wait until artwork indexing is stopped
            while (isIndexingArtwork)
            {
                await Task.Delay(100);
            }

            //await trackRepository.EnableNeedsAlbumArtworkIndexingForAllTracksAsync(onlyWhenHasNoCover);

            AddArtworkInBackgroundAsync(true, false);
            AddArtistImagesInBackgroundAsync(true, false);
        }

    }
}
