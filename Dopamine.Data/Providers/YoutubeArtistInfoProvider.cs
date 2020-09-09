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
    public class YoutubeArtistInfoProvider : IArtistInfoProvider
    {

        public YoutubeArtistInfoProvider(String artist)
        {
            RequestedImages = 1;
            Success = Init(artist);
        }

        public int RequestedImages { get; set;}

        private bool Init(String artist)
        {
            if (string.IsNullOrEmpty(artist))
            {
                Debug.Print("YoutubeArtistInfoProvider. Missing artist name");
                return false;
            }
            Uri uri = new Uri(string.Format("https://music.youtube.com/search?q={0}", System.Uri.EscapeDataString(artist)));

            try
            {

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36");
                    client.DefaultRequestHeaders.ExpectContinue = false;
                    // Get the search page
                    var response = client.GetAsync(uri).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    // Find the channel page
                    var regex = new Regex("{\\\\\"browseId\\\\\":\\\\\"(.*?)\\\\\",\\\\\"browseEndpointContextSupportedConfigs\\\\\":{\\\\\"browseEndpointContextMusicConfig\\\\\":{\\\\\"pageType\\\\\":\\\\\"MUSIC_PAGE_TYPE_ARTIST");
                    MatchCollection matches = regex.Matches(result);
                    if (matches.Count == 0)
                        return false;
                    string channel = matches[0].Groups[1].Value;
                    // Download the channel page
                    uri = new Uri(string.Format("https://music.youtube.com/channel/{0}", channel));
                    response = client.GetAsync(uri).Result;
                    result = response.Content.ReadAsStringAsync().Result;
                    // Find the Bio
                    regex = new Regex("description\\\\\\\":{\\\\\\\"runs\\\\\\\":\\[{\\\\\\\"text\\\\\\\":\\\\\\\"(.*?)\\\\\\\"}\\]}");
                    matches = regex.Matches(result);
                    if (matches.Count > 0)
                    {
                        Bio = matches[0].Groups[1].Value;
                    }
                    // Find the images
                    List<byte[]> images = new List<byte[]>();
                    regex = new Regex("thumbnail\\\\\\\":{\\\\\\\"thumbnails\\\\\\\":\\[{\\\\\\\"url\\\\\\\":\\\\\\\"(.*?)\\\\\\\",\\\\\\\"width\\\\\\\":(.*?),");
                    matches = regex.Matches(result);
                    foreach (Match match in matches)
                    {
                        int width = int.Parse(match.Groups[2].Value);
                        if (width < 540)
                        {
                            Trace.WriteLine("Avoiding Image. Width is less than 540 :" + width.ToString());
                            continue;
                        }
                        string url = match.Groups[1].Value.Replace("\\/", "/");
                        uri = new Uri(url);
                        response = client.GetAsync(uri).Result;
                        byte[] bRes = response.Content.ReadAsByteArrayAsync().Result;
                        images.Add(bRes);
                        if (images.Count >= RequestedImages)
                            break;
                    }
                    Images = images.ToArray();
                    return true;

                    //=== ALEX TODO
                    // Find the Albums, Members, Genres

                }
            }
            catch (Exception ex)
            {
                OnException("", ex);
            }
            return false;
        }

        private void OnException(string message, 
            Exception ex, 
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Trace.Indent();
            Trace.WriteLine("message: " + message);
            Trace.WriteLine("exception: " + ex.Message);
            Trace.WriteLine("member name: " + memberName);
            Trace.WriteLine("source file path: " + sourceFilePath);
            Trace.WriteLine("source line number: " + sourceLineNumber);
            Trace.Unindent();
        }

        public string ProviderName
        {
            get { return "GOOGLE_ARTISTS"; }
        }

        public byte[][] Images { get; private set; }

        public string Bio { get; private set; }

        public string[] Albums { get; private set; }

        public string[] Songs { get; private set; }

        public string[] Members { get; private set; }

        public string[] Genres { get; private set; }

        public bool Success { get; private set; }
    }
}
