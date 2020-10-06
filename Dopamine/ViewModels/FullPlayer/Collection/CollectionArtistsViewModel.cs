using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Alex; //Digimezzo.Foundation.Core.Settings
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Prism;
using Dopamine.Data;
using Dopamine.Services.Collection;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.Playback;
using Dopamine.Services.Playlist;
using Dopamine.Services.Search;
using Dopamine.Services.Utils;
using Dopamine.ViewModels.Common.Base;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.ComponentModel;
using System.Diagnostics;

/* ALEX COMMENT
--- MAP OF VARIOUS EVENTS THAT TRIGGERS Data Refresh

[XAML] SelectionChanged (CollectionArtists.xaml)
	-> SelectedArtistsCommand 
        -> SelectedArtistsHandlerAsync(IList<ArtistViewModel>)
			
[XAML] Loaded (Event) (CollectionArtists.xaml)
	-> CommonViewModelBase::LoadedCommand
		-> LoadedCommandAsync
			-> FillListsAsync
	
CollectionService.CollectionChanged += async (_, __) => await this.FillListsAsync(); // Refreshes the lists when the Collection has changed
FoldersService.FoldersChanged += async (_, __) => await this.FillListsAsync(); // Refreshes the lists when marked folders have changed
IndexingService.RefreshLists += async (_, __) => await this.FillListsAsync(); // Refreshes the lists when the indexer has finished indexing
IndexingService.AlbumImagesAdded += async (_, __) => await this.FillListsAsync(); // Refreshes the lists when the indexer has finished indexing
IndexingService.ArtistImagesAdded += async (_, __) => await this.FillListsAsync(); // Refreshes the lists when the indexer has finished indexing
TracksViewModelBase::MetadataChangedHandlerAsync -> FillListsAsync
CommonViewModelBase:: LoadedCommand
	-> TracksViewModelBase::LoadedCommandAsync -> FillListsAsync
TracksViewModelBase::RefreshLanguage -> FillListsAsync

CommonViewModelBase::UnloadedCommand
	-> TracksViewModelBase::UnloadedCommandAsync -> EmptyListsAsync


---- LOCAL FUNCTION THA TRIGGER Data refresh

FillListsAsync()
	-> GetArtistsAsync (Triggers SelectionChanged)
	-> if (selectedArtists.Count == 0) GetArtistAlbumsAsync(this.SelectedArtists, this.AlbumOrder);
	-> if GetTracksAsync(this.SelectedArtists, null, this.SelectedAlbums, this.TrackOrder);

SelectedArtistsHandlerAsync(IList<ArtistViewModel>)
	-> RaisePropertyChanged("HasSelectedArtists")
	-> AlbumsViewModelBase::GetArtistAlbumsAsync(IList<ArtistViewModel> selectedArtists, this.AlbumOrder);
	-> TracksViewModelBase::GetTracksAsync(IList<ArtistViewModel> this.SelectedArtists, IList<GenreViewModel> null, IList<AlbumViewModel> this.SelectedAlbums, this.TrackOrder);

	
EmptyListsAsync


 */

namespace Dopamine.ViewModels.FullPlayer.Collection
{
    public class CollectionArtistsViewModel : AlbumsViewModelBase, ISemanticZoomViewModel
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ICollectionService collectionService;
        private IPlaybackService playbackService;
        private IPlaylistService playlistService;
        private IIndexingService indexingService;
        private IDialogService dialogService;
        private IEventAggregator eventAggregator;
        private CollectionViewSource collectionViewSource;
        private IList<ArtistViewModel> selectedItems = new List<ArtistViewModel>();
        private ObservableCollection<ISemanticZoomSelector> zoomSelectors;
        private bool isZoomVisible;
        private long itemCount;
        private double leftPaneWidthPercent;
        private double rightPaneWidthPercent;
        private IList<long> selectedIDs;
        private bool _ignoreSelectionChangedEvent;
        private string _searchString = "";
        private string orderText;
        private ArtistOrder order;




