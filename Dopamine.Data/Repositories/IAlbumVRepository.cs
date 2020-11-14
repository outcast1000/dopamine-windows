using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IAlbumVRepository
    {
        List<AlbumV> GetAlbums(QueryOptions qo = null);

        List<AlbumV> GetAlbumsWithText(string searchString, QueryOptions qo = null);


        List<AlbumV> GetAlbumsWithArtists(List<long> artistIds, QueryOptions qo = null);

        List<AlbumV> GetAlbumsWithGenres(List<long> genreIds, QueryOptions qo = null);

        List<AlbumV> GetAlbumsWithoutImages(bool incudeFailedDownloads);

        AlbumV GetAlbum(long albumId, QueryOptions qo = null);

        AlbumV GetAlbumOfTrackId(long trackId, QueryOptions qo = null);

    }
}
