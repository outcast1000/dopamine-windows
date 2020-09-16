using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IArtistImageRepository
    {
        IList<ArtistImage> GetArtistImages();

    }
}
