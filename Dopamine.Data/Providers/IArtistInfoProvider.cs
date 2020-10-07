using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class OriginatedData<T>
    {
        public T Data;
        public string Origin;
    }

    public enum InfoProviderResult{
        Success,
        Fail_Generic,
        Fail_InternetFailed
    }
    public class ArtistInfoProviderData
    {
        public InfoProviderResult result;
        public OriginatedData<Byte[]>[] Images { get; set; }
        public OriginatedData<string>[] Biography { get; set; }
        public OriginatedData<string[]>[] Albums { get; set; }
        public OriginatedData<string[]>[] Tracks { get; set; }
        public OriginatedData<string[]>[] Members { get; set; }
        public OriginatedData<string[]>[] Genres { get; set; }
    }

    public interface IArtistInfoProvider
    {

        ArtistInfoProviderData get(string artist);

        string ProviderName { get; }
    }
}
