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
    public class LastFMAlbumInfoProvider : IAlbumInfoProvider
    {

        public LastFMAlbumInfoProvider(String album, string[] artists)
        {
            Success = false;
            if (string.IsNullOrEmpty(album) || artists == null)
            {
                Debug.Print("LastFmAlbumImageProvider. Missing album info");
                return;
            }
            Data = new AlbumInfoProviderData();

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
                            Data.Images = new Byte[][] { client.DownloadData(uri)};
                            Success = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Could not download file to temporary cache. Exception: {0}", ex.Message);
                        Success = false;
                    }
                    break;
                }
            }

        }


        public string ProviderName
        {
            get { return "LAST_FM_ALBUMS"; }
        }

        public AlbumInfoProviderData Data
        {
            get; private set;
        }

        public bool Success { get; private set; }
    }
}
