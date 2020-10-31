using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Settings;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using Dopamine.Services.Cache;
using Dopamine.Services.Collection;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.Playback;
using Dopamine.Services.Playlist;
using Dopamine.Services.Search;
using Dopamine.Views.Common;
using Prism.Commands;
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


namespace Dopamine.ViewModels.Common.Base
{
    public abstract class AlbumsViewModelBase : TracksViewModelBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IContainerProvider _container;
        private ICollectionService _collectionService;
        private IPlaybackService _playbackService;
        private IDialogService _dialogService;
        private ISearchService _searchService;
        private IPlaylistService _playlistService;
        private IIndexingService _indexingService;
        //private ObservableCollection<AlbumViewModel> albums;
        private CollectionViewSource albumsCvs;
        private IList<AlbumViewModel> _selectedItems;
        private bool delaySelectedAlbums;
        private IList<long> _selectedIDs;
        private long _albumsCount;
		private string _searchString;
       	private string _orderText;
		private AlbumOrder _order;
        private double coverSize;
        private double albumWidth;
        private double albumHeight;
        private CoverSizeType selectedCoverSize;
        
		
		public delegate void EnsureSelectedItemVisibleAction(AlbumViewModel item);
        public event EnsureSelectedItemVisibleAction EnsureItemVisible;

        public delegate void SelectionChangedAction();
        public event SelectionChangedAction SelectionChanged;
		
        public DelegateCommand ToggleAlbumOrderCommand { get; set; }

        public DelegateCommand<string> AddAlbumsToPlaylistCommand { get; set; }

        public DelegateCommand<object> SelectedAlbumsCommand { get; set; }

        public DelegateCommand EditAlbumCommand { get; set; }

        public DelegateCommand AddAlbumsToNowPlayingCommand { get; set; }

        public DelegateCommand<string> SetCoverSizeCommand { get; set; }

        public DelegateCommand DelaySelectedAlbumsCommand { get; set; }

        public DelegateCommand ShuffleSelectedAlbumsCommand { get; set; }
		
		public DelegateCommand<AlbumViewModel> DownloadImageAlbumCommand { get; set; }

        public DelegateCommand<AlbumViewModel> PlayAlbumCommand { get; set; }
		
        public DelegateCommand<AlbumViewModel> EnqueueAlbumCommand { get; set; }
        
		public DelegateCommand<AlbumViewModel> LoveAlbumCommand { get; set; }
		
        public new double UpscaledCoverSize => this.CoverSize * Constants.CoverUpscaleFactor;

        public bool IsSmallCoverSizeSelected => this.selectedCoverSize == CoverSizeType.Small;

        public bool IsMediumCoverSizeSelected => this.selectedCoverSize == CoverSizeType.Medium;

        public bool IsLargeCoverSizeSelected => this.selectedCoverSize == CoverSizeType.Large;

        public string AlbumOrderText => this._orderText;

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

        public bool InSearchMode { get { return !string.IsNullOrEmpty(_searchString); } }

        public CollectionViewSource AlbumsCvs
        {
            get { return this.albumsCvs; }
            set { SetProperty<CollectionViewSource>(ref this.albumsCvs, value); }
        }

        public IList<AlbumViewModel> SelectedAlbums
        {
            get { return this._selectedItems; }
            set
            {
                SetProperty<IList<AlbumViewModel>>(ref this._selectedItems, value);
            }
        }

        public long AlbumsCount
        {
            get { return this._albumsCount; }
            set { SetProperty<long>(ref this._albumsCount, value); }
        }

        public AlbumOrder AlbumOrder
        {
            get { return this._order; }
            set
            {
                SetProperty<AlbumOrder>(ref this._order, value);
                OrderItems();
                this.UpdateAlbumOrderText(value);
            }
        }

