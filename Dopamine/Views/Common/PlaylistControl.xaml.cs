using Dopamine.Views.Common.Base;
using Dopamine.Core.Prism;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using Dopamine.ViewModels.Common;
using System.Windows.Controls;
using System.Threading.Tasks;
using System;
using Digimezzo.Foundation.WPF.Controls;
using System.Windows.Media;
using Dopamine.Services.Entities;
using Dopamine.Services.Playback;
using Digimezzo.Foundation.Core.IO;
using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Base;
using Dopamine.Services.Utils;
using Dopamine.Data;

namespace Dopamine.Views.Common
{
    public partial class PlaylistControl : CommonViewBase
    {
        public PlaylistControl()
        {
            InitializeComponent();

            this.ViewInExplorerCommand = new DelegateCommand(() => this.ViewInExplorer(this.ListBoxTracks));
            this.JumpToPlayingTrackCommand = new DelegateCommand(async () => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));

            // PubSub Events
            this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Subscribe(async (_) => await this.ScrollToPlayingTrackAsync(this.ListBoxTracks));
        }
      
        private async void ListBoxTracks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            await this.ActionHandler(sender, e.OriginalSource as DependencyObject, PlaylistMode.Play, false);
        }

        protected override async Task ActionHandler(Object sender, DependencyObject source, PlaylistMode playlistMode, bool includeTheRestOfTheList = false)
        {
            PlaylistControlViewModel vm = (PlaylistControlViewModel)this.DataContext;
            if (vm.InSearchMode)
            {
                //await base.ActionHandler(sender, source, enqueue, false);
            }
            else
            {
                ListBox lb = (ListBox)sender;
                if (lb.SelectedItem == null)
                    return;
                if (source == null)
                    return;
                while (source != null && !(source is MultiSelectListBox.MultiSelectListBoxItem))
                {
                    source = VisualTreeHelper.GetParent(source);
                }
                if (source == null || source.GetType() != typeof(MultiSelectListBox.MultiSelectListBoxItem))
                    return;

                // The user just wants to play the selected item. Don't enqueue.
                if (lb.SelectedItem.GetType().Name == typeof(PlaylistItem).Name)
                {
                    await this.playbackService.SetPlaylistPositionAsync(lb.SelectedIndex, false);
                    //await this.playbackService.PlaySelectedAsync((TrackViewModel)lb.SelectedItem);
                }
            }
        }

        private void ListBoxTracks_KeyUp(object sender, KeyEventArgs e)
        {
            Task unAwaitedTask = this.KeyUpHandlerAsync(sender, e);
        }

        private void ListBoxTracks_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Task unAwaitedTask = this.ActionHandler(sender, e.OriginalSource as DependencyObject, PlaylistMode.Play, true);
            }
        }

        protected override void ViewInExplorer(Object sender)
        {
            try
            {
                // Cast sender to ListBox
                ListBox lb = (ListBox)sender;

                if (lb.SelectedItem != null)
                {
                    Actions.TryViewInExplorer(((PlaylistItem)lb.SelectedItem).TrackViewModel.Data.Path);
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not view track in Windows Explorer. Exception: {0}", ex.Message);
            }
        }

        protected override async Task KeyUpHandlerAsync(object sender, KeyEventArgs e)
        {
            ListBox lb = (ListBox)sender;

            if (e.Key == Key.J && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await this.ScrollToPlayingTrackAsync(lb);

            }
            else if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (lb.SelectedItem != null)
                {
                    Actions.TryViewInExplorer(((PlaylistItem)lb.SelectedItem).TrackViewModel.Data.Path);
                }
            }
        }

        protected override async Task ScrollToPlayingTrackAsync(Object sender)
        {
            try
            {
                // Cast sender to ListBox
                ListBox lb = (ListBox)sender;

                // This should provide a smoother experience because after this wait,
                // other animations on the UI should have finished executing.
                await Task.Delay(Convert.ToInt32(Constants.ScrollToPlayingTrackTimeoutSeconds * 1000));

                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    await ScrollUtils.ScrollToPlayingTrackAsync(lb);
                });
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not scroll to the playing track. Exception: {0}", ex.Message);
            }
        }


    }
}
