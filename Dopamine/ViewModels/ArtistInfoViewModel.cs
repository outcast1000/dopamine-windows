using Digimezzo.Foundation.Core.IO;
using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Api.Fanart;
using Dopamine.Core.Api.Lastfm;
using Dopamine.Services.Cache;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dopamine.ViewModels
{
    public class ArtistInfoViewModel : BindableBase
    {
        private ObservableCollection<SimilarArtistViewModel> similarArtists;
        private string _image;
        private string _biography;
        private string _artistName;

        public DelegateCommand<string> OpenLinkCommand { get; set; }

        public bool HasBiography
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_biography);
            }
        }

        public bool HasSimilarArtists
        {
            get { return this.SimilarArtists != null && this.SimilarArtists.Count > 0; }
        }

        public bool HasImage
        {
            get { return !string.IsNullOrEmpty(this._image); }
        }

        public ObservableCollection<SimilarArtistViewModel> SimilarArtists
        {
            get { return this.similarArtists; }
            set { SetProperty<ObservableCollection<SimilarArtistViewModel>>(ref this.similarArtists, value); }
        }

        /*
        public async Task SetArtistInformation(LastFmArtist lfmArtist, string artistImageUrl)
        {
            this.lfmArtist = lfmArtist;

            RaisePropertyChanged(nameof(this.ArtistName));
            RaisePropertyChanged(nameof(this.Biography));
            RaisePropertyChanged(nameof(this.HasBiography));
            RaisePropertyChanged(nameof(this.CleanedBiographyContent));
            RaisePropertyChanged(nameof(this.Url));
            RaisePropertyChanged(nameof(this.UrlText));

            await this.FillSimilarArtistsAsync();
            await this.FillImageAsync(artistImageUrl);
        }
        */

        public string ArtistImage
        {
            get { return this._image; }
            set { SetProperty<string>(ref this._image, value); }
        }

        public string ArtistName
        {
            get { return this._artistName; }
            set { SetProperty<string>(ref this._artistName, value); }
        }

        /*
        public string Url
        {
            get
            {
                if (this.lfmArtist == null) return string.Empty;

                return this.lfmArtist.Url;
            }
        }

        public string UrlText
        {
            get
            {
                if (this.Biography == null) return string.Empty;

                Regex regex = new Regex(@"(>.*<\/a>)");
                Match match = regex.Match(this.Biography.Content);

                if (match.Success)
                {
                    return match.Groups[0].Value.Replace("</a>", "").Replace(">", "");
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        */

        public string Biography
        {
            get { return this._biography; }
            set 
            {
                string cleanedBiography = string.IsNullOrEmpty(value) ? string.Empty : Regex.Replace(value, @"(<a.*$)", "").Trim();
                SetProperty<string>(ref this._biography, cleanedBiography); }
        }

        public ArtistInfoViewModel()
        {
            this.OpenLinkCommand = new DelegateCommand<string>((url) =>
            {
                try
                {
                    Actions.TryOpenLink(url);
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not open link {0}. Exception: {1}", url, ex.Message);
                }
            });
        }
        
        /*
        private async Task FillSimilarArtistsAsync()
        {
            if (this.lfmArtist != null && this.lfmArtist.SimilarArtists != null && this.lfmArtist.SimilarArtists.Count > 0)
            {
                await Task.Run(async () =>
                {
                    var localSimilarArtists = new ObservableCollection<SimilarArtistViewModel>();

                    foreach (LastFmArtist similarArtist in this.lfmArtist.SimilarArtists)
                    {
                        string artistImageUrl = string.Empty;

                        try
                        {
                            // Last.fm was so nice to break their artist image API. So we need to get images from elsewhere.  
                            LastFmArtist lfmArtist = await LastfmApi.ArtistGetInfo(similarArtist.Name, true, ResourceUtils.GetString("Language_ISO639-1"));
                            artistImageUrl = await FanartApi.GetArtistThumbnailAsync(lfmArtist.MusicBrainzId);
                        }
                        catch (Exception ex)
                        {
                            LogClient.Warning($"Could not get artist image from Fanart for artist {similarArtist.Name}. Exception: {ex}");
                        }

                        localSimilarArtists.Add(new SimilarArtistViewModel { Name = similarArtist.Name, Url = similarArtist.Url, ImageUrl = artistImageUrl });
                    }

                    this.SimilarArtists = localSimilarArtists;
                });
            }

            RaisePropertyChanged(nameof(this.SimilarArtists));
            RaisePropertyChanged(nameof(this.HasSimilarArtists));
        }
        */

    }
}
