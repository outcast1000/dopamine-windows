using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Dopamine.Data.Providers
{
    public class MainArtistInfoProvider : IArtistInfoProvider
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        YoutubeArtistInfoProvider _ytaProvider;
        GoogleArtistInfoProvider _gaProvider;

        public MainArtistInfoProvider(IInternetDownloaderCreator internetDownloaderCreator)
        {
            RequestedImages = 1;
            _ytaProvider = new YoutubeArtistInfoProvider(internetDownloaderCreator);
            _gaProvider = new GoogleArtistInfoProvider(internetDownloaderCreator);
        }

        public int RequestedImages { get; set; }

        public ArtistInfoProviderData get(String artist)
        {
            ArtistInfoProviderData data = _ytaProvider.get(artist);
            if (data.Images?.Length == 0)
            {
                ArtistInfoProviderData gaData = _gaProvider.get(artist);
                if (gaData.Images?.Length > 0)
                {
                    data.Images = gaData.Images;
                    data.result = InfoProviderResult.Success;
                }
            }
            return data;
        }

        public string ProviderName
        {
            get { return "MAIN_ARTIST_PROVIDER"; }
        }

    }
}
