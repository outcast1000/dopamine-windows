using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IAlbumVRepository
    {
        List<AlbumV> GetAlbums(string searchString = null);

        List<AlbumV> GetAlbumsWithArtists(List<long> artistIds);

        List<AlbumV> GetAlbumsWithGenres(List<long> genreIds);

        List<AlbumV> GetAlbumsWithoutImages(bool incudeFailedDownloads);

        AlbumV GetAlbumOfTrackId(long trackId);

    }
}
