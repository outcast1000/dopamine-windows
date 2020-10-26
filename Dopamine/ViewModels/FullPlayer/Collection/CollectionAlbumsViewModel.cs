using Dopamine.Core.Alex;  //Digimezzo.Foundation.Core.Settings
using Dopamine.Core.Base;
using Dopamine.Data;
using Dopamine.Services.Collection;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.ViewModels.Common.Base;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Dopamine.ViewModels.FullPlayer.Collection
{
    public class CollectionAlbumsViewModel : AlbumsViewModelBase
    {
        private ICollectionService _collectionService;
        private IIndexingService _indexingService;
        private IEventAggregator _eventAggregator;
        private double _leftPaneWidthPercent;
        private IList<long> _selectedIDs;
        private bool _ignoreSelectionChangedEvent;

        //public delegate void EnsureSelectedItemVisibleAction(AlbumViewModel item);
        //public event EnsureSelectedItemVisibleAction EnsureItemVisible;
        public double LeftPaneWidthPercent
        {
            get { return _leftPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref _leftPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", "AlbumsLeftPaneWidthPercent", Convert.ToInt32(value));
            }
        }

        public CollectionAlbumsViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            _collectionService = container.Resolve<ICollectionService>();
            _indexingService = container.Resolve<IIndexingService>();
            _eventAggregator = container.Resolve<IEventAggregator>();

            // Commands
            ToggleTrackOrderCommand = new DelegateCommand(async () => await ToggleTrackOrderAsync());
            ToggleAlbumOrderCommand = new DelegateCommand(async () => await ToggleOrderAsync());
            RemoveSelectedTracksCommand = new DelegateCommand(async () => await RemoveTracksFromCollectionAsync(SelectedTracks), () => !IsIndexing);
            // Settings
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += async (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
                {
                    EnableRating = (bool)e.Entry.Value;
                    SetTrackOrder("AlbumsTrackOrder");
                    await GetTracksAsync(null, null, SelectedAlbums, TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
                {
                    EnableLove = (bool)e.Entry.Value;
                    SetTrackOrder("AlbumsTrackOrder");
                    await GetTracksAsync(null, null, SelectedAlbums, TrackOrder);
                }
            };



            // Set the initial AlbumOrder
            AlbumOrder = (AlbumOrder)SettingsClient.Get<int>("Ordering", "AlbumsAlbumOrder");

            // Set the initial TrackOrder
            SetTrackOrder("AlbumsTrackOrder");

            // Set width of the panels
            LeftPaneWidthPercent = SettingsClient.Get<int>("ColumnWidths", "AlbumsLeftPaneWidthPercent");

            // Cover size
            Task unAwaitedTask = SetCoversizeAsync((CoverSizeType)SettingsClient.Get<int>("CoverSizes", "AlbumsCoverSize"));
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
        private async Task ToggleTrackOrderAsync()
        {
            base.ToggleTrackOrder();

            SettingsClient.Set<int>("Ordering", "AlbumsTrackOrder", (int)TrackOrder);
            await GetTracksCommonAsync(Tracks, TrackOrder);
        }

        private async Task ToggleOrderAsync()
        {
            ToggleAlbumOrder();
            SettingsClient.Set<int>("Ordering", "AlbumsAlbumOrder", (int)AlbumOrder);
            EnsureSelectedAlbumVisible();
        }

        protected async override Task SetCoversizeAsync(CoverSizeType coverSize)
        {
            await base.SetCoversizeAsync(coverSize);
            SettingsClient.Set<int>("CoverSizes", "AlbumsCoverSize", (int)coverSize);
        }

        /*
        private void EnsureVisible()
        {
            if (SelectedAlbums.Count > 0)
                EnsureItemVisible?.Invoke(SelectedAlbums[0]);
        }
        */

        protected async override Task FillListsAsync()
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                _ignoreSelectionChangedEvent = true;
                await GetAllAlbumsAsync(AlbumOrder);
                await GetTracksAsync(null, null, SelectedAlbums, TrackOrder);
                _ignoreSelectionChangedEvent = false;
            });
        }

        protected async override Task EmptyListsAsync()
        {
            ClearAlbums();
            ClearTracks();
        }

        protected async override Task SelectedAlbumsHandlerAsync(object parameter)
        {
            await base.SelectedAlbumsHandlerAsync(parameter);

            SetTrackOrder("AlbumsTrackOrder");
            await GetTracksAsync(null, null, SelectedAlbums, TrackOrder);
        }

        protected override void RefreshLanguage()
        {
            UpdateAlbumOrderText(AlbumOrder);
            UpdateTrackOrderText(TrackOrder);
            base.RefreshLanguage();
        }
    }
}
