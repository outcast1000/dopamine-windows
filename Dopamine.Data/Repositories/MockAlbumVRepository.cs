using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class MockAlbumVRepository : IAlbumVRepository
    {
        public bool AddImage(AlbumV album, string path, bool asThumbnail)
        {
            throw new System.NotImplementedException();
        }

        public bool DeleteImage(AlbumV album)
        {
            throw new System.NotImplementedException();
        }

        public List<AlbumV> GetAlbums()
        {
            return new List<AlbumV>() { 
                new AlbumV() { Id = 1, Name = "album 1", TrackCount = 2, Genres = "Genre 1, Genre 2", MinYear = 1999, AlbumArtists="Album Artist 1", Artists="Artist 1" },
                new AlbumV() { Id = 2, Name = "album 2", TrackCount = 3, Genres = "Genre 1, Genre 3", Artists="Artist 2"   } 
            };
        }

        public List<AlbumV> GetAlbumsToIndex(bool includeFailed)
        {
            throw new System.NotImplementedException();
        }

        public List<AlbumV> GetAlbumsToIndexByProvider(string provider, bool includeFailed)
        {
            throw new System.NotImplementedException();
        }

        public List<AlbumV> GetAlbumsWithArtists(List<long> artistIds)
        {
            return GetAlbums();
        }

        public List<AlbumV> GetAlbumsWithGenres(List<long> genreIds)
        {
            throw new System.NotImplementedException();
        }
    }
}
