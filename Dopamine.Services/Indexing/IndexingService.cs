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
using System.Security.Cryptography;

/* DESIGN (CALLERS)

(WHEN YOU CHANGE THE SETTINGS) EVENT FROM SettingsClient
	-> private async void SettingsClient_SettingChanged(object sender, Digimezzo.Foundation.Core.Settings.SettingChangedEventArgs e)
		-> await watcherManager.StartWatchingAsync(); await watcherManager.StopWatchingAsync();

(ADD FOLDER) DIRECT CALL FROM CollectionFoldersSettingsViewModel (AddFolderResult.Success)
	-> public async void OnFoldersChanged()
		-> await watcherManager.StartWatchingAsync();

(REMOVE FOLDER) DIRECT CALL FROM CollectionFoldersSettingsViewModel (RemoveFolderResult.Success)
	-> public async void OnFoldersChanged()
		-> await watcherManager.StartWatchingAsync();

(REMOVE FOLDER) DIRECT CALL FROM CollectionFoldersSettingsViewModel (RemoveFolderResult.Success)
	-> TriggerRefreshLists()
		-> RefreshLists(this, new EventArgs());

(WHEN A FOLDER HAS CHANGED) EVENT FROM watcherManager.FoldersChanged
	-> WatcherManager_FoldersChanged
		-> await RefreshCollectionAsync(false, false);
		
(FROM STARTUP) DIRECT CALL FROM App.InitializeShell
	-> if (!showOobe) Container.Resolve<IIndexingService>().RefreshCollectionAsync(false, false);

(FROM FULL PLAYER / REFRESH NOW COMMAND) FullPlayerAddMusicViewModel.RefreshNowCommand
	-> this.indexingService.RefreshCollectionAsync(true, false)
	
(? AFTER YOU CLOSE THE Manage Collections Dialog?)
	-> FullPlayerViewModel::ManageCollectionAsync()
		-> this.indexingService.RefreshCollectionAsync(false, false);
		
(WHEN YOU CLOSE THE OOBE) Oobe::Window_Closing
	-> this.indexingService.RefreshCollectionAsync(false, false);

*/


namespace Dopamine.Services.Indexing
{

    public class UpdateStatistics
    {
        public int TotalChecked = 0;
        public int Checked = 0;
        public int Added = 0;
        public int Updated = 0;
        public int Removed = 0;
        public int ResurrectedFiles = 0;
        public int Failed = 0;

        public void AddStatistics(UpdateStatistics updateStatistics)
        {
            TotalChecked += updateStatistics.TotalChecked;
            Checked += updateStatistics.Checked;
            Added += updateStatistics.Added;
            Updated += updateStatistics.Updated;
            Removed += updateStatistics.Removed;
            ResurrectedFiles += updateStatistics.ResurrectedFiles;
            Failed += updateStatistics.Failed;
        }

        public bool IsSomethingChanged()
        {
            return (Added + Updated + Removed + ResurrectedFiles) > 0;
        }

        override public string ToString()
        {
            return $"TotalChecked: {TotalChecked}, Checked: {Checked}, Added: {Added}, Updated: {Updated}, Removed: {Removed}, ResurrectedFiles: {ResurrectedFiles}, Failed: {Failed}";
        }
    }

    public class TimeCounter
    {
        long _startTick;
        long _endTick = 0;
        public TimeCounter()
        {
            Start();
        }
        public void Start()
        {
            _startTick = DateTime.Now.Ticks;
        }

        public void Stop()
        {
            _endTick = DateTime.Now.Ticks;
        }

        public double GetMs(bool bStop)
        {
            if (bStop)
                Stop();
            if (_endTick == 0)
                return (DateTime.Now.Ticks - _startTick) / 10000.0;
            return (_endTick - _startTick) / 10000.0;
        }
    }


    public class IndexingService : IIndexingService
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        // Services
        private IInfoDownloadService infoDownloadService;
        private AlbumInfoIndexingQueue _albumInfoIndexingQueue;
        private ArtistInfoIndexingQueue _artistInfoIndexingQueue;


        // Repositories
        private ITrackVRepository trackVRepository;
        private IAlbumVRepository albumVRepository;
        private IArtistVRepository artistVRepository;
        private IFolderVRepository folderVRepository;
        private IInfoRepository infoRepository;

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

