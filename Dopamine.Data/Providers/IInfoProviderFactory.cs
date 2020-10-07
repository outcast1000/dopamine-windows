using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public interface IInfoProviderFactory
    {
        IAlbumInfoProvider GetAlbumInfoProvider();
        IArtistInfoProvider GetArtistInfoProvider();
        ITrackInfoProvider GetTrackInfoProvider();


    }
}
