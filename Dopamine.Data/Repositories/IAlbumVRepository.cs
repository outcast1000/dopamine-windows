using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IAlbumVRepository
    {
        List<AlbumV> GetAlbums(bool bGetHistory, string searchString = null);

        List<AlbumV> GetAlbumsWithArtists(List<long> artistIds, bool bGetHistory);

        List<AlbumV> GetAlbumsWithGenres(List<long> genreIds, bool bGetHistory);

        List<AlbumV> GetAlbumsWithoutImages(bool incudeFailedDownloads);

        AlbumV GetAlbum(long albumId, bool bGetHistory);

        AlbumV GetAlbumOfTrackId(long trackId, bool bGetHistory);

    }
}
