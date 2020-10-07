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
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        IInternetDownloaderCreator _internetDownloaderCreator;

        public GoogleArtistInfoProvider(IInternetDownloaderCreator internetDownloaderCreator)
        {
            RequestedImages = 1;
            _internetDownloaderCreator = internetDownloaderCreator;
        }

        public int RequestedImages { get; set; }

        public ArtistInfoProviderData get(String artist)
        {
            ArtistInfoProviderData data = new ArtistInfoProviderData() { result = InfoProviderResult.Success };
            if (string.IsNullOrEmpty(artist))
            {
                Debug.Print("Missing artist name");
                data.result = InfoProviderResult.Fail_Generic;
                return data;
            }

            try
            {
                using (var client = _internetDownloaderCreator.create())
                {
                    Uri uri = new Uri(string.Format("https://www.google.com/search?q={0}+live", System.Uri.EscapeDataString(artist)));
                    string result = client.DownloadString(uri);
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
                            byte[] imageData = Convert.FromBase64String(base64);
                            images.Add(imageData);
                        }
                        catch (Exception e)
                        {
                            Debug.Print("Exception for {0}: {1}, :{2}", matchCount, e.Message, match.Groups[1].Value);
                        }
                        matchCount++;
                        if (matchCount >= RequestedImages)
                            break;
                    }
                    data.Images = images.Select(x => new OriginatedData<Byte[]>() { Data = x, Origin = ProviderName }).ToArray();
                }
            }
            catch (Exception ex)
            {
                OnException("", ex);
                data.result = InfoProviderResult.Fail_InternetFailed;
            }
            return data;
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
            get { return "GOOGLE_IMAGES_ARTISTS"; }
        }

    }
}
