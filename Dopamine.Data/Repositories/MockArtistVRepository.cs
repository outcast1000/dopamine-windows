using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class MockArtistVRepository: IArtistVRepository
    {
        public ArtistV GetArtist(long artistID, QueryOptions qo = null)
        {
            throw new System.NotImplementedException();
        }

        public List<ArtistV> GetArtists(QueryOptions qo = null)
        {
            return new List<ArtistV>() { 
                new ArtistV() { Id = 1, Name = "Test 1", TrackCount = 2, Genres = "Genre 1, Genre 2" },
                new ArtistV() { Id = 2, Name = "Test 2", TrackCount = 3, Genres = "Genre 1, Genre 3"  } 
            };
        }

        public List<ArtistV> GetArtistsWithText(string searchString, QueryOptions qo = null)
        {
            return new List<ArtistV>() {
                new ArtistV() { Id = 1, Name = "Test 1", TrackCount = 2, Genres = "Genre 1, Genre 2" },
                new ArtistV() { Id = 2, Name = "Test 2", TrackCount = 3, Genres = "Genre 1, Genre 3"  }
            };
        }

        public List<ArtistV> GetArtistsOfTrack(long track_id, QueryOptions qo = null)
        {
            throw new System.NotImplementedException();
        }

        public List<ArtistV> GetArtistsWithoutImages(bool incudeFailedDownloads)
        {
            throw new System.NotImplementedException();
        }
    }
}
