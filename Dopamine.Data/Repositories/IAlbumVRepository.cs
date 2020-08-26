using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IAlbumVRepository
    {
        List<AlbumV> GetAlbums();

        List<AlbumV> GetAlbumsWithArtists(List<long> artistIds);

        List<AlbumV> GetAlbumsWithGenres(List<long> genreIds);

        List<AlbumV> GetAlbumsToIndex(bool includeFailed);

        bool AddImage(AlbumV album, string path, bool asThumbnail);

        bool DeleteImage(AlbumV album);

    }
}
