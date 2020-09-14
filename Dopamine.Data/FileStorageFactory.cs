using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data
{
    public class FileStorageFactory: IFileStorageFactory
    {
        IFileStorage _fileStorageArtist;
        IFileStorage _fileStorageAlbum;
        IFileStorage _fileStorageGenre;
        public IFileStorage getArtistFileStorage()
        {
            if (_fileStorageArtist == null)
                _fileStorageArtist = new FileStorage("artists");
            return _fileStorageArtist;
        }
        public IFileStorage getAlbumFileStorage()
        {
            if (_fileStorageAlbum == null)
                _fileStorageAlbum = new FileStorage("albums");
            return _fileStorageAlbum;
        }
        public IFileStorage getGenreFileStorage()
        {
            if (_fileStorageGenre == null)
                _fileStorageGenre = new FileStorage("genres");
            return _fileStorageGenre;
        }


    }
}
