using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using Dopamine.Services.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Services.Collection
{
    public interface ICollectionService
    {
        Task<RemoveTracksResult> RemoveTracksFromCollectionAsync(IList<TrackViewModel> selectedTracks, bool bAlsoDeleteFromDisk);

        Task<IList<ArtistViewModel>> GetArtistsAsync(DataRichnessEnum dataRichness, string searchString = null);

        Task<IList<GenreViewModel>> GetGenresAsync(string searchString = null);

        Task<IList<AlbumViewModel>> GetAlbumsAsync(string searchString = null);

        Task<IList<AlbumViewModel>> GetArtistAlbumsAsync(IList<ArtistViewModel> selectedArtists);

        Task<IList<AlbumViewModel>> GetGenreAlbumsAsync(IList<GenreViewModel> selectedGenres);

        Task<IList<AlbumViewModel>> OrderAlbumsAsync(IList<AlbumViewModel> albums, AlbumOrder albumOrder);

        event EventHandler CollectionChanged;
    }
}
