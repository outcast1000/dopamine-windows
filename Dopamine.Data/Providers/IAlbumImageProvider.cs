using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    interface IAlbumImageProvider
    {
        Byte[] AlbumImage { get; }
        //string AlbumImageUniqueID { get; }
        void next();
        string ProviderName { get; }
    }
}
