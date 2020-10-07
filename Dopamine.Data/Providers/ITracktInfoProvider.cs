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
        TrackInfoProviderData Get(string artist, string album, string track);
        string ProviderName { get; }
    }
}
