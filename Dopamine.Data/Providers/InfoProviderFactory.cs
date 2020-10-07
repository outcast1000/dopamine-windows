using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class InfoProviderFactory: IInfoProviderFactory
    {
        public IAlbumInfoProvider GetAlbumInfoProvider()
        {
            return new LastFMAlbumInfoProvider(new DefaultInternetDownloaderCreator());
        }
        public IAlbumInfoProvider GetLocalAlbumInfoProvider() => throw new NotImplementedException(); //=== Check the folder of the track for images
        public IArtistInfoProvider GetArtistInfoProvider()
        {
            return new MainArtistInfoProvider(new DefaultInternetDownloaderCreator());
        }
        public ITrackInfoProvider GetTrackInfoProvider()
        {
            throw new NotImplementedException();
        }


    }
}