        // Events
        public event EventHandler IndexingStopped = delegate { };
        public event EventHandler IndexingStarted = delegate { };
        public event Action<IndexingStatusEventArgs> IndexingStatusChanged = delegate { };
        public event EventHandler RefreshLists = delegate { };
        public event EventHandler RefreshArtwork = delegate { };
        //public event AlbumImagesAddedEventHandler AlbumImagesAdded = delegate { };
        //public event ArtistImagesAddedEventHandler ArtistImagesAdded = delegate { };

        private bool _shouldCancelIndexing = false;

        public bool IsIndexing
        {
            get { return isIndexingFiles; }
        }

        public IndexingService(ISQLiteConnectionFactory sQLiteConnectionFactory, IInfoDownloadService infoDownloadService,
            ITrackVRepository trackVRepository, IFolderVRepository folderVRepository, IAlbumVRepository albumVRepository,
            IUnitOfWorksFactory unitOfWorksFactory, IInfoRepository infoRepository, IArtistVRepository artistVRepository, 
            IInfoProviderFactory infoProviderFactory, IFileStorage fileStorage)
        {
            this.infoDownloadService = infoDownloadService;
            this.trackVRepository = trackVRepository;
            this.albumVRepository = albumVRepository;
            this.artistVRepository = artistVRepository;
            this.folderVRepository = folderVRepository;
            this.sQLiteConnectionFactory = sQLiteConnectionFactory;
            this.unitOfWorksFactory = unitOfWorksFactory;
            this.infoRepository = infoRepository;
            this.infoProviderFactory = infoProviderFactory;
            this.fileStorage = fileStorage;
            _albumInfoIndexingQueue = new AlbumInfoIndexingQueue(infoProviderFactory.GetAlbumInfoProvider());
            _artistInfoIndexingQueue = new ArtistInfoIndexingQueue(infoProviderFactory.GetArtistInfoProvider());
            _albumInfoIndexingQueue.InfoDownloaded += _albumInfoIndexingQueue_InfoDownloaded;
            _artistInfoIndexingQueue.InfoDownloaded += _artistInfoIndexingQueue_InfoDownloaded;

            watcherManager = new FolderWatcherManager(folderVRepository);

            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += SettingsClient_SettingChanged;
            //watcherManager.FoldersChanged += WatcherManager_FoldersChanged;
            watcherManager.FilesRenamed += WatcherManager_FilesRenamed;
            watcherManager.FilesChanged += WatcherManager_FilesChanged;
            isIndexingFiles = false;
        }

        private async void WatcherManager_FilesRenamed(object sender, long folderId, List<RenamedEventArgs> e)
        {
            long filesChanged = 0;
            await StartProcessingFilesAsync();
            await Task.Run(() =>
            {
                foreach (RenamedEventArgs ev in e)
                {
                    TrackV track = trackVRepository.GetTrackWithPath(ev.OldFullPath, QueryOptions.IncludeAll());
                    if (track == null)
                        continue; // It may be a file that it is not handled or something else
                    filesChanged++;
                    if (!FileFormats.SupportedMediaExtensions.Contains(Path.GetExtension(ev.FullPath.ToLower())))
                    {
                        //Delete track
                        trackVRepository.UpdateDeleteValue(track.Id, true);
                        continue;
                    }
                    trackVRepository.UpdateLocation(track.Id, ev.FullPath);
                }
            });
            if (filesChanged > 0)
                RefreshLists(this, new EventArgs());
            await StopProcessingFilesAsync();
        }

        private async void WatcherManager_FilesChanged(object sender, long folderId, List<FileSystemEventArgs> e)
        {
            long filesChanged = 0;
            await StartProcessingFilesAsync();
            foreach (FileSystemEventArgs ev in e)
            {
                TrackV track = trackVRepository.GetTrackWithPath(ev.FullPath, QueryOptions.IncludeAll());
                ProcessFileResult res = ProcessFile(ev.FullPath, folderId, track);
                filesChanged += (res == ProcessFileResult.Failed) ? 0 : 1;
            }
            isIndexingFiles = false;
            if (filesChanged > 0)
                RefreshLists(this, new EventArgs());
            await StopProcessingFilesAsync();
        }


