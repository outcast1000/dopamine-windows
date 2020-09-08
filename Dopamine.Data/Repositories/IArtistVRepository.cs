using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IArtistVRepository
    {
        List<ArtistV> GetArtists();

        List<ArtistV> GetArtistToIndexByProvider(string provider, bool includeFailed);

    }
}
