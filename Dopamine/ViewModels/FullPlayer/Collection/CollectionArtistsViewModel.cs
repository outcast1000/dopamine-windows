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
using Dopamine.Services.Provider;

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
    public class CollectionArtistsViewModel : TracksViewModelBase, ISemanticZoomViewModel
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ICollectionService _collectionService;
        private IPlaybackService _playbackService;
        private IPlaylistService _playlistService;
        private IIndexingService _indexingService;
        private IDialogService _dialogService;
        private IEventAggregator _eventAggregator;
        private IProviderService _providerService;
        private CollectionViewSource _collectionViewSource;
        private IList<ArtistViewModel> _selectedItems = new List<ArtistViewModel>();
        private ObservableCollection<ISemanticZoomSelector> _zoomSelectors;
        private bool _isZoomVisible;
        private long _itemCount;
        private double _leftPaneWidthPercent;
        private double _rightPaneWidthPercent;
        private IList<long> _selectedIDs;
        private bool _ignoreSelectionChangedEvent;
        private string _searchString = "";
        private string _orderText;
        private ArtistOrder _order;
        private readonly string Setting_LeftPaneWidthPercent = "ArtistsLeftPaneWidthPercent";
        private readonly string Setting_RightPaneWidthPercent = "ArtistsRightPaneWidthPercent";
        private ObservableCollection<SearchProvider> artistContextMenuSearchProviders;




        public delegate void EnsureSelectedItemVisibleAction(ArtistViewModel item);
        public event EnsureSelectedItemVisibleAction EnsureItemVisible;

        public delegate void SelectionChangedAction();
        public event SelectionChangedAction SelectionChanged;

        public DelegateCommand ToggleArtistOrderCommand { get; set; }


        public DelegateCommand<string> AddArtistsToPlaylistCommand { get; set; }


        public DelegateCommand<object> SelectedArtistsCommand { get; set; }

        public DelegateCommand ShowArtistsZoomCommand { get; set; }

        public DelegateCommand<string> SemanticJumpCommand { get; set; }

        public DelegateCommand AddArtistsToNowPlayingCommand { get; set; }

        public DelegateCommand ShuffleSelectedArtistsCommand { get; set; }

        public DelegateCommand<ArtistViewModel> DownloadImageArtistsCommand { get; set; }
        
        public DelegateCommand<ArtistViewModel> PlayArtistCommand { get; set; }

        public DelegateCommand<ArtistViewModel> EnqueueArtistCommand { get; set; }

        public DelegateCommand<ArtistViewModel> LoveArtistCommand { get; set; }


        public DelegateCommand<string> ArtistSearchOnlineCommand { get; set; }

        public ObservableCollection<SearchProvider> ArtistContextMenuSearchProviders
        {
            get { return this.artistContextMenuSearchProviders; }
            set
            {
                SetProperty<ObservableCollection<SearchProvider>>(ref this.artistContextMenuSearchProviders, value);
                RaisePropertyChanged(nameof(this.HasContextMenuSearchProviders));
            }
        }

        protected bool HasArtistContextMenuSearchProviders => this.ContextMenuSearchProviders != null && this.ContextMenuSearchProviders.Count > 0;

        private async void GetArtistsSearchProvidersAsync()
        {
            this.artistContextMenuSearchProviders = null;

            List<SearchProvider> providersList = await _providerService.GetSearchProvidersAsync(SearchProvider.ProviderType.Artist);
            var localProviders = new ObservableCollection<SearchProvider>();

            await Task.Run(() =>
            {
                foreach (SearchProvider vp in providersList)
                {
                    localProviders.Add(vp);
                }
            });

            this.artistContextMenuSearchProviders = localProviders;
        }

        private void ArtistSearchOnline(string id)
        {
            if (SelectedArtists?.Count > 0)
            {
                _providerService.SearchOnline(id, new string[] { this.SelectedArtists.First().Name });
            }
        }


        public double LeftPaneWidthPercent
        {
            get { return _leftPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref _leftPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", Setting_LeftPaneWidthPercent, Convert.ToInt32(value));
            }
        }


        public double RightPaneWidthPercent
        {
            get { return _rightPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref _rightPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", Setting_RightPaneWidthPercent, Convert.ToInt32(value));
            }
        }

        public bool InSearchMode { get { return !string.IsNullOrEmpty(_searchString); } }

        public CollectionViewSource ArtistsCvs
        {
            get { return _collectionViewSource; }
            set { SetProperty<CollectionViewSource>(ref _collectionViewSource, value); }
        }

        public IList<ArtistViewModel> SelectedArtists
        {
            get { return _selectedItems; }
            set { SetProperty<IList<ArtistViewModel>>(ref _selectedItems, value); }
        }

        public ArtistOrder ArtistOrder
        {
            get { return _order; }
            set
            {
                SetProperty<ArtistOrder>(ref _order, value);

                UpdateArtistOrderText(value);
            }
        }

        public long ArtistsCount
        {
            get { return _itemCount; }
            set { SetProperty<long>(ref _itemCount, value); }
        }

        public bool IsArtistsZoomVisible
        {
            get { return _isZoomVisible; }
            set { SetProperty<bool>(ref _isZoomVisible, value); }
        }

        public string ArtistOrderText => _orderText;

        public ObservableCollection<ISemanticZoomSelector> ArtistsZoomSelectors
        {
            get { return _zoomSelectors; }
            set { SetProperty<ObservableCollection<ISemanticZoomSelector>>(ref _zoomSelectors, value); }
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
                return (SelectedArtists?.Count > 0);
            }
        }

        public CollectionArtistsViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            _collectionService = container.Resolve<ICollectionService>();
            _playbackService = container.Resolve<IPlaybackService>();
            _playlistService = container.Resolve<IPlaylistService>();
            _indexingService = container.Resolve<IIndexingService>();
            _dialogService = container.Resolve<IDialogService>();
            _eventAggregator = container.Resolve<IEventAggregator>();
            _providerService = container.Resolve<IProviderService>();

            // Commands
            ToggleTrackOrderCommand = new DelegateCommand(async () => await ToggleTrackOrderAsync());
            ToggleArtistOrderCommand = new DelegateCommand(async () => await ToggleOrderAsync());
            RemoveSelectedTracksCommand = new DelegateCommand(async () => await RemoveTracksFromCollectionAsync(SelectedTracks), () => !IsIndexing);
            AddArtistsToPlaylistCommand = new DelegateCommand<string>(async (playlistName) => await AddItemsToPlaylistAsync(SelectedArtists, playlistName));
            SelectedArtistsCommand = new DelegateCommand<object>(async (parameter) => await SelectedItemsHandlerAsync(parameter));
            ShowArtistsZoomCommand = new DelegateCommand(async () => await ShowSemanticZoomAsync());
            AddArtistsToNowPlayingCommand = new DelegateCommand(async () => await AddItemsToNowPlayingAsync(SelectedArtists));
            ShuffleSelectedArtistsCommand = new DelegateCommand(async () =>
            {
                await _playbackService.PlayArtistsAsync(SelectedArtists, PlaylistMode.Play, true);
            });
            DownloadImageArtistsCommand = new DelegateCommand<ArtistViewModel>(async (artist) =>
            {
                await artist.RequestImageDownload(true, true);
            });
            PlayArtistCommand = new DelegateCommand<ArtistViewModel>(async (vm) => {
                await _playbackService.PlayArtistsAsync(new List<ArtistViewModel>() { vm }, PlaylistMode.Play);
            });
            EnqueueArtistCommand = new DelegateCommand<ArtistViewModel>(async (vm) => await _playbackService.PlayArtistsAsync(new List<ArtistViewModel>() { vm }, PlaylistMode.Enqueue));
            LoveArtistCommand = new DelegateCommand<ArtistViewModel>((avm) => Debug.Assert(false, "ALEX TODO"));

            _providerService.SearchProvidersChanged += (_, __) => { GetArtistsSearchProvidersAsync(); };
            this.GetArtistsSearchProvidersAsync();
            this.ArtistSearchOnlineCommand = new DelegateCommand<string>((id) => ArtistSearchOnline(id));


            SemanticJumpCommand = new DelegateCommand<string>((header) =>
            {
                HideSemanticZoom();
                _eventAggregator.GetEvent<PerformSemanticJump>().Publish(new Tuple<string, string>("Artists", header));
            });

            // Settings
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += async (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
                {
                    EnableRating = (bool)e.Entry.Value;
                    SetTrackOrder("ArtistsTrackOrder");
                    await GetTracksAsync(SelectedArtists, null, null, TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
                {
                    EnableLove = (bool)e.Entry.Value;
                    SetTrackOrder("ArtistsTrackOrder");
                    await GetTracksAsync(SelectedArtists, null, null, TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "State", "SelectedArtistIDs"))
                {
                    LoadSelectedItems();
                }

            };

            // PubSub Events
            _eventAggregator.GetEvent<ShellMouseUp>().Subscribe((_) => IsArtistsZoomVisible = false);

            // ALEX WARNING. EVERYTIME YOU NEED TO ADD A NEW SETTING YOU HAVE TO:
            //  1. Update the \BaseSettings.xml of the project
            //  2. Update the  C:\Users\Alex\AppData\Roaming\Dopamine\Settings.xml
            ArtistOrder = (ArtistOrder)SettingsClient.Get<int>("Ordering", "ArtistsArtistOrder");

            // Set the initial TrackOrder
            SetTrackOrder("ArtistsTrackOrder");

            // Set width of the panels
            LeftPaneWidthPercent = SettingsClient.Get<int>("ColumnWidths", "ArtistsLeftPaneWidthPercent");

            LoadSelectedItems();

        }

        private void LoadSelectedItems()
        {
            try
            {
                string s = SettingsClient.Get<String>("State", "SelectedArtistIDs");
                if (!string.IsNullOrEmpty(s))
                {
                    _selectedIDs = s.Split(',').Select(x => long.Parse(x)).ToList();
                    return;
                }
            }
            catch (Exception)
            {

            }
            _selectedIDs = new List<long>();
        }

        private void SaveSelectedItems()
        {
            string s = string.Join(",", _selectedIDs);
            SettingsClient.Set<String>("State", "SelectedArtistIDs", s);
        }

        public async Task ShowSemanticZoomAsync()
        {
            ArtistsZoomSelectors = await SemanticZoomUtils.UpdateSemanticZoomSelectors(ArtistsCvs.View);
            IsArtistsZoomVisible = true;
        }

        public void HideSemanticZoom()
        {

            IsArtistsZoomVisible = false;
        }

        public void UpdateSemanticZoomHeaders()
        {
            string previousHeader = string.Empty;

            foreach (ArtistViewModel vm in ArtistsCvs.View)
            {
                if (_order == ArtistOrder.AlphabeticalAscending || _order == ArtistOrder.AlphabeticalDescending)
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
                ArtistsCvs = null;
            });

        }


        private async Task GetItemsAsync()
        {
            ObservableCollection<ISemanticZoomable> items;
            try
            {
                // Get the viewModels
                var viewModels = new ObservableCollection<ArtistViewModel>(await _collectionService.GetArtistsAsync(true, _searchString));// Using history
                // Unless we are in Search Mode, we should re-store the selected items. The cases are:
                //  1. at the beginning of the application
                //  2. after the search mode is finished 
                if (string.IsNullOrEmpty(_searchString))
                {
                    _selectedItems = new List<ArtistViewModel>();
                    foreach (long id in _selectedIDs)
                    {
                        ArtistViewModel vm = viewModels.Where(x => x.Id == id).FirstOrDefault();
                        if (vm != null)
                        {
                            vm.IsSelected = _selectedIDs.Contains(vm.Id);
                            _selectedItems.Add(vm);
                        }
                    }
                    if (_selectedItems.Count == 0 && viewModels.Count > 0)
                    {
                        // This may happen when
                        //  1. The collection was previously empty
                        //  2. The collection with the previous selection has been removed
                        //  3. The previous selection has been removed and the collection has been refreshed
                        ArtistViewModel sel = viewModels[0];
                        sel.IsSelected = true;
                        _selectedItems.Add(sel);
                        _selectedIDs.Add(sel.Id);
                        SaveSelectedItems();
                    }
                }
                items = new ObservableCollection<ISemanticZoomable>(viewModels);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while getting Items. Exception: {0}", ex.Message);
                items = new ObservableCollection<ISemanticZoomable>();
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Populate CollectionViewSource
                ArtistsCvs = new CollectionViewSource { Source = items };
                OrderItems();
                EnsureVisible();
                ArtistsCount = ArtistsCvs.View.Cast<ISemanticZoomable>().Count();
            });
        }

        private void OrderItems()
        {
            SortDescription sd = new SortDescription();
            switch (_order)
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
            UpdateSemanticZoomHeaders();
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
                _selectedIDs.Clear();
                _selectedItems.Clear();
                foreach (ArtistViewModel item in (IList)parameter)
                {
                    _selectedIDs.Add(item.Id);
                    _selectedItems.Add(item);
                    // Mark it as selected
                    item.IsSelected = true;
                }
            }
            
            if (bKeepOldSelections)
            {
                // Keep the previous selection if possible. Otherwise select All
                // This is the case when we have refresh the collection etc.
                List<long> validSelectedIDs = new List<long>();
                _selectedItems.Clear();
                IEnumerable<ArtistViewModel> artists = ArtistsCvs.View.Cast<ArtistViewModel>();
                foreach (long id in _selectedIDs)
                {
                    ArtistViewModel sel = artists.Where(x => x.Id == id).FirstOrDefault();
                    if (sel != null)
                    {
                        validSelectedIDs.Add(id);
                        sel.IsSelected = true;
                        _selectedItems.Add(sel);
                    }
                }
                _selectedIDs = validSelectedIDs;

            }

            RaisePropertyChanged(nameof(HasSelectedArtists));
            Task saveSelection = Task.Run(() => SaveSelectedItems());
            // Update the tracks
            SetTrackOrder("ArtistsTrackOrder");
            Task tracks = GetTracksAsync(SelectedArtists, null, null, TrackOrder);
            await Task.WhenAll(tracks, saveSelection);
            SelectionChanged?.Invoke();

        }

        private async Task AddItemsToPlaylistAsync(IList<ArtistViewModel> artists, string playlistName)
        {
            CreateNewPlaylistResult addPlaylistResult = CreateNewPlaylistResult.Success; // Default Success

            // If no playlist is provided, first create one.
            if (playlistName == null)
            {
                var responseText = ResourceUtils.GetString("Language_New_Playlist");

                if (_dialogService.ShowInputDialog(
                    0xea37,
                    16,
                    ResourceUtils.GetString("Language_New_Playlist"),
                    ResourceUtils.GetString("Language_Enter_Name_For_Playlist"),
                    ResourceUtils.GetString("Language_Ok"),
                    ResourceUtils.GetString("Language_Cancel"),
                    ref responseText))
                {
                    playlistName = responseText;
                    addPlaylistResult = await _playlistService.CreateNewPlaylistAsync(new EditablePlaylistViewModel(playlistName, PlaylistType.Static));
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
                    AddTracksToPlaylistResult result = await _playlistService.AddArtistsToStaticPlaylistAsync(artists, playlistName);

                    if (result == AddTracksToPlaylistResult.Error)
                    {
                        _dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetString("Language_Error"), ResourceUtils.GetString("Language_Error_Adding_Songs_To_Playlist").Replace("{playlistname}", "\"" + playlistName + "\""), ResourceUtils.GetString("Language_Ok"), true, ResourceUtils.GetString("Language_Log_File"));
                    }
                    break;
                case CreateNewPlaylistResult.Error:
                    _dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Error_Adding_Playlist"),
                        ResourceUtils.GetString("Language_Ok"),
                        true,
                        ResourceUtils.GetString("Language_Log_File"));
                    break;
                case CreateNewPlaylistResult.Blank:
                    _dialogService.ShowNotification(
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
            await _playbackService.PlayArtistsAsync(items, PlaylistMode.Enqueue);
        }

        private async Task ToggleTrackOrderAsync()
        {
            base.ToggleTrackOrder();

            SettingsClient.Set<int>("Ordering", "ArtistsTrackOrder", (int)TrackOrder);
            await GetTracksCommonAsync(Tracks, TrackOrder);
        }

        private async Task ToggleOrderAsync()
        {
            ToggleArtistOrder();
            SettingsClient.Set<int>("Ordering", "ArtistsArtistOrder", (int)ArtistOrder);
            OrderItems();
            EnsureVisible();
        }

        private void EnsureVisible()
        {
            if (SelectedArtists.Count > 0)
                EnsureItemVisible?.Invoke(SelectedArtists[0]);
        }

        protected async override Task FillListsAsync()
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {

                _ignoreSelectionChangedEvent = true;
                if (string.IsNullOrEmpty(_searchString))
                {
                    await GetItemsAsync();
                    await GetTracksAsync(SelectedArtists, null, null, TrackOrder);
                }
                else
                {
                    FilterListsAsync(_searchString);
                }
                _ignoreSelectionChangedEvent = false;
            });
            
        }

        protected async override Task EmptyListsAsync()
        {
            ClearItems();
            ClearTracks();
        }

        protected override async void FilterListsAsync(string searchText)
        {
            if (!_searchString.Equals(searchText))
            {
                _searchString = searchText;
                await GetItemsAsync();
            }
            if (!string.IsNullOrEmpty(searchText))
                base.FilterListsAsync(searchText);
        }

        protected override void RefreshLanguage()
        {
            UpdateArtistOrderText(ArtistOrder);
            UpdateTrackOrderText(TrackOrder);
            base.RefreshLanguage();
        }

        protected virtual void ToggleArtistOrder()
        {
            switch (_order)
            {
                case ArtistOrder.AlphabeticalAscending:
                    ArtistOrder = ArtistOrder.AlphabeticalDescending;
                    break;
                case ArtistOrder.AlphabeticalDescending:
                    ArtistOrder = ArtistOrder.ByDateAdded;
                    break;
                case ArtistOrder.ByDateAdded:
                    ArtistOrder = ArtistOrder.ByDateCreated;
                    break;
                case ArtistOrder.ByDateCreated:
                    ArtistOrder = ArtistOrder.ByTrackCount;
                    break;
                case ArtistOrder.ByTrackCount:
                    ArtistOrder = ArtistOrder.ByYearAscending;
                    break;
                case ArtistOrder.ByYearAscending:
                    ArtistOrder = ArtistOrder.ByYearDescending;
                    break;
                case ArtistOrder.ByYearDescending:
                    ArtistOrder = ArtistOrder.AlphabeticalAscending;
                    break;
                default:
                    // Cannot happen, but just in case.
                    ArtistOrder = ArtistOrder.AlphabeticalAscending;
                    break;
            }
        }
        protected void UpdateArtistOrderText(ArtistOrder order)
        {
            switch (order)
            {
                case ArtistOrder.AlphabeticalAscending:
                    _orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
                case ArtistOrder.AlphabeticalDescending:
                    _orderText = ResourceUtils.GetString("Language_Z_A");
                    break;
                case ArtistOrder.ByDateAdded:
                    _orderText = ResourceUtils.GetString("Language_By_Date_Added");
                    break;
                case ArtistOrder.ByDateCreated:
                    _orderText = ResourceUtils.GetString("Language_By_Date_Created");
                    break;
                case ArtistOrder.ByTrackCount:
                    _orderText = ResourceUtils.GetString("Language_By_Track_Count");
                    break;
                case ArtistOrder.ByYearDescending:
                    _orderText = ResourceUtils.GetString("Language_By_Year_Descending");
                    break;
                case ArtistOrder.ByYearAscending:
                    _orderText = ResourceUtils.GetString("Language_By_Year_Ascending");
                    break;
                default:
                    // Cannot happen, but just in case.
                    _orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
            }

            RaisePropertyChanged(nameof(ArtistOrderText));
        }
    }
}
