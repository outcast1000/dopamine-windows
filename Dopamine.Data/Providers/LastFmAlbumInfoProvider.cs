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
    public class LastFMAlbumInfoProvider : IAlbumInfoProvider
    {
        IInternetDownloaderCreator _internetDownloaderCreator;
        public LastFMAlbumInfoProvider(IInternetDownloaderCreator internetDownloaderCreator)
        {
            _internetDownloaderCreator = internetDownloaderCreator;
        }




        public AlbumInfoProviderData Get(String album, string[] artists)
        {
            AlbumInfoProviderData data = new AlbumInfoProviderData() { result = InfoProviderResult.Success };

            if (string.IsNullOrEmpty(album) || artists == null)
            {
                Debug.Print("LastFmAlbumImageProvider. Missing album info");
                data.result = InfoProviderResult.Fail_Generic;
                return data;
            }

            foreach (string artist in artists)
            {
                LastFmAlbum lfmAlbum = LastfmApi.AlbumGetInfo(artist, album, false, "EN").Result;

                if (!string.IsNullOrEmpty(lfmAlbum.LargestImage()))
                {
                    try
                    {
                        var uri = new Uri(lfmAlbum.LargestImage());
                        using (var client = _internetDownloaderCreator.create())
                        {
                            data.Images = new OriginatedData<byte[]>[] { new OriginatedData<byte[]>() { Data = client.DownloadData(uri), Origin = ProviderName } };
                            data.result = InfoProviderResult.Success;
                            return data;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("Could not download file to temporary cache. Exception: {0}", ex.Message);
                        data.result = InfoProviderResult.Fail_InternetFailed;
                        return data;
                    }
                }
            }
            data.result = InfoProviderResult.Fail_Generic;
            return data;
        }
        public string ProviderName
        {
            get { return "LAST_FM_ALBUMS"; }
        }
    }
}
