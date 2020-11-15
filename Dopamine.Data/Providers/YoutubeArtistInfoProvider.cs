using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Dopamine.Data.Providers
{
    public class YoutubeArtistInfoProvider : IArtistInfoProvider
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        IInternetDownloaderCreator _internetDownloaderCreator;

        public YoutubeArtistInfoProvider(IInternetDownloaderCreator internetDownloaderCreator)
        {
            RequestedImages = 1;
            _internetDownloaderCreator = internetDownloaderCreator;
        }

        public int RequestedImages { get; set; }

        private string GetChannel_v1(IInternetDownloader internetDownloader, string artist)
        {
            Uri uri = new Uri(string.Format("https://music.youtube.com/search?q={0}", System.Uri.EscapeDataString(artist)));
            string result = internetDownloader.DownloadString(uri);
            if (result?.Length <= 0)
            {
                Logger.Warn($"Download search page failed. Artist: '{artist}' URL: {uri.AbsolutePath}. Exiting...");
                return null;
            }
            var regex = new Regex("{\\\\\"browseId\\\\\":\\\\\"(.*?)\\\\\",\\\\\"browseEndpointContextSupportedConfigs\\\\\":{\\\\\"browseEndpointContextMusicConfig\\\\\":{\\\\\"pageType\\\\\":\\\\\"MUSIC_PAGE_TYPE_ARTIST");
            MatchCollection matches = regex.Matches(result);
            if (matches.Count == 0)
            {
                Logger.Warn($"Find search page failed for channel page. Artist: '{artist}'. URL: {uri.AbsolutePath}. Exiting...");
                return null;
            }
            return matches[0].Groups[1].Value;
        }

        private string GetChannel_v2(IInternetDownloader internetDownloader, string artist)
        {
            Uri uri = new Uri(string.Format("https://www.youtube.com/results?search_query={0}", System.Uri.EscapeDataString(artist)));
            string result = internetDownloader.DownloadString(uri);
            if (result?.Length <= 0)
            {
                Logger.Warn($"GetChannel_v2: Download page failed. Artist: '{artist}' URL: {uri.AbsolutePath}");
                return null;
            }
            //=== universalWatchCardRenderer.*?channel\/(.*?)"
            var regex = new Regex("universalWatchCardRenderer.*?channel\\/(.*?)\"");
            MatchCollection matches = regex.Matches(result);
            if (matches.Count == 0)
            {
                Logger.Warn($"GetChannel_v2: Find channel failed. Artist: '{artist}'. URL: {uri.AbsolutePath}");
                return null;
            }
            return matches[0].Groups[1].Value;
        }

        public ArtistInfoProviderData Get(String artist)
        {
            ArtistInfoProviderData data = new ArtistInfoProviderData() { result = InfoProviderResult.Success };
            if (string.IsNullOrEmpty(artist))
            {
                Debug.Print("YoutubeArtistInfoProvider. Missing artist name");
                data.result = InfoProviderResult.Fail_Generic;
                return data;
            }
            try
            {
                using (var client = _internetDownloaderCreator.create())
                {
                    // Download the search page. Find the channel page
                    string channel = null;
                    channel = GetChannel_v1(client, artist);
                    if (channel == null)
                        return data;
                    // Download the channel page
                    Uri uri = new Uri(string.Format("https://music.youtube.com/channel/{0}", channel));
                    string result = client.DownloadString(uri);
                    if (result?.Length <= 0)
                    {
                        Logger.Warn($"Download channel page failed. Artist: '{artist}' URL: {uri.AbsolutePath}. Exiting...");
                        data.result = InfoProviderResult.Fail_InternetFailed;
                        return data;
                    }
                    // Find the Bio: description\\\":{\\\"runs\\\":\[{\\\"text\\\":\\\"(.*?)\\\"}\]}
                    Regex regex = new Regex("description\\\\\\\":{\\\\\\\"runs\\\\\\\":\\[{\\\\\\\"text\\\\\\\":\\\\\\\"(.*?)\\\\\\\"}\\]}");
                    MatchCollection matches = regex.Matches(result);
                    if (matches.Count > 0)
                    {
                        // May have escape chars like  \\\"futuristic \/
                        string bio = matches[0].Groups[1].Value;
                        bio = bio.Replace(@"\\\""", "\"");
                        bio = bio.Replace(@"\\n", "\n");
                        bio = bio.Replace(@"\/", "/");
                        bio = bio.Replace(@"\\u0026", "&");
                        data.Biography = new OriginatedData<string>() { Data = bio, Origin = ProviderName };
                    }
                    else
                        Logger.Info($"Bio not found. Artist: '{artist}'. URL: {uri.AbsolutePath}");
                    // Find the images
                    List<byte[]> images = new List<byte[]>();
                    regex = new Regex("thumbnail\\\\\\\":{\\\\\\\"thumbnails\\\\\\\":\\[{\\\\\\\"url\\\\\\\":\\\\\\\"(.*?)\\\\\\\",\\\\\\\"width\\\\\\\":(.*?),");
                    matches = regex.Matches(result);
                    foreach (Match match in matches)
                    {
                        int width = int.Parse(match.Groups[2].Value);
                        if (width < 540)
                        {
                            continue;
                        }
                        string url = match.Groups[1].Value.Replace("\\/", "/");
                        uri = new Uri(url);
                        byte[] bRes = client.DownloadData(uri);
                        if (bRes?.Length == 0)
                        {
                            Logger.Info($"Artist Image Download failed. Artist: '{artist}'. URL: {uri.AbsoluteUri}");
                            continue;
                        }
                        images.Add(bRes);
                        if (images.Count >= RequestedImages)
                            break;
                    }
                    
                    if (images.Count > 0)
                        data.Images = images.Select(x => new OriginatedData<Byte[]>() { Data = x, Origin = ProviderName }).ToArray();
                    else
                        Logger.Debug($"Artist Image not found. Artist: '{artist}'. Matches: {matches.Count}. URL: {uri.AbsoluteUri}");

                    //=== Find the tracks (5 most popular?)
                    // \[{\\"musicResponsiveListItemFlexColumnRenderer\\":{\\"text\\":{\\"runs\\":\[{\\"text\\":\\"(.*?)\\",
                    List<string> tracks = new List<string>();
                    regex = new Regex("\\[{\\\\\"musicResponsiveListItemFlexColumnRenderer\\\\\":{\\\\\"text\\\\\":{\\\\\"runs\\\\\":\\[{\\\\\"text\\\\\":\\\\\"(.*?)\\\\\",");
                    matches = regex.Matches(result);
                    foreach (Match match in matches)
                        tracks.Add(match.Groups[1].Value);
                    if (tracks.Count > 0)
                        data.Tracks = new OriginatedData<string[]>() { Data = tracks.ToArray(), Origin = ProviderName };
                    else
                        Logger.Debug($"Artist Tracks not found. Artist: '{artist}'. URL: {uri.AbsoluteUri}");

                    //=== ALEX TODO
                    // Find the Albums, Members, Genres

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
            get { return "GOOGLE_ARTISTS"; }
        }

    }
}
