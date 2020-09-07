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
    public class LastFmAlbumImageProvider : IAlbumImageProvider
    {
        private int _counter = 0;
        private Byte[] _AlbumImage = null;

        public LastFmAlbumImageProvider(String album, string[] artists)
        {
            if (string.IsNullOrEmpty(album) || artists == null)
            {
                Debug.Print("LastFmAlbumImageProvider. Missing album info");
                return;
            }

            foreach (string artist in artists)
            {
                LastFmAlbum lfmAlbum = LastfmApi.AlbumGetInfo(artist, album, false, "EN").Result;

                if (!string.IsNullOrEmpty(lfmAlbum.LargestImage()))
                {
                    
                    try
                    {
                        var uri = new Uri(lfmAlbum.LargestImage());
                        using (var client = new WebClient())
                        {
                            _AlbumImage = client.DownloadData(uri);
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Could not download file to temporary cache. Exception: {0}", ex.Message);
                    }

                    break;
                }
            }

        }

        public Byte[] AlbumImage { get {
                if (_counter != 0)//== Only one image is supported
                    return null;
                return _AlbumImage;
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
            get { return "TAG"; }
        }
    }
}
