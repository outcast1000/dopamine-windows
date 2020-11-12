using Digimezzo.Foundation.WPF.Controls;
using Dopamine.Core.Alex;
using Dopamine.Core.Base;
using Dopamine.Core.Enums;
using Dopamine.Core.Prism;
using Dopamine.Views.FullPlayer.Collection;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System.Windows;

namespace Dopamine.ViewModels.FullPlayer.Collection
{
    public class CollectionViewModel : BindableBase
    {
        private int slideInFrom;
        private IRegionManager regionManager;
        private readonly string Settings_NameSpace = "Collection";

        public int SlideInFrom
        {
            get { return this.slideInFrom; }
            set { SetProperty<int>(ref this.slideInFrom, value); }
        }

        // Comment: We need to keep Both Left & Right PaneWidth because we need to work with *. And we need to work with * because otherwise the Gridsplitter do not respect the min Widths.
        //      In the previous implementation i was able to keep only the LeftPaneWidth in pixes but i had the forementioning problem.
        //      Maybe there will be a better way
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

        public CollectionViewModel(IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            this.regionManager = regionManager;

            eventAggregator.GetEvent<IsCollectionPageChanged>().Subscribe(tuple =>
            {
                this.NagivateToPage(tuple.Item1, tuple.Item2);
            });
            LeftPaneWidth = CollectionUtils.String2GridLength(SettingsClient.Get<string>(Settings_NameSpace, CollectionUtils.Setting_LeftPaneGridLength));
            RightPaneWidth = CollectionUtils.String2GridLength(SettingsClient.Get<string>(Settings_NameSpace, CollectionUtils.Setting_RightPaneGridLength));
        }

        private void NagivateToPage(SlideDirection direction, CollectionPage page)
        {
            this.SlideInFrom = direction == SlideDirection.RightToLeft ? Constants.SlideDistance : -Constants.SlideDistance;

            switch (page)
            {
                case CollectionPage.Artists:
                    this.regionManager.RequestNavigate(RegionNames.CollectionRegion, typeof(CollectionArtists).FullName);
                    break;
                case CollectionPage.Genres:
                    this.regionManager.RequestNavigate(RegionNames.CollectionRegion, typeof(CollectionGenres).FullName);
                    break;
                case CollectionPage.Albums:
                    this.regionManager.RequestNavigate(RegionNames.CollectionRegion, typeof(CollectionAlbums).FullName);
                    break;
                case CollectionPage.Songs:
                    this.regionManager.RequestNavigate(RegionNames.CollectionRegion, typeof(CollectionTracks).FullName);
                    break;
                case CollectionPage.Playlists:
                    this.regionManager.RequestNavigate(RegionNames.CollectionRegion, typeof(CollectionPlaylists).FullName);
                    break;
                case CollectionPage.Folders:
                    this.regionManager.RequestNavigate(RegionNames.CollectionRegion, typeof(CollectionFolders).FullName);
                    break;
                case CollectionPage.History:
                    this.regionManager.RequestNavigate(RegionNames.CollectionRegion, typeof(CollectionHistoryLog).FullName);
                    // ALEX TODO. Re-enable this in the same tab // this.regionManager.RequestNavigate(RegionNames.CollectionRegion, typeof(CollectionHistory).FullName);
                    break;
                default:
                    break;
            }
        }
    }
}
