using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data
{
    public interface IFileStorageFactory
    {
        IFileStorage getArtistFileStorage();
        IFileStorage getAlbumFileStorage();
        IFileStorage getGenreFileStorage();
    }
}