        public AlbumsViewModelBase(IContainerProvider container) : base(container)
        {
            // Dependency injection
            this._container = container;
            this._collectionService = container.Resolve<ICollectionService>();
            this._playbackService = container.Resolve<IPlaybackService>();
            this._dialogService = container.Resolve<IDialogService>();
            this._searchService = container.Resolve<ISearchService>();
            this._playlistService = container.Resolve<IPlaylistService>();
            this._indexingService = container.Resolve<IIndexingService>();
            //this.albumArtworkRepository = container.Resolve<IAlbumArtworkRepository>();

            // Commands
            this.ToggleAlbumOrderCommand = new DelegateCommand(() => this.ToggleAlbumOrder());
            this.ShuffleSelectedAlbumsCommand = new DelegateCommand(async () => {
                await this._playbackService.PlayAlbumsAsync(this.SelectedAlbums, PlaylistMode.Play, TrackOrder.Random); 
            });
            this.AddAlbumsToPlaylistCommand = new DelegateCommand<string>(async (playlistName) => await this.AddAlbumsToPlaylistAsync(this.SelectedAlbums, playlistName));
            this.EditAlbumCommand = new DelegateCommand(() => this.EditSelectedAlbum(), () => !this.IsIndexing);
            this.AddAlbumsToNowPlayingCommand = new DelegateCommand(async () => await this.AddAlbumsToNowPlayingAsync(this.SelectedAlbums));

            DownloadImageAlbumCommand = new DelegateCommand<AlbumViewModel>((album) =>
            {
                Task unwaitedTask = album.RequestImageDownload(true, true);
            });
            PlayAlbumCommand = new DelegateCommand<AlbumViewModel>(async (vm) => {
                await _playbackService.PlayAlbumsAsync(new List<AlbumViewModel>() { vm }, PlaylistMode.Play);
            });
            EnqueueAlbumCommand = new DelegateCommand<AlbumViewModel>(async (vm) => await _playbackService.PlayAlbumsAsync(new List<AlbumViewModel>() { vm }, PlaylistMode.Enqueue));
            LoveAlbumCommand = new DelegateCommand<AlbumViewModel>((avm) => Debug.Assert(false, "ALEX TODO"));
			
			this.DelaySelectedAlbumsCommand = new DelegateCommand(() => this.delaySelectedAlbums = true);

            // Events
            //this.indexingService.AlbumArtworkAdded += async (_, e) => await this.RefreshAlbumArtworkAsync(e.AlbumKeys);

            this.SelectedAlbumsCommand = new DelegateCommand<object>(async (parameter) =>
            {
                if (this.delaySelectedAlbums)
                {
                    await Task.Delay(Constants.DelaySelectedAlbumsDelay);
                }

                this.delaySelectedAlbums = false;
                await this.SelectedAlbumsHandlerAsync(parameter);
            });

            this.SetCoverSizeCommand = new DelegateCommand<string>(async (coverSize) =>
            {
                if (int.TryParse(coverSize, out int selectedCoverSize))
                {
                    await this.SetCoversizeAsync((CoverSizeType)selectedCoverSize);
                }
            });
            LoadSelectedItems();
        }


