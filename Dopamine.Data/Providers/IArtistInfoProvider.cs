using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public interface IArtistInfoProvider
    {
        bool Success { get;  }
        Byte[][] Images { get; }
        string Bio { get; }
        string[] Albums { get; }
        string[] Songs { get; }
        string[] Members { get; }
        string[] Genres { get; }
        string ProviderName { get; }
    }
}
