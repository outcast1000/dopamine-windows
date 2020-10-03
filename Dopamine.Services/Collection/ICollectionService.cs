using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Services.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Services.Collection
{
    public interface ICollectionService
    {
        Task<RemoveTracksResult> RemoveTracksFromCollectionAsync(IList<TrackViewModel> selectedTracks, bool bAlsoDeleteFromDisk);

        Task<IList<ArtistViewModel>> GetArtistsAsync(string searchString = null);

        Task<IList<GenreViewModel>> GetAllGenresAsync();

        Task<IList<AlbumViewModel>> GetAllAlbumsAsync();

        Task<IList<AlbumViewModel>> GetArtistAlbumsAsync(IList<ArtistViewModel> selectedArtists);

        Task<IList<AlbumViewModel>> GetGenreAlbumsAsync(IList<GenreViewModel> selectedGenres);

        Task<IList<AlbumViewModel>> OrderAlbumsAsync(IList<AlbumViewModel> albums, AlbumOrder albumOrder);

        event EventHandler CollectionChanged;
    }
}
