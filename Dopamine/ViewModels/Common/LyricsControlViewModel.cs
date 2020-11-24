using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Alex; //Digimezzo.Foundation.Core.Settings
using Dopamine.Core.Api.Lyrics;
using Dopamine.Core.Base;
using Dopamine.Core.Enums;
using Dopamine.Core.Helpers;
using Dopamine.Core.Prism;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Services.I18n;
using Dopamine.Services.Metadata;
using Dopamine.Services.Playback;
using Dopamine.ViewModels.Common.Base;
using Prism.Commands;
using Prism.Events;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Prism.Ioc;
using Dopamine.Services.Entities;
using Dopamine.Data.Repositories;
using Dopamine.Views.Common.Base;
using Dopamine.Services.Provider;

namespace Dopamine.ViewModels.Common
{
    public class LyricsControlViewModel : ContextMenuViewModelBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IContainerProvider container;
        private ILocalizationInfo info;
        private IMetadataService metadataService;
        private IPlaybackService playbackService;
        private II18nService i18NService;
        private IInfoRepository infoRepository;
        private LyricsViewModel lyricsViewModel;
        private TrackViewModel previousTrack;
        private int contentSlideInFrom;
        private Timer highlightTimer = new Timer();
        private int highlightTimerIntervalMilliseconds = 100;
        private IEventAggregator eventAggregator;
        private Object lockObject = new Object();
        private Timer updateLyricsAfterEditingTimer = new Timer();
        private int updateLyricsAfterEditingTimerIntervalMilliseconds = 100;
        private bool isDownloadingLyrics;
        private bool canHighlight;
        private Timer refreshTimer = new Timer();
        private int refreshTimerIntervalMilliseconds = 500;
        //private bool isNowPlayingPageActive;
        //private bool isNowPlayingLyricsPageActive;
        private LyricsFactory lyricsFactory;

        public DelegateCommand RefreshLyricsCommand { get; set; }

        public int ContentSlideInFrom
        {
            get { return this.contentSlideInFrom; }
            set { SetProperty<int>(ref this.contentSlideInFrom, value); }
        }

        public LyricsViewModel LyricsViewModel
        {
            get { return this.lyricsViewModel; }
            set { SetProperty<LyricsViewModel>(ref this.lyricsViewModel, value); }
        }

        public bool IsDownloadingLyrics
        {
            get { return this.isDownloadingLyrics; }
            set
            {
                SetProperty<bool>(ref this.isDownloadingLyrics, value);
                this.RefreshLyricsCommand.RaiseCanExecuteChanged();
            }
        }

        public LyricsControlViewModel(IContainerProvider container) : base(container)
        {
            this.container = container;
            this.info = container.Resolve<ILocalizationInfo>();
            this.metadataService = container.Resolve<IMetadataService>();
            this.playbackService = container.Resolve<IPlaybackService>();
            this.eventAggregator = container.Resolve<IEventAggregator>();
            this.i18NService = container.Resolve<II18nService>();
            this.infoRepository = container.Resolve<IInfoRepository>();

            /*
            this.eventAggregator.GetEvent<IsNowPlayingSubPageChanged>().Subscribe(tuple =>
            {
                this.isNowPlayingLyricsPageActive = tuple.Item2 == NowPlayingSubPage.Lyrics;
                this.RestartRefreshTimer();
            });
            */

            this.RefreshLyricsCommand = new DelegateCommand(async () => {
                TrackViewModel track = playbackService.CurrentTrack;
                if (track == null)
                    return;
                Logger.Debug($"RefreshLyricsCommand. Lyrics downloaded TrackID: {track.Id}");
                TrackLyrics trackLyrics = await DownloadLyricsFromInternet(track);
                if (trackLyrics != null)
                    ApplyLyrics(track, trackLyrics, true, false);

            }, () => !this.IsDownloadingLyrics && playbackService.CurrentTrack != null);

            ApplicationCommands.RefreshLyricsCommand.RegisterCommand(this.RefreshLyricsCommand);

        }

