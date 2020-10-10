using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Alex; //Digimezzo.Foundation.Core.Settings
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Data.Repositories;
using Dopamine.Services.Cache;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.Playback;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Dopamine.Data.UnitOfWorks;
using System.Diagnostics;

namespace Dopamine.Services.Metadata
{
    public class MetadataService : IMetadataService
    {
        private IPlaybackService playbackService;
        private ITrackVRepository trackRepository;
        private IInfoRepository infoRepository;
        private IUnitOfWorksFactory unitOfWorksFactory;
        IFileStorage fileStorage;
        private FileMetadataUpdater updater;
        ObjectCache artworkCache = MemoryCache.Default;
        object artworkCacheLock = new object();

        public event Action<MetadataChangedEventArgs> MetadataChanged = delegate { };
        public event Action<RatingChangedEventArgs> RatingChanged = delegate { };
        public event Action<LoveChangedEventArgs> LoveChanged = delegate { };

        public MetadataService(IPlaybackService playbackService, ITrackVRepository trackRepository, IInfoRepository infoRepository, 
            IUnitOfWorksFactory unitOfWorksFactory, IFileStorage fileStorage)
        {
            this.playbackService = playbackService;
            this.trackRepository = trackRepository;
            this.infoRepository = infoRepository;
            this.unitOfWorksFactory = unitOfWorksFactory;
            this.fileStorage = fileStorage;

            this.updater = new FileMetadataUpdater(this.playbackService, this.trackRepository);
        }

        public FileMetadata GetFileMetadata(string path)
        {
            // First, check if there is a fileMetadata which is queued for saving.
            // If yes, use that, as it has more up to date information.
            FileMetadata fileMetadata = this.updater.GetFileMetadataToUpdate(path);

            if (fileMetadata == null)
            {
                // If not, create a new fileMetadata from the file path.
                fileMetadata = new FileMetadata(path);
            }

            return fileMetadata;
        }

        public async Task<FileMetadata> GetFileMetadataAsync(string path)
        {
            FileMetadata fileMetadata = null;

            await Task.Run(() => fileMetadata = this.GetFileMetadata(path));

            return fileMetadata;
        }

        public async Task UpdateTrackRatingAsync(string path, int rating)
        {
            trackRepository.UpdateRating(path, rating);

            // Update the rating in the file if the user selected this option
            if (SettingsClient.Get<bool>("Behaviour", "SaveRatingToAudioFiles"))
            {
                // Only for MP3's
                if (Path.GetExtension(path).ToLower().Equals(FileFormats.MP3))
                {
                    FileMetadata fmd = await this.GetFileMetadataAsync(path);
                    fmd.Rating = new MetadataRatingValue() { Value = rating };
                    await this.updater.UpdateFileMetadataAsync(new FileMetadata[] { fmd }.ToList());
                }
            }

            this.RatingChanged(new RatingChangedEventArgs(path.ToSafePath(), rating));
        }

        public async Task UpdateTrackLoveAsync(string path, bool love)
        {
            this.trackRepository.UpdateLove(path, love ? 1 : 0);

            this.LoveChanged(new LoveChangedEventArgs(path.ToSafePath(), love));
        }

        private byte[] GetEmbeddedArtwork(string filename, int size)
        {
            byte[] artwork = null;

            FileMetadata fmd = this.GetFileMetadata(filename);

            if (fmd.ArtworkData.Value != null)
            {
                // If size > 0, resize the artwork. Otherwise, get the full artwork.
                artwork = size > 0 ? ImageUtils.ResizeImageInByteArray(fmd.ArtworkData.Value, size, size) : fmd.ArtworkData.Value;
            }

            return artwork;
        }

        private byte[] GetExternalArtwork(string filename, int size)
        {
            byte[] artwork = IndexerUtils.GetExternalArtwork(filename, size, size);

            return artwork;
        }

        private byte[] GetAlbumArtwork(string filename, int size)
        {
            byte[] artwork = null;

            AlbumImage albumImage = infoRepository.GetAlbumImageForTrackWithPath(filename);
            if (albumImage != null)
            {
                string artworkPath = fileStorage.GetRealPath(albumImage.Location);

                if (!string.IsNullOrEmpty(artworkPath))
                {
                    artwork = ImageUtils.Image2ByteArray(artworkPath, size, size);
                }
            }

            return artwork;
        }

