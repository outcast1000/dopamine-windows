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
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        IInternetDownloaderCreator _internetDownloaderCreator;
        public LastFMArtistInfoProvider()
        {
        }
		
		public ArtistInfoProviderData get(String artist)
		{
            ArtistInfoProviderData data = new ArtistInfoProviderData() { result = InfoProviderResult.Success };
            if (string.IsNullOrEmpty(artist))
            {
                Debug.Print("LastFMArtistImageProvider. Missing artist name");
                data.result = InfoProviderResult.Fail_Generic;
                return data;
            }
            try
            {
                LastFmArtist lf = LastfmApi.ArtistGetInfo(artist, false, "EN").Result;
                if (lf.Biography != null)
                    data.Biography = new OriginatedData<string>[] { new OriginatedData<string>() { Data = lf.Biography.Content, Origin = ProviderName } };
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
            get { return "LAST_FM_ARTISTS"; }
        }

    }
}
