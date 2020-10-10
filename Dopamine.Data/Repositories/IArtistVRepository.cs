using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IArtistVRepository
    {
        List<ArtistV> GetArtists(string searchString = null);

        List<ArtistV> GetArtistsWithoutImages(bool incudeFailedDownloads);

        ArtistV GetArtist(long artistID);


    }
}
