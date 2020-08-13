using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class MockAlbumVRepository : IAlbumVRepository
    {

        public List<AlbumV> GetAlbums()
        {
            return new List<AlbumV>() { 
                new AlbumV() { Id = 1, Name = "album 1", TrackCount = 2, Genres = "Genre 1, Genre 2", Year = 1999, AlbumArtist="Album Artist 1", Artists="Artist 1" },
                new AlbumV() { Id = 2, Name = "album 2", TrackCount = 3, Genres = "Genre 1, Genre 3", Artists="Artist 2"   } 
            };
        }

        public List<AlbumV> GetAlbumsByArtist(long artistId)
        {
            return GetAlbums();
        }
    }
}
