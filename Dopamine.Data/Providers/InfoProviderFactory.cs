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
        public IAlbumInfoProvider GetAlbumInfoProvider(string album, string[] artists)
        {
            return new LastFMAlbumInfoProvider(album, artists);
        }
        public IAlbumInfoProvider GetLocalAlbumInfoProvider(string path) => throw new NotImplementedException(); //=== Check the folder of the track for images
        public IArtistInfoProvider GetArtistInfoProvider(string artist)
        {
            return new GoogleArtistInfoProvider(artist);
        }
        public ITrackInfoProvider GetTrackInfoProvider(string artist, string album, string track)
        {
            throw new NotImplementedException();
        }
        public ITrackInfoProvider GetTrackInfoProviderFromTag(FileMetadata fileMetadata)
        {
            return new TagTrackInfoProvider(fileMetadata);
        }


    }
}
