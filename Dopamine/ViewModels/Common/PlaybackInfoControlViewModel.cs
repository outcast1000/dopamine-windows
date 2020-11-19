using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Alex; //Digimezzo.Foundation.Core.Settings
using Digimezzo.Foundation.WPF.Controls;
using Dopamine.Core.Utils;
using Dopamine.Services.Entities;
using Dopamine.Services.Metadata;
using Dopamine.Services.Playback;
using Dopamine.Services.Scrobbling;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Timers;
using Prism.Commands;

namespace Dopamine.ViewModels.Common
{
    public class PlaybackInfoControlViewModel : BindableBase
    {
        private PlaybackInfoViewModel playbackInfoViewModel;
        private IPlaybackService playbackService;
        private IMetadataService metadataService;
        private IScrobblingService scrobblingService;
        private SlideDirection slideDirection;
        private TrackViewModel previousTrack;
        private TrackViewModel track;
        private Timer refreshTimer = new Timer();
        private int refreshTimerIntervalMilliseconds = 250;
        private bool enableRating;
        private bool enableLove;

        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand UnloadedCommand { get; set; }

        public int Rating
        {
            get
            {
                return this.track == null ? 0 : NumberUtils.ConvertToInt32(this.track.Rating);
            }
            set
            {
                if (this.track != null)
                {
                    this.track.Rating = value;
                    RaisePropertyChanged(nameof(this.Rating));
                    this.metadataService.UpdateTrackRatingAsync(this.track.Path, value);
                }
            }
        }

        public bool Love
        {
            get
            {
                return this.track == null ? false : this.track.Love;
            }
            set
            {
                if (this.track != null)
                {
                    // Update the UI
                    this.track.Love = value;
                    RaisePropertyChanged(nameof(this.Love));

                    // Update Love in the database
                    this.metadataService.UpdateTrackLoveAsync(this.track.Path, value);

                    // Send Love/Unlove to the scrobbling service
                    this.scrobblingService.SendTrackLoveAsync(this.track, value);
                }
            }
        }

        public PlaybackInfoViewModel PlaybackInfoViewModel
        {
            get { return this.playbackInfoViewModel; }
            set { SetProperty<PlaybackInfoViewModel>(ref this.playbackInfoViewModel, value); }
        }

        public SlideDirection SlideDirection
        {
            get { return this.slideDirection; }
            set { SetProperty<SlideDirection>(ref this.slideDirection, value); }
        }

        public bool EnableRating
        {
            get { return this.enableRating; }
            set { SetProperty<bool>(ref this.enableRating, value); }
        }

        public bool EnableLove
        {
            get { return this.enableLove; }
            set { SetProperty<bool>(ref this.enableLove, value); }
        }

        public PlaybackInfoControlViewModel(IPlaybackService playbackService, IMetadataService metadataService, IScrobblingService scrobblingService)
        {
            this.playbackService = playbackService;
            this.metadataService = metadataService;
            this.scrobblingService = scrobblingService;

            this.refreshTimer.Interval = this.refreshTimerIntervalMilliseconds;

            LoadedCommand = new DelegateCommand(() => { OnLoad(); });
            UnloadedCommand = new DelegateCommand(() => { OnUnLoad(); });

            // Defaults
            this.SlideDirection = SlideDirection.DownToUp;
            this.RefreshPlaybackInfoAsync(this.playbackService.CurrentTrack, false);
            this.EnableRating = SettingsClient.Get<bool>("Behaviour", "EnableRating");
            this.EnableLove = SettingsClient.Get<bool>("Behaviour", "EnableLove");
        }

        protected virtual void OnLoad()
        {
            this.refreshTimer.Elapsed += RefreshTimer_Elapsed;
            this.playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            this.playbackService.PlaybackStopped += PlaybackService_PlaybackStopped;
            this.playbackService.PlaybackProgressChanged += PlaybackService_PlaybackProgressChanged;
            this.playbackService.PlayingTrackChanged += PlaybackService_PlayingTrackChanged;
            this.metadataService.RatingChanged += MetadataService_RatingChanged;
            this.metadataService.LoveChanged += MetadataService_LoveChanged;
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += SettingsClient_SettingChanged;
        }