        private void _artistInfoIndexingQueue_InfoDownloaded(ArtistV requestedArtist, ArtistInfoProviderData data)
        {
            // Note: This runs on its ProcessQueued Thread (not on the main thread
            bool bImageAdded = false;
            if (data?.result == InfoProviderResult.Success)
            {
                Logger.Info($"_artistInfoIndexingQueue_InfoDownloaded. Success. {requestedArtist.Name}");
                if (data.Images?.Length > 0)
                {
                    string cacheId = fileStorage.SaveImageToCache(data.Images[0].Data, FileStorageItemType.Artist);
                    infoRepository.SetArtistImage(new ArtistImage()
                    {
                        ArtistId = requestedArtist.Id,
                        DateAdded = DateTime.Now.Ticks,
                        Location = cacheId,
                        Origin = data.Images[0].Origin,
                        OriginType = OriginType.Internet
                    });
                    requestedArtist.Thumbnail = cacheId;
                    bImageAdded = true;
                    Logger.Info($"_artistInfoIndexingQueue_InfoDownloaded. Image Saved. {requestedArtist.Name}");
                }
                if (data.Biography?.Data?.Length > 0)
                {
                    infoRepository.SetArtistBiography(new ArtistBiography()
                    {
                        ArtistId = requestedArtist.Id,
                        DateAdded = DateTime.Now.Ticks,
                        Biography = data.Biography.Data,
                        Origin = data.Biography.Origin,
                        OriginType = OriginType.Internet
                    });
                    Logger.Info($"_artistInfoIndexingQueue_InfoDownloaded. Bio Saved. {requestedArtist.Name}");
                }
            }
            else
            {
                Logger.Warn($"_artistInfoIndexingQueue_InfoDownloaded. Failed for: {requestedArtist.Name} - Res: {data.result}");
            }
            if (!bImageAdded && !(data?.result == InfoProviderResult.Fail_InternetFailed))
                infoRepository.SetArtistImageFailed(requestedArtist);
            ArtistInfoDownloaded?.Invoke(requestedArtist, data?.result == InfoProviderResult.Success);
        }

