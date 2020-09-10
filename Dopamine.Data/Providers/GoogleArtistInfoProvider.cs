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
    public class GoogleArtistInfoProvider : IArtistInfoProvider
    {

        public GoogleArtistInfoProvider(String artist)
        {
            Init(artist);
        }

        private void Init(String artist)
        {
            Success = false;
            if (string.IsNullOrEmpty(artist))
            {
                Debug.Print("GoogleArtistImageProvider. Missing artist name");
                return;
            }
            Data = new ArtistInfoProviderData();
            Uri uri = new Uri(string.Format("https://www.google.com/search?q={0}+(band)", System.Uri.EscapeDataString(artist)));

            try
            {

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.ExpectContinue = false;
                    var response = client.GetAsync(uri).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    var regex = new Regex("data:image\\/jpeg;base64,(.*?)';");
                    MatchCollection matches = regex.Matches(result);
                    int matchCount = 0;
                    List<byte[]> images = new List<byte[]>();
                    foreach (Match match in matches)
                    {
                        //if (match.Groups.Count > 1)
                        try
                        {
                            string base64 = match.Groups[1].Value.Replace("\\x3d", "=");
                            byte[] data = Convert.FromBase64String(base64);
                            images.Add(data);
                        }
                        catch (Exception e)
                        {
                            Debug.Print("Exception for {0}: {1}, :{2}", matchCount, e.Message, match.Groups[1].Value);
                        }
                        matchCount++;
                    }
                    Success = true;
                    Data.Images = images.ToArray();
                }
            }
            catch (Exception ex)
            {
                OnException("", ex);
            }
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

        public bool Success { get; private set; }

        public ArtistInfoProviderData Data { get; private set; }
    }
}
