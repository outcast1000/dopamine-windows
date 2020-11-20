using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Prism;
using Dopamine.Data;
using Dopamine.Services.Entities;
using Dopamine.Services.Playback;
using Dopamine.Services.Utils;
using Dopamine.ViewModels.FullPlayer.Collection;
using Dopamine.Views.Common.Base;
using Prism.Commands;
using Prism.Events;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dopamine.Views.FullPlayer.Collection
{
    public partial class CollectionAlbums : TracksViewBase
    {
        public CollectionAlbums() : base()
        {
            InitializeComponent();

            // Commands
            this.ViewInExplorerCommand = new DelegateCommand(() => this.ViewInExplorer(this.ListBoxTracks));
            this.JumpToPlayingTrackCommand = new DelegateCommand(async () => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));

        }

        private void Vm_SelectionChanged()
        {
            ScrollViewer scrollViewer = (ScrollViewer)VisualTreeUtils.GetDescendantByType(ListBoxTracks, typeof(ScrollViewer));
            scrollViewer?.ScrollToTop();
        }


        SubscriptionToken _stScrollToPlayingTrack;
        SubscriptionToken _stPerformSemanticJump;
        SubscriptionToken _stLocateItemTrackViewModel;
        SubscriptionToken _stLocateItemAlbumViewModel;
        void OnLoad(object sender, RoutedEventArgs e)
        {
            _stScrollToPlayingTrack = this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Subscribe(async (_) => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));

            _stPerformSemanticJump = this.eventAggregator.GetEvent<PerformSemanticJump>().Subscribe(async (data) =>
            {
                try
                {
                    if (data.Item1.Equals("Albums"))
                    {
                        await SemanticZoomUtils.SemanticScrollAsync(this.ListBoxAlbums, data.Item2);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not perform semantic zoom on Albums. Exception: {0}", ex.Message);
                }
            });

            _stLocateItemTrackViewModel = eventAggregator.GetEvent<LocateItem<TrackViewModel>>().Subscribe((TrackViewModel item) => LocateItem(item));
            _stLocateItemAlbumViewModel = eventAggregator.GetEvent<LocateItem<AlbumViewModel>>().Subscribe((AlbumViewModel item) => LocateItem(item));
            
            CollectionAlbumsViewModel vm = (CollectionAlbumsViewModel)DataContext;// I am trying to reset the TrackList when the list changes. This wild hack must be removed
            vm.SelectionChanged += Vm_SelectionChanged;
        }

        void OnUnload(object sender, RoutedEventArgs e)
        {
            this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Unsubscribe(_stScrollToPlayingTrack);
            this.eventAggregator.GetEvent<PerformSemanticJump>().Unsubscribe(_stPerformSemanticJump);
            this.eventAggregator.GetEvent<LocateItem<TrackViewModel>>().Unsubscribe(_stLocateItemTrackViewModel);
            this.eventAggregator.GetEvent<LocateItem<AlbumViewModel>>().Unsubscribe(_stLocateItemAlbumViewModel);
            CollectionAlbumsViewModel vm = (CollectionAlbumsViewModel)DataContext;// I am trying to reset the TrackList when the list changes. This wild hack must be removed
            vm.SelectionChanged -= Vm_SelectionChanged;
        }

        private async void ListBoxAlbums_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, PlaylistMode.Shuffle);
        }



        private async void ListBoxAlbums_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, PlaylistMode.Shuffle);
            }
        }

        private async void ListBoxTracks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CollectionAlbumsViewModel vm = (CollectionAlbumsViewModel)DataContext;
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, PlaylistMode.Play, !vm.InSearchMode);
        }

        private async void ListBoxTracks_KeyUp(object sender, KeyEventArgs e)
        {
            await this.KeyUpHandlerAsync(sender, e);
        }

        private async void ListBoxTracks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CollectionAlbumsViewModel vm = (CollectionAlbumsViewModel)DataContext;
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, PlaylistMode.Play, !vm.InSearchMode);
            }
        }

        private void LocateItem(AlbumViewModel item)
        {
            if (item == null)
                return;
            foreach (var listBoxItem in ListBoxAlbums.Items)
            {
                if (item.Name.Equals(((AlbumViewModel)listBoxItem).Name))
                {
                    ListBoxAlbums.SelectedItem = listBoxItem;
                    ListBoxAlbums.ScrollIntoView(listBoxItem);
                    break;
                }
            }
        }

        private void LocateItem(TrackViewModel item)
        {
            if (item == null)
                return;
            foreach (var listBoxItem in ListBoxAlbums.Items)
            {
                if (item.AlbumTitle.Equals(((AlbumViewModel)listBoxItem).Name))
                {
                    ListBoxAlbums.SelectedItem = listBoxItem;
                    ListBoxAlbums.ScrollIntoView(listBoxItem);
                    SelectTrackId(item.Id);// We use this async function in order to give time to the control to fill with data
                    break;
                }
            }
        }

        private async void SelectTrackId(long trackId)
        {
            await Task.Delay(100);
            foreach (var track in ListBoxTracks.Items)
            {
                if (trackId == ((TrackViewModel)track).Id)
                {
                    ListBoxTracks.SelectedItem = track;
                    ListBoxTracks.ScrollIntoView(track);
                    break;
                }
            }
        }

    }
}
