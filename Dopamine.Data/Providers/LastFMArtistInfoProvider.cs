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
    //=== Currently support only Bio. Images are invalidated by LastFM (9/9/2020)
    public class LastFMArtistInfoProvider : IArtistInfoProvider
    {
        public LastFMArtistInfoProvider(String artist)
        {
            Success = false;
            if (string.IsNullOrEmpty(artist))
            {
                Debug.Print("LastFMArtistImageProvider. Missing artist name");
                return;
            }

            LastFmArtist lf = LastfmApi.ArtistGetInfo(artist, false, "EN").Result;
            if (lf.Biography  != null)
            {
                Bio = lf.Biography.Content;
            }

            Success = true;
        }


        public string ProviderName
        {
            get { return "LAST_FM_ARTISTS"; }
        }

        public bool Success { get; private set; }

        public byte[][] Images { get; private set; }

        public string Bio { get; private set; }

        public string[] Albums { get; private set; }

        public string[] Songs { get; private set; }

        public string[] Members { get; private set; }

        public string[] Genres { get; private set; }
    }
}
