using Digimezzo.Foundation.Core.Logging;
using Dopamine.Views.Common.Base;
using Dopamine.Core.Prism;
using Dopamine.Utils;
using Dopamine.ViewModels;
using Prism.Commands;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dopamine.Services.Utils;
using Dopamine.Services.Entities;
using Dopamine.ViewModels.FullPlayer.Collection;

namespace Dopamine.Views.FullPlayer.Collection
{
    public partial class CollectionGenres : TracksViewBase
    {
        public CollectionGenres() : base()
        {
            InitializeComponent();

            // Commands
            this.ViewInExplorerCommand = new DelegateCommand(() => this.ViewInExplorer(this.ListBoxTracks));
            this.JumpToPlayingTrackCommand = new DelegateCommand(async () => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));

            // PubSub Events
            this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Subscribe(async (_) => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));

            this.eventAggregator.GetEvent<PerformSemanticJump>().Subscribe(async (data) => {
                try
                {
                    if (data.Item1.Equals("Genres"))
                    {
                        await SemanticZoomUtils.SemanticScrollAsync(this.ListBoxGenres, data.Item2);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not perform semantic zoom on Genres. Exception: {0}", ex.Message);
                }
            });

            CollectionGenresViewModel vm = (CollectionGenresViewModel)DataContext;
            vm.EnsureItemVisible += (GenreViewModel genreViewModel) =>
            {
                ListBoxGenres.ScrollIntoView(genreViewModel);// ListBoxArtists.SelectedItem);
            };
        }

        protected async Task SemanticScrollToGenreAsync(ListBox listBox, string letter)
        {
            await Task.Run(() =>
            {
                try
                {
                    foreach (GenreViewModel genre in listBox.Items)
                    {

                        if (SemanticZoomUtils.GetGroupHeader(genre.Name).ToLower().Equals(letter.ToLower()))
                        {
                            // We can only access the ListBox from the UI Thread
                            Application.Current.Dispatcher.Invoke(() => listBox.ScrollIntoView(genre));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not perform semantic scroll Genre. Exception: {0}", ex.Message);
                }

            });
        }

        private async void ListBoxGenres_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, Services.Playback.PlaylistMode.Shuffle);
        }

        private async void ListBoxGenres_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, Services.Playback.PlaylistMode.Shuffle);
            }
        }

        private async void ListBoxAlbums_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CollectionGenresViewModel vm = (CollectionGenresViewModel)DataContext;
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, Services.Playback.PlaylistMode.Play, !vm.InSearchMode);
        }

        private async void ListBoxAlbums_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, Services.Playback.PlaylistMode.Shuffle);
            }
        }

        private async void ListBoxTracks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CollectionGenresViewModel vm = (CollectionGenresViewModel)DataContext;
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, Services.Playback.PlaylistMode.Play, !vm.InSearchMode);
        }

        private async void ListBoxTracks_KeyUp(object sender, KeyEventArgs e)
        {
            await this.KeyUpHandlerAsync(sender, e);
        }

        private async void ListBoxTracks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CollectionGenresViewModel vm = (CollectionGenresViewModel)DataContext;
                await this.ActionHandler(sender, e.OriginalSource as DependencyObject, Services.Playback.PlaylistMode.Play, !vm.InSearchMode);
            }
        }

        private void GenresButton_Click(object sender, RoutedEventArgs e)
        {
            this.ListBoxGenres.SelectedItem = null;
        }
    }
}
