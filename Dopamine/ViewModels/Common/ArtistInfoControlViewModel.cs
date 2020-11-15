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
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IContainerProvider container;
        private ArtistInfoViewModel artistInfoViewModel;
        private IPlaybackService playbackService;
        private II18nService i18nService;
        private string artistName;
        private SlideDirection slideDirection;
        private bool isBusy;
        private IArtistVRepository _artistVRepository;
        private IInfoRepository _infoRepository;
        private IIndexingService _indexingService;
        private TrackViewModel _currentTrack = null;

        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand UnloadedCommand { get; set; }
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
            _indexingService = container.Resolve<IIndexingService>();


            LoadedCommand = new DelegateCommand(() => { OnLoad(); });
            UnloadedCommand = new DelegateCommand(() => { OnUnload(); });

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

        private void OnLoad()
        {
            this.playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            this.i18nService.LanguageChanged += I18nService_LanguageChanged;
            _indexingService.ArtistInfoDownloaded += IndexingService_ArtistInfoDownloaded;
            _currentTrack = null;

            // Defaults
            this.SlideDirection = SlideDirection.LeftToRight;
            Task unAwaitedTask = this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, true);
        }

        private void OnUnload()
        {
            this.playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            this.i18nService.LanguageChanged -= I18nService_LanguageChanged;
            _indexingService.ArtistInfoDownloaded -= IndexingService_ArtistInfoDownloaded;
            _currentTrack = null;
        }

        private async void I18nService_LanguageChanged(object sender, EventArgs e)
        {
            if (this.playbackService.HasCurrentTrack) await this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, false);
        }


        private async void PlaybackService_PlaybackSuccess(object sender, PlaybackSuccessEventArgs e)
        {
            this.SlideDirection = e.IsPlayingPreviousTrack ? SlideDirection.RightToLeft : SlideDirection.LeftToRight;
            await this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, false);
        }

        private System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1);

        private async Task ShowArtistInfoAsync(TrackViewModel track, bool forceReload)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (track == null)
                {
                    Logger.Info("ShowArtistInfoAsync (EXIT) Null Track");
                    return;
                }
                if (track.Id == _currentTrack?.Id)
                {
                    Logger.Info("ShowArtistInfoAsync (EXIT) Same Track");
                    return;
                }
                _currentTrack = track;
                if (string.IsNullOrEmpty(track.ArtistName))
                {
                    Logger.Info("ShowArtistInfoAsync (EXIT) Empty Artist");
                    return;
                }
                if (track.ArtistName.Equals(this.artistName) && !forceReload)
                {
                    Logger.Info("ShowArtistInfoAsync (EXIT) Same artist && !forceReload");
                    return;
                }

                await Task.Run(async () =>
                {
                    List<ArtistV> artists = _artistVRepository.GetArtistsOfTrack(track.Id);

                    if (artists.Count == 0)
                    {
                        this.ArtistInfoViewModel = this.container.Resolve<ArtistInfoViewModel>();
                        this.artistName = string.Empty;
                        return;
                    }
                    ArtistV mainArtist = artists[0]; // ALEX TODO. Arbitrary selection. You may improve it by showing all
                    this.artistName = mainArtist.Name;

                    // The artist changed: we need to show new artist info.
                    string artworkPath = string.Empty;

                    RefreshInfo(mainArtist);
                    if (string.IsNullOrEmpty(ArtistInfoViewModel.ArtistImage) || string.IsNullOrEmpty(ArtistInfoViewModel.Biography))
                    {
                        if (SettingsClient.Get<bool>("Lastfm", "DownloadArtistInformation"))
                        {
                            if (await _indexingService.RequestArtistInfoAsync(mainArtist, true, true))
                                this.IsBusy = true;
                        }
                    }

                });
            }
            finally
            {
                _semaphore.Release();
            }

        }

        private void IndexingService_ArtistInfoDownloaded(ArtistV requestedArtist, bool success)
        {
            if (requestedArtist.Name.Equals(artistName))
            {
                if (success)
                    RefreshInfo(requestedArtist);
            }
            this.IsBusy = false;
        }

        private void RefreshInfo(ArtistV artist)
        {
            ArtistInfoViewModel vm = this.container.Resolve<ArtistInfoViewModel>();
            ArtistBiography artistBiography = _infoRepository.GetArtistBiography(artist.Id);
            vm.ArtistName = artist.Name;
            vm.ArtistImage = artist.Thumbnail;
            vm.Biography = artistBiography?.Biography;
            if (vm != ArtistInfoViewModel)
                ArtistInfoViewModel = vm;
            IsBusy = false;
        }
    }
}
