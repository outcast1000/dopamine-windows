using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IGenreVRepository
    {
        List<GenreV> GetGenres(QueryOptions qo = null);

        List<GenreV> GetGenresWithText(string text, QueryOptions qo = null);

        List<GenreV> GetGenresByArtistId(long artistId, QueryOptions qo = null);

        List<GenreV> GetGenresByAlbumId(long albumId, QueryOptions qo = null);

    }
}
