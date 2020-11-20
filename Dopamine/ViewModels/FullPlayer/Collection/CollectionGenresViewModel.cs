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

namespace Dopamine.ViewModels.FullPlayer.Collection
{
    public class CollectionGenresViewModel : TracksViewModelBase, ISemanticZoomViewModel
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ICollectionService _collectionService;
        private IPlaybackService _playbackService;
        private IPlaylistService _playlistService;
        private IIndexingService _indexingService;
        private IDialogService _dialogService;
        private IEventAggregator _eventAggregator;
        private IContainerProvider _container;
        private CollectionViewSource _collectionViewSource;
        private CollectionViewSource _selectedItemsCvs;
        private IList<GenreViewModel> _selectedItems = new List<GenreViewModel>();
        private ObservableCollection<ISemanticZoomSelector> _zoomSelectors;
        private bool _isZoomVisible;
        private long _itemCount;
        private IList<long> _selectedIDs;
        private bool _ignoreSelectionChangedEvent;
        private string _searchString = "";
        private string _orderText;
        private GenreOrder _order;
        private ListItemSizeType selectedListItemSizeType;
        private readonly string Settings_NameSpace = "CollectionGenres";
        private readonly string Setting_ListBoxScrollPos = "ListBoxScrollPos";
        private readonly string Setting_SelectedIDs = "SelectedIDs";
        private readonly string Setting_ItemOrder = "ItemOrder";
        private readonly string Setting_ListItemSize = "ListItemSize";

        public delegate void SelectionChangedAction();
        public event SelectionChangedAction SelectionChanged;

        public DelegateCommand ToggleOrderCommand { get; set; }
        public DelegateCommand<string> AddItemsToPlaylistCommand { get; set; }
        public DelegateCommand<object> SelectedGenresCommand { get; set; }
        public DelegateCommand ShowZoomCommand { get; set; }
        public DelegateCommand<string> SemanticJumpCommand { get; set; }
        public DelegateCommand ShuffleItemsCommand { get; set; }
        public DelegateCommand PlayItemsCommand { get; set; }
        public DelegateCommand EnqueueItemsCommand { get; set; }

        public DelegateCommand<GenreViewModel> EnsureItemVisibleCommand { get; set; }

        
        public DelegateCommand<GenreViewModel> PlayItemCommand { get; set; }

        public DelegateCommand<GenreViewModel> EnqueueItemCommand { get; set; }

        public DelegateCommand<CollectionViewGroup> PlayGroupItemCommand { get; set; }

        public DelegateCommand<CollectionViewGroup> EnqueueGroupItemCommand { get; set; }
        public DelegateCommand<string> SetListItemSizeCommand { get; set; }
        private double _listBoxScrollPos;
        public double ListBoxScrollPos
        {
            get { return _listBoxScrollPos; }
            set
            {
                SetProperty<double>(ref _listBoxScrollPos, value);
                if (!InSearchMode)
                    SettingsClient.Set<double>(Settings_NameSpace, Setting_ListBoxScrollPos, value);
            }
        }

