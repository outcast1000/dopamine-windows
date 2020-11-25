using Digimezzo.Foundation.Core.Utils;
using Dopamine.Data.Entities;
using Dopamine.ViewModels;
using Dopamine.Services.Dialog;
using Dopamine.Services.Playback;
using Dopamine.Services.Playlist;
using Dopamine.Services.Provider;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Dopamine.Services.Entities;

namespace Dopamine.ViewModels.Common.Base
{
    public abstract class ContextMenuViewModelBase : BindableBase
    {
        private IProviderService providerService;
        private IPlaylistService playlistService;
        private IPlaybackService playbackService;
        private IDialogService dialogService;
        private ObservableCollection<SearchProvider> contextMenuSearchProviders;
        private ObservableCollection<PlaylistViewModel> contextMenuPlaylists;
        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand UnloadedCommand { get; set; }

        public DelegateCommand<string> SearchOnlineCommand { get; set; }
        public DelegateCommand<string> AddPlayingTrackToPlaylistCommand { get; set; }

        public bool HasContextMenuPlaylists => this.ContextMenuPlaylists != null && this.ContextMenuPlaylists.Count > 0;

        public ObservableCollection<SearchProvider> ContextMenuSearchProviders
        {
            get { return this.contextMenuSearchProviders; }
            set
            {
                SetProperty<ObservableCollection<SearchProvider>>(ref this.contextMenuSearchProviders, value);
                RaisePropertyChanged(nameof(this.HasContextMenuSearchProviders));
            }
        }

        public ObservableCollection<PlaylistViewModel> ContextMenuPlaylists
        {
            get { return this.contextMenuPlaylists; }
            set
            {
                SetProperty<ObservableCollection<PlaylistViewModel>>(ref this.contextMenuPlaylists, value);
                RaisePropertyChanged(nameof(this.HasContextMenuPlaylists));
            }
        }

        public ContextMenuViewModelBase(IContainerProvider container)
        {
            // Dependency injection
            this.providerService = container.Resolve<IProviderService>();
            this.playlistService = container.Resolve<IPlaylistService>();
            this.playbackService = container.Resolve<IPlaybackService>();
            this.dialogService = container.Resolve<IDialogService>();

            // Commands
            this.SearchOnlineCommand = new DelegateCommand<string>((id) => this.SearchOnline(id));
            this.AddPlayingTrackToPlaylistCommand = new DelegateCommand<string>(
            async (playlistName) => await this.AddPlayingTrackToPlaylistAsync(playlistName), (_) => this.playbackService.HasCurrentTrack);

            LoadedCommand = new DelegateCommand(() => { OnLoad(); });
            UnloadedCommand = new DelegateCommand(() => { OnUnLoad(); });
            _providerType = GetSearchProviderType();
            if (_providerType.HasValue)
                this.GetSearchProvidersAsync(_providerType.Value);
            // Initialize the playlists in the ContextMenu
            this.GetContextMenuPlaylistsAsync();
        }

        private void PlaylistService_PlaylistFolderChanged(object sender, EventArgs e)
        {
            this.GetContextMenuPlaylistsAsync();
        }

        private void PlaybackService_PlaybackResumed(object sender, EventArgs e)
        {
            this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
        }

        private void PlaybackService_PlaybackPaused(object sender, PlaybackPausedEventArgs e)
        {
            this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
        }

        private void PlaybackService_PlaybackStopped(object sender, EventArgs e)
        {
            this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
        }

        private void PlaybackService_PlaybackFailed(object sender, PlaybackFailedEventArgs e)
        {
            this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
        }

        private void PlaybackService_PlaybackSuccess(object sender, PlaybackSuccessEventArgs e)
        {
            this.AddPlayingTrackToPlaylistCommand.RaiseCanExecuteChanged();
        }

        private SearchProvider.ProviderType? _providerType;
        protected virtual void OnLoad()
        {
            playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            this.playbackService.PlaybackFailed += PlaybackService_PlaybackFailed;
            this.playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            this.playbackService.PlaybackStopped += PlaybackService_PlaybackStopped;
            this.playbackService.PlaybackPaused += PlaybackService_PlaybackPaused;
            this.playbackService.PlaybackResumed += PlaybackService_PlaybackResumed;
            this.playlistService.PlaylistFolderChanged += PlaylistService_PlaylistFolderChanged;
            // Initialize the search providers in the ContextMenu
            if (_providerType.HasValue)
                this.providerService.SearchProvidersChanged += ProviderService_SearchProvidersChanged;
        }

        private void ProviderService_SearchProvidersChanged(object sender, EventArgs e)
        {
            this.GetSearchProvidersAsync(_providerType.Value);
        }

