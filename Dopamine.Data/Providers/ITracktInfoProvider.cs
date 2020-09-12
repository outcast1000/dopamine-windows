using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{

    public class TrackInfoProviderData
    {
        public Byte[][] Images { get; set; }
        public string[] Lyrics { get; set; }
    }

    public interface ITrackInfoProvider
    {

        bool Success { get;  }

        TrackInfoProviderData Data { get; }

        string ProviderName { get; }
    }
}
