using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{

    public class ArtistInfoProviderData
    {
        public Byte[][] Images { get; set; }
        public string Biography { get; set; }
        public string[] Albums { get; set; }
        public string[] Tracks { get; set; }
        public string[] Members { get; set; }
        public string[] Genres { get; set; }
    }

    public interface IArtistInfoProvider
    {

        bool Success { get;  }

        ArtistInfoProviderData Data { get; }

        string ProviderName { get; }
    }
}