        protected virtual void OnUnLoad()
        {
            playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            this.playbackService.PlaybackFailed -= PlaybackService_PlaybackFailed;
            this.playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            this.playbackService.PlaybackStopped -= PlaybackService_PlaybackStopped;
            this.playbackService.PlaybackPaused -= PlaybackService_PlaybackPaused;
            this.playbackService.PlaybackResumed -= PlaybackService_PlaybackResumed;
            this.playlistService.PlaylistFolderChanged -= PlaylistService_PlaylistFolderChanged;
            this.providerService.SearchProvidersChanged -= ProviderService_SearchProvidersChanged;
        }


        protected virtual SearchProvider.ProviderType? GetSearchProviderType()
        {
            return null;
        }

        private async Task AddPlayingTrackToPlaylistAsync(string playlistName)
        {
            if (!this.playbackService.HasCurrentTrack)
            {
                return;
            }

            var playingTrack = new List<TrackViewModel>() { this.playbackService.CurrentTrack };
            await this.AddTracksToPlaylistAsync(playlistName, playingTrack);
        }

        protected async void GetSearchProvidersAsync(SearchProvider.ProviderType providerType)
        {
            this.ContextMenuSearchProviders = null;

            List<SearchProvider> providersList = await this.providerService.GetSearchProvidersAsync(providerType);
            var localProviders = new ObservableCollection<SearchProvider>();

            await Task.Run(() =>
            {
                foreach (SearchProvider vp in providersList)
                {
                    localProviders.Add(vp);
                }
            });

            this.ContextMenuSearchProviders = localProviders;
        }

        public async void GetContextMenuPlaylistsAsync()
        {
            try
            {
                // Unbind to improve UI performance
                this.ContextMenuPlaylists = null;
                
                // Populate an ObservableCollection
                var playlistViewModels = new ObservableCollection<PlaylistViewModel>(await this.playlistService.GetStaticPlaylistsAsync());

                // Re-bind to update the UI
                this.ContextMenuPlaylists = playlistViewModels;

            }
            catch (Exception)
            {
                // If loading from the database failed, create and empty Collection.
                this.ContextMenuPlaylists = new ObservableCollection<PlaylistViewModel>();
            }
        }

        protected bool HasContextMenuSearchProviders => this.ContextMenuSearchProviders != null && this.ContextMenuSearchProviders.Count > 0;

        protected async Task AddTracksToPlaylistAsync(string playlistName, IList<TrackViewModel> tracks)
        {
            CreateNewPlaylistResult addPlaylistResult = CreateNewPlaylistResult.Success; // Default Success

            // If no playlist is provided, first create one.
            if (playlistName == null)
            {
                var responseText = ResourceUtils.GetString("Language_New_Playlist");

                if (this.dialogService.ShowInputDialog(
                    0xea37,
                    16,
                    ResourceUtils.GetString("Language_New_Playlist"),
                    ResourceUtils.GetString("Language_Enter_Name_For_Playlist"),
                    ResourceUtils.GetString("Language_Ok"),
                    ResourceUtils.GetString("Language_Cancel"),
                    ref responseText))
                {
                    playlistName = responseText;
                    addPlaylistResult = await this.playlistService.CreateNewPlaylistAsync(new EditablePlaylistViewModel(playlistName, PlaylistType.Static));
                }
            }

            // If playlist name is still null, the user clicked cancel on the previous dialog. Stop here.
            if (playlistName == null) return;

            // Verify if the playlist was added
            switch (addPlaylistResult)
            {
                case CreateNewPlaylistResult.Success:
                case CreateNewPlaylistResult.Duplicate:
                    // Add items to playlist
                    AddTracksToPlaylistResult result = await this.playlistService.AddTracksToStaticPlaylistAsync(tracks, playlistName);

                    if (result == AddTracksToPlaylistResult.Error)
                    {
                        this.dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetString("Language_Error"), ResourceUtils.GetString("Language_Error_Adding_Songs_To_Playlist").Replace("{playlistname}", "\"" + playlistName + "\""), ResourceUtils.GetString("Language_Ok"), true, ResourceUtils.GetString("Language_Log_File"));
                    }
                    break;
                case CreateNewPlaylistResult.Error:
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Error_Adding_Playlist"),
                        ResourceUtils.GetString("Language_Ok"),
                        true,
                        ResourceUtils.GetString("Language_Log_File"));
                    break;
                case CreateNewPlaylistResult.Blank:
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Provide_Playlist_Name"),
                        ResourceUtils.GetString("Language_Ok"),
                        false,
                        string.Empty);
                    break;
                default:
                    // Never happens
                    break;
            }
        }

        protected abstract void SearchOnline(string id);
    }
}
