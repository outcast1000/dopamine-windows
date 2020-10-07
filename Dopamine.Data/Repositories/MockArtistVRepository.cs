using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class MockArtistVRepository: IArtistVRepository
    {

        public List<ArtistV> GetArtists(string searchString = null)
        {
            return new List<ArtistV>() { 
                new ArtistV() { Id = 1, Name = "Test 1", TrackCount = 2, Genres = "Genre 1, Genre 2" },
                new ArtistV() { Id = 2, Name = "Test 2", TrackCount = 3, Genres = "Genre 1, Genre 3"  } 
            };
        }

        public List<ArtistV> GetArtistsWithoutImages(bool incudeFailedDownloads)
        {
            throw new System.NotImplementedException();
        }
    }
}