        public delegate void EnsureSelectedItemVisibleAction(ArtistViewModel artist);
        public event EnsureSelectedItemVisibleAction EnsureItemVisible;

        public DelegateCommand ToggleArtistOrderCommand { get; set; }


        public DelegateCommand<string> AddArtistsToPlaylistCommand { get; set; }


        public DelegateCommand<object> SelectedArtistsCommand { get; set; }

        public DelegateCommand ShowArtistsZoomCommand { get; set; }

        public DelegateCommand<string> SemanticJumpCommand { get; set; }

        public DelegateCommand AddArtistsToNowPlayingCommand { get; set; }

        public DelegateCommand ShuffleSelectedArtistsCommand { get; set; }

        public DelegateCommand<ArtistViewModel> PlayArtistCommand { get; set; }
        public DelegateCommand<ArtistViewModel> EnqueueArtistCommand { get; set; }
        public DelegateCommand<ArtistViewModel> LoveArtistCommand { get; set; }

        public double LeftPaneWidthPercent
        {
            get { return this.leftPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref this.leftPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", "ArtistsLeftPaneWidthPercent", Convert.ToInt32(value));
            }
        }

        public double RightPaneWidthPercent
        {
            get { return this.rightPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref this.rightPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", "ArtistsRightPaneWidthPercent", Convert.ToInt32(value));
            }
        }

        public CollectionViewSource ArtistsCvs
        {
            get { return this.collectionViewSource; }
            set { SetProperty<CollectionViewSource>(ref this.collectionViewSource, value); }
        }

        public IList<ArtistViewModel> SelectedArtists
        {
            get { return this.selectedItems; }
            set { SetProperty<IList<ArtistViewModel>>(ref this.selectedItems, value); }
        }

        public ArtistOrder ArtistOrder
        {
            get { return this.order; }
            set
            {
                SetProperty<ArtistOrder>(ref this.order, value);

                this.UpdateArtistOrderText(value);
            }
        }

        public long ArtistsCount
        {
            get { return this.itemCount; }
            set { SetProperty<long>(ref this.itemCount, value); }
        }

        public bool IsArtistsZoomVisible
        {
            get { return this.isZoomVisible; }
            set { SetProperty<bool>(ref this.isZoomVisible, value); }
        }

        public string ArtistOrderText => this.orderText;

        public ObservableCollection<ISemanticZoomSelector> ArtistsZoomSelectors
        {
            get { return this.zoomSelectors; }
            set { SetProperty<ObservableCollection<ISemanticZoomSelector>>(ref this.zoomSelectors, value); }
        }
        ObservableCollection<ISemanticZoomSelector> ISemanticZoomViewModel.SemanticZoomSelectors
        {
            get { return ArtistsZoomSelectors; }
            set { ArtistsZoomSelectors = value; }
        }

        public bool HasSelectedArtists
        {
            get
            {
                return (this.SelectedArtists?.Count > 0);
            }
        }

        public CollectionArtistsViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            this.collectionService = container.Resolve<ICollectionService>();
            this.playbackService = container.Resolve<IPlaybackService>();
            this.playlistService = container.Resolve<IPlaylistService>();
            this.indexingService = container.Resolve<IIndexingService>();
            this.dialogService = container.Resolve<IDialogService>();
            this.eventAggregator = container.Resolve<IEventAggregator>();

