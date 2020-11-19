using Dopamine.Core.Alex;  //Digimezzo.Foundation.Core.Settings
using Dopamine.Core.Prism;
using Dopamine.Data;
using Dopamine.Services.Entities;
using Dopamine.Services.File;
using Dopamine.Services.Folders;
using Dopamine.Services.Playback;
using Dopamine.ViewModels.Common.Base;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Dopamine.ViewModels.FullPlayer.Collection
{
    public class CollectionFoldersViewModel : TracksViewModelBase
    {
        private IFoldersService foldersService;
        private IFileService fileService;
        private IPlaybackService playbackService;
        private IEventAggregator eventAggregator;
        private ObservableCollection<FolderViewModel> folders;
        private ObservableCollection<SubfolderViewModel> subfolders;
        private FolderViewModel selectedFolder;
        private string activeSubfolderPath;
        private ObservableCollection<SubfolderBreadCrumbViewModel> subfolderBreadCrumbs;
		private readonly string Settings_NameSpace = "CollectionFolders";


        public DelegateCommand<string> JumpSubfolderCommand { get; set; }

        public ObservableCollection<SubfolderBreadCrumbViewModel> SubfolderBreadCrumbs
        {
            get { return this.subfolderBreadCrumbs; }
            set { SetProperty<ObservableCollection<SubfolderBreadCrumbViewModel>>(ref this.subfolderBreadCrumbs, value); }
        }

        private GridLength _leftPaneGridLength;
        public GridLength LeftPaneWidth
        {
            get => _leftPaneGridLength;
            set
            {
                SetProperty<GridLength>(ref _leftPaneGridLength, value);
                SettingsClient.Set<string>(Settings_NameSpace, CollectionUtils.Setting_LeftPaneGridLength, CollectionUtils.GridLength2String(value));
            }
        }

        private GridLength _rightPaneGridLength;
        public GridLength RightPaneWidth
        {
            get => _rightPaneGridLength;
            set
            {
                SetProperty<GridLength>(ref _rightPaneGridLength, value);
                SettingsClient.Set<string>(Settings_NameSpace, CollectionUtils.Setting_RightPaneGridLength, CollectionUtils.GridLength2String(value));
            }
        }

        public ObservableCollection<FolderViewModel> Folders
        {
            get { return this.folders; }
            set { SetProperty<ObservableCollection<FolderViewModel>>(ref this.folders, value); }
        }

        public ObservableCollection<SubfolderViewModel> Subfolders
        {
            get { return this.subfolders; }
            set { SetProperty<ObservableCollection<SubfolderViewModel>>(ref this.subfolders, value); }
        }

        public FolderViewModel SelectedFolder
        {
            get { return this.selectedFolder; }
            set
            {
                SetProperty<FolderViewModel>(ref this.selectedFolder, value);
                SettingsClient.Set<string>("Selections", "SelectedFolder", value != null ? value.Path : string.Empty);
                Task unAwaitedTask = this.GetSubfoldersAsync(null);
            }
        }

        public CollectionFoldersViewModel(IContainerProvider container, IFoldersService foldersService, IFileService fileService,
            IPlaybackService playbackService, IEventAggregator eventAggregator) : base(container)
        {
            this.foldersService = foldersService;
            this.fileService = fileService;
            this.playbackService = playbackService;
            this.eventAggregator = eventAggregator;

            // Commands
            this.JumpSubfolderCommand = new DelegateCommand<string>(async (subfolderPath) => await this.GetSubfoldersAsync(new SubfolderViewModel(subfolderPath, false)));


            // Events

            LeftPaneWidth = CollectionUtils.String2GridLength(SettingsClient.Get<string>(Settings_NameSpace, CollectionUtils.Setting_LeftPaneGridLength));
            RightPaneWidth = CollectionUtils.String2GridLength(SettingsClient.Get<string>(Settings_NameSpace, CollectionUtils.Setting_RightPaneGridLength));

        }

        SubscriptionToken _ActiveSubfolderChangedSubscriptionToken;
        protected override void OnLoad()
        {
            base.OnLoad();
            this.foldersService.FoldersChanged += FoldersService_FoldersChanged;
            this.playbackService.PlaybackFailed += PlaybackService_PlaybackFailed;
            this.playbackService.PlaybackPaused += PlaybackService_PlaybackPaused;
            this.playbackService.PlaybackResumed += PlaybackService_PlaybackResumed;
            this.playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            this.playbackService.PlaybackStopped += PlaybackService_PlaybackStopped;

            _ActiveSubfolderChangedSubscriptionToken = this.eventAggregator.GetEvent<ActiveSubfolderChanged>().Subscribe(async (activeSubfolder) =>
            {
                await this.GetSubfoldersAsync(activeSubfolder as SubfolderViewModel);
            });
        }

        protected override void OnUnLoad()
        {
            this.foldersService.FoldersChanged -= FoldersService_FoldersChanged;
            this.playbackService.PlaybackFailed -= PlaybackService_PlaybackFailed;
            this.playbackService.PlaybackPaused -= PlaybackService_PlaybackPaused;
            this.playbackService.PlaybackResumed -= PlaybackService_PlaybackResumed;
            this.playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            this.playbackService.PlaybackStopped -= PlaybackService_PlaybackStopped;
            this.eventAggregator.GetEvent<ActiveSubfolderChanged>().Unsubscribe(_ActiveSubfolderChangedSubscriptionToken);
            base.OnUnLoad();
        }

        private async void PlaybackService_PlaybackStopped(object sender, EventArgs e)
        {
            await this.foldersService.SetPlayingSubFolderAsync(this.Subfolders);
        }

        private async void PlaybackService_PlaybackSuccess(object sender, PlaybackSuccessEventArgs e)
        {
            await this.foldersService.SetPlayingSubFolderAsync(this.Subfolders);
        }

        private async void PlaybackService_PlaybackResumed(object sender, EventArgs e)
        {
            await this.foldersService.SetPlayingSubFolderAsync(this.Subfolders);
        }

        private async void PlaybackService_PlaybackPaused(object sender, PlaybackPausedEventArgs e)
        {
            await this.foldersService.SetPlayingSubFolderAsync(this.Subfolders);
        }

        private async void PlaybackService_PlaybackFailed(object sender, PlaybackFailedEventArgs e)
        {
            await this.foldersService.SetPlayingSubFolderAsync(this.Subfolders);
        }



        private async void FoldersService_FoldersChanged(object sender, EventArgs e)
        {
            await this.FillListsAsync();
        }

        private void ClearFolders()
        {
            this.folders = null;
            this.Subfolders = null;
            this.SubfolderBreadCrumbs = null;
        }

        private async Task GetFoldersAsync()
        {
            this.Folders = new ObservableCollection<FolderViewModel>(await this.foldersService.GetFoldersAsync());
            FolderViewModel proposedSelectedFolder = await this.foldersService.GetSelectedFolderAsync();
            this.selectedFolder = this.Folders.Where(x => x.Equals(proposedSelectedFolder)).FirstOrDefault();
            this.RaisePropertyChanged(nameof(this.SelectedFolder));
        }

        private async Task GetSubfoldersAsync(SubfolderViewModel activeSubfolder)
        {
            this.Subfolders = null; // Required to correctly reset the selectedSubfolder
            this.SubfolderBreadCrumbs = null;
            this.activeSubfolderPath = string.Empty;

            if (this.selectedFolder != null)
            {
                this.Subfolders = new ObservableCollection<SubfolderViewModel>(await this.foldersService.GetSubfoldersAsync(this.selectedFolder, activeSubfolder));
                this.activeSubfolderPath = this.subfolders.Count > 0 && this.subfolders.Any(x => x.IsGoToParent) ? this.subfolders.Where(x => x.IsGoToParent).First().Path : this.selectedFolder.Path;
                this.SubfolderBreadCrumbs = new ObservableCollection<SubfolderBreadCrumbViewModel>(this.foldersService.GetSubfolderBreadCrumbs(this.selectedFolder, this.activeSubfolderPath));
                await this.GetTracksAsync();
                await this.foldersService.SetPlayingSubFolderAsync(this.Subfolders);
            }
        }

        private async Task GetTracksAsync()
        {
            IList<TrackViewModel> tracks = await this.fileService.ProcessFilesInDirectoryAsync(this.activeSubfolderPath);
            await this.GetTracksCommonAsync(tracks, TrackOrder.None);
        }

        protected async override Task FillListsAsync()
        {
            await this.GetFoldersAsync();
            await this.GetSubfoldersAsync(null);
        }

        protected async override Task EmptyListsAsync()
        {
            this.ClearFolders();
            this.ClearTracks();
        }
    }
}
