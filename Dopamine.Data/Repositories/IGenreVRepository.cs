using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IGenreVRepository
    {
        List<GenreV> GetGenres(string searchString = null);

        List<GenreV> GetGenresByArtistId(long artistId);

        List<GenreV> GetGenresByAlbumId(long albumId);

    }
}
