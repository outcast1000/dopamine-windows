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
        IAlbumInfoProvider GetAlbumInfoProvider(string album, string[] artists);
        IAlbumInfoProvider GetLocalAlbumInfoProvider(string path);//=== Check the folder of the track for images
        IArtistInfoProvider GetArtistInfoProvider();
        ITrackInfoProvider GetTrackInfoProvider(string artist, string album, string track);
        ITrackInfoProvider GetTrackInfoProviderFromTag(FileMetadata fileMetadata);


    }
}
