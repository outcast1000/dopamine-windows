using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Core.Prism;
using Dopamine.Data;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.File;
using Dopamine.Services.Playback;
using Dopamine.Services.Provider;
using Dopamine.Services.Search;
using Dopamine.ViewModels.Common.Base;
using GongSolutions.Wpf.DragDrop;
using Prism.Commands;
using Prism.Events;
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
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IPlaybackService playbackService;
        private IDialogService dialogService;
        private IFileService fileService;
        private IProviderService providerService;
        private ISearchService searchService;
        private IEventAggregator eventAggregator;

        protected bool isDroppingTracks;
        public DelegateCommand ShufflePlaylistCommand { get; set; }
        public DelegateCommand ClearPlaylistCommand { get; set; }


        public PlaylistControlViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            this.playbackService = container.Resolve<IPlaybackService>();
            this.dialogService = container.Resolve<IDialogService>();
            this.fileService = container.Resolve<IFileService>();
            this.providerService = container.Resolve<IProviderService>();
            this.searchService = container.Resolve<ISearchService>();
            this.eventAggregator = container.Resolve<IEventAggregator>();


            // Commands
            this.RemoveSelectedTracksCommand = new DelegateCommand(async () => await RemoveSelectedTracksFromNowPlayingAsync());
            this.AddTracksToPlaylistCommand = new DelegateCommand<string>(async (playlistName) => await this.AddTracksToPlaylistAsync(playlistName, this.SelectedTracks.Select(t => t.TrackViewModel).ToList()));
            this.LocateTrackCommand = new DelegateCommand(async () =>
            {
                if (SelectedTracks.Count > 0)
                    await LocateTrack(SelectedTracks[0].TrackViewModel);
            });

            ShufflePlaylistCommand = new DelegateCommand(async () => await ShufflePlaylistAsync());
            ClearPlaylistCommand = new DelegateCommand(() => ClearPlaylist());
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            //this.playbackService.PlaybackSuccess += PlaybackService_PlaybackSuccess;
            this.playbackService.PlaylistChanged += PlaybackService_PlaylistChanged;
            this.playbackService.PlaylistPositionChanged += PlaybackService_PlaylistPositionChanged;
        }
        protected override void OnUnLoad()
        {
            //this.playbackService.PlaybackSuccess -= PlaybackService_PlaybackSuccess;
            this.playbackService.PlaylistChanged -= PlaybackService_PlaylistChanged;
            this.playbackService.PlaylistPositionChanged -= PlaybackService_PlaylistPositionChanged;
            base.OnUnLoad();
        }

        private async Task LocateTrack(TrackViewModel vm)
        {
            if (searchService.SearchText != "")
            {
                // Exit the search mode. 
                searchService.SearchText = "";
                // We will wait for the view to refill
                for (int i = 0; i < 20; i++)
                {
                    NLog.LogManager.GetLogger("DEBUG").Info("Waiting to send locate message...");
                    await Task.Delay(100);
                    if (SelectedTracks.Count == 0) // This is when the list has been refreshed
                        break;
                }
            }
            NLog.LogManager.GetLogger("DEBUG").Info("Sending locate message");
            eventAggregator.GetEvent<LocateItem<TrackViewModel>>().Publish(vm);
        }

        private void PlaybackService_PlaylistPositionChanged(object sender, EventArgs e)
        {
            this.UpdateNowPlaying();
        }

        private void PlaybackService_PlaylistChanged(object sender, EventArgs e)
        {
            this.UpdateNowPlaying();
        }


        private async void UpdateNowPlaying()
        {
            await GetTracksAsync();
        }

        private async Task ShufflePlaylistAsync()
        {
            await playbackService.RandomizePlaylistAsync();
        }
        private void ClearPlaylist()
        {
            playbackService.ClearPlaylist();
        }
        
        private ObservableCollection<PlaylistItem> _playlistItems;

        protected async Task GetTracksAsync()
        {
            if (!this.isDroppingTracks)
            {
                _playlistItems = new ObservableCollection<PlaylistItem>(playbackService.PlaylistItems);
                RefreshView();
            }
        }

        private CollectionViewSource tracksCvs;

        public CollectionViewSource TracksCvs
        {
            get { return this.tracksCvs; }
            set { SetProperty<CollectionViewSource>(ref this.tracksCvs, value); }
        }

        public bool IsPlaylistEmpty => _playlistItems.IsNullOrEmpty();


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
            RaisePropertyChanged(nameof(this.IsPlaylistEmpty));


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
                RaisePropertyChanged(nameof(this.IsSingleItemSelected));
                RaisePropertyChanged(nameof(this.IsMultipleItemsSelected));
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

        protected override async Task FilterListsAsync(string searchText)
        {
            // Not implemented here. Maybe we should stop inheriting this
        }

        protected override async Task FillListsAsync()
        {
            // Not implemented here. Maybe we should stop inheriting this
        }

        protected async override Task EmptyListsAsync()
        {
            // Not implemented here. Maybe we should stop inheriting this
            /*await Task.Run(() =>
            {
                this.ClearTracks();
            });*/
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