        protected override void OnLoad()
        {
            base.OnLoad();
            this.highlightTimer.Interval = this.highlightTimerIntervalMilliseconds;
            this.highlightTimer.Elapsed += HighlightTimer_Elapsed;

            this.updateLyricsAfterEditingTimer.Interval = this.updateLyricsAfterEditingTimerIntervalMilliseconds;
            this.updateLyricsAfterEditingTimer.Elapsed += UpdateLyricsAfterEditingTimer_Elapsed;

            this.refreshTimer.Interval = this.refreshTimerIntervalMilliseconds;
            this.refreshTimer.Elapsed += RefreshTimer_Elapsed;

            this.playbackService.PlaybackPaused += PlaybackService_PlaybackPaused;
            this.playbackService.PlaybackResumed += PlaybackService_PlaybackResumed;
            this.playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;

            this.metadataService.MetadataChanged += MetadataService_MetadataChanged;


            I18NService_LanguageChanged(null, null);
            this.i18NService.LanguageChanged += I18NService_LanguageChanged;

            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += SettingsClient_SettingChanged;

            this.ClearLyrics(null); // Makes sure the loading animation can be shown even at first start

            this.RestartRefreshTimer();
        }

        protected override void OnUnLoad()
        {
            this.highlightTimer.Elapsed -= HighlightTimer_Elapsed;
            this.updateLyricsAfterEditingTimer.Elapsed -= UpdateLyricsAfterEditingTimer_Elapsed;
            this.refreshTimer.Elapsed -= RefreshTimer_Elapsed;

            this.playbackService.PlaybackPaused -= PlaybackService_PlaybackPaused;
            this.playbackService.PlaybackResumed -= PlaybackService_PlaybackResumed;
            this.playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;

            this.metadataService.MetadataChanged -= MetadataService_MetadataChanged;

            this.i18NService.LanguageChanged -= I18NService_LanguageChanged;

            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged -= SettingsClient_SettingChanged;

            _lastTrackIdOnRefreshLyrics = 0;

            base.OnUnLoad();

        }

        private void PlaybackService_PlaybackSuccess(object sender, PlaybackSuccessEventArgs e)
        {
            this.ContentSlideInFrom = e.IsPlayingPreviousTrack ? -30 : 30;
            this.RestartRefreshTimer();
        }

        private void SettingsClient_SettingChanged(object sender, Digimezzo.Foundation.Core.Settings.SettingChangedEventArgs e)
        {
            if (SettingsClient.IsSettingChanged(e, "Lyrics", "DownloadLyrics"))
            {
                if ((bool)e.Entry.Value)
                {
                    this.RestartRefreshTimer();
                }
            }
        }



        private void MetadataService_MetadataChanged(MetadataChangedEventArgs obj)
        {
            this.RestartRefreshTimer();
        }

        private void PlaybackService_PlaybackResumed(object sender, EventArgs e)
        {
            this.highlightTimer.Start();
        }

        private void PlaybackService_PlaybackPaused(object sender, PlaybackPausedEventArgs e)
        {
            this.highlightTimer.Stop();
        }



        private void I18NService_LanguageChanged(object sender, EventArgs e)
        {
            this.lyricsFactory = new LyricsFactory(SettingsClient.Get<int>("Lyrics", "TimeoutSeconds"),
                SettingsClient.Get<string>("Lyrics", "Providers"), this.info);
        }