        private void LoadSelectedItems()
        {
            try
            {
                string s = SettingsClient.Get<String>("State", "SelectedAlbumIDs");
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
            SettingsClient.Set<String>("State", "SelectedAlbumIDs", s);
        }


        private void EditSelectedAlbum()
        {
            if (this.SelectedAlbums == null || this.SelectedAlbums.Count == 0)
            {
                return;
            }

            EditAlbum view = this._container.Resolve<EditAlbum>();
            view.DataContext = this._container.Resolve<Func<AlbumViewModel, EditAlbumViewModel>>()(this.SelectedAlbums.First());

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


        protected void UpdateAlbumOrderText(AlbumOrder albumOrder)
        {
            switch (albumOrder)
            {
                case AlbumOrder.AlphabeticalAscending:
                    this._orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
                case AlbumOrder.AlphabeticalDescending:
                    this._orderText = ResourceUtils.GetString("Language_Z_A");
                    break;
                case AlbumOrder.ByDateAdded:
                    this._orderText = ResourceUtils.GetString("Language_By_Date_Added");
                    break;
                case AlbumOrder.ByAlbumArtistAscending:
                    this._orderText = ResourceUtils.GetString("Language_By_Album_Artist") + " (\u2191)";
                    break;
                case AlbumOrder.ByAlbumArtistDescending:
                    this._orderText = ResourceUtils.GetString("Language_By_Album_Artist") + " (\u2193)";
                    break;
                case AlbumOrder.ByYearDescending:
                    this._orderText = ResourceUtils.GetString("Language_By_Year_Descending");
                    break;
                case AlbumOrder.ByYearAscending:
                    this._orderText = ResourceUtils.GetString("Language_By_Year_Ascending");
                    break;
                default:
                    // Cannot happen, but just in case.
                    this._orderText = ResourceUtils.GetString("Language_A_Z");
                    break;
            }

            RaisePropertyChanged(nameof(this.AlbumOrderText));
        }

        protected async Task GetArtistAlbumsAsync(IList<ArtistViewModel> selectedArtists, AlbumOrder albumOrder)
        {
            await this.GetAlbumsCommonAsync(await this._collectionService.GetArtistAlbumsAsync(selectedArtists), albumOrder);
        }

        protected async Task GetFilteredAlbumsAsync(string searchFilter, AlbumOrder albumOrder)
        {
            await this.GetAlbumsCommonAsync(await this._collectionService.GetAlbumsAsync(searchFilter), albumOrder);
        }

        protected async Task GetGenreAlbumsAsync(IList<GenreViewModel> selectedGenres, AlbumOrder albumOrder)
        {
            if (!selectedGenres.IsNullOrEmpty())
            {
                await this.GetAlbumsCommonAsync(await this._collectionService.GetGenreAlbumsAsync(selectedGenres), albumOrder);

                return;
            }

            await this.GetAlbumsCommonAsync(await this._collectionService.GetAlbumsAsync(), albumOrder);
        }

        protected async Task GetAllAlbumsAsync(AlbumOrder albumOrder)
        {
            await this.GetAlbumsCommonAsync(await this._collectionService.GetAlbumsAsync(), albumOrder);
        }

        protected void ClearAlbums()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.AlbumsCvs = null;
            });
        }

        protected async Task GetAlbumsCommonAsync(IList<AlbumViewModel> viewModels, AlbumOrder albumOrder)
        {
			ObservableCollection<AlbumViewModel> items;
            try
            {
                // Get the viewModels
                items = new ObservableCollection<AlbumViewModel>(viewModels);// Using history
                // Unless we are in Search Mode, we should re-store the selected items. The cases are:
                //  1. at the beginning of the application
                //  2. after the search mode is finished 
                if (string.IsNullOrEmpty(_searchString))
                {
                    _selectedItems = new List<AlbumViewModel>();
                    foreach (long id in _selectedIDs)
                    {
                        AlbumViewModel vm = viewModels.Where(x => x.Id == id).FirstOrDefault();
                        if (vm != null)
                        {
                            vm.IsSelected = true;
                            _selectedItems.Add(vm);
                        }
                    }
                    if (_selectedItems.Count == 0 && viewModels.Count > 0)
                    {
                        // This may happen when
                        //  1. The collection was previously empty
                        //  2. The collection with the previous selection has been removed
                        //  3. The previous selection has been removed and the collection has been refreshed
                        AlbumViewModel sel = viewModels[0];
                        sel.IsSelected = true;
                        _selectedItems.Add(sel);
                        _selectedIDs.Add(sel.Id);
                        SaveSelectedItems();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while getting Items. Exception: {0}", ex.Message);
                items = new ObservableCollection<AlbumViewModel>();
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Populate CollectionViewSource
                AlbumsCvs = new CollectionViewSource { Source = items };
                OrderItems();
                EnsureSelectedAlbumVisible();
                AlbumsCount = AlbumsCvs.View.Cast<AlbumViewModel>().Count();
            });

            // Set Album artwork
            //this.LoadAlbumArtworkAsync(Constants.ArtworkLoadDelay);
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
            if (albumsCvs != null)
            {
                AlbumsCvs.SortDescriptions.Clear();
                AlbumsCvs.SortDescriptions.Add(sd);
            }
        }

        protected async virtual Task SelectedAlbumsHandlerAsync(object parameter)
        {
            // This happens when the user select an item
            // We should also ignore it if we are in Search Mode AND the user does not selected anything. For example when we enter the search mode
            if (!string.IsNullOrEmpty(_searchString) && ((IList)parameter).Count == 0)
                return;
            // We should also ignore it if we have an empty list (for example when we clear the list)
            if (AlbumsCvs == null)
                return;
            bool bKeepOldSelections = true;
            if (parameter != null && ((IList)parameter).Count > 0)
            {
				// This is the most usual case. The user has just selected one or more items
                bKeepOldSelections = false;
                List<AlbumViewModel> selectedAlbums = new List<AlbumViewModel>();
                _selectedIDs.Clear();
                foreach (AlbumViewModel item in (IList)parameter)
                {
                    selectedAlbums.Add(item);
                    _selectedIDs.Add(item.Id);
                    // Mark it as selected
                    item.IsSelected = true;
                }
				SelectedAlbums = selectedAlbums;
                SaveSelectedItems();
            }
            
            if (bKeepOldSelections)
            {
                // Keep the previous selection if possible. Otherwise select All
                // This is the case when we have refresh the collection etc.
                List<long> validSelectedIDs = new List<long>();
                _selectedItems.Clear();
                IEnumerable<AlbumViewModel> albums = AlbumsCvs.View.Cast<AlbumViewModel>();
                foreach (long id in _selectedIDs)
                {
                    AlbumViewModel sel = albums.Where(x => x.Id == id).FirstOrDefault();
                    if (sel != null)
                    {
                        validSelectedIDs.Add(id);
                        sel.IsSelected = true;
                        _selectedItems.Add(sel);
                    }
                }
                _selectedIDs = validSelectedIDs;

            }
    		SelectionChanged?.Invoke();
        }
        protected async Task AddAlbumsToPlaylistAsync(IList<AlbumViewModel> albumViewModels, string playlistName)
        {
            CreateNewPlaylistResult addPlaylistResult = CreateNewPlaylistResult.Success; // Default Success

            // If no playlist is provided, first create one.
            if (playlistName == null)
            {
                var responseText = ResourceUtils.GetString("Language_New_Playlist");

                if (this._dialogService.ShowInputDialog(
                    0xea37,
                    16,
                    ResourceUtils.GetString("Language_New_Playlist"),
                    ResourceUtils.GetString("Language_Enter_Name_For_Playlist"),
                    ResourceUtils.GetString("Language_Ok"),
                    ResourceUtils.GetString("Language_Cancel"),
                    ref responseText))
                {
                    playlistName = responseText;
                    addPlaylistResult = await this._playlistService.CreateNewPlaylistAsync(new EditablePlaylistViewModel(playlistName, PlaylistType.Static));
                }
            }

            // If playlist name is still null, the user clicked cancel on the previous dialog. Stop here.
            if (playlistName == null)
            {
                return;
            }

            // Verify if the playlist was added
            switch (addPlaylistResult)
            {
                case CreateNewPlaylistResult.Success:
                case CreateNewPlaylistResult.Duplicate:
                    // Add items to playlist
                    AddTracksToPlaylistResult result = await this._playlistService.AddAlbumsToStaticPlaylistAsync(albumViewModels, playlistName);

                    if (result == AddTracksToPlaylistResult.Error)
                    {
                        this._dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetString("Language_Error"), ResourceUtils.GetString("Language_Error_Adding_Songs_To_Playlist").Replace("{playlistname}", "\"" + playlistName + "\""), ResourceUtils.GetString("Language_Ok"), true, ResourceUtils.GetString("Language_Log_File"));
                    }
                    break;
                case CreateNewPlaylistResult.Error:
                    this._dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Error_Adding_Playlist"),
                        ResourceUtils.GetString("Language_Ok"),
                        true,
                        ResourceUtils.GetString("Language_Log_File"));
                    break;
                case CreateNewPlaylistResult.Blank:
                    this._dialogService.ShowNotification(
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

        protected async Task AddAlbumsToNowPlayingAsync(IList<AlbumViewModel> albumViewModel)
        {
            await this._playbackService.PlayAlbumsAsync(albumViewModel, PlaylistMode.Enqueue);
        }
		
		protected void EnsureSelectedAlbumVisible()
        {
            if (SelectedAlbums.Count > 0)
                EnsureItemVisible?.Invoke(SelectedAlbums[0]);
        }





        protected override async void FilterListsAsync(string searchText)
        {
            _searchString = searchText;
            await GetFilteredAlbumsAsync(_searchString, AlbumOrder);
            base.FilterListsAsync(searchText);
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
		


		
		protected override void SetEditCommands()
        {
            base.SetEditCommands();

            if (this.EditAlbumCommand != null)
            {
                this.EditAlbumCommand.RaiseCanExecuteChanged();
            }
        }
		
		protected virtual async Task SetCoversizeAsync(CoverSizeType coverSize)
        {
            await Task.Run(() =>
            {
                this.selectedCoverSize = coverSize;

                switch (coverSize)
                {
                    case CoverSizeType.Small:
                        this.CoverSize = Constants.CoverSmallSize;
                        break;
                    case CoverSizeType.Medium:
                        this.CoverSize = Constants.CoverMediumSize;
                        break;
                    case CoverSizeType.Large:
                        this.CoverSize = Constants.CoverLargeSize;
                        break;
                    default:
                        this.CoverSize = Constants.CoverMediumSize;
                        this.selectedCoverSize = CoverSizeType.Medium;
                        break;
                }

                // this.AlbumWidth = this.CoverSize + Constants.AlbumTilePadding.Left + Constants.AlbumTilePadding.Right + Constants.AlbumTileMargin.Left + Constants.AlbumTileMargin.Right;
                this.AlbumWidth = this.CoverSize + Constants.AlbumTileMargin.Left + Constants.AlbumTileMargin.Right;
                this.AlbumHeight = this.AlbumWidth + Constants.AlbumTileAlbumInfoHeight + Constants.AlbumSelectionBorderSize;

                RaisePropertyChanged(nameof(this.CoverSize));
                RaisePropertyChanged(nameof(this.AlbumWidth));
                RaisePropertyChanged(nameof(this.AlbumHeight));
                RaisePropertyChanged(nameof(this.UpscaledCoverSize));
                RaisePropertyChanged(nameof(this.IsSmallCoverSizeSelected));
                RaisePropertyChanged(nameof(this.IsMediumCoverSizeSelected));
                RaisePropertyChanged(nameof(this.IsLargeCoverSizeSelected));
            });
        }
    }
}
