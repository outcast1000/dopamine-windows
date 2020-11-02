using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Data;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.File;
using Dopamine.Services.Playback;
using Dopamine.Services.Provider;
using Dopamine.ViewModels.Common.Base;
using GongSolutions.Wpf.DragDrop;
using Prism.Commands;
using Prism.Ioc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Dopamine.ViewModels.Common
{
    public class PlaylistControlViewModel : TracksViewModelBase, IDropTarget
    {
        private IPlaybackService playbackService;
        private IDialogService dialogService;
        private IFileService fileService;
        private IProviderService providerService;

        protected bool isDroppingTracks;
        public DelegateCommand ShufflePlaylistCommand { get; set; }


        public PlaylistControlViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            this.playbackService = container.Resolve<IPlaybackService>();
            this.dialogService = container.Resolve<IDialogService>();
            this.fileService = container.Resolve<IFileService>();
            this.providerService = container.Resolve<IProviderService>();

            this.playbackService.PlaybackSuccess += (_, __) => this.UpdateNowPlaying();
            this.playbackService.PlaylistChanged += (_, __) => this.UpdateNowPlaying();
            this.playbackService.PlaylistPositionChanged += (_, __) => this.UpdateNowPlaying();

            // Commands
            this.RemoveSelectedTracksCommand = new DelegateCommand(async () => await RemoveSelectedTracksFromNowPlayingAsync());

            ShufflePlaylistCommand = new DelegateCommand(async () => await ShufflePlaylistAsync());
        }

        private void UpdateNowPlaying()
        {
            GetTracksAsync();
        }

        private async Task ShufflePlaylistAsync()
        {
            await playbackService.RandomizePlaylistAsync();
        }

        private ObservableCollection<PlaylistItem> _playlistItems;

        protected async Task GetTracksAsync()
        {
            _playlistItems = new ObservableCollection<PlaylistItem>(playbackService.PlaylistItems);
            RefreshView();
        }

        private CollectionViewSource tracksCvs;

        public CollectionViewSource TracksCvs
        {
            get { return this.tracksCvs; }
            set { SetProperty<CollectionViewSource>(ref this.tracksCvs, value); }
        }

        private void RefreshView()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Populate CollectionViewSource

                this.TracksCvs = new CollectionViewSource { Source = _playlistItems };
                // Update count
                this.TracksCount = _playlistItems.Count;


                this.CalculateSizeInformationAsync(this.TracksCvs);
            });

            // Update duration and size

            // Show playing Track
        }

        private IList<PlaylistItem> selectedTracks;

        public IList<PlaylistItem> SelectedTracks
        {
            get { return this.selectedTracks; }
            set { SetProperty<IList<PlaylistItem>>(ref this.selectedTracks, value); }
        }

        protected override void SelectedTracksHandler(object parameter)
        {
            if (parameter != null)
            {
                this.SelectedTracks = new List<PlaylistItem>();

                foreach (PlaylistItem item in (IList)parameter)
                {
                    this.SelectedTracks.Add(item);
                }
            }
        }

        protected async void CalculateSizeInformationAsync(CollectionViewSource source)
        {
            if (source == null)
            {
                this.SetSizeInformation(0, 0);
                return;
            }
            IList<PlaylistItem> vmList = (IList<PlaylistItem>)source.Source;
            await Task.Run(() =>
            {
                long totalDuration = vmList.Select(x => x.TrackViewModel.Data.Duration.HasValue ? x.TrackViewModel.Data.Duration.Value : 0).Sum();
                long totalSize = vmList.Select(x => x.TrackViewModel.Data.FileSize.HasValue ? x.TrackViewModel.Data.FileSize.Value : 0).Sum();
                SetSizeInformation(totalDuration, totalSize);
            });

            RaisePropertyChanged(nameof(this.TotalDurationInformation));
            RaisePropertyChanged(nameof(this.TotalSizeInformation));
            RaisePropertyChanged(nameof(this.TotalTracksInformation));
        }

        protected override async void FilterListsAsync(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                InSearchMode = false;
                await GetTracksAsync();
            }
            else
            {
                InSearchMode = true;
                base.FilterListsAsync(searchText);
            }
        }

        public bool InSearchMode { get; set; }

        protected override async Task FillListsAsync()
        {
            // Not implemented here. We use our own LoadedCommandAsync here, because we need our own delay and tracks source.
        }

        protected async override Task EmptyListsAsync()
        {
            this.ClearTracks();
        }

        protected async override Task LoadedCommandAsync()
        {
            // Wait for the UI to slide in
            await Task.Delay(Constants.NowPlayingListLoadDelay);  

            // If there is a queue, get the tracks.
            if (this.playbackService.HasQueue)
            {
                await this.GetTracksAsync();
            }

            // Listen to queue changes.
            this.playbackService.PlaylistChanged += async (_, __) =>
            {
                if (!this.isDroppingTracks)
                {
                    await this.GetTracksAsync();
                }
            };
        }

        public void DragOver(IDropInfo dropInfo)
        {
            DragDrop.DefaultDropHandler.DragOver(dropInfo);

            try
            {
                // We don't allow dragging playlists
                if (dropInfo.Data is PlaylistViewModel) return;

                // If we're dragging files, we need to be dragging valid files.
                bool isDraggingFiles = dropInfo.IsDraggingFilesOrDirectories();
                bool isDraggingValidFiles = false;
                if (isDraggingFiles) isDraggingValidFiles = dropInfo.IsDraggingMediaFiles();
                if (isDraggingFiles & !isDraggingValidFiles) return;

                // In all other cases, allow dragging.
                GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.DragOver(dropInfo);
                dropInfo.NotHandled = true;
            }
            catch (Exception ex)
            {
                dropInfo.NotHandled = false;
                LogClient.Error("Could not drag tracks. Exception: {0}", ex.Message);
            }
        }

        private async Task UpdateQueueOrderAsync(IDropInfo dropInfo)
        {
            this.isDroppingTracks = true;

            var droppedTracks = new List<PlaylistItem>();

            // TargetCollection contains all tracks of the queue, in the new order.
            foreach (var item in dropInfo.TargetCollection)
            {
                droppedTracks.Add((PlaylistItem)item);
            }

            await this.playbackService.UpdateQueueOrderAsync(droppedTracks);

            this.isDroppingTracks = false;
        }

        public async void Drop(IDropInfo dropInfo)
        {
            try
            {
                if (dropInfo.IsDraggingFilesOrDirectories())
                {
                    if (dropInfo.IsDraggingMediaFiles())
                    {
                        await this.AddDroppedFilesToQueue(dropInfo);
                    }
                }
                else
                {
                    DragDrop.DefaultDropHandler.Drop(dropInfo); // Automatically performs built-in reorder
                    await this.UpdateQueueOrderAsync(dropInfo);
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not perform drop. Exception: {0}", ex.Message);
            }
        }

        private async Task AddDroppedFilesToQueue(IDropInfo dropInfo)
        {
            try
            {
                IList<string> filenames = dropInfo.GetDroppedFilenames();
                IList<TrackViewModel> tracks = await this.fileService.ProcessFilesAsync(filenames, true);
                await this.playbackService.PlayTracksAsync(tracks, PlaylistMode.Enqueue);
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not add dropped files to playback queue. Exception: {0}", ex.Message);
            }
        }

        public bool IsMultipleItemsSelected
        {
            get { return selectedTracks?.Count > 1; }
        }

        public bool IsSingleItemSelected
        {
            get { return selectedTracks?.Count == 1; }
        }

        private async Task RemoveSelectedTracksFromNowPlayingAsync()
        {
            // Remove Tracks from PlaybackService (this dequeues the Tracks)
            IList<PlaylistItem> selectedTracks = this.SelectedTracks;
            bool bSuccess = await this.playbackService.RemovePlaylistItems(selectedTracks);

            if (!bSuccess)
            {
                this.dialogService.ShowNotification(
                     0xe711,
                     16,
                     ResourceUtils.GetString("Language_Error"),
                     ResourceUtils.GetString("Language_Error_Removing_From_Now_Playing"),
                     ResourceUtils.GetString("Language_Ok"),
                     true,
                     ResourceUtils.GetString("Language_Log_File"));
            }

            // An event should be sent to refresh the view so this code will be disabled
            /*
            // Remove the ViewModels from Tracks (this updates the UI)
            foreach (TrackViewModel track in selectedTracks)
            {
                if (this.Tracks.Contains(track))
                {
                    this.Tracks.Remove(track);
                }
            }

            this.TracksCount = this.Tracks.Count;
            */
        }

        protected override void ShowSelectedTrackInformation()
        {
            // Don't try to show the file information when nothing is selected
            if (this.SelectedTracks == null || this.SelectedTracks.Count == 0) return;

            this.ShowFileInformation(this.SelectedTracks.Select(t => t.TrackViewModel.Path).ToList());
        }

        protected override void EditSelectedTracks()
        {
            if (this.SelectedTracks == null || this.SelectedTracks.Count == 0) return;

            this.EditFiles(this.SelectedTracks.Select(t => t.TrackViewModel.Path).ToList());
        }

        protected override void SearchOnline(string id)
        {
            if (this.SelectedTracks != null && this.SelectedTracks.Count > 0)
            {
                this.providerService.SearchOnline(id, new string[] { this.SelectedTracks.First().TrackViewModel.ArtistName, this.SelectedTracks.First().TrackViewModel.TrackTitle });
            }
        }




    }
}
