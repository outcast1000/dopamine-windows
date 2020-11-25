using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Core.Api.Lyrics
{
    public class LyricsFactory
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IList<ILyricsApi> lyricsApis;
        private readonly IList<ILyricsApi> lyricsApisPipe;

        public LyricsFactory(int timeoutSeconds, string providers, ILocalizationInfo info)
        {
            lyricsApis = new List<ILyricsApi>();
            lyricsApisPipe = new List<ILyricsApi>();

            if (providers.ToLower().Contains("chartlyrics")) lyricsApis.Add(new ChartLyricsApi(timeoutSeconds));
            if (providers.ToLower().Contains("lololyrics")) lyricsApis.Add(new LololyricsApi(timeoutSeconds));
            if (providers.ToLower().Contains("lyricwikia")) lyricsApis.Add(new LyricWikiaApi(timeoutSeconds));
            if (providers.ToLower().Contains("metrolyrics")) lyricsApis.Add(new MetroLyricsApi(timeoutSeconds));
            if (providers.ToLower().Contains("xiamilyrics")) lyricsApis.Add(new XiamiLyricsApi(timeoutSeconds, info));
            if (providers.ToLower().Contains("neteaselyrics")) lyricsApis.Add(new NeteaseLyricsApi(timeoutSeconds, info));
        }

        public async Task<Lyrics> GetLyricsAsync(string artist, string title)
        {
            Logger.Info($"GetLyricsAsync {artist} - {title}");
            foreach (var item in lyricsApis)
                lyricsApisPipe.Add(item);
            var api = this.GetRandomApi();
            while (api != null)
            {
                try
                {
                    Logger.Info($"GetLyricsAsync. Trying '{api}'");
                    string lyricsText = await api.GetLyricsAsync(artist, title);
                    if (!string.IsNullOrEmpty(lyricsText))
                    {
                        Logger.Info($"GetLyricsAsync. Lyrics Found on '{api}'");
                        return new Lyrics(lyricsText, api.SourceName);
                    }
                }
                catch (Exception ex)
                {
                    // No need to fill the log with known parse errors
                    Logger.Info($"GetLyricsAsync: Exception on '{api}' ('{ex.Message}')");
                    //Logger.Error(ex, $"GetLyricsAsync: Exception on '{api}' ('{ex.Message}')");
                }

                api = this.GetRandomApi();
            }
            Logger.Info($"GetLyricsAsync NOT FOUND {artist} - {title}");
            return null;
        }

        private ILyricsApi GetRandomApi()
        {
            ILyricsApi api = null;

            if (lyricsApisPipe.Count > 0)
            {
                var rnd = new Random();
                int index = rnd.Next(lyricsApisPipe.Count);
                api = lyricsApisPipe[index];
                lyricsApisPipe.RemoveAt(index);
            }

            return api;
        }
    }
}
