using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class AlbumInfoProviderData
    {
        public Byte[][] Images { get; set; }
        public string Review { get; set; }
        public string Year { get; set; }
        public string[] Genres { get; set; }
        public string[] Tracks { get; set; }
    }

    public interface IAlbumInfoProvider
    {

        bool Success { get; }

        AlbumInfoProviderData Data { get; }

        string ProviderName { get; }
    }
}