        private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.refreshTimer.Stop();
            this.RefreshLyricsAsync(this.playbackService.CurrentTrack);
        }

        private void UpdateLyricsAfterEditingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.updateLyricsAfterEditingTimer.Stop();
            this.RefreshLyricsAsync(this.playbackService.CurrentTrack);
        }

        private async void HighlightTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.highlightTimer.Stop();
            await HighlightLyricsLineAsync();
            this.highlightTimer.Start();
        }

        private void RestartRefreshTimer()
        {
            this.refreshTimer.Stop();
            this.refreshTimer.Start();
        }

        private void StartHighlighting()
        {
            this.highlightTimer.Start();
            this.canHighlight = true;
        }

        private void StopHighlighting()
        {
            this.canHighlight = false;
            this.highlightTimer.Stop();
        }

        private void ClearLyrics(TrackViewModel track)
        {
            this.LyricsViewModel = new LyricsViewModel(this.container, track);
        }

        private long _lastTrackIdOnRefreshLyrics = 0;

        private System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1);

        private async void RefreshLyricsAsync(TrackViewModel track)
        {
            //Logger.Debug($"ENTERING RefreshLyricsAsync Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId} _semaphore count {_semaphore.CurrentCount}");
            await _semaphore.WaitAsync();
            try
            {
                /*
                if (!this.isNowPlayingPageActive || !this.isNowPlayingLyricsPageActive)
                {
                    Logger.Debug("EXIT RefreshLyricsAsync (Now Playing is not active)");
                    return;
                }
                */
                if (track == null)
                {
                    Logger.Debug("EXIT RefreshLyricsAsync (No current track)");
                    return;
                }
                if (track.Id == _lastTrackIdOnRefreshLyrics)
                {
                    Logger.Debug("EXIT RefreshLyricsAsync (Already did this track)");
                    return;
                }
                _lastTrackIdOnRefreshLyrics = track.Id;
                if (this.LyricsViewModel != null && this.LyricsViewModel.IsEditing)
                {
                    // If we're in editing mode, delay changing the lyrics.
                    this.updateLyricsAfterEditingTimer.Start();
                    Logger.Debug("EXIT RefreshLyricsAsync (Editing mode, delay changing the lyrics.)");
                    return;
                }

                this.previousTrack = track;
                this.StopHighlighting();
                ClearLyrics(track);

                try
                {
                    await Task.Run(async () =>
                    {
                        // Try to get lyrics from the DB
                        TrackLyrics trackLyrics = infoRepository.GetTrackLyrics(track.Id);
                        if (trackLyrics != null)
                        {
                            Logger.Debug($"RefreshLyricsAsync. Lyrics added from the DB TrackID: {track.Id}");
                            ApplyLyrics(track, trackLyrics, false, false);
                        }
                        if (trackLyrics == null)
                        {
                            // If the audio file has no lyrics, try to find lyrics in a local lyrics file.
                            trackLyrics = await GetLyricsFromExternalFileAsync(track);
                            if (trackLyrics != null)
                            {
                                Logger.Debug($"RefreshLyricsAsync. Lyrics added from an external file TrackID: {track.Id}");
                                ApplyLyrics(track, trackLyrics, true, false);
                            }
                        }
                        if (trackLyrics == null)
                        {
                            // If we still don't have lyrics and the user enabled automatic download of lyrics: try to download them online.
                            if (SettingsClient.Get<bool>("Lyrics", "DownloadLyrics"))
                            {
                                Logger.Debug($"RefreshLyricsAsync. Lyrics downloaded TrackID: {track.Id}");
                                trackLyrics = await DownloadLyricsFromInternet(track);
                                if (trackLyrics != null)
                                    ApplyLyrics(track, trackLyrics, true, false);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "RefreshLyricsAsync: Could not show lyrics for Track {0}. Exception: {1}", track.Path, ex.Message);
                    this.ClearLyrics(track);
                }

                this.StartHighlighting();

                
            }
            finally
            {
                //Logger.Debug($"EXITING RefreshLyricsAsync (1) Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId} _semaphore count {_semaphore.CurrentCount}");
                _semaphore.Release();
                //Logger.Debug($"EXITING RefreshLyricsAsync (2) Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId} _semaphore count {_semaphore.CurrentCount}");
            }
        }

        private SourceTypeEnum OriginTypeToSourceType(OriginType originType)
        {
            switch (originType)
            {
                case OriginType.Internet:
                    return SourceTypeEnum.Online;
                case OriginType.ExternalFile:
                    return SourceTypeEnum.Lrc;
                case OriginType.User:
                case OriginType.File:
                case OriginType.Unknown:
                default:
                    return SourceTypeEnum.Audio;
            }
        }

        private void ApplyLyrics(TrackViewModel track, TrackLyrics trackLyrics, bool applyToDB, bool applyToFile)
        {
            this.LyricsViewModel = new LyricsViewModel(container, track);
            this.LyricsViewModel.SetLyrics(new Lyrics(trackLyrics.Lyrics, trackLyrics.Origin, OriginTypeToSourceType(trackLyrics.OriginType)));
            if (applyToDB)
            {
                trackLyrics.TrackId = track.Id;
                infoRepository.SetTrackLyrics(trackLyrics, true);
            }
            if (applyToFile)
            {
                // TODO
            }
        }

        private async Task<TrackLyrics> GetLyricsFromExternalFileAsync(TrackViewModel track)
        {
            var lrcFile = Path.Combine(Path.GetDirectoryName(track.Path), Path.GetFileNameWithoutExtension(track.Path) + FileFormats.LRC);

            if (File.Exists(lrcFile))
            {
                using (var fs = new FileStream(lrcFile, FileMode.Open, FileAccess.Read))
                {
                    using (var sr = new StreamReader(fs, Encoding.Default))
                    {
                        string lyricstext = await sr.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(lyricstext))
                        {
                            return new TrackLyrics() {TrackId = track.Id, Lyrics = await sr.ReadToEndAsync(), Origin = String.Empty, OriginType = OriginType.ExternalFile };
                        }
                    }
                }
            }
            return null;
        }

        protected override SearchProvider.ProviderType? GetSearchProviderType()
        {
            return SearchProvider.ProviderType.Track;
        }

        private async Task<TrackLyrics> DownloadLyricsFromInternet(TrackViewModel track)
        {
            TrackLyrics trackLyrics = null;
            this.IsDownloadingLyrics = true;
            try
            {
                Lyrics lyrics = await this.lyricsFactory.GetLyricsAsync(track.ArtistName, track.TrackTitle);
                if (lyrics != null && lyrics.HasText)
                    trackLyrics = new TrackLyrics() { TrackId = track.Id, Lyrics = lyrics.Text, Origin = lyrics.Source, OriginType = OriginType.Internet };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not get lyrics online {0}. Exception: {1}", track.Path, ex.Message);
            }
            this.IsDownloadingLyrics = false;
            return trackLyrics;
        }

        private async Task HighlightLyricsLineAsync()
        {
            if (!this.canHighlight)
            {
                return;
            }

            if (this.LyricsViewModel == null || this.LyricsViewModel.LyricsLines == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < this.LyricsViewModel.LyricsLines.Count; i++)
                    {
                        if (!this.canHighlight)
                        {
                            break;
                        }

                        double progressTime = this.playbackService.GetCurrentTime.TotalMilliseconds;
                        double lyricsLineTime = this.LyricsViewModel.LyricsLines[i].Time.TotalMilliseconds;
                        double nextLyricsLineTime = 0;

                        int j = 1;

                        while (i + j < this.LyricsViewModel.LyricsLines.Count && nextLyricsLineTime <= lyricsLineTime)
                        {
                            if (!this.canHighlight)
                            {
                                break;
                            }

                            nextLyricsLineTime = this.LyricsViewModel.LyricsLines[i + j].Time.TotalMilliseconds;
                            j++;
                        }

                        if (progressTime >= lyricsLineTime & (nextLyricsLineTime >= progressTime | nextLyricsLineTime == 0))
                        {
                            this.LyricsViewModel.LyricsLines[i].IsHighlighted = true;

                            if (this.LyricsViewModel.AutomaticScrolling & this.canHighlight)
                            {
                                this.eventAggregator.GetEvent<ScrollToHighlightedLyricsLine>().Publish(null);
                            }
                        }
                        else
                        {
                            this.LyricsViewModel.LyricsLines[i].IsHighlighted = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not highlight the lyrics. Exception: {0}", ex.Message);
                }

            });
        }

        protected override void SearchOnline(string id)
        {
            // No implementation required here
        }
    }
}