        public int SemanticRows
        {
            get
            {
                return ZoomSelectors == null ? 12 : ZoomSelectors.Count / 4 + 1;
            }
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

        private bool _inSearchMode;
        public bool InSearchMode
        {
            get { return _inSearchMode; }
            set {
                if (_inSearchMode != value)
                {
                    if (value == true)
                    {
                        _listBoxScrollPosInNormalMode = _listBoxScrollPos;
                    }
                    else
                    {
                        ListBoxScrollPos = _listBoxScrollPosInNormalMode;
                    }
                }
                SetProperty<bool>(ref _inSearchMode, value); }
        }
        
        public CollectionViewSource ItemsCvs
        {
            get { return _collectionViewSource; }
            set { SetProperty<CollectionViewSource>(ref _collectionViewSource, value); }
        }

        public IList<GenreViewModel> SelectedItems
        {
            get { return _selectedItems; }
            set { SetProperty<IList<GenreViewModel>>(ref _selectedItems, value); }
        }

        public CollectionViewSource SelectedItemsCvs
        {
            get { return _selectedItemsCvs; }
            set { SetProperty<CollectionViewSource>(ref _selectedItemsCvs, value); }
        }

        public GenreOrder GenreOrder
        {
            get { return _order; }
            set
            {
                SetProperty<GenreOrder>(ref _order, value);

                UpdateGenreOrderText(value);
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

        private bool _isImageVisible;
        public bool IsImageVisible
        {
            get { return _isImageVisible; }
            set { SetProperty<bool>(ref _isImageVisible, value); }
        }

        private bool _isExtraInfoVisible;
        public bool IsExtraInfoVisible
        {
            get { return _isExtraInfoVisible; }
            set { SetProperty<bool>(ref _isExtraInfoVisible, value); }
        }

        private int _imageHeight;
        public int ImageHeight
        {
            get { return _imageHeight; }
            set { SetProperty<int>(ref _imageHeight, value); }
        }

        public bool IsSmallListItemSizeSelected => this.selectedListItemSizeType == ListItemSizeType.Small;
        public bool IsMediumListItemSizeSelected => this.selectedListItemSizeType == ListItemSizeType.Medium;
        public bool IsLargeListItemSizeSelected => this.selectedListItemSizeType == ListItemSizeType.Large;
        public bool ShowTrackCount => _order == GenreOrder.ByTrackCount;
        public bool ShowPlaycount => _order == GenreOrder.ByPlayCount;
        public CollectionGenresViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            _container = container;
            _collectionService = container.Resolve<ICollectionService>();
            _playbackService = container.Resolve<IPlaybackService>();
            _playlistService = container.Resolve<IPlaylistService>();
            _indexingService = container.Resolve<IIndexingService>();
            _dialogService = container.Resolve<IDialogService>();
            _eventAggregator = container.Resolve<IEventAggregator>();

            // Commands
            ToggleTrackOrderCommand = new DelegateCommand(async () => await ToggleTrackOrderAsync());
            ToggleOrderCommand = new DelegateCommand(async () => await ToggleOrderAsync());
            RemoveSelectedTracksCommand = new DelegateCommand(async () => await RemoveTracksFromCollectionAsync(SelectedTracks), () => !IsIndexing);
            AddItemsToPlaylistCommand = new DelegateCommand<string>(async (playlistName) => await AddItemsToPlaylistAsync(SelectedItems, playlistName));
            SelectedGenresCommand = new DelegateCommand<object>(async (parameter) => await SelectedItemsHandlerAsync(parameter));
            ShowZoomCommand = new DelegateCommand(async () => await ShowSemanticZoomAsync());
            ShuffleItemsCommand = new DelegateCommand(async () => await _playbackService.PlayGenresAsync(SelectedItems, PlaylistMode.Play, TrackOrder.Random));
            PlayItemsCommand = new DelegateCommand(async () => await _playbackService.PlayGenresAsync(SelectedItems, PlaylistMode.Play));
            EnqueueItemsCommand = new DelegateCommand(async () => await _playbackService.PlayGenresAsync(SelectedItems, PlaylistMode.Enqueue));
            EnsureItemVisibleCommand = new DelegateCommand<GenreViewModel>(async (item) =>
            {
                _eventAggregator.GetEvent<LocateItem<GenreViewModel>>().Publish(item);
            });
            PlayItemCommand = new DelegateCommand<GenreViewModel>(async (vm) => {
                await _playbackService.PlayGenresAsync(new List<GenreViewModel>() { vm }, PlaylistMode.Play);
            });
            EnqueueItemCommand = new DelegateCommand<GenreViewModel>(async (vm) => await _playbackService.PlayGenresAsync(new List<GenreViewModel>() { vm }, PlaylistMode.Enqueue));
            PlayGroupItemCommand = new DelegateCommand<CollectionViewGroup>(async (vm) => await _playbackService.PlayTracksAsync(vm.Items.Cast<TrackViewModel>().ToList(), PlaylistMode.Play));
            EnqueueGroupItemCommand = new DelegateCommand<CollectionViewGroup>(async (vm) => await _playbackService.PlayTracksAsync(vm.Items.Cast<TrackViewModel>().ToList(), PlaylistMode.Enqueue));

            SemanticJumpCommand = new DelegateCommand<string>((header) =>
            {
                HideSemanticZoom();
                _eventAggregator.GetEvent<PerformSemanticJump>().Publish(new Tuple<string, string>("Genres", header));
            });

            SetListItemSizeCommand = new DelegateCommand<string>(async (listItemSize) =>
            {
                if (int.TryParse(listItemSize, out int selectedListItemSize))
                {
                    SetListItemSize((ListItemSizeType)selectedListItemSize);
                }
            });


        }

        private SubscriptionToken _shellMouseUpSubscriptionToken;
        protected override void OnLoad()
        {
            base.OnLoad();
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += SettingsClient_SettingChanged;
            _shellMouseUpSubscriptionToken = _eventAggregator.GetEvent<ShellMouseUp>().Subscribe((_) => IsZoomVisible = false);
            GenreOrder = (GenreOrder)SettingsClient.Get<int>(Settings_NameSpace, Setting_ItemOrder);
            SetTrackOrder((TrackOrder)SettingsClient.Get<int>(Settings_NameSpace, CollectionUtils.Setting_TrackOrder));
            ListBoxScrollPos = SettingsClient.Get<double>(Settings_NameSpace, Setting_ListBoxScrollPos);
            LeftPaneWidth = CollectionUtils.String2GridLength(SettingsClient.Get<string>(Settings_NameSpace, CollectionUtils.Setting_LeftPaneGridLength));
            RightPaneWidth = CollectionUtils.String2GridLength(SettingsClient.Get<string>(Settings_NameSpace, CollectionUtils.Setting_RightPaneGridLength));
            SetListItemSize((ListItemSizeType)SettingsClient.Get<int>(Settings_NameSpace, Setting_ListItemSize));
            LoadSelectedItems();

        }

        protected override void OnUnLoad()
        {
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged -= SettingsClient_SettingChanged;
            _eventAggregator.GetEvent<ShellMouseUp>().Unsubscribe(_shellMouseUpSubscriptionToken);
            base.OnUnLoad();
        }

        private async void SettingsClient_SettingChanged(object sender, Digimezzo.Foundation.Core.Settings.SettingChangedEventArgs e)
        {
            if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
            {
                EnableRating = (bool)e.Entry.Value;
                SetTrackOrder(TrackOrder);
                await GetTracksAsync(null, SelectedItems, null, TrackOrder);
            }

            if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
            {
                EnableLove = (bool)e.Entry.Value;
                SetTrackOrder(TrackOrder);
                await GetTracksAsync(null, SelectedItems, null, TrackOrder);
            }

            if (SettingsClient.IsSettingChanged(e, Settings_NameSpace, Setting_SelectedIDs))
            {
                LoadSelectedItems();
            }
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

            foreach (GenreViewModel vm in ItemsCvs.View)
            {
                if (_order == GenreOrder.AlphabeticalAscending || _order == GenreOrder.AlphabeticalDescending)
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
                var viewModels = new ObservableCollection<GenreViewModel>(await _collectionService.GetGenresAsync(_searchString));
                // Unless we are in Search Mode, we should re-store the selected items. The cases are:
                //  1. at the beginning of the application
                //  2. after the search mode is finished 
                if (string.IsNullOrEmpty(_searchString))
                {
                    _selectedItems = new List<GenreViewModel>();
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
            ItemsCvs.SortDescriptions.Clear();
            switch (_order)
            {
                case GenreOrder.AlphabeticalAscending:
                    ItemsCvs.SortDescriptions.Add(new SortDescription("Data.Name", ListSortDirection.Ascending));
                    break;
                case GenreOrder.AlphabeticalDescending:
                    ItemsCvs.SortDescriptions.Add(new SortDescription("Data.Name", ListSortDirection.Descending));
                    break;
                case GenreOrder.ByTrackCount:
                    ItemsCvs.SortDescriptions.Add(new SortDescription("Data.TrackCount", ListSortDirection.Descending));
                    ItemsCvs.SortDescriptions.Add(new SortDescription("Data.Name", ListSortDirection.Ascending));
                    break;
                case GenreOrder.ByPlayCount:
                    ItemsCvs.SortDescriptions.Add(new SortDescription("Data.PlayCount", ListSortDirection.Descending));
                    ItemsCvs.SortDescriptions.Add(new SortDescription("Data.Name", ListSortDirection.Ascending));
                    break;
                default:
                    break;
            }
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
                foreach (GenreViewModel item in (IList)parameter)
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
                IEnumerable<GenreViewModel> genres = _collectionViewSource.View.Cast<GenreViewModel>();
                foreach (long id in _selectedIDs)
                {
                    var sel = genres.Where(x => x.Id == id).FirstOrDefault();
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
            Task tracks = GetTracksAsync(null, SelectedItems, null, TrackOrder);
            await Task.WhenAll(tracks, saveSelection);
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedItemsCvs = new CollectionViewSource { Source = _selectedItems };
            });
            SelectionChanged?.Invoke();

        }

        private async Task AddItemsToPlaylistAsync(IList<GenreViewModel> items, string playlistName)
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
                    AddTracksToPlaylistResult result = await _playlistService.AddGenresToStaticPlaylistAsync(items, playlistName);

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

        private async Task AddItemsToNowPlayingAsync(IList<GenreViewModel> items)
        {
            await _playbackService.PlayGenresAsync(items, PlaylistMode.Enqueue);
        }

        private async Task ToggleTrackOrderAsync()
        {
            base.ToggleTrackOrder();

            SettingsClient.Set<int>(Settings_NameSpace, CollectionUtils.Setting_TrackOrder, (int)TrackOrder);
            await GetTracksCommonAsync(Tracks, TrackOrder);
        }

        private async Task ToggleOrderAsync()
        {
            await Task.Run(() =>
            {
                ToggleGenreOrder();
                SettingsClient.Set<int>(Settings_NameSpace, Setting_ItemOrder, (int)GenreOrder);
                OrderItems();
                //EnsureVisible();
            });
        }

        protected async override Task FillListsAsync()
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {

                _ignoreSelectionChangedEvent = true;
                if (string.IsNullOrEmpty(_searchString))
                {
                    await GetItemsAsync();
	            	await GetTracksAsync(null, SelectedItems, null, TrackOrder);
                }
                else
                {
                    await FilterListsAsync(_searchString);
                }
                _ignoreSelectionChangedEvent = false;
            });
            
        }

        protected async override Task EmptyListsAsync()
        {
            await Task.Run(() =>
            {
                ClearItems();
                ClearTracks();
            });
        }

        private double _listBoxScrollPosInNormalMode = 0;
        private bool _bSelectedItemChangedDuringSearchMode = false;
        protected override async Task FilterListsAsync(string searchText)
        {
            if (_searchString.Equals(searchText))
                return;

            if (!string.IsNullOrEmpty(searchText))
            {
                _bSelectedItemChangedDuringSearchMode = false;
                // We are searching
                if (InSearchMode == false)
                {
                    // we are entering Search Mode
                    InSearchMode = true;
                    // Lets keep the current list pos
                    _listBoxScrollPosInNormalMode = ListBoxScrollPos;
                }
                else
                {
                    // we are searching again something else
                }
                ListBoxScrollPos = 0;
                _searchString = searchText;
                await GetItemsAsync();
                // In every case we reset the ListBox Position
                await base.FilterListsAsync(searchText);
            }
            else
            {
                // We are not searching
                if (InSearchMode == false)
                {
                    // Nothing changed. Nominal usage without search. Should not happen
                }
                else
                {
                    // We turned from search to normal mode
                    InSearchMode = false;
                    // Lets restore the list box position
                    _searchString = "";
                    await GetItemsAsync();
                    if (_bSelectedItemChangedDuringSearchMode)
                    {
                        // Ensure Visible the new item. Less intrusively the last position
                        ListBoxScrollPos = _listBoxScrollPosInNormalMode;
                        // The track list have already been refreshed
                    }
                    else
                    {
                        // Refresh the track list
                        // Restore the old list position
                        ListBoxScrollPos = _listBoxScrollPosInNormalMode;
                        await GetTracksAsync(null, SelectedItems, null, TrackOrder);
                    }

                }
            }
        }

        protected override void RefreshLanguage()
        {
            UpdateGenreOrderText(GenreOrder);
            UpdateTrackOrderText(TrackOrder);
            base.RefreshLanguage();
        }

        protected virtual void ToggleGenreOrder()
        {
            switch (_order)
            {
                case GenreOrder.AlphabeticalAscending:
                    GenreOrder = GenreOrder.AlphabeticalDescending;
                    break;
                case GenreOrder.AlphabeticalDescending:
                    GenreOrder = GenreOrder.ByTrackCount;
                    break;
                case GenreOrder.ByTrackCount:
                    GenreOrder = GenreOrder.ByPlayCount;
                    break;
                case GenreOrder.ByPlayCount:
                    GenreOrder = GenreOrder.AlphabeticalAscending;
                    break;
                default:
                    // Cannot happen, but just in case.
                    GenreOrder = GenreOrder.AlphabeticalAscending;
                    break;
            }
        }
        protected void UpdateGenreOrderText(GenreOrder order)
        {
            switch (order)
            {
                case GenreOrder.AlphabeticalAscending:
                    _orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
                case GenreOrder.AlphabeticalDescending:
                    _orderText = ResourceUtils.GetString("Language_Z_A");
                    break;
                case GenreOrder.ByTrackCount:
                    _orderText = ResourceUtils.GetString("Language_By_Track_Count");
                    break;
                case GenreOrder.ByPlayCount:
                    _orderText = ResourceUtils.GetString("Language_By_PlayCount");
                    break;
                default:
                    // Cannot happen, but just in case.
                    _orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
            }

            RaisePropertyChanged(nameof(ItemOrderText));
        }
		
		private void SetListItemSize(ListItemSizeType listItemSize)
        {
			SettingsClient.Set<int>(Settings_NameSpace, Setting_ListItemSize, (int)listItemSize);
            selectedListItemSizeType = listItemSize;

            switch (listItemSize)
            {
                case ListItemSizeType.Small:
                    IsImageVisible = false;
                    IsExtraInfoVisible = false;
                    break;
                default:
                case ListItemSizeType.Medium:
                    IsImageVisible = true;
                    ImageHeight = 100;
                    IsExtraInfoVisible = true;
                    break;
                case ListItemSizeType.Large:
                    IsImageVisible = true;
                    ImageHeight = 200;
                    IsExtraInfoVisible = true;
                    break;
            }
            RaisePropertyChanged(nameof(this.IsSmallListItemSizeSelected));
            RaisePropertyChanged(nameof(this.IsMediumListItemSizeSelected));
            RaisePropertyChanged(nameof(this.IsLargeListItemSizeSelected));
        }
    }
}
