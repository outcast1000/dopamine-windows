using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using Dopamine.Services.Cache;
using Dopamine.Services.Entities;
using Dopamine.Services.Playback;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Dopamine.Services.Collection
{
    public class CollectionService : ICollectionService
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IPlaybackService playbackService;
        private IContainerProvider container;

        //=== ALEX
        private ITrackVRepository trackVRepository;
        private IArtistVRepository artistVRepository;
        private IAlbumVRepository albumVRepository;
        private IGenreVRepository genreVRepository;
        private IFolderVRepository folderVRepository;


        public CollectionService(ITrackVRepository trackVRepository, IFolderVRepository folderVRepository, IPlaybackService playbackService, IContainerProvider container,
            IArtistVRepository artistVRepository, IAlbumVRepository albumVRepository, IGenreVRepository genreVRepository)
        {
            this.folderVRepository = folderVRepository;
            this.playbackService = playbackService;
            this.container = container;
            this.trackVRepository = trackVRepository;
            this.artistVRepository = artistVRepository;
            this.albumVRepository = albumVRepository;
            this.genreVRepository = genreVRepository;


        }

        public event EventHandler CollectionChanged = delegate { };

        public async Task<RemoveTracksResult> RemoveTracksFromCollectionAsync(IList<TrackViewModel> selectedTracks, bool bAlsoDeleteFromDisk)
        {
            var sendToRecycleBinResult = RemoveTracksResult.Success;
            RemoveTracksResult result = this.trackVRepository.RemoveTracks(selectedTracks.Select(t => t.Id).ToList());
            if (result == RemoveTracksResult.Success)
            {
                // If result is Success: we can assume that all selected tracks were removed from the collection,
                // as this happens in a transaction in trackRepository. If removing 1 or more tracks fails, the
                // transaction is rolled back and no tracks are removed.
                foreach (TrackViewModel track in selectedTracks)
                {
                    // When the track is playing, the corresponding file is handled by IPlayer.
                    // To delete the file properly, PlaybackService must release this handle.
                    await this.playbackService.StopIfPlayingAsync(track);

                    try
                    {
                        // Delete file from disk
                        FileUtils.SendToRecycleBinSilent(track.Path);
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error($"Error while removing track '{track.TrackTitle}' from disk. Exception: {ex.Message}");
                        sendToRecycleBinResult = RemoveTracksResult.Error;
                    }
                }
                this.CollectionChanged(this, new EventArgs());
            }

            if (sendToRecycleBinResult == RemoveTracksResult.Success && result == RemoveTracksResult.Success)
                return RemoveTracksResult.Success;
            return RemoveTracksResult.Error;
        }

        public async Task<RemoveTracksResult> RemoveTracksFromDiskAsync(IList<TrackViewModel> selectedTracks)
        {
            var sendToRecycleBinResult = RemoveTracksResult.Success;
            var result = this.trackVRepository.RemoveTracks(selectedTracks.Select(t => t.Id).ToList());

            if (result == RemoveTracksResult.Success)
            {
                // If result is Success: we can assume that all selected tracks were removed from the collection,
                // as this happens in a transaction in trackRepository. If removing 1 or more tracks fails, the
                // transaction is rolled back and no tracks are removed.
                foreach (TrackViewModel track in selectedTracks)
                {
                    // When the track is playing, the corresponding file is handled by IPlayer.
                    // To delete the file properly, PlaybackService must release this handle.
                    await this.playbackService.StopIfPlayingAsync(track);

                    try
                    {
                        // Delete file from disk
                        FileUtils.SendToRecycleBinSilent(track.Path);
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error($"Error while removing track '{track.TrackTitle}' from disk. Exception: {ex.Message}");
                        sendToRecycleBinResult = RemoveTracksResult.Error;
                    }
                }

                this.CollectionChanged(this, new EventArgs());
            }

            if (sendToRecycleBinResult == RemoveTracksResult.Success && result == RemoveTracksResult.Success)
                return RemoveTracksResult.Success;
            return RemoveTracksResult.Error;
        }
        /*
        private async Task<IList<ArtistViewModel>> GetUniqueArtistsAsync(IList<string> artists)
        {
            IList<ArtistViewModel> uniqueArtists = new List<ArtistViewModel>();

            await Task.Run(() =>
            {
                bool hasUnknownArtists = false;

                foreach (string artist in artists)
                {
                    if (!string.IsNullOrEmpty(artist))
                    {
                        var newArtist = new ArtistViewModel(artist);

                        if (!uniqueArtists.Contains(newArtist))
                        {
                            uniqueArtists.Add(newArtist);
                        }
                    }
                    else
                    {
                        hasUnknownArtists = true;
                    }
                }

                if (hasUnknownArtists)
                {
                    var unknownArtist = new ArtistViewModel(ResourceUtils.GetString("Language_Unknown_Artist"));

                    if (!uniqueArtists.Contains(unknownArtist))
                    {
                        uniqueArtists.Add(unknownArtist);
                    }
                }
            });

            return uniqueArtists;
        }
        */


        /*
        private async Task<IList<AlbumViewModel>> GetUniqueAlbumsAsync(IList<AlbumData> albums)
        {
            IList<AlbumViewModel> uniqueAlbums = new List<AlbumViewModel>();

            await Task.Run(() =>
            {
                foreach (AlbumData album in albums)
                {
                    var newAlbum = new AlbumViewModel(album);

                    if (!uniqueAlbums.Contains(newAlbum))
                    {
                        uniqueAlbums.Add(newAlbum);
                    }
                }
            });

            return uniqueAlbums;
        }
        */

        public async Task<IList<GenreViewModel>> GetGenresAsync(string searchString = null)
        {
            List<GenreViewModel> viewModels = new List<GenreViewModel>();
            await Task.Run(() =>
            {
                IList<GenreV> items = this.genreVRepository.GetGenres(searchString);
                if (items == null)
                    Logger.Warn($"GetGenresAsync genreVRepository({searchString}) return null");
                else
                    viewModels = items.Select(x => new GenreViewModel(x)).ToList();
            });
            return viewModels;
        }

        public async Task<IList<ArtistViewModel>> GetArtistsAsync(string searchString = null)
        {
            List<ArtistViewModel> viewModels = new List<ArtistViewModel>();
            await Task.Run(() =>
            {
               IList<ArtistV> items = artistVRepository.GetArtists(searchString);
               if (items == null)
                    Logger.Warn($"GetArtistsAsync artistVRepository.GetArtists({searchString}) return null");
               else
                    viewModels = items.Select(x => new ArtistViewModel(x)).ToList();
            });
            return viewModels;
        }

        public async Task<IList<AlbumViewModel>> GetAlbumsAsync(string searchString = null)
        {
            IList<AlbumViewModel> avm = new List<AlbumViewModel>();
            await Task.Run(() =>
            {
                IList<AlbumV> items = albumVRepository.GetAlbums(searchString);
                if (items == null)
                    Logger.Warn($"GetAlbumsAsync albumVRepository.GetAlbums({searchString}) return null");
                else
                    avm = items.Select(a => new AlbumViewModel(a)).ToList();
            });
            return avm;
        }

        public async Task<IList<AlbumViewModel>> GetArtistAlbumsAsync(IList<ArtistViewModel> selectedArtists)
        {
            IList<AlbumViewModel> avm = null;
            await Task.Run(() =>
            {
                IList<AlbumV> albums = albumVRepository.GetAlbumsWithArtists(selectedArtists.Select(x => x.Id).ToList());
                avm = albums.Select(x => new AlbumViewModel(x)).ToList();
            });
            return avm;
        }

        public async Task<IList<AlbumViewModel>> GetGenreAlbumsAsync(IList<GenreViewModel> selectedGenres)
        {
            IList<AlbumViewModel> avm = null;
            await Task.Run(() =>
            {
                IList<AlbumV> albums = albumVRepository.GetAlbumsWithGenres(selectedGenres.Select(x => x.Id).ToList());
                avm = albums.Select(x => new AlbumViewModel(x)).ToList();
            });
            return avm;
        }

        public async Task<IList<AlbumViewModel>> OrderAlbumsAsync(IList<AlbumViewModel> albums, AlbumOrder albumOrder)
        {
            var orderedAlbums = new List<AlbumViewModel>();

            await Task.Run(() =>
            {
                switch (albumOrder)
                {
                    case AlbumOrder.Alphabetical:
                        orderedAlbums = albums.OrderBy((a) => FormatUtils.GetSortableString(a.Name)).ToList();
                        break;
                    case AlbumOrder.ByDateAdded:
                        orderedAlbums = albums.OrderByDescending((a) => a.DateAdded).ToList();
                        break;
                    case AlbumOrder.ByDateCreated:
                        orderedAlbums = albums.OrderByDescending((a) => a.DateFileCreated).ToList();
                        break;
                    case AlbumOrder.ByAlbumArtist:
                        orderedAlbums = albums.OrderBy((a) => FormatUtils.GetSortableString(a.AlbumArtists, true)).ToList();
                        break;
                    case AlbumOrder.ByYearAscending:
                        orderedAlbums = albums.OrderBy((a) => a.MinYear).ToList();
                        break;
                    case AlbumOrder.ByYearDescending:
                        orderedAlbums = albums.OrderByDescending((a) => a.MinYear).ToList();
                        break;
                    default:
                        // Alphabetical
                        orderedAlbums = albums.OrderBy((a) => FormatUtils.GetSortableString(a.Name)).ToList();
                        break;
                }
                /* ALEX AVOID Headers
                foreach (AlbumViewModel alb in orderedAlbums)
                {
                    string mainHeader = alb.AlbumTitle;
                    string subHeader = alb.AlbumArtist;

                    switch (albumOrder)
                    {
                        case AlbumOrder.ByAlbumArtist:
                            mainHeader = alb.AlbumArtist;
                            subHeader = alb.AlbumTitle;
                            break;
                        case AlbumOrder.ByYearAscending:
                        case AlbumOrder.ByYearDescending:
                            mainHeader = alb.Year;
                            subHeader = alb.AlbumTitle;
                            break;
                        case AlbumOrder.Alphabetical:
                        case AlbumOrder.ByDateAdded:
                        case AlbumOrder.ByDateCreated:
                        default:
                            // Do nothing
                            break;
                    }

                    alb.MainHeader = mainHeader;
                    alb.SubHeader = subHeader;
                }
                */
            });

            return orderedAlbums;
        }
    }
}