            // Commands
            this.ToggleTrackOrderCommand = new DelegateCommand(async () => await this.ToggleTrackOrderAsync());
            this.ToggleAlbumOrderCommand = new DelegateCommand(async () => await this.ToggleAlbumOrderAsync());
            this.ToggleArtistOrderCommand = new DelegateCommand(async () => await this.ToggleOrderAsync());
            this.RemoveSelectedTracksCommand = new DelegateCommand(async () => await this.RemoveTracksFromCollectionAsync(this.SelectedTracks), () => !this.IsIndexing);
            this.AddArtistsToPlaylistCommand = new DelegateCommand<string>(async (playlistName) => await this.AddItemsToPlaylistAsync(this.SelectedArtists, playlistName));
            this.SelectedArtistsCommand = new DelegateCommand<object>(async (parameter) => await this.SelectedItemsHandlerAsync(parameter));
            this.ShowArtistsZoomCommand = new DelegateCommand(async () => await this.ShowSemanticZoomAsync());
            this.AddArtistsToNowPlayingCommand = new DelegateCommand(async () => await this.AddItemsToNowPlayingAsync(this.SelectedArtists));
            this.ShuffleSelectedArtistsCommand = new DelegateCommand(async () =>
            {
                await this.playbackService.PlayArtistsAsync(SelectedArtists, PlaylistMode.Play, true);
            });
            this.PlayArtistCommand = new DelegateCommand<ArtistViewModel>(async (vm) => {
                await this.playbackService.PlayArtistsAsync(new List<ArtistViewModel>() { vm }, PlaylistMode.Play);
            });
            this.EnqueueArtistCommand = new DelegateCommand<ArtistViewModel>(async (vm) => await this.playbackService.PlayArtistsAsync(new List<ArtistViewModel>() { vm }, PlaylistMode.Enqueue));
            this.LoveArtistCommand = new DelegateCommand<ArtistViewModel>((avm) => Debug.Assert(false, "ALEX TODO"));



            this.SemanticJumpCommand = new DelegateCommand<string>((header) =>
            {
                this.HideSemanticZoom();
                this.eventAggregator.GetEvent<PerformSemanticJump>().Publish(new Tuple<string, string>("Artists", header));
            });