        public async Task<byte[]> GetArtworkAsync(string filename, int size = 0)
        {
            byte[] artwork = null;

            if (System.IO.File.Exists(filename))
            {
                await Task.Run(() =>
                {
                    lock (artworkCacheLock)
                    {
                        // First, try to find artwork in the memory cache
                        artwork = this.artworkCache[filename] as byte[];

                        if (artwork == null)
                        {
                            // If no artwork was found in the cache, try to find embedded artwork.
                            artwork = this.GetEmbeddedArtwork(filename, size);

                            if (artwork == null)
                            {
                                // If no embedded artwork was found, try to find external artwork.
                                artwork = this.GetExternalArtwork(filename, size);
                            }

                            if (artwork == null)
                            {
                                // If no external artwork was found, try to find album artwork.
                                artwork = this.GetAlbumArtwork(filename, size);
                            }

                            if (artwork != null)
                            {
                                // If artwork was found, add it to the cache.
                                CacheItemPolicy policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(5.0) };
                                this.artworkCache.Set(filename, artwork, policy);
                            }
                        }
                    }
                });
            }

            return artwork;
        }

        public async Task ForceSaveFileMetadataAsync()
        {
            await this.updater.ForceUpdateFileMetadataAsync();
        }

        private async Task UpdateDatabaseMetadataAsync(FileMetadata fileMetadata, bool updateAlbumArtwork)
        {
            // Get the track from the database
            TrackV track = trackRepository.GetTrackWithPath(fileMetadata.SafePath);

            if (track == null)
            {
                return;
            }

            // Update track fields
            await Task.Run(() => MetadataUtils.FillTrackBase(fileMetadata, ref track));

            // Update the Track in the database
            this.trackRepository.UpdateTrack(track);

            if (updateAlbumArtwork)
            {
                // Cache the new artwork
                //string artworkID = await this.cacheService.CacheArtworkAsync(fileMetadata.ArtworkData.Value);

                Debug.Assert(false, "ALEX TODO");
                // Add or update AlbumArtwork in the database
                //albumImageRepository.UpdateAlbumArtworkAsync(track.AlbumTitle, artworkID);
            }
        }

        private async Task UpdateDatabaseMetadataAsync(IList<FileMetadata> fileMetadatas, bool updateAlbumArtwork)
        {
            foreach (FileMetadata fileMetadata in fileMetadatas)
            {
                try
                {
                    await this.UpdateDatabaseMetadataAsync(fileMetadata, updateAlbumArtwork);
                }
                catch (Exception ex)
                {
                    LogClient.Error("Unable to update database metadata for Track '{0}'. Exception: {1}", fileMetadata.SafePath, ex.Message);
                }
            }
        }

        public async Task UpdateTracksAsync(IList<FileMetadata> fileMetadatas, bool updateAlbumArtwork)
        {
            // Update metadata in the files
             await this.updater.UpdateFileMetadataAsync(fileMetadatas);

            // Update metadata in the database
            await this.UpdateDatabaseMetadataAsync(fileMetadatas, updateAlbumArtwork);

            // Update metadata in the PlaybackService queue
            await this.playbackService.UpdateQueueMetadataAsync(fileMetadatas);

            // Raise event
            this.MetadataChanged(new MetadataChangedEventArgs());
        }

        public async Task UpdateAlbumAsync(AlbumViewModel albumViewModel, MetadataArtworkValue artwork, bool updateFileArtwork)
        {
            Debug.Assert(false, "ALEX TODO");
            // Cache the new artwork
            //string artworkID = await this.cacheService.CacheArtworkAsync(artwork.Value);

            // Add or update AlbumArtwork in the database
            using (IUpdateCollectionUnitOfWork uc = unitOfWorksFactory.getUpdateCollectionUnitOfWork())
            {
                //String realImagePath = cacheService.GetCachedArtworkPath("cache://" + artworkID);
                //long len = new FileInfo(realImagePath).Length;
                uc.SetAlbumImage(new AlbumImage()
                {
                    AlbumId = albumViewModel.Id,
                    //Location = "cache://" + artworkID,
                    DateAdded = DateTime.Now.Ticks,
                    Source = "[EDIT]"
                }, true);
                //albumViewModel.Id, "cache://" + artworkID, len, null, "[file]", true);
            }

            if (updateFileArtwork)
            {
                // Get the tracks for this album
                IList<TrackV> tracks = this.trackRepository.GetTracksOfAlbums(new List<long> { albumViewModel.Id});
                IList<FileMetadata> fileMetadatas = new List<FileMetadata>();

                foreach (TrackV track in tracks)
                {
                    FileMetadata fileMetadata = await this.GetFileMetadataAsync(track.Path);
                    fileMetadata.ArtworkData = artwork;
                    fileMetadatas.Add(fileMetadata);
                }

                // Update metadata in the files
                await this.updater.UpdateFileMetadataAsync(fileMetadatas);
            }

            // Raise event
            this.MetadataChanged(new MetadataChangedEventArgs());
        }
    }
}
