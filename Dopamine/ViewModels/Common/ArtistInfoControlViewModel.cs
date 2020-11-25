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
        private double slideDuration;
        private bool isBusy;
        private IArtistVRepository _artistVRepository;
        private IInfoRepository _infoRepository;
        private IIndexingService _indexingService;
        private static readonly double _normalSlideDuration = .5;

        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand UnloadedCommand { get; set; }
        public DelegateCommand<string> OpenLinkCommand { get; set; }

        public SlideDirection SlideDirection
        {
            get { return this.slideDirection; }
            set { SetProperty<SlideDirection>(ref this.slideDirection, value); }
        }

        public double SlideDuration
        {
            get { return this.slideDuration; }
            set { SetProperty<double>(ref this.slideDuration, value); }
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

        private bool _alreadyLoaded = false;
        private void OnLoad()
        {
            if (_alreadyLoaded == true)
            {
                // This is a workaround. I do not know why we get Load Again.
                // The only different that I found in the call stack is that the "ogiginal" load comes form 'AnimatedRenderMessageHandler' and the later from 'RenderMessageHandler'
                Logger.Warn("ArtistInfoControlViewModel Reloaded (!). Applying workaround");
                return;
            }
            _alreadyLoaded = true;

            this.playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            this.i18nService.LanguageChanged += I18nService_LanguageChanged;
            _indexingService.ArtistInfoDownloaded += IndexingService_ArtistInfoDownloaded;
            ClearInfo();
            // Defaults
            this.SlideDirection = SlideDirection.DownToUp;
            SlideDuration = _normalSlideDuration;
            Task unAwaitedTask = this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, true);
        }

        private void OnUnload()
        {
            this.playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            this.i18nService.LanguageChanged -= I18nService_LanguageChanged;
            _indexingService.ArtistInfoDownloaded -= IndexingService_ArtistInfoDownloaded;
            ClearInfo();
            _alreadyLoaded = false;
        }

        private async void I18nService_LanguageChanged(object sender, EventArgs e)
        {
            if (this.playbackService.HasCurrentTrack) await this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, true);
        }


        private async void PlaybackService_PlaybackSuccess(object sender, PlaybackSuccessEventArgs e)
        {
            this.SlideDirection = e.IsPlayingPreviousTrack ? SlideDirection.UpToDown : SlideDirection.DownToUp;
            SlideDuration = _normalSlideDuration;
            await this.ShowArtistInfoAsync(this.playbackService.CurrentTrack, false);
        }

        private System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1);

        private async Task ShowArtistInfoAsync(TrackViewModel track, bool forceReload)
        {
            Logger.Info($"ShowArtistInfoAsync for {track?.ArtistName} - {track?.TrackTitle}, {forceReload}");
            await _semaphore.WaitAsync();
            try
            {
                Logger.Info($"ShowArtistInfoAsync (Inside Semaphore) {track?.ArtistName}");
                if (track == null)
                {
                    ClearInfo();
                    Logger.Info("ShowArtistInfoAsync (ClearInfo / EXIT) Null Track");
                    return;
                }
                if (string.IsNullOrEmpty(track.ArtistName))
                {
                    ClearInfo();
                    Logger.Info("ShowArtistInfoAsync (ClearInfo / EXIT) Empty Artist");
                    return;
                }
                if (track.ArtistName.Equals(this.artistName) && !forceReload)
                {
                    Logger.Info("ShowArtistInfoAsync (Keep Old / EXIT) Same artist && !forceReload");
                    return;
                }

                await Task.Run(async () =>
                {
                    List<ArtistV> artists = _artistVRepository.GetArtistsOfTrack(track.Id, QueryOptions.IncludeAll());
                    if (artists.Count == 0)
                    {
                        ClearInfo();
                        Logger.Warn("ShowArtistInfoAsync (Keep Old / EXIT) Artist Not Found(!)");
                        return;
                    }
                    // There is a new artist to be displayed
                    ArtistInfoViewModel vm = GetArtistInfoViewModel(artists[0]);
                    artistName = artists[0].Name;
                    IsBusy = false;
                    if (string.IsNullOrEmpty(vm.ArtistImage) || string.IsNullOrEmpty(vm.Biography))
                    {
                        if (SettingsClient.Get<bool>("Lastfm", "DownloadArtistInformation"))
                        {
                            Logger.Info("ShowArtistInfoAsync Requesting new Info");
                            if (await _indexingService.RequestArtistInfoAsync(artists[0], false, false))
                            {
                                this.IsBusy = true;
                            }
                            else
                            {
                                Logger.Warn("ShowArtistInfoAsync Request Failed");
                            }
                        }
                    }
                    Logger.Info("ShowArtistInfoAsync Showing what we have");
                    ArtistInfoViewModel = vm;

                });
            }
            finally
            {
                _semaphore.Release();
            }

        }

        private void IndexingService_ArtistInfoDownloaded(ArtistV requestedArtist, bool success)
        {
            Logger.Info($"IndexingService_ArtistInfoDownloaded Getting {requestedArtist.Name}");
            if (requestedArtist.Name.Equals(artistName))
            {
                Logger.Info($"IndexingService_ArtistInfoDownloaded Got our Artist");
                this.IsBusy = false;
                if (success)
                {
                    Logger.Info($"IndexingService_ArtistInfoDownloaded Success");
                    ArtistInfoViewModel vm = GetArtistInfoViewModel(requestedArtist);
                    if (vm.ArtistImage != null && !vm.ArtistImage.Equals(ArtistInfoViewModel.ArtistImage))
                    {
                        Logger.Info($"IndexingService_ArtistInfoDownloaded Upating Image");
                        SlideDuration = 0;
                        ArtistInfoViewModel = vm;
                    }
                    if (vm.Biography != null && !vm.Biography.Equals(ArtistInfoViewModel.Biography))
                    {
                        Logger.Info($"IndexingService_ArtistInfoDownloaded Upating Biography");
                        SlideDuration = 0;
                        ArtistInfoViewModel = vm;
                    }
                }
                else
                {
                    Logger.Warn($"IndexingService_ArtistInfoDownloaded Failure");
                }
            }
        }

        private ArtistInfoViewModel GetArtistInfoViewModel(ArtistV artist)
        {
            ArtistInfoViewModel vm = this.container.Resolve<ArtistInfoViewModel>();
            ArtistBiography artistBiography = _infoRepository.GetArtistBiography(artist.Id);
            vm.ArtistName = artist.Name;
            vm.ArtistImage = artist.Thumbnail;
            vm.Biography = artistBiography?.Biography;
            return vm;
        }

        private void RefreshInfo(ArtistV artist)
        {
            artistName = artist.Name;
            ArtistInfoViewModel vm = GetArtistInfoViewModel(artist);
            if (vm != ArtistInfoViewModel)
                ArtistInfoViewModel = vm;
            IsBusy = false;
        }

        private void ClearInfo()
        {
            artistName = String.Empty;
            ArtistInfoViewModel = this.container.Resolve<ArtistInfoViewModel>();
            IsBusy = false;
        }
    }
}