            // Settings
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += async (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
                {
                    this.EnableRating = (bool)e.Entry.Value;
                    this.SetTrackOrder("ArtistsTrackOrder");
                    await this.GetTracksAsync(this.SelectedArtists, null, this.SelectedAlbums, this.TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
                {
                    this.EnableLove = (bool)e.Entry.Value;
                    this.SetTrackOrder("ArtistsTrackOrder");
                    await this.GetTracksAsync(this.SelectedArtists, null, this.SelectedAlbums, this.TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "State", "SelectedArtistIDs"))
                {
                    LoadSelectedItems();
                }

            };

            // PubSub Events
            this.eventAggregator.GetEvent<ShellMouseUp>().Subscribe((_) => this.IsArtistsZoomVisible = false);

            // Set the initial AlbumOrder
            this.AlbumOrder = (AlbumOrder)SettingsClient.Get<int>("Ordering", "ArtistsAlbumOrder");

            // ALEX WARNING. EVERYTIME YOU NEED TO ADD A NEW SETTING YOU HAVE TO:
            //  1. Update the \BaseSettings.xml of the project
            //  2. Update the  C:\Users\Alex\AppData\Roaming\Dopamine\Settings.xml
            this.ArtistOrder = (ArtistOrder)SettingsClient.Get<int>("Ordering", "ArtistsArtistOrder");

            // Set the initial TrackOrder
            this.SetTrackOrder("ArtistsTrackOrder");

            // Set width of the panels
            this.LeftPaneWidthPercent = SettingsClient.Get<int>("ColumnWidths", "ArtistsLeftPaneWidthPercent");
            this.RightPaneWidthPercent = SettingsClient.Get<int>("ColumnWidths", "ArtistsRightPaneWidthPercent");

            // Cover size
            this.SetCoversizeAsync((CoverSizeType)SettingsClient.Get<int>("CoverSizes", "ArtistsCoverSize"));
            LoadSelectedItems();

        }

        private void LoadSelectedItems()
        {
            try
            {
                string s = SettingsClient.Get<String>("State", "SelectedArtistIDs");
                if (!string.IsNullOrEmpty(s))
                {
                    selectedIDs = s.Split(',').Select(x => long.Parse(x)).ToList();
                    return;
                }
            }
            catch (Exception _)
            {

            }
            selectedIDs = new List<long>();
        }

        private void SaveSelectedItems()
        {
            string s = string.Join(",", selectedIDs);// SettingsClient.Get<String>("State", "SelectedArtistIDs");
            SettingsClient.Set<String>("State", "SelectedArtistIDs", s);
        }

        public async Task ShowSemanticZoomAsync()
        {
            this.ArtistsZoomSelectors = await SemanticZoomUtils.UpdateSemanticZoomSelectors(this.ArtistsCvs.View);
            this.IsArtistsZoomVisible = true;
        }

        public void HideSemanticZoom()
        {

            this.IsArtistsZoomVisible = false;
        }

        public void UpdateSemanticZoomHeaders()
        {
            string previousHeader = string.Empty;

            foreach (ArtistViewModel vm in this.ArtistsCvs.View)
            {
                if (order == ArtistOrder.AlphabeticalAscending || order == ArtistOrder.AlphabeticalDescending)
                {
                    if (string.IsNullOrEmpty(previousHeader) || !vm.Header.Equals(previousHeader))
                    {
                        previousHeader = vm.Header;
                        vm.IsHeader = true;
                    }
                    else
                    {
                        vm.IsHeader = false;
                    }
                }
                else
                {
                    vm.IsHeader = false;
                }
            }
        }

        private void ClearItems()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.ArtistsCvs = null;
            });

        }


        private async Task GetItemsAsync()
        {
            ObservableCollection<ISemanticZoomable> items;
            try
            {
                // Get the viewModels
                var viewModels = new ObservableCollection<ArtistViewModel>(await this.collectionService.GetArtistsAsync(_searchString));
                // Unless we are in Search Mode, we should re-store the selected items. The cases are:
                //  1. at the beginning of the application
                //  2. after the search mode is finished 
                if (string.IsNullOrEmpty(_searchString))
                {
                    selectedItems = new List<ArtistViewModel>();
                    foreach (long id in selectedIDs)
                    {
                        ArtistViewModel avm = viewModels.Where(x => x.Id == id).FirstOrDefault();
                        if (avm != null)
                        {
                            avm.IsSelected = selectedIDs.Contains(avm.Id);
                            selectedItems.Add(avm);
                        }
                    }
                }
                items = new ObservableCollection<ISemanticZoomable>(viewModels);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while getting Artists. Exception: {0}", ex.Message);
                // Failed getting Artists. Create empty ObservableCollection.
                items = new ObservableCollection<ISemanticZoomable>();
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Populate CollectionViewSource
                this.ArtistsCvs = new CollectionViewSource { Source = items };
                OrderItems();
                EnsureVisible();
                this.ArtistsCount = ArtistsCvs.View.Cast<ISemanticZoomable>().Count();
            });
        }

        private void OrderItems()
        {
            SortDescription sd = new SortDescription();
            switch (order)
            {
                case ArtistOrder.AlphabeticalAscending:
                    sd = new SortDescription("Name", ListSortDirection.Ascending);
                    break;
                case ArtistOrder.AlphabeticalDescending:
                    sd = new SortDescription("Name", ListSortDirection.Descending);
                    break;
                case ArtistOrder.ByTrackCount:
                    sd = new SortDescription("TrackCount", ListSortDirection.Descending);
                    break;
                case ArtistOrder.ByDateAdded:
                    sd = new SortDescription("DateAdded", ListSortDirection.Descending);
                    break;
                case ArtistOrder.ByDateCreated:
                    sd = new SortDescription("DateCreated", ListSortDirection.Descending);
                    break;
                case ArtistOrder.ByYearAscending:
                    sd = new SortDescription("Year", ListSortDirection.Ascending);
                    break;
                case ArtistOrder.ByYearDescending:
                    sd = new SortDescription("Year", ListSortDirection.Descending);
                    break;
                default:
                    break;
            }
            ArtistsCvs.SortDescriptions.Clear();
            ArtistsCvs.SortDescriptions.Add(sd);
            this.UpdateSemanticZoomHeaders();
        }

        private async Task SelectedItemsHandlerAsync(object parameter)
        {
            // This happens when the user select an item
            // We should ignore this event when for example we are just refreshing the collection (app is starting)
            if (_ignoreSelectionChangedEvent)
                return;
            // We should also ignore it if we are in Search Mode AND the user does not selected anything. For example when we enter the search mode
            if (!string.IsNullOrEmpty(_searchString) && ((IList)parameter).Count == 0)
                return;
            // We should also ignore it if we have an empty list (for example when we clear the list)
            if (ArtistsCvs == null)
                return;
            bool bKeepOldSelections = true;
            if (parameter != null && ((IList)parameter).Count > 0)
            {
                // This is the most usual case. The user has just selected one or more items
                bKeepOldSelections = false;
                selectedIDs.Clear();
                selectedItems.Clear();
                foreach (ArtistViewModel item in (IList)parameter)
                {
                    // Keep them in an array
                    selectedIDs.Add(item.Id);
                    selectedItems.Add(item);
                    // Mark it as selected
                    item.IsSelected = true;
                }
            }
            
            if (bKeepOldSelections)
            {
                // Keep the previous selection if possible. Otherwise select All
                // This is the case when we have refresh the collection etc.
                List<long> validSelectedArtistIDs = new List<long>();
                selectedItems.Clear();
                IEnumerable<ArtistViewModel> artists = ArtistsCvs.View.Cast<ArtistViewModel>();
                foreach (long id in selectedIDs)
                {
                    ArtistViewModel sel = artists.Where(x => x.Id == id).FirstOrDefault();
                    if (sel != null)
                    {
                        validSelectedArtistIDs.Add(id);
                        sel.IsSelected = true;
                        selectedItems.Add(sel);
                    }
                }
                selectedIDs = validSelectedArtistIDs;

            }

            this.RaisePropertyChanged(nameof(this.HasSelectedArtists));
            Task saveSelectedArtists = Task.Run(() => SaveSelectedItems());
            // Update the albums
            Task albums = GetArtistAlbumsAsync(selectedItems, this.AlbumOrder);
            // Update the tracks
            this.SetTrackOrder("ArtistsTrackOrder");
            Task tracks = GetTracksAsync(this.SelectedArtists, null, this.SelectedAlbums, this.TrackOrder);
            Task.WhenAll(albums, tracks, saveSelectedArtists);

        }

        private async Task AddItemsToPlaylistAsync(IList<ArtistViewModel> artists, string playlistName)
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
                    AddTracksToPlaylistResult result = await this.playlistService.AddArtistsToStaticPlaylistAsync(artists, playlistName);

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

        private async Task AddItemsToNowPlayingAsync(IList<ArtistViewModel> items)
        {
            await this.playbackService.PlayArtistsAsync(items, PlaylistMode.Enqueue);
        }

        private async Task ToggleTrackOrderAsync()
        {
            base.ToggleTrackOrder();

            SettingsClient.Set<int>("Ordering", "ArtistsTrackOrder", (int)this.TrackOrder);
            await this.GetTracksCommonAsync(this.Tracks, this.TrackOrder);
        }

        private async Task ToggleAlbumOrderAsync()
        {

            base.ToggleAlbumOrder();

            SettingsClient.Set<int>("Ordering", "ArtistsAlbumOrder", (int)this.AlbumOrder);
            await this.GetAlbumsCommonAsync(this.Albums, this.AlbumOrder);
        }

        private async Task ToggleOrderAsync()
        {

            ToggleArtistOrder();
            SettingsClient.Set<int>("Ordering", "ArtistsArtistOrder", (int)this.ArtistOrder);
            OrderItems();
            EnsureVisible();
        }

        protected async override Task SetCoversizeAsync(CoverSizeType coverSize)
        {
            await base.SetCoversizeAsync(coverSize);
            SettingsClient.Set<int>("CoverSizes", "ArtistsCoverSize", (int)coverSize);
        }

        private void EnsureVisible()
        {
            if (SelectedArtists.Count > 0)
                EnsureItemVisible?.Invoke(SelectedArtists[0]);
        }

        protected async override Task FillListsAsync()
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {

                _ignoreSelectionChangedEvent = true;
	            await this.GetItemsAsync();
                await GetArtistAlbumsAsync(this.SelectedArtists, this.AlbumOrder);
                await GetTracksAsync(this.SelectedArtists, null, this.SelectedAlbums, this.TrackOrder);
                _ignoreSelectionChangedEvent = false;
                /*
                List<Task> tasks = new List<Task>();
                tasks.Add(GetArtistsAsync(ArtistType));
                tasks.Add(GetArtistAlbumsAsync(this.SelectedArtists, this.ArtistType, this.AlbumOrder));
                tasks.Add(GetTracksAsync(this.SelectedArtists, null, this.SelectedAlbums, this.TrackOrder));
                Task.WhenAll(tasks.ToArray());
                */
            });
            
        }

        protected async override Task EmptyListsAsync()
        {
            this.ClearItems();
            this.ClearAlbums();
            this.ClearTracks();
        }

        protected override void FilterLists(string searchText)
        {
            if (!_searchString.Equals(searchText))
            {
                _searchString = searchText;
                GetItemsAsync();
            }
            if (!string.IsNullOrEmpty(searchText))
                base.FilterLists(searchText);
        }

        protected async override Task SelectedAlbumsHandlerAsync(object parameter)
        {
            await base.SelectedAlbumsHandlerAsync(parameter);

            this.SetTrackOrder("ArtistsTrackOrder");
            await this.GetTracksAsync(this.SelectedArtists, null, this.SelectedAlbums, this.TrackOrder);
        }

        protected override void RefreshLanguage()
        {
            this.UpdateArtistOrderText(this.ArtistOrder);
            this.UpdateAlbumOrderText(this.AlbumOrder);
            this.UpdateTrackOrderText(this.TrackOrder);
            base.RefreshLanguage();
        }

        protected virtual void ToggleArtistOrder()
        {
            switch (this.order)
            {
                case ArtistOrder.AlphabeticalAscending:
                    this.ArtistOrder = ArtistOrder.AlphabeticalDescending;
                    break;
                case ArtistOrder.AlphabeticalDescending:
                    this.ArtistOrder = ArtistOrder.ByDateAdded;
                    break;
                case ArtistOrder.ByDateAdded:
                    this.ArtistOrder = ArtistOrder.ByDateCreated;
                    break;
                case ArtistOrder.ByDateCreated:
                    this.ArtistOrder = ArtistOrder.ByTrackCount;
                    break;
                case ArtistOrder.ByTrackCount:
                    this.ArtistOrder = ArtistOrder.ByYearAscending;
                    break;
                case ArtistOrder.ByYearAscending:
                    this.ArtistOrder = ArtistOrder.ByYearDescending;
                    break;
                case ArtistOrder.ByYearDescending:
                    this.ArtistOrder = ArtistOrder.AlphabeticalAscending;
                    break;
                default:
                    // Cannot happen, but just in case.
                    this.ArtistOrder = ArtistOrder.AlphabeticalAscending;
                    break;
            }
        }
        protected void UpdateArtistOrderText(ArtistOrder order)
        {
            switch (order)
            {
                case ArtistOrder.AlphabeticalAscending:
                    this.orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
                case ArtistOrder.AlphabeticalDescending:
                    this.orderText = ResourceUtils.GetString("Language_Z_A");
                    break;
                case ArtistOrder.ByDateAdded:
                    this.orderText = ResourceUtils.GetString("Language_By_Date_Added");
                    break;
                case ArtistOrder.ByDateCreated:
                    this.orderText = ResourceUtils.GetString("Language_By_Date_Created");
                    break;
                case ArtistOrder.ByTrackCount:
                    this.orderText = ResourceUtils.GetString("Language_By_Track_Count");
                    break;
                case ArtistOrder.ByYearDescending:
                    this.orderText = ResourceUtils.GetString("Language_By_Year_Descending");
                    break;
                case ArtistOrder.ByYearAscending:
                    this.orderText = ResourceUtils.GetString("Language_By_Year_Ascending");
                    break;
                default:
                    // Cannot happen, but just in case.
                    this.orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
            }

            RaisePropertyChanged(nameof(this.ArtistOrderText));
        }
    }
}
