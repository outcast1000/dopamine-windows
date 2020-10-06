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
    public class CollectionGenresViewModel : AlbumsViewModelBase, ISemanticZoomViewModel
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private ICollectionService collectionService;
        private IPlaybackService playbackService;
        private IPlaylistService playlistService;
        private IIndexingService indexingService;
        private IDialogService dialogService;
        private IEventAggregator eventAggregator;
        private CollectionViewSource collectionViewSource;
        private IList<GenreViewModel> selectedItems = new List<GenreViewModel>();
        private ObservableCollection<ISemanticZoomSelector> zoomSelectors;
        private bool isZoomVisible;
        private long itemCount;
        private double leftPaneWidthPercent;
        private double rightPaneWidthPercent;
        private IList<long> selectedIDs;
        private bool _ignoreSelectionChangedEvent;
        private string _searchString = "";
        private string orderText;
        private GenreOrder order;

        public delegate void EnsureSelectedItemVisibleAction(GenreViewModel item);
        public event EnsureSelectedItemVisibleAction EnsureItemVisible;
        public DelegateCommand ToggleGenreOrderCommand { get; set; }

        public DelegateCommand<string> AddGenresToPlaylistCommand { get; set; }

        public DelegateCommand<object> SelectedGenresCommand { get; set; }

        public DelegateCommand ShowGenresZoomCommand { get; set; }

        public DelegateCommand<string> SemanticJumpCommand { get; set; }

        public DelegateCommand AddGenresToNowPlayingCommand { get; set; }

        public DelegateCommand ShuffleSelectedGenresCommand { get; set; }

        public DelegateCommand<GenreViewModel> PlayGenreCommand { get; set; }
        public DelegateCommand<GenreViewModel> EnqueueGenreCommand { get; set; }
        public double LeftPaneWidthPercent
        {
            get { return this.leftPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref this.leftPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", "GenresLeftPaneWidthPercent", Convert.ToInt32(value));
            }
        }

        public double RightPaneWidthPercent
        {
            get { return this.rightPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref this.rightPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", "GenresRightPaneWidthPercent", Convert.ToInt32(value));
            }
        }

        public CollectionViewSource GenresCvs
        {
            get { return this.collectionViewSource; }
            set { SetProperty<CollectionViewSource>(ref this.collectionViewSource, value); }
        }

        public IList<GenreViewModel> SelectedGenres
        {
            get { return this.selectedItems; }
            set { SetProperty<IList<GenreViewModel>>(ref this.selectedItems, value); }
        }

        public GenreOrder GenreOrder
        {
            get { return this.order; }
            set
            {
                SetProperty<GenreOrder>(ref this.order, value);

                this.UpdateGenreOrderText(value);
            }
        }
        public long GenresCount
        {
            get { return this.itemCount; }
            set { SetProperty<long>(ref this.itemCount, value); }
        }

        public bool IsGenresZoomVisible
        {
            get { return this.isZoomVisible; }
            set { SetProperty<bool>(ref this.isZoomVisible, value); }
        }

        public string GenreOrderText => this.orderText;
        public ObservableCollection<ISemanticZoomSelector> GenresZoomSelectors
        {
            get { return this.zoomSelectors; }
            set { SetProperty<ObservableCollection<ISemanticZoomSelector>>(ref this.zoomSelectors, value); }
        }
        ObservableCollection<ISemanticZoomSelector> ISemanticZoomViewModel.SemanticZoomSelectors
        {
            get { return GenresZoomSelectors; }
            set { GenresZoomSelectors = value; }
        }

        public bool HasSelectedGenres
        {
            get
            {
                return (this.SelectedGenres?.Count > 0);
            }
        }



        public CollectionGenresViewModel(IContainerProvider container) : base(container)
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
            this.ToggleGenreOrderCommand = new DelegateCommand(async () => await this.ToggleOrderAsync());

            this.RemoveSelectedTracksCommand = new DelegateCommand(async () => await this.RemoveTracksFromCollectionAsync(this.SelectedTracks), () => !this.IsIndexing);
            this.AddGenresToPlaylistCommand = new DelegateCommand<string>(async (playlistName) => await this.AddItemsToPlaylistAsync(this.SelectedGenres, playlistName));
            this.SelectedGenresCommand = new DelegateCommand<object>(async (parameter) => await this.SelectedItemsHandlerAsync(parameter));
            this.ShowGenresZoomCommand = new DelegateCommand(async () => await this.ShowSemanticZoomAsync());
            this.AddGenresToNowPlayingCommand = new DelegateCommand(async () => await this.AddItemsToNowPlayingAsync(this.SelectedGenres));
            this.ShuffleSelectedGenresCommand = new DelegateCommand(async () => {
                await this.playbackService.PlayGenresAsync(this.SelectedGenres, PlaylistMode.Play, true);
            });
            this.PlayGenreCommand = new DelegateCommand<GenreViewModel>(async (vm) => {
                await this.playbackService.PlayGenresAsync(new List<GenreViewModel>() { vm }, PlaylistMode.Play);
            });
            this.EnqueueGenreCommand = new DelegateCommand<GenreViewModel>(async (vm) => await this.playbackService.PlayGenresAsync(new List<GenreViewModel>() { vm }, PlaylistMode.Enqueue));

            this.SemanticJumpCommand = new DelegateCommand<string>((header) =>
            {
                this.HideSemanticZoom();
                this.eventAggregator.GetEvent<PerformSemanticJump>().Publish(new Tuple<string, string>("Genres", header));
            });

            // Settings
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += async (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
                {
                    this.EnableRating = (bool)e.Entry.Value;
                    this.SetTrackOrder("GenresTrackOrder");
                    await this.GetTracksAsync(null, this.SelectedGenres, this.SelectedAlbums, this.TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
                {
                    this.EnableLove = (bool)e.Entry.Value;
                    this.SetTrackOrder("GenresTrackOrder");
                    await this.GetTracksAsync(null, this.SelectedGenres, this.SelectedAlbums, this.TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "State", "SelectedGenreIDs"))
                {
                    LoadSelectedItems();
                }

            };

            // PubSub Events
            this.eventAggregator.GetEvent<ShellMouseUp>().Subscribe((_) => this.IsGenresZoomVisible = false);

            // Set the initial AlbumOrder
            this.AlbumOrder = (AlbumOrder)SettingsClient.Get<int>("Ordering", "GenresAlbumOrder");

            // ALEX WARNING. EVERYTIME YOU NEED TO ADD A NEW SETTING YOU HAVE TO:
            //  1. Update the \BaseSettings.xml of the project
            //  2. Update the  C:\Users\Alex\AppData\Roaming\Dopamine\Settings.xml
            this.GenreOrder = (GenreOrder)SettingsClient.Get<int>("Ordering", "GenresGenreOrder");
            // Set the initial TrackOrder
            this.SetTrackOrder("GenresTrackOrder");

            // Set width of the panels
            this.LeftPaneWidthPercent = SettingsClient.Get<int>("ColumnWidths", "GenresLeftPaneWidthPercent");
            this.RightPaneWidthPercent = SettingsClient.Get<int>("ColumnWidths", "GenresRightPaneWidthPercent");

            // Cover size
            this.SetCoversizeAsync((CoverSizeType)SettingsClient.Get<int>("CoverSizes", "GenresCoverSize"));
            LoadSelectedItems();
        }

        private void LoadSelectedItems()
        {
            try
            {
                string s = SettingsClient.Get<String>("State", "SelectedGenreIDs");
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
            string s = string.Join(",", selectedIDs);// SettingsClient.Get<String>("State", "SelectedGenreIDs");
            SettingsClient.Set<String>("State", "SelectedGenreIDs", s);
        }
        public async Task ShowSemanticZoomAsync()
        {
            this.GenresZoomSelectors = await SemanticZoomUtils.UpdateSemanticZoomSelectors(this.GenresCvs.View);

            this.IsGenresZoomVisible = true;
        }

        public void HideSemanticZoom()
        {
            this.IsGenresZoomVisible = false;
        }

        public void UpdateSemanticZoomHeaders()
        {
            string previousHeader = string.Empty;

            foreach (GenreViewModel vm in this.GenresCvs.View)
            {
                if (order == GenreOrder.AlphabeticalAscending || order == GenreOrder.AlphabeticalDescending)
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
                this.GenresCvs = null;
            });

        }


        private async Task GetItemsAsync()
        {
            ObservableCollection<ISemanticZoomable> items;
            try
            {
                // Get the viewModels
                var viewModels = new ObservableCollection<GenreViewModel>(await this.collectionService.GetGenresAsync(_searchString));
                // Unless we are in Search Mode, we should re-store the selected items. The cases are:
                //  1. at the beginning of the application
                //  2. after the search mode is finished 
                if (string.IsNullOrEmpty(_searchString))
                {
                    selectedItems = new List<GenreViewModel>();
                    foreach (long id in selectedIDs)
                    {
                        GenreViewModel vm = viewModels.Where(x => x.Id == id).FirstOrDefault();
                        if (vm != null)
                        {
                            vm.IsSelected = selectedIDs.Contains(vm.Id);
                            selectedItems.Add(vm);
                        }
                    }
                }
                items = new ObservableCollection<ISemanticZoomable>(viewModels);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while getting Genres. Exception: {0}", ex.Message);
                // Failed getting Genres. Create empty ObservableCollection.
                items = new ObservableCollection<ISemanticZoomable>();
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Populate CollectionViewSource
                this.GenresCvs = new CollectionViewSource { Source = items };
                OrderItems();
                EnsureVisible();
                this.GenresCount = GenresCvs.View.Cast<ISemanticZoomable>().Count();
            });
        }

        private void OrderItems()
        {
            SortDescription sd = new SortDescription();
            switch (order)
            {
                case GenreOrder.AlphabeticalAscending:
                    sd = new SortDescription("Name", ListSortDirection.Ascending);
                    break;
                case GenreOrder.AlphabeticalDescending:
                    sd = new SortDescription("Name", ListSortDirection.Descending);
                    break;
                case GenreOrder.ByTrackCount:
                    sd = new SortDescription("TrackCount", ListSortDirection.Descending);
                    break;
                default:
                    break;
            }
            GenresCvs.SortDescriptions.Clear();
            GenresCvs.SortDescriptions.Add(sd);
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
            if (GenresCvs == null)
                return;
            bool bKeepOldSelections = true;
            if (parameter != null && ((IList)parameter).Count > 0)
            {
                // This is the most usual case. The user has just selected one or more items
                bKeepOldSelections = false;
                selectedIDs.Clear();
                selectedItems.Clear();
                foreach (GenreViewModel item in (IList)parameter)
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
                List<long> validSelectedIDs = new List<long>();
                selectedItems.Clear();
                IEnumerable<GenreViewModel> genres = collectionViewSource.View.Cast<GenreViewModel>();
                foreach (long id in selectedIDs)
                {
                    GenreViewModel sel = genres.Where(x => x.Id == id).FirstOrDefault();
                    if (sel != null)
                    {
                        validSelectedIDs.Add(id);
                        sel.IsSelected = true;
                        selectedItems.Add(sel);
                    }
                }
                selectedIDs = validSelectedIDs;

            }

            this.RaisePropertyChanged(nameof(this.HasSelectedGenres));
            Task saveSelection = Task.Run(() => SaveSelectedItems());
            // Update the albums
            Task albums = GetGenreAlbumsAsync(selectedItems, this.AlbumOrder);
            // Update the tracks
            this.SetTrackOrder("GenresTrackOrder");
            Task tracks = GetTracksAsync(null, this.SelectedGenres, this.SelectedAlbums, this.TrackOrder);
            Task.WhenAll(albums, tracks, saveSelection);

        }

        private async Task AddItemsToPlaylistAsync(IList<GenreViewModel> genres, string playlistName)
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
                    AddTracksToPlaylistResult result = await this.playlistService.AddGenresToStaticPlaylistAsync(genres, playlistName);

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

        private async Task AddItemsToNowPlayingAsync(IList<GenreViewModel> items)
        {
            await this.playbackService.PlayGenresAsync(items, PlaylistMode.Enqueue);
        }

        private async Task ToggleTrackOrderAsync()
        {
            base.ToggleTrackOrder();

            SettingsClient.Set<int>("Ordering", "GenresTrackOrder", (int)this.TrackOrder);
            await this.GetTracksCommonAsync(this.Tracks, this.TrackOrder);
        }

        private async Task ToggleAlbumOrderAsync()
        {

            base.ToggleAlbumOrder();

            SettingsClient.Set<int>("Ordering", "GenresAlbumOrder", (int)this.AlbumOrder);
            await this.GetAlbumsCommonAsync(this.Albums, this.AlbumOrder);
        }

        private async Task ToggleOrderAsync()
        {

            ToggleGenreOrder();
            SettingsClient.Set<int>("Ordering", "GenresGenreOrder", (int)this.order);
            OrderItems();
            EnsureVisible();
        }

        protected async override Task SetCoversizeAsync(CoverSizeType coverSize)
        {
            await base.SetCoversizeAsync(coverSize);
            SettingsClient.Set<int>("CoverSizes", "GenresCoverSize", (int)coverSize);
        }

        private void EnsureVisible()
        {
            if (SelectedGenres.Count > 0)
                EnsureItemVisible?.Invoke(SelectedGenres[0]);
        }
        protected async override Task FillListsAsync()
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {

                _ignoreSelectionChangedEvent = true;
	            await this.GetItemsAsync();
	            await this.GetGenreAlbumsAsync(this.SelectedGenres, this.AlbumOrder);
	            await this.GetTracksAsync(null, this.SelectedGenres, this.SelectedAlbums, this.TrackOrder);
                _ignoreSelectionChangedEvent = false;
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

            this.SetTrackOrder("GenresTrackOrder");
            await this.GetTracksAsync(null, this.SelectedGenres, this.SelectedAlbums, this.TrackOrder);
        }

        protected override void RefreshLanguage()
        {
            this.UpdateGenreOrderText(this.GenreOrder);
            this.UpdateAlbumOrderText(this.AlbumOrder);
            this.UpdateTrackOrderText(this.TrackOrder);
            base.RefreshLanguage();
        }



        protected virtual void ToggleGenreOrder()
        {
            switch (this.order)
            {
                case GenreOrder.AlphabeticalAscending:
                    this.GenreOrder = GenreOrder.AlphabeticalDescending;
                    break;
                case GenreOrder.AlphabeticalDescending:
                    this.GenreOrder = GenreOrder.ByTrackCount;
                    break;
                case GenreOrder.ByTrackCount:
                    this.GenreOrder = GenreOrder.AlphabeticalDescending;
                    break;
                default:
                    // Cannot happen, but just in case.
                    this.GenreOrder = GenreOrder.AlphabeticalAscending;
                    break;
            }
        }
        protected void UpdateGenreOrderText(GenreOrder order)
        {
            switch (order)
            {
                case GenreOrder.AlphabeticalAscending:
                    this.orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
                case GenreOrder.AlphabeticalDescending:
                    this.orderText = ResourceUtils.GetString("Language_Z_A");
                    break;
                case GenreOrder.ByTrackCount:
                    this.orderText = ResourceUtils.GetString("Language_By_Track_Count");
                    break;
                default:
                    // Cannot happen, but just in case.
                    this.orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
            }

            RaisePropertyChanged(nameof(this.GenreOrderText));
        }
    }
}
