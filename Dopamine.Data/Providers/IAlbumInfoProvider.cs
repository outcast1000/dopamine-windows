using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class AlbumInfoProviderData
    {
        public InfoProviderResult result;
        public OriginatedData<Byte[]>[] Images { get; set; }
        public OriginatedData<string> Review { get; set; }
        public OriginatedData<string> Year { get; set; }
        public OriginatedData<string[]> Genres { get; set; }
        public OriginatedData<string[]> Tracks { get; set; }
    }
  
    public interface IAlbumInfoProvider
    {
        AlbumInfoProviderData Get(string album, string[] artists);

        string ProviderName { get; }
    }
}
