﻿using Digimezzo.Foundation.Core.IO;
using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Utils;
using Dopamine.ViewModels;
using Dopamine.Views.Common.Base;
using Dopamine.Core.Prism;
using Prism.Commands;
using Prism.Events;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dopamine.Services.Entities;
using Dopamine.Services.Utils;
using System.Reflection;
using System.ComponentModel;
using Dopamine.ViewModels.FullPlayer.Collection;
using Dopamine.Services.Playback;
using System.Collections.Generic;
using Dopamine.Data;

namespace Dopamine.Views.FullPlayer.Collection
{
    public partial class CollectionHistoryLog : CommonViewBase
    {
        public CollectionHistoryLog() : base()
        {
            InitializeComponent();

            // Commands
            this.ViewInExplorerCommand = new DelegateCommand(() => this.ViewInExplorer(this.DataGridTracks));
            this.JumpToPlayingTrackCommand = new DelegateCommand(async () => await this.ScrollToPlayingTrackAsync(this.DataGridTracks));

        }


        SubscriptionToken _stScrollToPlayingTrack;
        SubscriptionToken _stLocateItemTrackViewModel;
        void OnLoad(object sender, RoutedEventArgs e)
        {
            _stScrollToPlayingTrack = this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Subscribe(async (_) => await this.ScrollToPlayingTrackAsync(this.DataGridTracks));
            _stLocateItemTrackViewModel = eventAggregator.GetEvent<LocateItem<TrackViewModel>>().Subscribe((TrackViewModel item) => LocateItem(item));
        }

        void OnUnload(object sender, RoutedEventArgs e)
        {
            this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Unsubscribe(_stScrollToPlayingTrack);
            this.eventAggregator.GetEvent<LocateItem<TrackViewModel>>().Unsubscribe(_stLocateItemTrackViewModel);
        }


        private async void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, null, Services.Playback.PlaylistMode.Play);
        }

        private async void DataGridTracks_KeyUp(object sender, KeyEventArgs e)
        {
            await this.KeyUpHandlerAsync(sender, e);
        }

        private async void DataGridTracks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Prevent DataGrid.KeyDown to make the selection to go to the next row when pressing Enter
                e.Handled = true;

                // Makes sure that this action is triggered by a DataGridCell. This prevents  
                // enqueuing when clicking other ListBox elements (e.g. the ScrollBar)
                DataGridCell dataGridCell = VisualTreeUtils.FindAncestor<DataGridCell>((DependencyObject)e.OriginalSource);

                if (dataGridCell == null) return;

                DataGrid dg = (DataGrid)sender;
                await this.ActionHandler(sender, null, PlaylistMode.Play);
            }
        }

        protected async override Task ActionHandler(Object sender, DependencyObject source, PlaylistMode playlistMode, bool includeTheRestOfTheList = false)
        {
            try
            {
                var dg = VisualTreeUtils.FindAncestor<DataGrid>((DataGridRow)sender);
                if (includeTheRestOfTheList)
                    await this.playbackService.PlayTracksAndStartOnTrack(dg.Items.OfType<TrackViewModel>().ToList(), (TrackViewModel)dg.SelectedItem);
                else
                    await this.playbackService.PlayTracksAsync(new List<TrackViewModel>() { (TrackViewModel)dg.SelectedItem }, playlistMode);

            }
            catch (Exception ex)
            {
                LogClient.Error("Error while handling DataGrid action. Exception: {0}", ex.Message);
            }
        }

        protected override async Task KeyUpHandlerAsync(object sender, KeyEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;

            if (e.Key == Key.J && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await this.ScrollToPlayingTrackAsync(dg);

            }
            else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (dg.SelectedItem != null)
                {
                    Actions.TryViewInExplorer(((TrackViewModel)dg.SelectedItem).Path);
                }
            }
        }

        protected override async Task ScrollToPlayingTrackAsync(object sender)
        {
            try
            {
                // Cast sender to ListBox
                DataGrid dg = (DataGrid)sender;

                // This should provide a smoother experience because after this wait,
                // other animations on the UI should have finished executing.
                await Task.Delay(Convert.ToInt32(Constants.ScrollToPlayingTrackTimeoutSeconds * 1000));

                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    TrackViewModel vm = playbackService.CurrentTrack;
                    if (vm != null)
                        await ScrollUtils.ScrollToPlayingTrackAsync(dg, vm.Id);
                });
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not scroll to the playing track. Exception: {1}", ex.Message);
            }
        }

        public void SortDataGrid(DataGrid dataGrid, string sortMemberPath, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            DataGridColumn column = dataGrid.Columns.Where(x => x.SortMemberPath.Equals(sortMemberPath)).FirstOrDefault();

            if(column == null)
            {
                return;
            }

            // Clear current sort descriptions
            dataGrid.Items.SortDescriptions.Clear();

            // Add the new sort description
            dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            // Apply sort
            foreach (var col in dataGrid.Columns)
            {
                col.SortDirection = null;
            }

            column.SortDirection = sortDirection;

            // Refresh items to display sort
            dataGrid.Items.Refresh();
        }

        protected override void ViewInExplorer(object sender)
        {
            try
            {
                // Cast sender to DataGrid
                DataGrid dg = (DataGrid)sender;

                if (dg.SelectedItem != null)
                {
                    Actions.TryViewInExplorer(((TrackViewModel)dg.SelectedItem).Path);
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not view track in Windows Explorer. Exception: {0}", ex.Message);
            }
        }

        private void DataGridTracks_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // See: https://stackoverflow.com/questions/9571178/datagrid-is-there-no-sorted-event
            this.Dispatcher.BeginInvoke((Action)delegate ()
            {
                // Runs after sorting is done
                CollectionUtils.SetColumnSorting(e.Column.SortMemberPath, e.Column.SortDirection);
            }, null);
        }

        private void DataGridTracks_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            if (e.Property == DataGrid.ItemsSourceProperty)
            {
                CollectionUtils.GetColumnSorting(out string sortMemberPath, out ListSortDirection sortDirection);

                if (!string.IsNullOrEmpty(sortMemberPath))
                {
                    this.Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        // Sorting is incorrect when not done via dispatcher. Probably it happens too soon.
                        this.SortDataGrid(this.DataGridTracks, sortMemberPath, sortDirection);
                    }, null);
                }
            }
        }
        private void LocateItem(TrackViewModel item)
        {
            foreach (var listItem in DataGridTracks.Items)
            {
                if (item.Id == ((TrackViewModel)listItem).Id)
                {
                    DataGridTracks.SelectedItem = listItem;
                    DataGridTracks.ScrollIntoView(listItem);
                    break;
                }
            }
        }
    }
}
