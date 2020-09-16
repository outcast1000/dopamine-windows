using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IGenreImageRepository
    {
        IList<GenreImage> GetGenreImages();

    }
}
