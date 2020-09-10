using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public interface IInternetDownloader: IDisposable
    {
        Byte[] DownloadData(Uri uri);
        string DownloadString(Uri uri);
    }

    public class HttpClientInternetDownloader: IInternetDownloader
    {
        public HttpClientInternetDownloader()
        {
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.83 Safari/537.36");
            HttpClient.DefaultRequestHeaders.ExpectContinue = false;
        }

        public HttpClient HttpClient { get; private set;}

        public void Dispose()
        {
            HttpClient.Dispose();
        }

        public Byte[] DownloadData(Uri uri)
        {
            var response = HttpClient.GetAsync(uri).Result;
            return response.Content.ReadAsByteArrayAsync().Result;
        }
        public string DownloadString(Uri uri)
        {
            var response = HttpClient.GetAsync(uri).Result;
            return response.Content.ReadAsStringAsync().Result;
        }
    }

    public interface IInternetDownloaderCreator
    {
        IInternetDownloader create();
    }

    public class DefaultInternetDownloaderCreator: IInternetDownloaderCreator
    {
        public IInternetDownloader create()
        {
            return new HttpClientInternetDownloader();
        }
    }

    public class InfoProviderCommon
    {

    }


}
