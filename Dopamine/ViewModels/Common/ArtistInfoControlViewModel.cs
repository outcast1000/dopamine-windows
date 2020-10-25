using Digimezzo.Foundation.Core.IO;
using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Alex;  //Digimezzo.Foundation.Core.Settings
using Digimezzo.Foundation.Core.Utils;
using Digimezzo.Foundation.WPF.Controls;
using Dopamine.Core.Api.Fanart;
using Dopamine.Core.Api.Lastfm;
using Dopamine.Services.Entities;
using Dopamine.Services.I18n;
using Dopamine.Services.Playback;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using Dopamine.Data.Providers;
using Dopamine.Data.Repositories;
using System.Collections;
using System.Windows.Documents;
using System.Collections.Generic;
using Dopamine.Data.Entities;
using Dopamine.Services.Indexing;

namespace Dopamine.ViewModels.Common
{
    public class ArtistInfoControlViewModel : BindableBase
    {
        private IContainerProvider container;
        private ArtistInfoViewModel artistInfoViewModel;
        private IPlaybackService playbackService;
        private II18nService i18nService;
        private string previousArtistName;
        private string artistName;
        private SlideDirection slideDirection;
        private bool isBusy;
        private IArtistVRepository _artistVRepository;
        private IInfoRepository _infoRepository;

        public DelegateCommand<string> OpenLinkCommand { get; set; }

        public SlideDirection SlideDirection
        {
            get { return this.slideDirection; }
            set { SetProperty<SlideDirection>(ref this.slideDirection, value); }
        }

        public ArtistInfoViewModel ArtistInfoViewModel
        {
            get { return this.artistInfoViewModel; }
            set { SetProperty<ArtistInfoViewModel>(ref this.artistInfoViewModel, value); }
        }

        public bool IsBusy
        {
            get { return this.isBusy; }
            set { SetProperty<bool>(ref this.isBusy, value); }
        }

        public ArtistInfoControlViewModel(IContainerProvider container, IPlaybackService playbackService, II18nService i18nService)
        {
            this.container = container;
            this.playbackService = playbackService;
            this.i18nService = i18nService;
            _artistVRepository = container.Resolve<IArtistVRepository>();
            _infoRepository = container.Resolve<IInfoRepository>();

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

            this.playbackService.PlaybackSuccess += async (_, e) =>
            {
                this.SlideDirection = e.IsPlayingPreviousTrack ? SlideDirection.RightToLeft : SlideDirection.LeftToRight;
                await this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, true);
            };

            this.i18nService.LanguageChanged += async (_, __) =>
            {
                if (this.playbackService.HasCurrentTrack) await this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, true);
            };

            // Defaults
            this.SlideDirection = SlideDirection.LeftToRight;
            this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, true);
        }

        private async Task ShowArtistInfoAsync(TrackViewModel track, bool forceReload)
        {
            this.previousArtistName = this.artistName;

            // User doesn't want to download artist info, or no track is selected.
            if (!SettingsClient.Get<bool>("Lastfm", "DownloadArtistInformation") || track == null)
            {
                this.ArtistInfoViewModel = this.container.Resolve<ArtistInfoViewModel>();
                this.artistName = string.Empty;
                return;
            }
            await Task.Run(() =>
            {
                List<ArtistV> artists = _artistVRepository.GetArtistsOfTrack(track.Id);

                // Artist name is unknown
                if (artists.Count == 0)
                {
                    //ArtistInfoViewModel localArtistInfoViewModel = this.container.Resolve<ArtistInfoViewModel>();
                    //await localArtistInfoViewModel.SetArtistInformation(new LastFmArtist { Name = string.Empty }, string.Empty);
                    this.ArtistInfoViewModel = this.container.Resolve<ArtistInfoViewModel>();
                    this.artistName = string.Empty;
                    return;
                }
                ArtistV mainArtist = artists[0]; // ALEX TODO. Arbitrary selection. You may improve it by showing all
                this.artistName = mainArtist.Name;

                // The artist didn't change: leave the previous artist info.
                if (this.artistName.Equals(this.previousArtistName) & !forceReload)
                {
                    return;
                }

                // The artist changed: we need to show new artist info.
                string artworkPath = string.Empty;

                this.ArtistInfoViewModel = this.container.Resolve<ArtistInfoViewModel>();
                RefreshInfo(mainArtist);
                if (string.IsNullOrEmpty(ArtistInfoViewModel.ArtistImage) && string.IsNullOrEmpty(ArtistInfoViewModel.Biography))
                {
                    this.IsBusy = true;
                    IIndexingService indexingService = container.Resolve<IIndexingService>();
                    indexingService.ArtistInfoDownloaded += IndexingService_ArtistInfoDownloaded;
                    indexingService.RequestArtistInfoAsync(mainArtist, false, false);
                }

            });
        }

        private void IndexingService_ArtistInfoDownloaded(ArtistV requestedArtist, bool success)
        {
            RefreshInfo(requestedArtist);
            this.IsBusy = false;
        }

        private void RefreshInfo(ArtistV artist)
        {
            ArtistBiography artistBiography = _infoRepository.GetArtistBiography(artist.Id);
            this.ArtistInfoViewModel.ArtistName = artist.Name;
            this.ArtistInfoViewModel.ArtistImage = artist.Thumbnail;
            this.ArtistInfoViewModel.Biography = artistBiography?.Biography;
        }
    }
}
