using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Dopamine.Data.Providers
{
    public class YoutubeArtistInfoProvider : IArtistInfoProvider
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public YoutubeArtistInfoProvider(String artist, IInternetDownloaderCreator internetDownloaderCreator = null)
        {
            RequestedImages = 1;
            Success = Init(artist, internetDownloaderCreator ?? new DefaultInternetDownloaderCreator());
        }

        public int RequestedImages { get; set; }

        private bool Init(String artist, IInternetDownloaderCreator internetDownloaderCreator)
        {
            if (string.IsNullOrEmpty(artist))
            {
                Debug.Print("YoutubeArtistInfoProvider. Missing artist name");
                return false;
            }
            Data = new ArtistInfoProviderData();
            Uri uri = new Uri(string.Format("https://music.youtube.com/search?q={0}", System.Uri.EscapeDataString(artist)));

            try
            {

                using (var client = internetDownloaderCreator.create())
                {
                    // Get the search page
                    string result = client.DownloadString(uri);
                    if (result?.Length <= 0)
                    {
                        Logger.Warn($"Download search page failed. Artist: '{artist}' URL: {uri.AbsolutePath}. Exiting...");
                        return false;
                    }
                    // Find the channel page
                    var regex = new Regex("{\\\\\"browseId\\\\\":\\\\\"(.*?)\\\\\",\\\\\"browseEndpointContextSupportedConfigs\\\\\":{\\\\\"browseEndpointContextMusicConfig\\\\\":{\\\\\"pageType\\\\\":\\\\\"MUSIC_PAGE_TYPE_ARTIST");
                    MatchCollection matches = regex.Matches(result);
                    if (matches.Count == 0)
                    {
                        Logger.Warn($"Find search page failed for channel page. Artist: '{artist}'. URL: {uri.AbsolutePath}. Exiting...");
                        return false;
                    }
                    string channel = matches[0].Groups[1].Value;
                    // Download the channel page
                    uri = new Uri(string.Format("https://music.youtube.com/channel/{0}", channel));
                    result = client.DownloadString(uri);
                    if (result?.Length <= 0)
                    {
                        Logger.Warn($"Download channel page failed. Artist: '{artist}' URL: {uri.AbsolutePath}. Exiting...");
                        return false;
                    }

                    // Find the Bio
                    regex = new Regex("description\\\\\\\":{\\\\\\\"runs\\\\\\\":\\[{\\\\\\\"text\\\\\\\":\\\\\\\"(.*?)\\\\\\\"}\\]}");
                    matches = regex.Matches(result);
                    if (matches.Count > 0)
                    {
                        Data.Bio = matches[0].Groups[1].Value;
                    }
                    else
                    {
                        Logger.Info($"Bio not found. Artist: '{artist}'. URL: {uri.AbsolutePath}");
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
                            continue;
                        }
                        string url = match.Groups[1].Value.Replace("\\/", "/");
                        uri = new Uri(url);
                        byte[] bRes = client.DownloadData(uri);
                        if (bRes?.Length == 0)
                        {
                            Logger.Info($"Artist Image Download failed. Artist: '{artist}'. URL: {uri.AbsolutePath}");
                            continue;
                        }
                        images.Add(bRes);
                        if (images.Count >= RequestedImages)
                            break;
                    }
                    Data.Images = images.ToArray();
                    if (Data.Images.Length == 0)
                    {
                        Logger.Info($"Artist Image not found. Artist: '{artist}'. Matches: {matches.Count}. URL: {uri.AbsolutePath}");
                    }

                    //=== Find the tracks (5 most popular?)
                    // \[{\\"musicResponsiveListItemFlexColumnRenderer\\":{\\"text\\":{\\"runs\\":\[{\\"text\\":\\"(.*?)\\",
                    List<string> tracks = new List<string>();
                    regex = new Regex("\\[{\\\\\"musicResponsiveListItemFlexColumnRenderer\\\\\":{\\\\\"text\\\\\":{\\\\\"runs\\\\\":\\[{\\\\\"text\\\\\":\\\\\"(.*?)\\\\\",");
                    matches = regex.Matches(result);
                    foreach (Match match in matches)
                    {
                        tracks.Add(match.Groups[1].Value);
                    }
                    Data.Tracks = tracks.ToArray();
                    if (Data.Images.Length == 0)
                    {
                        Logger.Info($"Artist Tracks not found. Artist: '{artist}'. URL: {uri.AbsolutePath}");
                    }

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

        public ArtistInfoProviderData Data
        {
            get; private set;
        }

        public bool Success { get; private set; }
    }
}
