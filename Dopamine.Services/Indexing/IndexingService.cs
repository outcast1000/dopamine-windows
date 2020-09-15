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
using NLog;

namespace Dopamine.Services.Indexing
{
    public class IndexingService : IIndexingService
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();
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
        private IFileStorage fileStorage;

        // Watcher
        private FolderWatcherManager watcherManager;

        // Paths

        // Flags
        private bool isIndexingFiles;
        private bool canIndexAlbumImages;
        private bool isIndexingAlbumImages = false;
        private bool canIndexArtistImages;
        private bool isIndexingArtistImages = false;

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
            get { return isIndexingFiles; }
        }

        public IndexingService(ISQLiteConnectionFactory sQLiteConnectionFactory, IInfoDownloadService infoDownloadService,
            ITrackVRepository trackVRepository, IFolderVRepository folderVRepository, IAlbumVRepository albumVRepository,
            IUnitOfWorksFactory unitOfWorksFactory, IAlbumImageRepository albumImageRepository, IArtistVRepository artistVRepository, 
            IInfoProviderFactory infoProviderFactory, IFileStorage fileStorage)
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
            this.fileStorage = fileStorage;

            watcherManager = new FolderWatcherManager(folderVRepository);

            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += SettingsClient_SettingChanged;
            watcherManager.FoldersChanged += WatcherManager_FoldersChanged;

            isIndexingFiles = false;
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
            Logger.Debug($"RefreshCollectionAsync bForce: {bForce} bReReadTags: {bReReadTags}");
            if (IsIndexing)
            {
                Logger.Debug("RefreshCollectionAsync EXIT (Already Indexing)");
                return;
            }
            isIndexingFiles = true;
            canIndexAlbumImages = false;
            // Wait until artwork indexing is stopped
            while (isIndexingAlbumImages)
            {
                await Task.Delay(100);
            }
            await watcherManager.StopWatchingAsync();
            IndexingStarted(this, new EventArgs());
            await Task.Run(async () =>
            {
                Logger.Debug("RefreshCollectionAsync ENTER Task");

                long addedFiles = 0;
                long updatedFiles = 0;
                long removedFiles = 0;
                long failedFiles = 0;
                List<FolderV> folders = folderVRepository.GetFolders();
                // Recursively get all the files in the collection folders
                bool bContinue = true;
                foreach (FolderV folder in folders)
                {
                    Logger.Debug($"Refreshing: {folder.Path}");
                    //=== STEP 1. DELETE ALL THE FILES from the collections that have been deleted from the disk 
                    //=== Get All the paths from the DB (in chunks of 1000)
                    //=== For each one of them
                    //=== if they exist then OK. Otherwise then set the date to "DELETED_AT"
                    if (!SettingsClient.Get<bool>("Indexing", "IgnoreRemovedFiles"))
                    {
                        Logger.Debug("STEP 1: Removing deleted files");
                        long offset = 0;
                        const long limit = 1000;
                        while (true)
                        {
                            IList<TrackV> tracks = trackVRepository.GetTracksOfFolders(new List<long>() { folder.Id }, new QueryOptions() { Limit = limit, Offset = offset, WhereIgnored = QueryOptionsBool.Ignore, WhereVisibleFolders = QueryOptionsBool.Ignore });
                            foreach (TrackV track in tracks)
                            {
                                if (!System.IO.File.Exists(track.Path))
                                {
                                    Logger.Debug($"File not found: {track.Path}");
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
                    Logger.Debug("STEP 2: Reading files in folder");

                    FileOperations.GetFiles(folder.Path,
                        (path) =>
                        {
                            //=== Check the extension
                            if (!FileFormats.SupportedMediaExtensions.Contains(Path.GetExtension(path.ToLower())))
                                return;
                            //=== Check the DB for the path
                            TrackV trackV = trackVRepository.GetTrackWithPath(path);
                            long DateFileModified = FileUtils.DateModifiedTicks(path);
                            if (trackV != null && DateFileModified == trackV.DateFileModified && !bReReadTags)
                            {
                                //Logger.Debug($">> File {path} not changed! Go to the next file");
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
                                Logger.Warn(ex, $"Unable to READ TAG from the file {path}. The process will continue with file name data.");
                                mediaFileData.Name = Path.GetFileNameWithoutExtension(path);
                                //=== TODO. Do something more advanced like getting tags from path
                            }
                            if (string.IsNullOrEmpty(mediaFileData.Name))
                                mediaFileData.Name = Path.GetFileNameWithoutExtension(path);

                            using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                            {
                                if (trackV == null)
                                {
                                    
                                    AddMediaFileResult result = uc.AddMediaFile(mediaFileData, folder.Id);
                                    if (result.Success)
                                    {
                                        addedFiles++;
                                        Logger.Debug($">> Adding ({addedFiles}) New Track in DB...{path} ");
                                        //=== If it has album image and we actually have an album
                                        if (result.AlbumId.HasValue && fileMetadata?.ArtworkData?.Value?.Length > 0)
                                        {
                                            //=== If Album do not have an image
                                            AlbumImage albumImage = albumImageRepository.GetAlbumImage((long)result.AlbumId);
                                            if (albumImage == null)
                                            {
                                                string location = fileStorage.SaveImageToCache(fileMetadata.ArtworkData.Value, FileStorageItemType.Album);
                                                uc.SetAlbumImage(new AlbumImage()
                                                {
                                                    AlbumId = (long)result.AlbumId,
                                                    DateAdded = mediaFileData.DateAdded,
                                                    Location = location,
                                                    Source = "[TAG]"
                                                }, false);
                                            }
                                        }
                                        //=== Add Lyrics
                                        if (fileMetadata.Lyrics != null && fileMetadata.Lyrics.Value.Length > 0)
                                        {
                                            uc.SetLyrics(new TrackLyrics()
                                            {
                                                TrackId = (long)result.TrackId,
                                                DateAdded = DateTime.Now.Ticks,
                                                Source = "[TAG]",
                                                Lyrics = fileMetadata.Lyrics.Value
                                            }, false);
                                        }
                                    }
                                    else
                                    {
                                        failedFiles++;
                                        Logger.Warn($">> Failed ({failedFiles})");
                                    }
                                }
                                else
                                {
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
                                        Logger.Debug($">> Updated ({updatedFiles}) Track in DB...{path} ");
                                        //=== If it has album image and we actually have an album
                                        if (fileMetadata?.ArtworkData?.Value?.Length > 0 && result.AlbumId.HasValue)
                                        {
                                            //=== If Album do not have an image
                                            AlbumImage albumImage = albumImageRepository.GetAlbumImage((long)result.AlbumId);
                                            if (albumImage == null || albumImage.Source == "[TAG]")
                                            {
                                                string location = fileStorage.SaveImageToCache(fileMetadata.ArtworkData.Value, FileStorageItemType.Album);
                                                if (albumImage != null && !albumImage.Location.Equals(location))
                                                {
                                                    uc.SetAlbumImage(new AlbumImage()
                                                    {
                                                        AlbumId = (long)result.AlbumId,
                                                        DateAdded = DateTime.Now.Ticks,
                                                        Location = location,
                                                        Source = "[TAG]"
                                                    }, true);
                                                }
                                            }
                                        }

                                        //=== Add Lyrics
                                        if (fileMetadata?.Lyrics?.Value?.Length > 0)
                                        {
                                            uc.SetLyrics(new TrackLyrics()
                                            {
                                                TrackId = (long)trackV.Id,
                                                DateAdded = DateTime.Now.Ticks,
                                                Source = "[TAG]",
                                                Lyrics = fileMetadata.Lyrics.Value
                                            }, true);
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
                            Logger.Error(ex, "Updating Collection");
                        }
                        );


                }
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
                    HashSet<string> imagePaths = new HashSet<string>(images.Select(x => Path.GetFileNameWithoutExtension(fileStorage.GetRealPath(x.Location))).ToList());
                    FileOperations.GetFiles(fileStorage.StorageImagePath,
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
                isIndexingFiles = false;
                IndexingStopped(this, new EventArgs());

                await RetrieveAlbumInfoAsync(false, false);
                await RetrieveArtistInfoAsync(false, false);


                if (SettingsClient.Get<bool>("Indexing", "RefreshCollectionAutomatically"))
                {
                    await watcherManager.StartWatchingAsync();
                }
                Debug.Print("EXITING PrivateRefreshCollectionAsync (TASK)");
            });
            Debug.Print("EXITING PrivateRefreshCollectionAsync");
        }

        
        private async void WatcherManager_FoldersChanged(object sender, EventArgs e)
        {
            await RefreshCollectionAsync(false, false);
        }

        private async Task RetrieveAlbumInfoAsync(bool rescanFailed, bool rescanAll)
        {
            Logger.Debug($"AddArtworkInBackgroundAsync rescanFailed:{rescanFailed} rescanAll:{rescanAll}");
            if (!SettingsClient.Get<bool>("Covers", "DownloadMissingAlbumCovers"))
            {
                Logger.Debug("EXITING: DownloadMissingAlbumCovers is false.");
                return;
            }
            if (isIndexingAlbumImages)
            {
                Logger.Debug("EXITING: AddArtworkInBackgroundAsync [ALREADY IN]");
                return;
            }
            canIndexAlbumImages = true;
            isIndexingAlbumImages = true;

            DateTime startTime = DateTime.Now;

            await Task.Run(() =>
            {
                try
                {
                    IList<AlbumV> albumsAdded = new List<AlbumV>();
                    IList<AlbumV> albumDatasToIndex = rescanAll ? albumVRepository.GetAlbums() : albumVRepository.GetAlbumsWithoutImages(rescanFailed);

                    foreach (AlbumV albumDataToIndex in albumDatasToIndex)
                    {
                        if (string.IsNullOrEmpty(albumDataToIndex.Name))
                            continue;
                        // Check if we must cancel artwork indexing
                        if (!canIndexAlbumImages)
                        {
                            try
                            {
                                Logger.Warn("+++ ABORTED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
                                AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { Albums = albumsAdded }); // Update UI
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Failed to commit changes while aborting adding artwork in background.");
                            }

                            isIndexingAlbumImages = false;

                            return;
                        }

                        if (rescanFailed || rescanAll)
                        {
                            using (var conn = this.sQLiteConnectionFactory.GetConnection())
                            {
                                conn.Execute("DELETE FROM AlbumImageFailed WHERE album_id=?", albumDataToIndex.Id);
                            }

                        }

                        Logger.Debug($"AddArtworkInBackgroundAsync: Downloading Album Image for {albumDataToIndex.Name} - {albumDataToIndex.AlbumArtists}");
                        //LastFMAlbumInfoProvider lf = new LastFMAlbumInfoProvider(albumDataToIndex.Name, DataUtils.SplitAndTrimColumnMultiValue(albumDataToIndex.AlbumArtists).ToArray());
                        IAlbumInfoProvider aip = infoProviderFactory.GetAlbumInfoProvider(albumDataToIndex.Name, DataUtils.SplitAndTrimColumnMultiValue(albumDataToIndex.AlbumArtists).ToArray());

                        if (aip.Success && aip.Data?.Images?.Length > 0)
                        {
                            using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                            {
                                string cacheId = fileStorage.SaveImageToCache(aip.Data.Images[0], FileStorageItemType.Album);
                                uc.SetAlbumImage(new AlbumImage()
                                {
                                    AlbumId = albumDataToIndex.Id,
                                    DateAdded = DateTime.Now.Ticks,
                                    Location = cacheId,
                                    Source = aip.ProviderName
                                }, true);// albumDataToIndex.Id, "cache://" + albumImageName, len, sourceHash, providerName, false);
                            }
                        }
                        else
                        {
                            using (var conn = this.sQLiteConnectionFactory.GetConnection())
                            {
                                conn.Insert(new AlbumImageFailed()
                                {
                                    AlbumId = albumDataToIndex.Id,
                                    DateAdded = DateTime.Now.Ticks
                                });
                            }
                        }

                        // If artwork was found for 20 albums, trigger a refresh of the UI.
                        if (albumsAdded.Count >= 20)
                        {

                            IList<AlbumV> eventArgs = albumsAdded.Select(item => item).ToList();
                            albumsAdded.Clear();
                            AlbumArtworkAdded(this, new AlbumArtworkAddedEventArgs() { Albums = eventArgs }); // Update UI
                            Logger.Debug($"RetrieveAlbumInfoAsync. Stopping to MAX LIMIT OF 20 (DEBUG)");
                            break;//=== TODO ALEX. Remove It. Here for Test Purposes
                        }
                    }

                }
                catch (Exception ex)
                {
                    LogClient.Error("Unexpected error occurred while updating artwork in the background. Exception: {0}", ex.Message);
                }
            });

            isIndexingAlbumImages = false;
            LogClient.Error("+++ FINISHED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
        }

        private async Task RetrieveArtistInfoAsync(bool rescanFailed, bool rescanAll)
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
            Logger.Info("RetrieveArtistInfoAsync starting");
            canIndexArtistImages = true;
            isIndexingArtistImages = true;

            DateTime startTime = DateTime.Now;

            await Task.Run(() =>
            {
                try
                {
                    IList<ArtistV> artistsAdded = new List<ArtistV>();
                    IList<ArtistV> artistsToIndex = rescanAll ? artistVRepository.GetArtists() : artistVRepository.GetArtistsWithoutImages(rescanFailed);

                    foreach (ArtistV artist in artistsToIndex)
                    {
                        if (string.IsNullOrEmpty(artist.Name))
                            continue;
                        Logger.Debug($"RetrieveArtistInfoAsync. Getting {artist.Name}");
                        // Check if we must cancel artwork indexing
                        if (!canIndexArtistImages)
                        {
                            try
                            {
                                Logger.Info("RetrieveArtistInfoAsync. Aborting ... Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
                                ArtistImagesAdded(this, new ArtistImagesAddedEventArgs() { Artists = artistsAdded }); // Update UI
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Failed to commit changes while aborting adding artwork in background.");
                            }

                            isIndexingAlbumImages = false;

                            return;
                        }


                        using (var conn = this.sQLiteConnectionFactory.GetConnection())
                        {
                            conn.Execute("DELETE FROM ArtistImageFailed WHERE artist_id=?", artist.Id);
                        }

                        // During the 2nd pass, look for artwork on the Internet and set NeedsAlbumArtworkIndexing = 0.
                        // We don't want future passes to index for this AlbumKey anymore.

                        //GetArtworkFromInternet(string albumTitle, IList<string> albumArtists, string trackTitle, IList<string> artists)

                        //IArtistInfoProvider ip = new GoogleArtistInfoProvider(artist.Name);
                        //IArtistInfoProvider ip = new YoutubeArtistInfoProvider(artist.Name);
                        IArtistInfoProvider ip = infoProviderFactory.GetArtistInfoProvider(artist.Name);


                        if (ip.Success && ip.Data.Images != null && ip.Data.Images.Length >= 1)
                        {
                            using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                            {
                                string cacheId = fileStorage.SaveImageToCache(ip.Data.Images[0], FileStorageItemType.Artist);
                                uc.SetArtistImage(new ArtistImage()
                                {
                                    ArtistId = artist.Id,
                                    DateAdded = DateTime.Now.Ticks,
                                    Location = cacheId,
                                    Source = ip.ProviderName
                                }, true);// albumDataToIndex.Id, "cache://" + albumImageName, len, sourceHash, providerName, false);
                                artistsAdded.Add(artist);
                            }
                        }
                        else
                        {
                            using (var conn = this.sQLiteConnectionFactory.GetConnection())
                            {
                                conn.Insert(new ArtistImageFailed()
                                {
                                    ArtistId = artist.Id,
                                    DateAdded = DateTime.Now.Ticks
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
                            Logger.Debug($"RetrieveArtistInfoAsync. Stopping to MAX LIMIT OF 20 (DEBUG)");
                            break;
                        }
                    }

                }
                catch (Exception ex)
                {
                    LogClient.Error("Unexpected error occurred while updating artwork in the background. Exception: {0}", ex.Message);
                }
            });

            isIndexingArtistImages = false;
            LogClient.Error("+++ FINISHED ADDING ARTWORK IN THE BACKGROUND. Time required: {0} ms +++", Convert.ToInt64(DateTime.Now.Subtract(startTime).TotalMilliseconds));
        }



        public async Task RetrieveInfoAsync(bool onlyWhenHasNoCover)
        {
            canIndexAlbumImages = false;

            // Wait until artwork indexing is stopped
            while (isIndexingAlbumImages)
            {
                await Task.Delay(100);
            }

            //await trackRepository.EnableNeedsAlbumArtworkIndexingForAllTracksAsync(onlyWhenHasNoCover);

            await RetrieveAlbumInfoAsync(true, false);
            await RetrieveArtistInfoAsync(true, false);
            //Task.WaitAll(albumInfo, artistInfo);

        }

    }
}
