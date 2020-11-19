using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Utils;
using Dopamine.ViewModels;
using Dopamine.Services.Dialog;
using Dopamine.Services.Playback;
using Dopamine.Views.Common;
using Prism.Commands;
using Prism.Ioc;

namespace Dopamine.ViewModels.Common.Base
{
    public class PlaybackControlsViewModelBase : ContextMenuViewModelBase
    {
        private PlaybackInfoViewModel playbackInfoViewModel;

        public DelegateCommand ShowEqualizerCommand { get; set; }

        public IPlaybackService PlaybackService { get; }
        public IDialogService DialogService { get; }

        public PlaybackInfoViewModel PlaybackInfoViewModel
        {
            get { return this.playbackInfoViewModel; }
            set { SetProperty<PlaybackInfoViewModel>(ref this.playbackInfoViewModel, value); }
        }

        public PlaybackControlsViewModelBase(IContainerProvider container) : base(container)
        {
            this.PlaybackService = container.Resolve<IPlaybackService>();
            this.DialogService = container.Resolve<IDialogService>();
            

            this.ShowEqualizerCommand = new DelegateCommand(() =>
            {
                EqualizerControl view = container.Resolve<EqualizerControl>();
                view.DataContext = container.Resolve<EqualizerControlViewModel>();

                this.DialogService.ShowCustomDialog(
                     new EqualizerIcon() { IsDialogIcon = true },
                     0,
                     ResourceUtils.GetString("Language_Equalizer"),
                     view,
                     570,
                     0,
                     false,
                     true,
                     true,
                     false,
                     ResourceUtils.GetString("Language_Close"),
                     string.Empty,
                     null);
            });

            this.Reset();
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            this.PlaybackService.PlaybackProgressChanged += PlaybackService_PlaybackProgressChanged;
        }
        protected override void OnUnLoad()
        {
            this.PlaybackService.PlaybackProgressChanged -= PlaybackService_PlaybackProgressChanged;
            base.OnUnLoad();
        }

        private void PlaybackService_PlaybackProgressChanged(object sender, System.EventArgs e)
        {
            this.UpdateTime();
        }



        protected void UpdateTime()
        {
            this.PlaybackInfoViewModel.CurrentTime = FormatUtils.FormatTime(this.PlaybackService.GetCurrentTime);
            this.PlaybackInfoViewModel.TotalTime = " / " + FormatUtils.FormatTime(this.PlaybackService.GetTotalTime);
        }

        protected void Reset()
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
        }

        protected override void SearchOnline(string id)
        {
            // No implementation required here
        }
    }
}
