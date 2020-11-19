using Dopamine.Services.Dialog;
using Dopamine.ViewModels.Common.Base;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common
{
    public class CollectionPlaybackControlsViewModel : PlaybackControlsViewModelBase
    {

        public bool IsPlaying
        {
            get { return !this.PlaybackService.IsStopped & this.PlaybackService.IsPlaying; }
        }

        public CollectionPlaybackControlsViewModel(IContainerProvider container, IDialogService dialogService) : base(container)
        {
            this.PlaybackService.PlaybackStopped += (_, __) =>
            {
                this.Reset();
                RaisePropertyChanged(nameof(this.IsPlaying));
            };
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            this.PlaybackService.PlaybackFailed += PlaybackService_PlaybackFailed;
            this.PlaybackService.PlaybackPaused += PlaybackService_PlaybackPaused;
            this.PlaybackService.PlaybackResumed += PlaybackService_PlaybackResumed;
            this.PlaybackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
        }
        protected override void OnUnLoad()
        {
            this.PlaybackService.PlaybackFailed -= PlaybackService_PlaybackFailed;
            this.PlaybackService.PlaybackPaused -= PlaybackService_PlaybackPaused;
            this.PlaybackService.PlaybackResumed -= PlaybackService_PlaybackResumed;
            this.PlaybackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            base.OnUnLoad();
        }

        private void PlaybackService_PlaybackSuccess(object sender, Services.Playback.PlaybackSuccessEventArgs e)
        {
            RaisePropertyChanged(nameof(this.IsPlaying));
        }

        private void PlaybackService_PlaybackResumed(object sender, System.EventArgs e)
        {
            RaisePropertyChanged(nameof(this.IsPlaying));
        }

        private void PlaybackService_PlaybackPaused(object sender, Services.Playback.PlaybackPausedEventArgs e)
        {
            RaisePropertyChanged(nameof(this.IsPlaying));
        }

        private void PlaybackService_PlaybackFailed(object sender, Services.Playback.PlaybackFailedEventArgs e)
        {
            RaisePropertyChanged(nameof(this.IsPlaying));
        }


    }
}
