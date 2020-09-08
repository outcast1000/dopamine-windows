using Dopamine.Core.Api.Lastfm;
using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class GoogleArtistImageProvider : IArtistImageProvider
    {
        private String _artist;
        private int _counter = 0;
        private IList<Byte[]> _images = new List<byte[]>();
        private bool _bInit = false;

        public GoogleArtistImageProvider(String artist)
        {
            _artist = artist;
        }

        private void Init()
        {
            _bInit = true;
            if (string.IsNullOrEmpty(_artist))
            {
                Debug.Print("GoogleArtistImageProvider. Missing artist name");
                return;
            }
            Uri uri = new Uri(string.Format("https://www.google.com/search?q={0}+(band)", System.Uri.EscapeDataString(_artist)));

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.ExpectContinue = false;
                var response = client.GetAsync(uri).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                var regex = new Regex("data:image\\/jpeg;base64,(.*?)';");
                MatchCollection matches = regex.Matches(result);
                int matchCount = 0;
                foreach (Match match in matches)
                {
                    //if (match.Groups.Count > 1)
                    try
                    {
                        string base64 = match.Groups[1].Value.Replace("\\x3d", "=");
                        byte[] data = Convert.FromBase64String(base64);
                        _images.Add(data);
                    }
                    catch (Exception e)
                    {
                        Debug.Print("Exception for {0}: {1}, :{2}", matchCount, e.Message, match.Groups[1].Value);
                    }
                    matchCount++;

                }
            }
        }

        public Byte[] Image { get {
                if (!_bInit)
                    Init();
                if (_counter >= _images.Count)//== Only one image is supported
                    return null;
                return _images[_counter];
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
            get { return "GOOGLE_ARTISTS"; }
        }
    }
}