        protected virtual void OnUnLoad()
        {
            this.refreshTimer.Elapsed -= RefreshTimer_Elapsed;
            this.playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            this.playbackService.PlaybackStopped -= PlaybackService_PlaybackStopped;
            this.playbackService.PlaybackProgressChanged -= PlaybackService_PlaybackProgressChanged;
            this.playbackService.PlayingTrackChanged -= PlaybackService_PlayingTrackChanged;
            this.metadataService.RatingChanged -= MetadataService_RatingChanged;
            this.metadataService.LoveChanged -= MetadataService_LoveChanged;
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged -= SettingsClient_SettingChanged;
        }

        private void SettingsClient_SettingChanged(object sender, Digimezzo.Foundation.Core.Settings.SettingChangedEventArgs e)
        {
            if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
            {
                this.EnableRating = (bool)e.Entry.Value;

            }

            if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
            {
                this.EnableLove = (bool)e.Entry.Value;
            }
        }

        private void MetadataService_LoveChanged(LoveChangedEventArgs e)
        {
            if (this.track != null && e.SafePath.Equals(this.track.SafePath))
            {
                this.track.UpdateVisibleLove(e.Love);
                this.RaisePropertyChanged(nameof(this.Love));
            }
        }

        private void MetadataService_RatingChanged(RatingChangedEventArgs e)
        {
            if (this.track != null && e.SafePath.Equals(this.track.SafePath))
            {
                this.track.UpdateVisibleRating(e.Rating);
                this.RaisePropertyChanged(nameof(this.Rating));
            }
        }

        private void PlaybackService_PlayingTrackChanged(object sender, EventArgs e)
        {
            this.RefreshPlaybackInfoAsync(this.playbackService.CurrentTrack, true);
        }

        private void PlaybackService_PlaybackProgressChanged(object sender, EventArgs e)
        {
            this.UpdateTime();
        }

        private void PlaybackService_PlaybackStopped(object sender, EventArgs e)
        {
            this.refreshTimer.Stop();
            this.refreshTimer.Start();
        }

        private void PlaybackService_PlaybackSuccess(object sender, PlaybackSuccessEventArgs e)
        {
            this.SlideDirection = e.IsPlayingPreviousTrack ? SlideDirection.UpToDown : SlideDirection.DownToUp;
            this.refreshTimer.Stop();
            this.refreshTimer.Start();
        }




        private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.refreshTimer.Stop();
            this.RefreshPlaybackInfoAsync(this.playbackService.CurrentTrack, false);
        }

        private void ClearPlaybackInfo()
        {
            this.PlaybackInfoViewModel = new PlaybackInfoViewModel
            {
                Title = string.Empty,
                Artist = string.Empty,
                Album = string.Empty,
                Year = string.Empty,
                CurrentTime = string.Empty,
                TotalTime = string.Empty
            };

            this.track = null;
        }

        private async void RefreshPlaybackInfoAsync(TrackViewModel track, bool allowRefreshingCurrentTrack)
        {
            await Task.Run(() =>
            {
                this.previousTrack = this.track;

                // No track selected: clear playback info.
                if (track == null)
                {
                    this.ClearPlaybackInfo();
                    return;
                }

                this.track = track;

                // The track didn't change: leave the previous playback info.
                if (!allowRefreshingCurrentTrack & this.track.Equals(this.previousTrack)) return;

                // The track changed: we need to show new playback info.
                try
                {
                    this.PlaybackInfoViewModel = new PlaybackInfoViewModel
                    {
                        Title = string.IsNullOrEmpty(track.TrackTitle) ? track.FileName : track.TrackTitle,
                        Artist = track.ArtistName,
                        Album = track.AlbumTitle,
                        Year = track.Year,
                        CurrentTime = FormatUtils.FormatTime(new TimeSpan(0)),
                        TotalTime = FormatUtils.FormatTime(new TimeSpan(0))
                    };
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not show playback information for Track {0}. Exception: {1}", track.Path, ex.Message);
                    this.ClearPlaybackInfo();
                }

                this.RaisePropertyChanged(nameof(Rating));
                this.RaisePropertyChanged(nameof(Love));
                this.UpdateTime();
            });
        }

        private void UpdateTime()
        {
            if(this.PlaybackInfoViewModel == null)
            {
                return;
            }

            this.PlaybackInfoViewModel.CurrentTime = FormatUtils.FormatTime(this.playbackService.GetCurrentTime);
            this.PlaybackInfoViewModel.TotalTime = " / " + FormatUtils.FormatTime(this.playbackService.GetTotalTime);
        }
    }
}
