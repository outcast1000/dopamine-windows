using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Alex;  //Digimezzo.Foundation.Core.Settings
using Dopamine.Core.Base;
using Dopamine.Core.Prism;
using Dopamine.Data;
using Dopamine.Services.Collection;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.Playback;
using Dopamine.Services.Playlist;
using Dopamine.Services.Provider;
using Dopamine.Services.Utils;
using Dopamine.ViewModels.Common;
using Dopamine.ViewModels.Common.Base;
using Dopamine.Views.Common;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Dopamine.ViewModels.FullPlayer.Collection
{
    public class CollectionAlbumsViewModel : TracksViewModelBase, ISemanticZoomViewModel
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ICollectionService _collectionService;
        private IPlaybackService _playbackService;
        private IPlaylistService _playlistService;
        private IIndexingService _indexingService;
        private IDialogService _dialogService;
        private IEventAggregator _eventAggregator;
        private IProviderService _providerService;
        private IContainerProvider _container;
        private CollectionViewSource _collectionViewSource;
        private CollectionViewSource _selectedItemsCvs;
        private IList<AlbumViewModel> _selectedItems = new List<AlbumViewModel>();
        private ObservableCollection<ISemanticZoomSelector> _zoomSelectors;
        private bool _isZoomVisible;
        private long _itemCount;
        private IList<long> _selectedIDs;
        private bool _ignoreSelectionChangedEvent;
        private string _searchString = "";
        private string _orderText;
        private AlbumOrder _order;
        private ObservableCollection<SearchProvider> albumContextMenuSearchProviders;
        private CoverSizeType selectedCoverSize;
        private double coverSize;
        private double albumWidth;
        private double albumHeight;
        private readonly string Settings_NameSpace = "CollectionAlbums";
        private readonly string Setting_ListBoxScrollPos = "ListBoxScrollPos";
        private readonly string Setting_SelectedIDs = "SelectedIDs";
        private readonly string Setting_ItemOrder = "ItemOrder";

        public delegate void EnsureSelectedItemVisibleAction(AlbumViewModel item);
        public event EnsureSelectedItemVisibleAction EnsureItemVisible;

        public delegate void SelectionChangedAction();
        public event SelectionChangedAction SelectionChanged;

        public DelegateCommand ToggleOrderCommand { get; set; }
        public DelegateCommand<string> AddItemsToPlaylistCommand { get; set; }
        public DelegateCommand<object> SelectedAlbumsCommand { get; set; }
        public DelegateCommand ShowZoomCommand { get; set; }
        public DelegateCommand<string> SemanticJumpCommand { get; set; }
        public DelegateCommand ShuffleItemsCommand { get; set; }
        public DelegateCommand PlayItemsCommand { get; set; }
        public DelegateCommand EnqueueItemsCommand { get; set; }


        public DelegateCommand<AlbumViewModel> EnsureItemVisibleCommand { get; set; }

        public DelegateCommand<AlbumViewModel> DownloadImageAlbumsCommand { get; set; }
        
        public DelegateCommand<AlbumViewModel> PlayItemCommand { get; set; }

        public DelegateCommand<AlbumViewModel> EnqueueItemCommand { get; set; }

        public DelegateCommand<AlbumViewModel> LoveItemCommand { get; set; }

        public DelegateCommand<string> AlbumSearchOnlineCommand { get; set; }

        public DelegateCommand<string> SetCoverSizeCommand { get; set; }
        public DelegateCommand<AlbumViewModel> DownloadImageAlbumCommand { get; set; }
        public DelegateCommand EditAlbumCommand { get; set; }

        public ObservableCollection<SearchProvider> AlbumContextMenuSearchProviders
        {
            get { return this.albumContextMenuSearchProviders; }
            set
            {
                SetProperty<ObservableCollection<SearchProvider>>(ref this.albumContextMenuSearchProviders, value);
                RaisePropertyChanged(nameof(this.HasContextMenuSearchProviders));
            }
        }

        public bool HasAlbumContextMenuSearchProviders => this.ContextMenuSearchProviders != null && this.ContextMenuSearchProviders.Count > 0;

        private async void GetAlbumsSearchProvidersAsync()
        {
            this.albumContextMenuSearchProviders = null;

            List<SearchProvider> providersList = await _providerService.GetSearchProvidersAsync(SearchProvider.ProviderType.Album);
            var localProviders = new ObservableCollection<SearchProvider>();

            await Task.Run(() =>
            {
                foreach (SearchProvider vp in providersList)
                {
                    localProviders.Add(vp);
                }
            });

            this.albumContextMenuSearchProviders = localProviders;
        }

        private void AlbumSearchOnline(string id)
        {
            if (SelectedItems?.Count > 0)
            {
                _providerService.SearchOnline(id, new string[] { this.SelectedItems.First().Name, SelectedItems.First().AlbumArtistComplete });
            }
        }

        private double _listBoxScrollPos;
        public double ListBoxScrollPos
        {
            get { return _listBoxScrollPos; }
            set
            {
                SetProperty<double>(ref _listBoxScrollPos, value);
                SettingsClient.Set<double>(Settings_NameSpace, Setting_ListBoxScrollPos, value);
            }
        }

        private GridLength _leftPaneGridLength;
        public GridLength LeftPaneWidth
        {
            get
            {
                return _leftPaneGridLength;
            }
            set
            {
                if (value.IsStar && value.Value > 1)
                    value = new GridLength(value.Value);
                SetProperty<GridLength>(ref _leftPaneGridLength, value);
                SettingsClient.Set<string>(Settings_NameSpace, CollectionUtils.Setting_LeftPaneGridLength, CollectionUtils.GridLength2String(value));
            }
        }

        public bool InSearchMode { get { return !string.IsNullOrEmpty(_searchString); } }

        public CollectionViewSource ItemsCvs
        {
            get { return _collectionViewSource; }
            set { SetProperty<CollectionViewSource>(ref _collectionViewSource, value); }
        }

        public IList<AlbumViewModel> SelectedItems
        {
            get { return _selectedItems; }
            set { SetProperty<IList<AlbumViewModel>>(ref _selectedItems, value); }
        }

        public CollectionViewSource SelectedItemsCvs
        {
            get { return _selectedItemsCvs; }
            set { SetProperty<CollectionViewSource>(ref _selectedItemsCvs, value); }
        }

        public AlbumOrder AlbumOrder
        {
            get { return _order; }
            set
            {
                SetProperty<AlbumOrder>(ref _order, value);

                UpdateAlbumOrderText(value);
            }
        }

        public long ItemsCount
        {
            get { return _itemCount; }
            set { SetProperty<long>(ref _itemCount, value); }
        }

        public bool IsZoomVisible
        {
            get { return _isZoomVisible; }
            set { SetProperty<bool>(ref _isZoomVisible, value); }
        }

        public string ItemOrderText => _orderText;

        public ObservableCollection<ISemanticZoomSelector> ZoomSelectors
        {
            get { return _zoomSelectors; }
            set { SetProperty<ObservableCollection<ISemanticZoomSelector>>(ref _zoomSelectors, value); }
        }
        ObservableCollection<ISemanticZoomSelector> ISemanticZoomViewModel.SemanticZoomSelectors
        {
            get { return ZoomSelectors; }
            set { ZoomSelectors = value; }
        }

        public double CoverSize
        {
            get { return this.coverSize; }
            set { SetProperty<double>(ref this.coverSize, value); }
        }

        public double AlbumWidth
        {
            get { return this.albumWidth; }
            set { SetProperty<double>(ref this.albumWidth, value); }
        }

        public double AlbumHeight
        {
            get { return this.albumHeight; }
            set { SetProperty<double>(ref this.albumHeight, value); }
        }

        public new double UpscaledCoverSize => this.CoverSize * Constants.CoverUpscaleFactor;

        public bool IsSmallCoverSizeSelected => this.selectedCoverSize == CoverSizeType.Small;

        public bool IsMediumCoverSizeSelected => this.selectedCoverSize == CoverSizeType.Medium;

        public bool IsLargeCoverSizeSelected => this.selectedCoverSize == CoverSizeType.Large;


        public CollectionAlbumsViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            _container = container;
            _collectionService = container.Resolve<ICollectionService>();
            _playbackService = container.Resolve<IPlaybackService>();
            _playlistService = container.Resolve<IPlaylistService>();
            _indexingService = container.Resolve<IIndexingService>();
            _dialogService = container.Resolve<IDialogService>();
            _eventAggregator = container.Resolve<IEventAggregator>();
            _providerService = container.Resolve<IProviderService>();

            // Commands
            ToggleTrackOrderCommand = new DelegateCommand(async () => await ToggleTrackOrderAsync());
            ToggleOrderCommand = new DelegateCommand(async () => await ToggleOrderAsync());
            RemoveSelectedTracksCommand = new DelegateCommand(async () => await RemoveTracksFromCollectionAsync(SelectedTracks), () => !IsIndexing);
            AddItemsToPlaylistCommand = new DelegateCommand<string>(async (playlistName) => await AddItemsToPlaylistAsync(SelectedItems, playlistName));
            SelectedAlbumsCommand = new DelegateCommand<object>(async (parameter) => await SelectedItemsHandlerAsync(parameter));
            ShowZoomCommand = new DelegateCommand(async () => await ShowSemanticZoomAsync());
            ShuffleItemsCommand = new DelegateCommand(async () => await _playbackService.PlayAlbumsAsync(SelectedItems, PlaylistMode.Play, TrackOrder.Random));
            PlayItemsCommand = new DelegateCommand(async () => await _playbackService.PlayAlbumsAsync(SelectedItems, PlaylistMode.Play));
            EnqueueItemsCommand = new DelegateCommand(async () => await _playbackService.PlayAlbumsAsync(SelectedItems, PlaylistMode.Enqueue));
            EnsureItemVisibleCommand = new DelegateCommand<AlbumViewModel>(async (item) =>
            {
                EnsureItemVisible?.Invoke(item);
            });
            DownloadImageAlbumsCommand = new DelegateCommand<AlbumViewModel>(async (artist) =>
            {
                await artist.RequestImageDownload(true, true);
            });
            PlayItemCommand = new DelegateCommand<AlbumViewModel>(async (vm) => {
                await _playbackService.PlayAlbumsAsync(new List<AlbumViewModel>() { vm }, PlaylistMode.Play);
            });
            EnqueueItemCommand = new DelegateCommand<AlbumViewModel>(async (vm) => await _playbackService.PlayAlbumsAsync(new List<AlbumViewModel>() { vm }, PlaylistMode.Enqueue));
            LoveItemCommand = new DelegateCommand<AlbumViewModel>((avm) => Debug.Assert(false, "ALEX TODO"));
            _providerService.SearchProvidersChanged += (_, __) => { GetAlbumsSearchProvidersAsync(); };
            this.GetAlbumsSearchProvidersAsync();
            this.AlbumSearchOnlineCommand = new DelegateCommand<string>((id) => AlbumSearchOnline(id));


            SemanticJumpCommand = new DelegateCommand<string>((header) =>
            {
                HideSemanticZoom();
                _eventAggregator.GetEvent<PerformSemanticJump>().Publish(new Tuple<string, string>("Albums", header));
            });

            SetCoverSizeCommand = new DelegateCommand<string>(async (coverSize) =>
            {
                if (int.TryParse(coverSize, out int selectedCoverSize))
                {
                    await this.SetCoversizeAsync((CoverSizeType)selectedCoverSize);
                }
            });

            DownloadImageAlbumCommand = new DelegateCommand<AlbumViewModel>((album) =>
            {
                Task unwaitedTask = album.RequestImageDownload(true, true);
            });

            EditAlbumCommand = new DelegateCommand(() => this.EditSelectedAlbum(), () => !this.IsIndexing);

            // Settings
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += async (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
                {
                    EnableRating = (bool)e.Entry.Value;
                    SetTrackOrder(TrackOrder);
                    await GetTracksAsync(null, null, SelectedItems, TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
                {
                    EnableLove = (bool)e.Entry.Value;
                    SetTrackOrder(TrackOrder);
                    await GetTracksAsync(null, null, SelectedItems, TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, Settings_NameSpace, Setting_SelectedIDs))
                {
                    LoadSelectedItems();
                }

            };

            // PubSub Events
            _eventAggregator.GetEvent<ShellMouseUp>().Subscribe((_) => IsZoomVisible = false);

            // ALEX WARNING. EVERYTIME YOU NEED TO ADD A NEW SETTING YOU HAVE TO:
            //  1. Update the \BaseSettings.xml and add the new / modified value
            //  2. Increase the version number (in order to update the C:\Users\Alex\AppData\Roaming\Dopamine\Settings.xml)
            AlbumOrder = (AlbumOrder)SettingsClient.Get<int>(Settings_NameSpace, Setting_ItemOrder);

            // Set the initial TrackOrder
            SetTrackOrder((TrackOrder)SettingsClient.Get<int>(Settings_NameSpace, CollectionUtils.Setting_TrackOrder));
            ListBoxScrollPos = SettingsClient.Get<double>(Settings_NameSpace, Setting_ListBoxScrollPos);
            LeftPaneWidth = CollectionUtils.String2GridLength(SettingsClient.Get<string>(Settings_NameSpace, CollectionUtils.Setting_LeftPaneGridLength));
            // Cover size
            Task unAwaitedTask = SetCoversizeAsync((CoverSizeType)SettingsClient.Get<int>("CoverSizes", "AlbumsCoverSize"));
            LoadSelectedItems();

        }



        private void LoadSelectedItems()
        {
            try
            {
                string s = SettingsClient.Get<String>(Settings_NameSpace, Setting_SelectedIDs);
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
            SettingsClient.Set<String>(Settings_NameSpace, Setting_SelectedIDs, s);
        }

        public async Task ShowSemanticZoomAsync()
        {
            ZoomSelectors = await SemanticZoomUtils.UpdateSemanticZoomSelectors(ItemsCvs.View);
            IsZoomVisible = true;
        }

        public void HideSemanticZoom()
        {
            IsZoomVisible = false;
        }

        public void UpdateSemanticZoomHeaders()
        {
            string previousHeader = string.Empty;

            foreach (AlbumViewModel vm in ItemsCvs.View)
            {
                if (_order == AlbumOrder.AlphabeticalAscending || _order == AlbumOrder.AlphabeticalDescending)
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
                ItemsCvs = null;
                SelectedItemsCvs = null;
            });

        }


        private async Task GetItemsAsync()
        {
            ObservableCollection<ISemanticZoomable> items;
            try
            {
                // Get the viewModels
                var viewModels = new ObservableCollection<AlbumViewModel>(await _collectionService.GetAlbumsAsync(_searchString));
                // Unless we are in Search Mode, we should re-store the selected items. The cases are:
                //  1. at the beginning of the application
                //  2. after the search mode is finished 
                if (string.IsNullOrEmpty(_searchString))
                {
                    _selectedItems = new List<AlbumViewModel>();
                    foreach (long id in _selectedIDs)
                    {
                        var vm = viewModels.Where(x => x.Id == id).FirstOrDefault();
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
                        var sel = viewModels[0];
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
                ItemsCvs = new CollectionViewSource { Source = items };
                SelectedItemsCvs = new CollectionViewSource { Source = _selectedItems };
                OrderItems();
                //EnsureVisible();
                ItemsCount = ItemsCvs.View.Cast<ISemanticZoomable>().Count();
            });
        }

		private void OrderItems()
        {
            SortDescription sd = new SortDescription();
            switch (AlbumOrder)
            {
                case AlbumOrder.AlphabeticalAscending:
                    sd = new SortDescription("Name", ListSortDirection.Ascending);
                    break;
                case AlbumOrder.AlphabeticalDescending:
                    sd = new SortDescription("Name", ListSortDirection.Descending);
                    break;
                case AlbumOrder.ByDateAdded:
                    sd = new SortDescription("DateAdded", ListSortDirection.Descending);
                    break;
                case AlbumOrder.ByYearAscending:
                    sd = new SortDescription("Year", ListSortDirection.Ascending);
                    break;
                case AlbumOrder.ByYearDescending:
                    sd = new SortDescription("Year", ListSortDirection.Descending);
                    break;
                case AlbumOrder.ByAlbumArtistAscending:
                    sd = new SortDescription("AlbumArtist", ListSortDirection.Ascending);
                    break;
                case AlbumOrder.ByAlbumArtistDescending:
                    sd = new SortDescription("AlbumArtist", ListSortDirection.Descending);
                    break;
                default:
                    break;
            }
            ItemsCvs.SortDescriptions.Clear();
            ItemsCvs.SortDescriptions.Add(sd);
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
            if (ItemsCvs == null)
                return;
            bool bKeepOldSelections = true;
            if (parameter != null && ((IList)parameter).Count > 0)
            {
                // This is the most usual case. The user has just selected one or more items
                bKeepOldSelections = false;
                _selectedIDs.Clear();
                _selectedItems.Clear();
                foreach (AlbumViewModel item in (IList)parameter)
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
                IEnumerable<AlbumViewModel> items = ItemsCvs.View.Cast<AlbumViewModel>();
                foreach (long id in _selectedIDs)
                {
                    var sel = items.Where(x => x.Id == id).FirstOrDefault();
                    if (sel != null)
                    {
                        validSelectedIDs.Add(id);
                        sel.IsSelected = true;
                        _selectedItems.Add(sel);
                    }
                }
                _selectedIDs = validSelectedIDs;

            }

            Task saveSelection = Task.Run(() => SaveSelectedItems());
            // Update the tracks
            SetTrackOrder(TrackOrder);
            Task tracks = GetTracksAsync(null, null, SelectedItems, TrackOrder);
            await Task.WhenAll(tracks, saveSelection);
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedItemsCvs = new CollectionViewSource { Source = _selectedItems };
            });
            SelectionChanged?.Invoke();

        }
        private async Task AddItemsToPlaylistAsync(IList<AlbumViewModel> items, string playlistName)
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
                    AddTracksToPlaylistResult result = await _playlistService.AddAlbumsToStaticPlaylistAsync(items, playlistName);

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

        private async Task AddItemsToNowPlayingAsync(IList<AlbumViewModel> items)
        {
            await _playbackService.PlayAlbumsAsync(items, PlaylistMode.Enqueue);
        }

        private async Task ToggleTrackOrderAsync()
        {
            base.ToggleTrackOrder();

            SettingsClient.Set<int>(Settings_NameSpace, CollectionUtils.Setting_TrackOrder, (int)TrackOrder);
            await GetTracksCommonAsync(Tracks, TrackOrder);
        }

        private async Task ToggleOrderAsync()
        {
            ToggleAlbumOrder();
            SettingsClient.Set<int>(Settings_NameSpace, Setting_ItemOrder, (int)AlbumOrder);
            OrderItems();
            //EnsureVisible();
        }

        private void EnsureVisible()
        {
            if (SelectedItems.Count > 0)
                EnsureItemVisible?.Invoke(SelectedItems[0]);
        }

        protected async override Task FillListsAsync()
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {

                _ignoreSelectionChangedEvent = true;
                if (string.IsNullOrEmpty(_searchString))
                {
                    await GetItemsAsync();
                	await GetTracksAsync(null, null, SelectedItems, TrackOrder);
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
            UpdateAlbumOrderText(AlbumOrder);
            UpdateTrackOrderText(TrackOrder);
            base.RefreshLanguage();
        }

        protected virtual void ToggleAlbumOrder()
        {
            switch (this.AlbumOrder)
            {
                case AlbumOrder.AlphabeticalAscending:
                    this.AlbumOrder = AlbumOrder.AlphabeticalDescending;
                    break;
                case AlbumOrder.AlphabeticalDescending:
                    this.AlbumOrder = AlbumOrder.ByDateAdded;
                    break;
                case AlbumOrder.ByDateAdded:
                    this.AlbumOrder = AlbumOrder.ByAlbumArtistAscending;
                    break;
                case AlbumOrder.ByAlbumArtistAscending:
                    this.AlbumOrder = AlbumOrder.ByAlbumArtistDescending;
                    break;
                case AlbumOrder.ByAlbumArtistDescending:
                    this.AlbumOrder = AlbumOrder.ByYearAscending;
                    break;
                case AlbumOrder.ByYearAscending:
                    this.AlbumOrder = AlbumOrder.ByYearDescending;
                    break;
                case AlbumOrder.ByYearDescending:
                    this.AlbumOrder = AlbumOrder.AlphabeticalAscending;
                    break;
                default:
                    // Cannot happen, but just in case.
                    this.AlbumOrder = AlbumOrder.AlphabeticalAscending;
                    break;
            }
        }
		
		protected void UpdateAlbumOrderText(AlbumOrder order)
        {
            switch (order)
            {
                case AlbumOrder.AlphabeticalAscending:
                   _orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
                case AlbumOrder.AlphabeticalDescending:
                   _orderText = ResourceUtils.GetString("Language_Z_A");
                    break;
                case AlbumOrder.ByDateAdded:
                   _orderText = ResourceUtils.GetString("Language_By_Date_Added");
                    break;
                case AlbumOrder.ByAlbumArtistAscending:
                   _orderText = ResourceUtils.GetString("Language_By_Album_Artist") + " (\u2191)";
                    break;
                case AlbumOrder.ByAlbumArtistDescending:
                   _orderText = ResourceUtils.GetString("Language_By_Album_Artist") + " (\u2193)";
                    break;
                case AlbumOrder.ByYearDescending:
                   _orderText = ResourceUtils.GetString("Language_By_Year_Descending");
                    break;
                case AlbumOrder.ByYearAscending:
                   _orderText = ResourceUtils.GetString("Language_By_Year_Ascending");
                    break;
                default:
                    // Cannot happen, but just in case.
                   _orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
            }

            RaisePropertyChanged(nameof(ItemOrderText));
        }
		
		private async Task SetCoversizeAsync(CoverSizeType coverSize)
        {
            await Task.Run(() =>
            {
				SettingsClient.Set<int>("CoverSizes", "AlbumsCoverSize", (int)coverSize);
               selectedCoverSize = coverSize;

                switch (coverSize)
                {
                    case CoverSizeType.Small:
                       CoverSize = Constants.CoverSmallSize;
                        break;
                    case CoverSizeType.Medium:
                       CoverSize = Constants.CoverMediumSize;
                        break;
                    case CoverSizeType.Large:
                       CoverSize = Constants.CoverLargeSize;
                        break;
                    default:
                       CoverSize = Constants.CoverMediumSize;
                       selectedCoverSize = CoverSizeType.Medium;
                        break;
                }

                //AlbumWidth =CoverSize + Constants.AlbumTilePadding.Left + Constants.AlbumTilePadding.Right + Constants.AlbumTileMargin.Left + Constants.AlbumTileMargin.Right;
               AlbumWidth = CoverSize + Constants.AlbumTileMargin.Left + Constants.AlbumTileMargin.Right;
               AlbumHeight = AlbumWidth + Constants.AlbumTileAlbumInfoHeight + Constants.AlbumSelectionBorderSize;

                RaisePropertyChanged(nameof(this.CoverSize));
                RaisePropertyChanged(nameof(this.AlbumWidth));
                RaisePropertyChanged(nameof(this.AlbumHeight));
                RaisePropertyChanged(nameof(this.UpscaledCoverSize));
                RaisePropertyChanged(nameof(this.IsSmallCoverSizeSelected));
                RaisePropertyChanged(nameof(this.IsMediumCoverSizeSelected));
                RaisePropertyChanged(nameof(this.IsLargeCoverSizeSelected));
            });
        }

        private void EditSelectedAlbum()
        {
            if (this.SelectedItems?.Count != 1)
                return;

            EditAlbum view = this._container.Resolve<EditAlbum>();
            view.DataContext = this._container.Resolve<Func<AlbumViewModel, EditAlbumViewModel>>()(this.SelectedItems.First());

            this._dialogService.ShowCustomDialog(
                0xe104,
                14,
                ResourceUtils.GetString("Language_Edit_Album"),
                view,
                405,
                450,
                false,
                true,
                true,
                true,
                ResourceUtils.GetString("Language_Ok"),
                ResourceUtils.GetString("Language_Cancel"),
                ((EditAlbumViewModel)view.DataContext).SaveAlbumAsync);
        }
    }
}
