using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Prism;
using Dopamine.Services.Entities;
using Dopamine.Services.Utils;
using Dopamine.ViewModels.FullPlayer.Collection;
using Dopamine.Views.Common.Base;
using Prism.Commands;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dopamine.Views.FullPlayer.Collection
{
    public partial class CollectionArtists : TracksViewBase
    {
        public CollectionArtists() : base()
        {
            InitializeComponent();

            // Commands
            this.ViewInExplorerCommand = new DelegateCommand(() => this.ViewInExplorer(this.ListBoxTracks));
            this.JumpToPlayingTrackCommand = new DelegateCommand(async () => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));

            // PubSub Events
            this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Subscribe(async (_) => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));

            this.eventAggregator.GetEvent<PerformSemanticJump>().Subscribe(async (data) =>
            {
                try
                {
                    if (data.Item1.Equals("Artists"))
                    {
                        await SemanticZoomUtils.SemanticScrollAsync(this.ListBoxArtists, data.Item2);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not perform semantic zoom on Artists. Exception: {0}", ex.Message);
                }
            });

            CollectionArtistsViewModel vm = (CollectionArtistsViewModel)DataContext;
            vm.EnsureItemVisible += (ArtistViewModel item) =>
            {
                //NLog.LogManager.GetLogger("TEMP").Debug("EnsureVisible is disabled");
                ListBoxArtists.ScrollIntoView(item);
            };
            vm.SelectionChanged += () =>
            {
                ScrollViewer scrollViewer = (ScrollViewer)VisualTreeUtils.GetDescendantByType(ListBoxTracks, typeof(ScrollViewer));
                scrollViewer?.ScrollToTop();
            };

        }

        private async void ListBoxArtists_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true, true);
        }



        private async void ListBoxArtists_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true, true);
            }
        }

        private async void ListBoxTracks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CollectionArtistsViewModel vm = (CollectionArtistsViewModel)DataContext;
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, false, !vm.InSearchMode);
        }

        private async void ListBoxTracks_KeyUp(object sender, KeyEventArgs e)
        {
            await this.KeyUpHandlerAsync(sender, e);
        }

        private async void ListBoxTracks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CollectionArtistsViewModel vm = (CollectionArtistsViewModel)DataContext;
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, false, !vm.InSearchMode);
            }
        }

        public void listBox_ScrollToSelectedItem(object sender, RoutedEventArgs e)
        {
            //ListBoxArtists.SelectedItem = lv.Items.GetItemAt(rows.Count - 1);
            ListBoxArtists.ScrollIntoView(ListBoxArtists.SelectedItem);
            //ListViewItem item = ListBoxArtists.ItemContainerGenerator.ContainerFromItem(lv.SelectedItem) as ListViewItem;
            //item.Focus();
        }


    }
}
