using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Prism;
using Dopamine.Services.Entities;
using Dopamine.Services.Utils;
using Dopamine.ViewModels.FullPlayer.Collection;
using Dopamine.Views.Common.Base;
using Prism.Commands;
using System;
using System.Diagnostics;
using System.Windows;
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
            vm.EnsureItemVisible += (ArtistViewModel artist) =>
            {
                //var item = ListBoxArtists.Items.GetItemAt(10);
                ListBoxArtists.ScrollIntoView(artist);// ListBoxArtists.SelectedItem);
            };
        }

        private async void ListBoxArtists_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
        }


        private async void ListBoxArtists_ItemPlayClick(object sender, MouseButtonEventArgs e)
        {
            //await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
            Debug.Print("Test");
            //await this.playbackService.EnqueueArtistsAsync(new List<ArtistViewModel> { ((ArtistViewModel)lb.SelectedItem) }, false, false);
        }

        private async void ListBoxArtists_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
            }
        }

        private async void ListBoxAlbums_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
        }

        private async void ListBoxAlbums_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
            }
        }

        private async void ListBoxTracks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
        }

        private async void ListBoxTracks_KeyUp(object sender, KeyEventArgs e)
        {
            await this.KeyUpHandlerAsync(sender, e);
        }

        private async void ListBoxTracks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, true);
            }
        }

        private void AlbumsButton_Click(object sender, RoutedEventArgs e)
        {
            this.ListBoxAlbums.SelectedItem = null;
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