        private void _albumInfoIndexingQueue_InfoDownloaded(AlbumV requestedAlbum, AlbumInfoProviderData data)
        {
            // Note: This runs on its ProcessQueued Thread (not on the main thread
            bool bImageAdded = false;
            if (data.result == InfoProviderResult.Success)
            {
                Logger.Info($"_albumInfoIndexingQueue_InfoDownloaded. Success. {requestedAlbum.Name}");
                if (data.Images?.Length > 0)
                {
                    string cacheId = fileStorage.SaveImageToCache(data.Images[0].Data, FileStorageItemType.Album);
                    infoRepository.SetAlbumImage(new AlbumImage()
                    {
                        AlbumId = requestedAlbum.Id,
                        DateAdded = DateTime.Now.Ticks,
                        Location = cacheId,
                        Origin = data.Images[0].Origin,
                        OriginType = OriginType.Internet
                    }, true);
                    requestedAlbum.Thumbnail = cacheId;
                    bImageAdded = true;
                    Logger.Info($"_albumInfoIndexingQueue_InfoDownloaded. Image Saved. {requestedAlbum.Name}");
                }
                if (data?.Review?.Data?.Length > 0)
                {
                    infoRepository.SetAlbumReview(new AlbumReview()
                    {
                        AlbumId = requestedAlbum.Id,
                        DateAdded = DateTime.Now.Ticks,
                        Review = data.Review.Data,
                        Origin = data.Review.Origin,
                        OriginType = OriginType.Internet
                    });
                    Logger.Info($"_albumInfoIndexingQueue_InfoDownloaded. Review Saved. {requestedAlbum.Name}");
                }
            }
            else
            {
                Logger.Warn($"_albumInfoIndexingQueue_InfoDownloaded. Feiled. {requestedAlbum.Name} - Res: {data.result}");
            }
            if (!bImageAdded && data.result != InfoProviderResult.Fail_InternetFailed)
                infoRepository.SetAlbumImageFailed(requestedAlbum);
            AlbumInfoDownloaded?.Invoke(requestedAlbum, data.result == InfoProviderResult.Success);
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

        public void TriggerRefreshLists()
        {
            RefreshLists(this, new EventArgs());
        }


        // This function find all the files in a collection and checks if the files actually exist in the FileSystem
        // If it cannot locate them then it sets the DELETED_AT filed at the current Date (Tick Count)
        // Memory Optimization: It does it in chuncks of 1000.
        private int UpdateRemovedFiles(long folderId)
        {
            int updateRemovedFilesCount = 0;
            long offset = 0;
            const long limit = 1000;
            while (true)
            {
                IList<TrackV> tracks = trackVRepository.GetTracksOfFolders(new List<long>() { folderId }, new QueryOptions() { Limit = limit, Offset = offset, WhereIgnored = QueryOptionsBool.Ignore, WhereVisibleFolders = QueryOptionsBool.Ignore });
                foreach (TrackV track in tracks)
                {
                    if (!System.IO.File.Exists(track.Path))
                    {
                        Logger.Debug($"File not found: {track.Path}");
                        if (trackVRepository.UpdateDeleteValue(track.Id, true))
                            updateRemovedFilesCount++;
                    }
                }
                if (tracks.Count < limit)
                    break;
                offset += limit;
            }
            return updateRemovedFilesCount;
        }

        private FileMetadata GetFileMetadata(string path)
        {
            try
            {
                return new FileMetadata(path);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Unable to READ TAG from the file {path}. The process will continue with file name data.");
            }
            return null;
        }

        private MediaFileData GetMediaFileData(string path, FileMetadata fileMetadata)
        {
            MediaFileData mediaFileData = new MediaFileData()
            {
                Path = path,
                Filesize = FileUtils.SizeInBytes(path),
                Language = null,
                DateAdded = DateTime.Now.Ticks,
                Love = null,
                DateFileCreated = FileUtils.DateCreatedTicks(path),
                DateFileModified = FileUtils.DateModifiedTicks(path),
                DateFileDeleted = null,
                DateIgnored = null
            };
            if (fileMetadata != null)
            {
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
            if (string.IsNullOrEmpty(mediaFileData.Name))
                mediaFileData.Name = Path.GetFileNameWithoutExtension(path);
            return mediaFileData;
        }

        private bool AddAlbumImageIfNecessary(long albumId, FileMetadata fileMetadata)
        {
            Debug.Assert(albumId > 0);
            Debug.Assert(fileMetadata != null);

            if (!(fileMetadata.ArtworkData?.Value?.Length > 0))
                return false; //=== TAG does not have embedded image. Exit.

            AlbumImage albumImage = infoRepository.GetAlbumImage(albumId);
            if (albumImage != null)
                return false; //=== This album has already an image
            string location = fileStorage.SaveImageToCache(fileMetadata.ArtworkData.Value, FileStorageItemType.Album);
            return infoRepository.SetAlbumImage(new AlbumImage()
            {
                AlbumId = albumId,
                DateAdded = DateTime.Now.Ticks,
                Location = location,
                Origin = String.Empty,
                OriginType = OriginType.File
            }, false);
        }

        private bool AddTrackLyrics(long trackId, FileMetadata fileMetadata)
        {
            Debug.Assert(trackId > 0);
            Debug.Assert(fileMetadata != null);
            if (!(fileMetadata.Lyrics?.Value?.Length > 0))
                return false;
            return infoRepository.SetTrackLyrics(new TrackLyrics()
            {
                TrackId = trackId,
                DateAdded = DateTime.Now.Ticks,
                OriginType = OriginType.File,
                Origin = String.Empty,
                Lyrics = fileMetadata.Lyrics.Value
            }, false);
        }

        private int _collectionFilesChanged = 0;
        private void OnCollectionFileChanged()
        {
            _collectionFilesChanged++;
            if (_collectionFilesChanged > 250)
            {
                _collectionFilesChanged = 0;
                RefreshLists(this, new EventArgs());
            }
        }

        private class FolderStats
        {
            public FolderStats() { TotalFiles = LastModifiedDateTicks = 0;}
            public string Hash;
            public long TotalFiles;
            public long LastModifiedDateTicks;
        }

        private FolderStats CalculateFolderStats(String folderPath)
        {
            FolderStats fs = new FolderStats();
            using (var hasher = new SHA1CryptoServiceProvider())
            {
                hasher.Initialize();
                FileOperations.GetFiles(folderPath,
                    (path) =>
                    {
                        //=== Check the extension
                        if (!FileFormats.SupportedMediaExtensions.Contains(Path.GetExtension(path.ToLower())))
                            return;
                        fs.TotalFiles++;
                        long dateFileModified = FileUtils.DateModifiedTicks(path);
                        if (dateFileModified > fs.LastModifiedDateTicks)
                            fs.LastModifiedDateTicks = dateFileModified;
                        hasher.TransformBlock(BitConverter.GetBytes(fs.LastModifiedDateTicks), 0, sizeof(long), null, 0);
                    },
                    () =>
                    {
                        return !_shouldCancelIndexing;
                    },
                    (ex) => { });
                hasher.TransformFinalBlock(new byte[0], 0, 0);
                fs.Hash = BitConverter.ToString(hasher.Hash);
            }
            return fs;


        }

        private UpdateStatistics RefreshCollection(FolderV folder, bool bReReadTags)
        {
            Logger.Debug($"RefreshCollection: {folder.Path}. Reading FS...");
            FolderStats folderStats = CalculateFolderStats(folder.Path);
            if (!bReReadTags)
            {
                if (folder.Hash != null && folder.Hash.Equals(folderStats.Hash))
                {
                    Logger.Debug("RefreshCollection: Hash is the same => Nothing changed");
                    return new UpdateStatistics();
                }
            }

            IndexingStatusChanged(new IndexingStatusEventArgs() { IndexingAction = IndexingAction.RemoveTracks, ExtraInfo = folder.Path });
            UpdateStatistics stats = new UpdateStatistics();
            TimeCounter tcRefreshCollection = new TimeCounter();
            if (!SettingsClient.Get<bool>("Indexing", "IgnoreRemovedFiles"))
            {
                //=== DELETE ALL THE FILES from the DB that have been deleted from the disk 
                Logger.Debug($"--> Removing files...");
                stats.Removed = UpdateRemovedFiles(folder.Id);
            }

            IndexingStatusChanged(new IndexingStatusEventArgs() { IndexingAction = IndexingAction.UpdateTracks, ExtraInfo = folder.Path, ProgressPercent = 0 });
            Logger.Debug("-->Reading file system...");
            
            FileOperations.GetFiles(folder.Path,
                (path) =>
                {
                    stats.TotalChecked++;
                    //=== Check the extension
                    if (!FileFormats.SupportedMediaExtensions.Contains(Path.GetExtension(path.ToLower())))
                        return;
                    stats.Checked++;
                    IndexingStatusChanged(new IndexingStatusEventArgs() { IndexingAction = IndexingAction.UpdateTracks, ExtraInfo = folder.Path, ProgressPercent = 100 * stats.Checked / (int)folderStats.TotalFiles });
                    //=== Check the DB for the path
                    TrackV trackV = trackVRepository.GetTrackWithPath(path, new QueryOptions() { WhereDeleted = QueryOptionsBool.Ignore, WhereInACollection = QueryOptionsBool.Ignore, WhereVisibleFolders = QueryOptionsBool.Ignore, WhereIgnored = QueryOptionsBool.Ignore });
                    long DateFileModified = FileUtils.DateModifiedTicks(path);
                    if (trackV != null && DateFileModified == trackV.DateFileModified && !bReReadTags)
                    {
                        //=== There is also a case when tha track exists but not in a collection 
                        //      There are 2 cases for that
                        //          1. a collection that has been removed
                        //          2. A file that has been played while it wasn't in a collection
                        if (trackV.FolderID == 0)
                        {
                            // Update the track record and set it to the new collection
                            if (trackVRepository.UpdateFolderIdValue(trackV.Id, folder.Id))
                            {
                                stats.ResurrectedFiles++;
                                OnCollectionFileChanged();
                            }
                        }
                        //Logger.Debug($">> File {path} not changed! Go to the next file");
                        return;
                    }
                    //=== Get File Info
                    ProcessFileResult res = ProcessFile(path, folder.Id, trackV);
                    switch (res)
                    {
                        case ProcessFileResult.Added:
                            stats.Added++;
                            break;
                        case ProcessFileResult.Updated:
                            stats.Updated++;
                            break;
                        case ProcessFileResult.Failed:
                        default:
                            stats.Failed++;
                            break;
                    }

                },
                () =>
                {
                    return !_shouldCancelIndexing;
                },
                (ex) =>
                {
                    Logger.Error(ex, $"Updating Collection {ex.Message}");
                }
            );
            folderVRepository.SetFolderIndexing(new FolderIndexing()
            {
                DateIndexed = DateTime.Now.Ticks,
                MaxFileDateModified = folderStats.LastModifiedDateTicks,
                TotalFiles = folderStats.TotalFiles,
                FolderId = folder.Id,
                Hash = folderStats.Hash
            });
            Logger.Debug($"Refreshing collection finished in {tcRefreshCollection.GetMs(true)}. Stats: {stats}");
            return stats;
        }

        private enum ProcessFileResult
        {
            Added,
            Updated,
            Failed
        }
        private ProcessFileResult ProcessFile(string path, long folderId, TrackV trackV)
        {
            FileMetadata fileMetadata = GetFileMetadata(path);
            MediaFileData mediaFileData = GetMediaFileData(path, fileMetadata);

            if (trackV == null)
            {
                AddMediaFileResult result;
                using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                {
                    result = uc.AddMediaFile(mediaFileData, folderId);
                }
                if (result.Success)
                {
                    OnCollectionFileChanged();
                    if (fileMetadata != null)
                    {
                        //=== Add Album Image
                        if (result.AlbumId.HasValue)
                            AddAlbumImageIfNecessary((long)result.AlbumId, fileMetadata);
                        //=== Add Lyrics
                        // Lyrics are already added in uc.AddMediaFile
                        //AddTrackLyrics((long)result.TrackId, fileMetadata);
                    }
                    return ProcessFileResult.Added;
                }
                else
                {
                    Logger.Warn($">> Failed to add ({path})");
                    return ProcessFileResult.Failed;
                }
            }
            else
            {
                // If we update the file we do not want to change these Dates
                mediaFileData.DateAdded = trackV.DateAdded;
                mediaFileData.DateIgnored = trackV.DateIgnored;
                // If the file was previously deleted then now it seem that i re-emerged
                mediaFileData.DateFileDeleted = null;
                // Love / Rating/ Language are not saved in tags
                mediaFileData.Love = trackV.Love;
                mediaFileData.Rating = trackV.Rating;
                mediaFileData.Language = trackV.Language;
                UpdateMediaFileResult result;
                using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
                {
                    result = uc.UpdateMediaFile(trackV, mediaFileData);
                }
                if (result.Success)
                {
                    OnCollectionFileChanged();
                    if (fileMetadata != null)
                    {
                        //=== Add Album Image
                        if (result.AlbumId.HasValue)
                            AddAlbumImageIfNecessary((long)result.AlbumId, fileMetadata);
                        //=== Add Lyrics
                        AddTrackLyrics(trackV.Id, fileMetadata);
                    }
                    return ProcessFileResult.Updated;

                }
                else
                {
                    Logger.Warn($">> Failed to update ({path})");
                    return ProcessFileResult.Failed;
                }

            }
        }

        public bool UpdateFile(FileMetadata fileMetadata)
        {
            //=== Check the DB for the path
            TrackV trackV = trackVRepository.GetTrackWithPath(fileMetadata.Path, new QueryOptions() { WhereDeleted = QueryOptionsBool.Ignore, WhereInACollection = QueryOptionsBool.Ignore, WhereVisibleFolders = QueryOptionsBool.Ignore, WhereIgnored = QueryOptionsBool.Ignore });
            if (trackV == null)
            {
                Logger.Warn($"UpdateFile. File not found in DB {fileMetadata.Path}");
                return false;
            }
            MediaFileData mediaFileData = GetMediaFileData(fileMetadata.Path, fileMetadata);
            // If we update the file we do not want to change these Dates
            mediaFileData.DateAdded = trackV.DateAdded;
            mediaFileData.DateIgnored = trackV.DateIgnored;
            // If the file was previously deleted then now it seem that i re-emerged
            mediaFileData.DateFileDeleted = null;
            // Love / Rating/ Language are not saved in tags
            mediaFileData.Love = trackV.Love;
            mediaFileData.Rating = trackV.Rating;
            mediaFileData.Language = trackV.Language;
            UpdateMediaFileResult result;
            using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
            {
                result = uc.UpdateMediaFile(trackV, mediaFileData);
            }
            if (result.Success)
            {
                if (fileMetadata != null)
                {
                    //=== Add Album Image
                    if (result.AlbumId.HasValue)
                        AddAlbumImageIfNecessary((long)result.AlbumId, fileMetadata);
                    //=== Add Lyrics
                    AddTrackLyrics(trackV.Id, fileMetadata);
                }
            }
            else
            {
                Logger.Warn($">> Failed to update ({fileMetadata.Path})");
                return false;
            }
            return true;
        }

        private long DeleteUnusedImagesFromDB()
        {
            Logger.Debug("CLEAN UP Database from image entities without tracks (Artists, albums, genres)");
            using (ICleanUpImagesUnitOfWork cleanUpAlbumImagesUnitOfWork = unitOfWorksFactory.getCleanUpAlbumImages())
            {
                return cleanUpAlbumImagesUnitOfWork.CleanUp();
            }
        }

        private long DeleteUnusedImagesFromTheDisk()
        {
            //=== CLEAN UP Images from cache that is not included in the DB
            long imageDeletions = 0;
            IList<string> images = infoRepository.GetAllImagePaths();
            if (!ListExtensions.IsNullOrEmpty(images))
            {
                HashSet<string> imagePaths = new HashSet<string>(images.Select(x => Path.GetFileNameWithoutExtension(fileStorage.GetRealPath(x))).ToList());
                FileOperations.GetFiles(fileStorage.StorageImagePath,
                    (path) =>
                    {
                        string ext = Path.GetExtension(path);
                        string name = Path.GetFileNameWithoutExtension(path);

                        if (!ext.Equals(".jpg"))
                            return;
                        if (imagePaths.Contains(name) == false)
                        {
                            imageDeletions++;
                            Logger.Debug($">> Deleting unused image: {name}");
                            System.IO.File.Delete(path);
                        }
                    },
                    () =>
                    {
                        return true;
                    },
                    (ex) =>
                    {
                        Logger.Info(ex, String.Format("Exception: {0}", ex.Message));
                    });
            }
            return imageDeletions;
        }

        public async Task CleanUp()
        {
            await Task.Run(() =>
            {
                DeleteUnusedImagesFromDB();
                DeleteUnusedImagesFromTheDisk();
                // ALEX TODO: DELETE ALL FILES with folder.id = null WHERE THERE IS NO HISTORY
                // +++

            });
        }

        public async Task RefreshCollectionAsync(bool bForce, bool bReReadTags = false)
        {
            Logger.Debug($"RefreshCollectionAsync bForce: {bForce} bReReadTags: {bReReadTags}");
            if (IsIndexing)
            {
                Logger.Debug("RefreshCollectionAsync EXIT (Already Indexing)");
                return;
            }

            await Task.Run(async () =>
            {
                await StartProcessingFilesAsync();

                Logger.Debug("RefreshCollectionAsync ENTER Task");
                UpdateStatistics totalStats = new UpdateStatistics();
                TimeCounter timerTotal = new TimeCounter();
                List<FolderV> folders = folderVRepository.GetShownFolders(DataRichnessEnum.Basic);
                foreach (FolderV folder in folders)
                {
                    TimeCounter timerFolderUpdate = new TimeCounter();
                    UpdateStatistics folderStats = RefreshCollection(folder, bReReadTags);
                    totalStats.AddStatistics(folderStats);
                }
                // Refresh lists
                if (totalStats.IsSomethingChanged())
                    RefreshLists(this, new EventArgs());
                await StopProcessingFilesAsync();
            });
        }

        private async Task StartProcessingFilesAsync()
        {
            isIndexingFiles = true;
            await watcherManager.StopWatchingAsync();
            IndexingStarted(this, new EventArgs());
        }

        private async Task StopProcessingFilesAsync()
        {
            isIndexingFiles = false;
            IndexingStopped(this, new EventArgs());
            if (SettingsClient.Get<bool>("Indexing", "RefreshCollectionAutomatically"))
            {
                await watcherManager.StartWatchingAsync();
            }
        }



        private async void WatcherManager_FoldersChanged(object sender, EventArgs e)
        {
            await RefreshCollectionAsync(false, false);
        }

        

        private async Task RetrieveAlbumInfoAsync(bool rescanFailed, bool rescanAll)
        {
            Logger.Debug($"RetrieveAlbumInfoAsync rescanFailed:{rescanFailed} rescanAll:{rescanAll}");
            await Task.Run(() =>
            {
                try
                {
                    IList<AlbumV> albumDatasToIndex = rescanAll ? albumVRepository.GetAlbums() : albumVRepository.GetAlbumsWithoutImages(rescanFailed);
                    IAlbumInfoProvider aip = infoProviderFactory.GetAlbumInfoProvider();
                    foreach (AlbumV album in albumDatasToIndex)
                    {
                        _albumInfoIndexingQueue.Enqueue(new AlbumInfoIndexingQueueJob() { Album = album });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected error occurred while updating artwork in the background. Exception: {0}", ex.Message);
                }
            });
        }

        private async Task RetrieveArtistInfoAsync(bool rescanFailed, bool rescanAll)
        {
            Logger.Debug($"RetrieveArtistInfoAsync rescanFailed:{rescanFailed} rescanAll:{rescanAll}");
            await Task.Run(() =>
            {
                try
                {
                    IList<ArtistV> artistsToIndex = rescanAll ? artistVRepository.GetArtists(new QueryOptions(DataRichnessEnum.Basic)) : artistVRepository.GetArtistsWithoutImages(rescanFailed);
                    IArtistInfoProvider ip = infoProviderFactory.GetArtistInfoProvider();
                    foreach (ArtistV artist in artistsToIndex)
                    {
                        _artistInfoIndexingQueue.Enqueue(new ArtistInfoIndexingQueueJob() { Artist = artist });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected error occurred while updating artwork in the background. Exception: {0}", ex.Message);
                }
            });
        }

        public async Task RetrieveInfoAsync(bool rescanFailed, bool rescanAll)
        {
            Task retrieveAlbumInfo = RetrieveAlbumInfoAsync(rescanFailed, rescanAll);
            Task retrieveArtistInfo = RetrieveArtistInfoAsync(rescanFailed, rescanAll);
            await Task.WhenAll(retrieveAlbumInfo, retrieveArtistInfo);// WhenAll is used in the place of WaitAll in order to suppress WARNING CS1998 https://stackoverflow.com/questions/49330335/async-await-deadlock-task-waitall-vs-task-whenall
        }

        //=== NEW

        public event AlbumInfoDownloaded AlbumInfoDownloaded;
        public event ArtistInfoDownloaded ArtistInfoDownloaded;

        public async Task<bool> RequestArtistInfoAsync(ArtistV artist, bool bIgnorePreviousFailures, bool bForce)
        {
            if (string.IsNullOrEmpty(artist.Name))
            {
                Logger.Warn("RequestArtistInfoAsync. Name is empty. Exiting");
                return false;
            }
            if (artist.ArtistImage != null && !bForce)
            {
                Logger.Warn("RequestArtistInfoAsync. Image Already Present. Exiting");
                return false;
            }
            if (!InternetAvailability.IsInternetAvailable())
            {
                Logger.Warn("RequestArtistInfoAsync. No Internet is Available. Exiting");
                return false;
            }
            bool bRet = true;
            await Task.Run(() =>
            {
                if (bIgnorePreviousFailures || bForce)
                    infoRepository.ClearArtistImageFailed(artist);
                if (!bIgnorePreviousFailures)
                {
                    if (infoRepository.HasArtistImageFailed(artist))
                    {
                        Logger.Warn("RequestArtistInfoAsync. Image has already failed once. Exiting");
                        bRet = false;
                        return;
                    }
                }
                Logger.Info($"RequestArtistInfoAsync. Requesting. {artist.Name}");
                _artistInfoIndexingQueue.Enqueue(new ArtistInfoIndexingQueueJob() { Artist = artist });
            });
            return bRet;

        }

        public async Task<bool> RequestAlbumInfoAsync(AlbumV album, bool bIgnorePreviousFailures, bool bForce)
        {
            if (string.IsNullOrEmpty(album.Name) || album.ArtistCount == 0)
            {
                Logger.Warn("RequestAlbumInfoAsync. Name is empty. Exiting");
                return false;
            }
            if (album.Thumbnail != null && !bForce)
            {
                Logger.Warn("RequestAlbumInfo. Image Already Present. Exiting");
                return false;
            }
            if (!InternetAvailability.IsInternetAvailable())
            {
                Logger.Warn("RequestArtistInfoAsync. No Internet is Available. Exiting");
                return false;
            }
            bool bRet = true;
            await Task.Run(() =>
            {
                if (bIgnorePreviousFailures || bForce)
                    infoRepository.RemoveAlbumImageFailed(album);
                if (!bIgnorePreviousFailures)
                {
                    if (infoRepository.HasAlbumImageFailed(album))
                    {
                        Logger.Warn("RequestAlbumInfo. Image has already failed once. Exiting");
                        bRet = false;
                        return;
                    }
                }
                Logger.Info($"RequestAlbumInfoAsync. Requesting. {album.Name}");
                _albumInfoIndexingQueue.Enqueue(new AlbumInfoIndexingQueueJob() { Album = album });
            });
            return bRet;
        }

        public bool SuspendFileSystemWatcher { get => watcherManager.IsSuspended; 
            set 
            {
                if (value)
                    watcherManager.Suspend();
                else
                    watcherManager.Resume();
            }
        }

    }
}
