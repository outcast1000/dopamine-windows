using Dopamine.Core.Enums;
using Dopamine.Services.Playback;
using Prism.Commands;
using Prism.Mvvm;

namespace Dopamine.ViewModels.Common.Base
{
    public abstract class NowPlayingViewModelBase : BindableBase
    {
        private IPlaybackService _playbackService;
        private NowPlayingPage _selectedNowPlayingPage;
        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand UnloadedCommand { get; set; }

        public NowPlayingPage SelectedNowPlayingPage
        {
            get { return _selectedNowPlayingPage; }
            set { SetProperty<NowPlayingPage>(ref this._selectedNowPlayingPage, value); }
        }

        public NowPlayingViewModelBase(IPlaybackService playbackService)
        {
            _playbackService = playbackService;
            LoadedCommand = new DelegateCommand(() => { OnLoad();  });
            UnloadedCommand = new DelegateCommand(() => { OnUnload(); });
        }

        private void PlaybackService_PlaylistChanged(object sender, System.EventArgs e)
        {
            SetNowPlaying();
        }

        private void PlaybackService_PlaybackSuccess(object sender, PlaybackSuccessEventArgs e)
        {
            SetNowPlaying();
        }

        private void SetNowPlaying()
        {
            SelectedNowPlayingPage = _playbackService.PlaylistItems.Count > 0 ? NowPlayingPage.NowPlaying : NowPlayingPage.NothingPlaying;
        }

        private void OnLoad()
        {
            _playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            _playbackService.PlaylistChanged += PlaybackService_PlaylistChanged;
            SetNowPlaying();
        }
        private void OnUnload()
        {
            _playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            _playbackService.PlaylistChanged -= PlaybackService_PlaylistChanged;

        }
    }
}
