using Dopamine.Core.Api.Lastfm;
using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class LastFMArtistImageProvider : IArtistImageProvider
    {
        private int _counter = 0;
        private Byte[] _Image = null;

        public LastFMArtistImageProvider(String artist)
        {
            if (string.IsNullOrEmpty(artist))
            {
                Debug.Print("LastFMArtistImageProvider. Missing artist name");
                return;
            }

            LastFmArtist lf = LastfmApi.ArtistGetInfo(artist, false, "EN").Result;


            if (!string.IsNullOrEmpty(lf.ImageLarge))
            {
                    
                try
                {
                    var uri = new Uri(lf.ImageLarge);
                    using (var client = new WebClient())
                    {
                        _Image = client.DownloadData(uri);
                    }

                }
                catch (Exception ex)
                {
                    Debug.Print("Could not download file. Exception: {0}", ex.Message);
                }

            }

        }

        public Byte[] Image { get {
                if (_counter != 0)//== Only one image is supported
                    return null;
                return _Image;
            } }
        /*
        public string AlbumImageUniqueID
        {
            get
            {
                if (_counter != 0)//== Only one image is supported
                    return null;
                return _AlbumImageUniqueID;
            }
        }
        */
        public void next()
        {
            _counter++;
        }
        public string ProviderName
        {
            get { return "LAST_FM_ARTISTS"; }
        }
    }
}
