using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IArtistVRepository
    {
        List<ArtistV> GetArtists(QueryOptions qo = null);

        List<ArtistV> GetArtistsWithText(string searchString, QueryOptions qo = null);


        List<ArtistV> GetArtistsOfTrack(long track_id, QueryOptions qo = null);

        List<ArtistV> GetArtistsWithoutImages(bool incudeFailedDownloads);

        ArtistV GetArtist(long artistID, QueryOptions qo = null);


    }
}
