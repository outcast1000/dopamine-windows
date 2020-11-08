﻿using Digimezzo.Foundation.Core.Logging;
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
            IUnitOfWorksFactory unitOfWorksFactory, IFileStorage fileStorage, IIndexingService indexingService)
        {
            this.playbackService = playbackService;
            this.trackRepository = trackRepository;
            this.infoRepository = infoRepository;
            this.unitOfWorksFactory = unitOfWorksFactory;
            this.fileStorage = fileStorage;

            this.updater = new FileMetadataUpdater(this.playbackService, this.trackRepository, indexingService);
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
            TrackV track = trackRepository.GetTrackWithPath(path);
            if (track == null)
                return;
            trackRepository.UpdateRating(track.Id, rating);

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
            bool bSuccess = false;
            await Task.Run(() => {
                TrackV track = trackRepository.GetTrackWithPath(path);
                if (track != null)
                {
                   bSuccess = trackRepository.UpdateLove(track.Id, love ? 1 : 0);
                }
                });
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

        private byte[] GetAlbumArtwork(TrackViewModel trackViewModel, int size)
        {
            byte[] artwork = null;

            if (trackViewModel.Data.AlbumImage != null)
            {
                string artworkPath = fileStorage.GetRealPath(trackViewModel.Data.AlbumImage);
                if (!string.IsNullOrEmpty(artworkPath))
                {
                    artwork = ImageUtils.Image2ByteArray(artworkPath, size, size);
                }
            }

            return artwork;
        }

        public async Task<byte[]> GetArtworkAsync(TrackViewModel trackViewModel)
        {

            byte[] artwork = null;

            if (System.IO.File.Exists(trackViewModel.Path))
            {
                await Task.Run(() =>
                {
                    lock (artworkCacheLock)
                    {
                        // First, try to find artwork in the memory cache
                        artwork = this.artworkCache[trackViewModel.Path] as byte[];
                        //if (artwork == null) // Disable Embedded Artwork
                        //    artwork = this.GetEmbeddedArtwork(trackViewModel.Path, 0);
                        if (artwork == null)
                            artwork = GetExternalArtwork(trackViewModel.Path, 0); // Get it from the folder
                        if (artwork == null)
                            artwork = GetAlbumArtwork(trackViewModel, 0);// If no external artwork was found, try to find from the cache
                        if (artwork != null)
                        {
                            // If artwork was found, add it to the cache.
                            CacheItemPolicy policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(5.0) };
                            this.artworkCache.Set(trackViewModel.Path, artwork, policy);
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

        private async Task UpdateDatabaseMetadataAsync(FileMetadata fileMetadata)
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

        }

        private async Task UpdateDatabaseMetadataAsync(IList<FileMetadata> fileMetadatas)
        {
            foreach (FileMetadata fileMetadata in fileMetadatas)
            {
                try
                {
                    await this.UpdateDatabaseMetadataAsync(fileMetadata);
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
            await this.UpdateDatabaseMetadataAsync(fileMetadatas);

            // Update metadata in the PlaybackService queue
            await this.playbackService.UpdatePlaylistMetadataAsync(fileMetadatas);

            // Raise event
            this.MetadataChanged(new MetadataChangedEventArgs());
        }

        public async Task UpdateAlbumAsync(AlbumViewModel albumViewModel, MetadataArtworkValue artwork, bool updateFileArtwork)
        {
            if (artwork?.Value != null)
            {
                string artworkID = fileStorage.SaveImageToCache(artwork.Value, FileStorageItemType.Album);
                // Add or update AlbumArtwork in the database
                infoRepository.SetAlbumImage(new AlbumImage()
                {
                    AlbumId = albumViewModel.Id,
                    Location = artworkID,
                    DateAdded = DateTime.Now.Ticks,
                    Origin = String.Empty,
                    OriginType = OriginType.User
                }, true);
            }
            else
            {
                //=== Remove & Add a record on Failed Downloads in order ot not download it again
                infoRepository.RemoveAlbumImage(albumViewModel.Id);
                infoRepository.SetAlbumImageFailed(albumViewModel.Data);
            }
            // Cache the new artwork


            if (updateFileArtwork)
            {
                // Get the tracks for this album
                IList<TrackV> tracks = this.trackRepository.GetTracksOfAlbums(new List<long> { albumViewModel.Id}, true);
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

        public async Task UpdateArtistAsync(ArtistViewModel artistViewModel, MetadataArtworkValue artwork)
        {
            if (artwork?.Value != null)
            {
                string artworkID = fileStorage.SaveImageToCache(artwork.Value, FileStorageItemType.Album);
                // Add or update AlbumArtwork in the database
                infoRepository.SetArtistImage(new ArtistImage()
                {
                    ArtistId = artistViewModel.Id,
                    Location = artworkID,
                    DateAdded = DateTime.Now.Ticks,
                    Origin = String.Empty,
                    OriginType = OriginType.User
                });
            }
            else
            {
                //=== Remove & Add a record on Failed Downloads in order ot not download it again
                infoRepository.RemoveAlbumImage(artistViewModel.Id);
                infoRepository.SetArtistImageFailed(artistViewModel.Data);
            }
            // Cache the new artwork

            // Raise event
            this.MetadataChanged(new MetadataChangedEventArgs());
        }
    }
}
