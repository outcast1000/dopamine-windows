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
        private ICollectionService collectionService;
        private IIndexingService indexingService;
        private IEventAggregator eventAggregator;
        private double leftPaneWidthPercent;
        private IList<long> selectedIDs;
        private bool _ignoreSelectionChangedEvent;

        public delegate void EnsureSelectedItemVisibleAction(AlbumViewModel item);
        public event EnsureSelectedItemVisibleAction EnsureItemVisible;
        public double LeftPaneWidthPercent
        {
            get { return this.leftPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref this.leftPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", "AlbumsLeftPaneWidthPercent", Convert.ToInt32(value));
            }
        }

        public CollectionAlbumsViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            this.collectionService = container.Resolve<ICollectionService>();
            this.indexingService = container.Resolve<IIndexingService>();
            this.eventAggregator = container.Resolve<IEventAggregator>();

            // Settings
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += async (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
                {
                    this.EnableRating = (bool)e.Entry.Value;
                    this.SetTrackOrder("AlbumsTrackOrder");
                    await this.GetTracksAsync(null, null, this.SelectedAlbums, this.TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
                {
                    this.EnableLove = (bool)e.Entry.Value;
                    this.SetTrackOrder("AlbumsTrackOrder");
                    await this.GetTracksAsync(null, null, this.SelectedAlbums, this.TrackOrder);
                }
            };

            //  Commands
            this.ToggleAlbumOrderCommand = new DelegateCommand(async () => await this.ToggleAlbumOrderAsync());
            this.ToggleTrackOrderCommand = new DelegateCommand(async () => await this.ToggleTrackOrderAsync());
            this.RemoveSelectedTracksCommand = new DelegateCommand(async () => await this.RemoveTracksFromCollectionAsync(this.SelectedTracks), () => !this.IsIndexing);

            // Set the initial AlbumOrder
            this.AlbumOrder = (AlbumOrder)SettingsClient.Get<int>("Ordering", "AlbumsAlbumOrder");

            // Set the initial TrackOrder
            this.SetTrackOrder("AlbumsTrackOrder");

            // Set width of the panels
            this.LeftPaneWidthPercent = SettingsClient.Get<int>("ColumnWidths", "AlbumsLeftPaneWidthPercent");

            // Cover size
            this.SetCoversizeAsync((CoverSizeType)SettingsClient.Get<int>("CoverSizes", "AlbumsCoverSize"));
            LoadSelectedItems();
        }

        private void LoadSelectedItems()
        {
            try
            {
                string s = SettingsClient.Get<String>("State", "SelectedAlbumIDs");
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
            string s = string.Join(",", selectedIDs);
            SettingsClient.Set<String>("State", "SelectedAlbumIDs", s);
        }
        private async Task ToggleTrackOrderAsync()
        {
            base.ToggleTrackOrder();

            SettingsClient.Set<int>("Ordering", "AlbumsTrackOrder", (int)this.TrackOrder);
            await this.GetTracksCommonAsync(this.Tracks, this.TrackOrder);
        }

        private async Task ToggleAlbumOrderAsync()
        {
            base.ToggleAlbumOrder();

            SettingsClient.Set<int>("Ordering", "AlbumsAlbumOrder", (int)this.AlbumOrder);
            await this.GetAlbumsCommonAsync(this.Albums, this.AlbumOrder);
        }

        protected async override Task SetCoversizeAsync(CoverSizeType coverSize)
        {
            await base.SetCoversizeAsync(coverSize);
            SettingsClient.Set<int>("CoverSizes", "AlbumsCoverSize", (int)coverSize);
        }

        private void EnsureVisible()
        {
            if (SelectedAlbums.Count > 0)
                EnsureItemVisible?.Invoke(SelectedAlbums[0]);
        }

        protected async override Task FillListsAsync()
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {

                _ignoreSelectionChangedEvent = true;
                await GetAllAlbumsAsync(this.AlbumOrder);
                await GetTracksAsync(null, null, this.SelectedAlbums, this.TrackOrder);
                _ignoreSelectionChangedEvent = false;
            });
        }

        protected async override Task EmptyListsAsync()
        {
            this.ClearAlbums();
            this.ClearTracks();
        }

        protected async override Task SelectedAlbumsHandlerAsync(object parameter)
        {
            await base.SelectedAlbumsHandlerAsync(parameter);

            this.SetTrackOrder("AlbumsTrackOrder");
            await this.GetTracksAsync(null, null, this.SelectedAlbums, this.TrackOrder);
        }

        protected override void RefreshLanguage()
        {
            this.UpdateAlbumOrderText(this.AlbumOrder);
            this.UpdateTrackOrderText(this.TrackOrder);
            base.RefreshLanguage();
        }
    }
}
