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

namespace Dopamine.ViewModels.Common
{
    public class LyricsControlViewModel : ContextMenuViewModelBase
    {
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
        private bool isNowPlayingPageActive;
        private bool isNowPlayingLyricsPageActive;
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

            this.highlightTimer.Interval = this.highlightTimerIntervalMilliseconds;
            this.highlightTimer.Elapsed += HighlightTimer_Elapsed;

            this.updateLyricsAfterEditingTimer.Interval = this.updateLyricsAfterEditingTimerIntervalMilliseconds;
            this.updateLyricsAfterEditingTimer.Elapsed += UpdateLyricsAfterEditingTimer_Elapsed;

            this.refreshTimer.Interval = this.refreshTimerIntervalMilliseconds;
            this.refreshTimer.Elapsed += RefreshTimer_Elapsed;

            this.playbackService.PlaybackPaused += (_, __) => this.highlightTimer.Stop();
            this.playbackService.PlaybackResumed += (_, __) => this.highlightTimer.Start();

            this.metadataService.MetadataChanged += (_) => this.RestartRefreshTimer();

            I18NService_LanguageChanged(null, null);
            this.i18NService.LanguageChanged += I18NService_LanguageChanged;

            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Lyrics", "DownloadLyrics"))
                {
                    if ((bool)e.Entry.Value)
                    {
                        this.RestartRefreshTimer();
                    }
                }
            };

            this.isNowPlayingPageActive = SettingsClient.Get<bool>("FullPlayer", "IsNowPlayingSelected");
            this.isNowPlayingLyricsPageActive = ((NowPlayingSubPage)SettingsClient.Get<int>("FullPlayer", "SelectedNowPlayingSubPage")) == NowPlayingSubPage.Lyrics;

            this.eventAggregator.GetEvent<IsNowPlayingPageActiveChanged>().Subscribe(isNowPlayingPageActive =>
            {
                this.isNowPlayingPageActive = isNowPlayingPageActive;
                this.RestartRefreshTimer();
            });

            this.eventAggregator.GetEvent<IsNowPlayingSubPageChanged>().Subscribe(tuple =>
            {
                this.isNowPlayingLyricsPageActive = tuple.Item2 == NowPlayingSubPage.Lyrics;
                this.RestartRefreshTimer();
            });

            this.RefreshLyricsCommand = new DelegateCommand(() => this.RestartRefreshTimer(), () => !this.IsDownloadingLyrics);
            ApplicationCommands.RefreshLyricsCommand.RegisterCommand(this.RefreshLyricsCommand);

            this.playbackService.PlaybackSuccess += (_, e) =>
            {
                this.ContentSlideInFrom = e.IsPlayingPreviousTrack ? -30 : 30;
                this.RestartRefreshTimer();
            };

            this.ClearLyrics(null); // Makes sure the loading animation can be shown even at first start

            this.RestartRefreshTimer();
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

        private object inRefreshLyricsAsync = false;

        private async void RefreshLyricsAsync(TrackViewModel track)
        {
            if (!this.isNowPlayingPageActive || !this.isNowPlayingLyricsPageActive) 
                return;
            if (track == null) 
                return;
            if (this.LyricsViewModel != null && this.LyricsViewModel.IsEditing)
            {
                // If we're in editing mode, delay changing the lyrics.
                this.updateLyricsAfterEditingTimer.Start();
                return;
            }
            lock (inRefreshLyricsAsync)
            {
                if (inRefreshLyricsAsync.Equals(true))
                    return;
                inRefreshLyricsAsync = true;
            }

            this.previousTrack = track;
            this.StopHighlighting();


            //FileMetadata fmd = await this.metadataService.GetFileMetadataAsync(track.Path);
            try
            {
                await Task.Run(async () =>
                {
                    Lyrics lyrics = null;
                    bool bLyricsAlreadyInDatabase = false;
                    TrackLyrics trackLyrics = infoRepository.GetTrackLyrics(track.Id);
                    // No FileMetadata available: clear the lyrics.
                    if (trackLyrics == null)
                    {
                        this.ClearLyrics(track);
                    }
                    else
                    {
                        lyrics = new Lyrics(trackLyrics.Lyrics, trackLyrics.Source, SourceTypeEnum.Online);
                        bLyricsAlreadyInDatabase = true;
                    }

                    // Try to get lyrics from the DB
                    //lyrics = new Lyrics(trackLyrics.Lyrics, trackLyrics.Source, SourceTypeEnum.Online);

                    // If the audio file has no lyrics, try to find lyrics in a local lyrics file.
                    if (lyrics == null)
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
                                        lyrics = new Lyrics(await sr.ReadToEndAsync(), String.Empty, SourceTypeEnum.Lrc);
                                    }
                                }
                            }
                        }
                    }
                    if (lyrics == null)
                    {
                        // If we still don't have lyrics and the user enabled automatic download of lyrics: try to download them online.
                        if (SettingsClient.Get<bool>("Lyrics", "DownloadLyrics"))
                        {
                            this.IsDownloadingLyrics = true;
                            try
                            {
                                lyrics = await this.lyricsFactory.GetLyricsAsync(track.ArtistName, track.TrackTitle);
                                lyrics.SourceType = SourceTypeEnum.Online;
                            }
                            catch (Exception ex)
                            {
                                LogClient.Error("Could not get lyrics online {0}. Exception: {1}", track.Path, ex.Message);
                            }

                            this.IsDownloadingLyrics = false;
                        }
                    }
                    if (lyrics != null)
                    {
                        this.LyricsViewModel = new LyricsViewModel(container, track);
                        this.LyricsViewModel.SetLyrics(lyrics);
                        if (!bLyricsAlreadyInDatabase)
                        {
                            infoRepository.SetTrackLyrics(new TrackLyrics() { TrackId = track.Id, Lyrics = lyrics.Text, Source = lyrics.Source, DateAdded = DateTime.Now.Ticks }, true);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not show lyrics for Track {0}. Exception: {1}", track.Path, ex.Message);
                this.ClearLyrics(track);
            }

            this.StartHighlighting();
            inRefreshLyricsAsync = false;
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