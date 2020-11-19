using Dopamine.Core.Alex;  //Digimezzo.Foundation.Core.Settings
using Digimezzo.Foundation.WPF.Controls;
using Dopamine.ViewModels.Common.Base;
using Dopamine.Core.Enums;
using Dopamine.Core.Prism;
using Prism.Commands;
using Prism.Events;
using System;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common
{
    public class NowPlayingPlaybackControlsViewModel : PlaybackControlsViewModelBase
    {
        private IEventAggregator eventAggregator;
        private NowPlayingSubPage previousSelectedNowPlayingSubPage;
        private NowPlayingSubPage selectedNowPlayingSubPage;

        public NowPlayingSubPage SelectedNowPlayingSubPage
        {
            get { return this.selectedNowPlayingSubPage; }
            set
            {
                SetProperty<NowPlayingSubPage>(ref this.selectedNowPlayingSubPage, value);
                SettingsClient.Set<int>("FullPlayer", "SelectedNowPlayingSubPage", (int)value);
                SlideDirection direction = value <= this.previousSelectedNowPlayingSubPage ? SlideDirection.LeftToRight : SlideDirection.RightToLeft;
                this.eventAggregator.GetEvent<IsNowPlayingSubPageChanged>().Publish(new Tuple<SlideDirection, NowPlayingSubPage>(direction, value));
                this.previousSelectedNowPlayingSubPage = value;
            }
        }

        public bool HasPlaybackQueue
        {
            get { return this.PlaybackService.PlaylistItems.Count > 0; }
        }

        public NowPlayingPlaybackControlsViewModel(IContainerProvider container, IEventAggregator eventAggregator) : base(container)
        {
            this.eventAggregator = eventAggregator;


        }

        protected override void OnLoad()
        {
            base.OnLoad();
            if (SettingsClient.Get<bool>("Startup", "ShowLastSelectedPage"))
            {
                this.SelectedNowPlayingSubPage = (NowPlayingSubPage)SettingsClient.Get<int>("FullPlayer", "SelectedNowPlayingSubPage");
            }
            else
            {
                this.SelectedNowPlayingSubPage = NowPlayingSubPage.ShowCase;
            }
            this.PlaybackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            this.PlaybackService.PlaylistChanged += PlaybackService_PlaylistChanged;
            this.PlaybackService.PlaybackStopped += PlaybackService_PlaybackStopped;
        }

        private void PlaybackService_PlaybackStopped(object sender, EventArgs e)
        {
            this.Reset();
        }

        private void PlaybackService_PlaylistChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(this.HasPlaybackQueue));
        }

        private void PlaybackService_PlaybackSuccess(object sender, Services.Playback.PlaybackSuccessEventArgs e)
        {
            RaisePropertyChanged(nameof(this.HasPlaybackQueue));
        }

        protected override void OnUnLoad()
        {
            this.PlaybackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            this.PlaybackService.PlaylistChanged -= PlaybackService_PlaylistChanged;
            this.PlaybackService.PlaybackStopped -= PlaybackService_PlaybackStopped;
            base.OnUnLoad();
        }
    }
}
