﻿using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.WPF.Controls;
using Dopamine.Data.Entities;
using Dopamine.ViewModels;
using Dopamine.Services.Cache;
using Dopamine.Services.Metadata;
using Dopamine.Services.Playback;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Timers;
using Dopamine.Services.Entities;
using Dopamine.Core.Alex;
using System.Windows;
using GongSolutions.Wpf.DragDrop.Utilities;

namespace Dopamine.ViewModels.Common
{
    public class CoverArtControlViewModel : BindableBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        protected CoverArtViewModel coverArtViewModel;
        protected IPlaybackService playbackService;
        private IMetadataService metadataService;
        private SlideDirection slideDirection;
        private string previousArtwork;
        private string artwork;

        public CoverArtViewModel CoverArtViewModel
        {
            get { return this.coverArtViewModel; }
            set { SetProperty<CoverArtViewModel>(ref this.coverArtViewModel, value); }
        }

        public SlideDirection SlideDirection
        {
            get { return this.slideDirection; }
            set { SetProperty<SlideDirection>(ref this.slideDirection, value); }
        }

        public bool HasImage
        {
            get { return CoverArtViewModel == null ? false : !string.IsNullOrEmpty(CoverArtViewModel.CoverArt); }
        }

        private void ClearArtwork()
        {
            this.CoverArtViewModel = new CoverArtViewModel { CoverArt = null };
            this.artwork = null;
            RaisePropertyChanged(nameof(this.HasImage));
        }

        public CoverArtControlViewModel(IPlaybackService playbackService, IMetadataService metadataService)
        {
            this.playbackService = playbackService;
            this.metadataService = metadataService;

            this.playbackService.PlaybackSuccess += (_, e) =>
            {
                this.SlideDirection = e.IsPlayingPreviousTrack ? SlideDirection.UpToDown : SlideDirection.DownToUp;
                this.RefreshCoverArtAsync(this.playbackService.CurrentTrack);
            };

            this.playbackService.PlayingTrackChanged += (_, __) => this.RefreshCoverArtAsync(this.playbackService.CurrentTrack);

            // Defaults
            this.SlideDirection = SlideDirection.DownToUp;
            this.RefreshCoverArtAsync(this.playbackService.CurrentTrack);
        }


        private System.Threading.SemaphoreSlim _semaphore = new System.Threading.SemaphoreSlim(1);


        protected async virtual void RefreshCoverArtAsync(TrackViewModel track)
        {
            await _semaphore.WaitAsync();
            try 
            {
                if (track == null)
                {
                    this.ClearArtwork();
                    return;
                }
                await Task.Delay(250);

                await Task.Run(() =>
                {
                    this.previousArtwork = this.artwork;

                    // Try to show the album Image
                    this.artwork = track.Data.AlbumImage;// == null ? track.Data.ArtistImage : track.Data.AlbumImage;
                    if (this.artwork == null)
                    {
                        // Album image again but now you will do a request to download it
                        this.artwork = track.GroupAlbumThumbnailSource;
                        track.PropertyChanged += Track_PropertyChanged;
                    }
                    if (this.artwork == null)
                    {
                        // Temporary show the artist image
                        this.artwork = track.Data.ArtistImage;
                    }
                    //this.CoverArtViewModel = new CoverArtViewModel { CoverArt = artwork };

                    // Verify if the artwork changed
                    if (this.artwork == previousArtwork)
                    {
                        return;
                    }
                    else if (this.artwork == null)
                    {
                        this.ClearArtwork();
                        return;
                    }
                    else //if (artwork != null)
                    {
                        try
                        {
                            this.CoverArtViewModel = new CoverArtViewModel { CoverArt = artwork };
                            RaisePropertyChanged(nameof(this.HasImage));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Could not show file artwork for Track {0}. Exception: {1}", track.Path, ex.Message);
                            this.ClearArtwork();
                        }

                        return;
                    }
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void Track_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            string s = sender.GetType().ToString();
            if (sender.GetType().ToString().Equals("Dopamine.Services.Entities.TrackViewModel") && e.PropertyName.Equals("GroupAlbumThumbnailSource"))
            {
                TrackViewModel track = (TrackViewModel)sender;
                // Remove the callback
                track.PropertyChanged -= Track_PropertyChanged;
                // Check if we still have the same Track
                if (playbackService?.CurrentTrack != null && playbackService.CurrentTrack.Id == track.Id)
                {
                    // We still have the same track. Lets Update the CoverArtViewModel
                    if (CoverArtViewModel != null && track.GroupAlbumThumbnailSource != null)
                        CoverArtViewModel.CoverArt = track.GroupAlbumThumbnailSource;
                }
            }
        }
    }
}