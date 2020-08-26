using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Services.Metadata;
using Dopamine.Services.Scrobbling;
using Prism.Mvvm;
using System;

namespace Dopamine.Services.Entities
{
    public class TrackViewModel : BindableBase
    {
        private int scaledTrackCoverSize = Convert.ToInt32(Constants.TrackCoverSize * Constants.CoverUpscaleFactor);
        private IMetadataService metadataService;
        private IScrobblingService scrobblingService;
        private bool isPlaying;
        private bool isPaused;
        private bool showTrackNumber;

        public TrackViewModel(IMetadataService metadataService, IScrobblingService scrobblingService, TrackV track)
        {
            this.metadataService = metadataService;
            this.scrobblingService = scrobblingService;
            this.Data = track;
        }

        public string PlaylistEntry { get; set; }

        public bool IsPlaylistEntry => !string.IsNullOrEmpty(this.PlaylistEntry);


        public long Id { get { return Data.Id; } }

        public TrackV Data { get; private set; }


        // SortDuration is used to correctly sort by Length, otherwise sorting goes like this: 1:00, 10:00, 2:00, 20:00.
        public long SortDuration => this.Data.Duration.HasValue ? this.Data.Duration.Value : 0;

        // SortAlbumTitle is used to sort by AlbumTitle, then by TrackNumber.
        public string SortAlbumTitle => this.AlbumTitle + this.Data.TrackNumber.Value.ToString("0000");

        // SortAlbumArtist is used to sort by AlbumArtists, then by AlbumTitle, then by TrackNumber.
        public string SortAlbumArtist => this.AlbumArtist + this.AlbumTitle + this.Data.TrackNumber.Value.ToString("0000");

        // SortArtistName is used to sort by ArtistName, then by AlbumTitle, then by TrackNumber.
        public string SortArtistName => this.ArtistName + this.AlbumTitle + this.Data.TrackNumber.Value.ToString("0000");

        public long SortBitrate => this.Data.BitRate.GetValueOrZero();

        public string SortPlayCount => this.Data.PlayCount.HasValueLargerThan(0) ? this.Data.PlayCount.Value.ToString("0000") : string.Empty;

        public string SortSkipCount => this.Data.SkipCount.HasValueLargerThan(0) ? this.Data.SkipCount.Value.ToString("0000") : string.Empty;

        public long SortTrackNumber => this.Data.TrackNumber.HasValue ? this.Data.TrackNumber.Value : 0;

        public string SortDiscNumber => this.Data.DiscNumber.HasValueLargerThan(0) ? this.Data.DiscNumber.Value.ToString("0000") : string.Empty;

        public long SortDateAdded => this.Data.DateAdded;

        public long SortDateFileCreated => this.Data.DateFileCreated;

        public string DateAdded => this.Data.DateAdded.HasValueLargerThan(0) ? new DateTime(this.Data.DateAdded).ToString("d") : string.Empty;

        public string DateFileCreated => this.Data.DateFileCreated.HasValueLargerThan(0) ? new DateTime(this.Data.DateFileCreated).ToString("d") : string.Empty;

        public bool HasLyrics => this.Data.HasLyrics == 1 ? true : false;

        public string Bitrate => this.Data.BitRate != null ? this.Data.BitRate + " kbps" : "";

        public string AlbumTitle => !string.IsNullOrEmpty(this.Data.AlbumTitle) ? this.Data.AlbumTitle : ResourceUtils.GetString("Language_Unknown_Album");

        public string PlayCount => this.Data.PlayCount.HasValueLargerThan(0) ? this.Data.PlayCount.Value.ToString() : string.Empty;

        public string SkipCount => this.Data.SkipCount.HasValueLargerThan(0) ? this.Data.SkipCount.Value.ToString() : string.Empty;

        public string DateLastPlayed => this.Data.DateLastPlayed.HasValueLargerThan(0) ? new DateTime(this.Data.DateLastPlayed.Value).ToString("g") : string.Empty;

        public long SortDateLastPlayed => this.Data.DateLastPlayed.HasValue ? this.Data.DateLastPlayed.Value : 0;

        public string TrackTitle => string.IsNullOrEmpty(this.Data.TrackTitle) ? this.Data.FileName : this.Data.TrackTitle;

        public string FileName => this.Data.FileName;

        public string Path => this.Data.Path;

        public string SafePath => this.Data.Path;

        public string ArtistName => !string.IsNullOrEmpty(this.Data.Artists) ? DataUtils.GetCommaSeparatedColumnMultiValue(this.Data.Artists) : ResourceUtils.GetString("Language_Unknown_Artist");

        public string AlbumArtist => this.GetAlbumArtist();

        public string Genre => !string.IsNullOrEmpty(this.Data.Genres) ? DataUtils.GetCommaSeparatedColumnMultiValue(this.Data.Genres) : ResourceUtils.GetString("Language_Unknown_Genre");

        public string FormattedTrackNumber => this.Data.TrackNumber.HasValueLargerThan(0) ? Data.TrackNumber.Value.ToString("00") : "--";

        public string TrackNumber => this.Data.TrackNumber.HasValueLargerThan(0) ? this.Data.TrackNumber.ToString() : string.Empty;

        public string DiscNumber => this.Data.DiscNumber.HasValueLargerThan(0) ? this.Data.DiscNumber.ToString() : string.Empty;

        public string Year => this.Data.Year.HasValueLargerThan(0) ? this.Data.Year.Value.ToString() : string.Empty;

        public string GroupHeader => this.Data.DiscCount.HasValueLargerThan(1) && this.Data.DiscNumber.HasValueLargerThan(0) ? $"{this.Data.AlbumTitle} ({this.Data.DiscNumber})" : this.Data.AlbumTitle;

        public string GroupSubHeader => this.AlbumArtist;

        public string GetAlbumArtist()
        {
            if (!string.IsNullOrEmpty(this.Data.AlbumArtists))
            {
                return DataUtils.GetCommaSeparatedColumnMultiValue(this.Data.AlbumArtists);
            }
            else if (!string.IsNullOrEmpty(this.Data.Artists))
            {
                return DataUtils.GetCommaSeparatedColumnMultiValue(this.Data.Artists);
            }

            return ResourceUtils.GetString("Language_Unknown_Artist");
        }

        public string Duration
        {
            get
            {
                if (this.Data.Duration.HasValue)
                {
                    TimeSpan ts = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(this.Data.Duration));

                    if (ts.Hours > 0)
                    {
                        return ts.ToString("hh\\:mm\\:ss");
                    }
                    else
                    {
                        return ts.ToString("m\\:ss");
                    }
                }
                else
                {
                    return "0:00";
                }
            }
        }

        public int Rating
        {
            get { return NumberUtils.ConvertToInt32(this.Data.Rating); }
            set
            {
                // Update the UI
                this.Data.Rating = (long?)value;
                this.RaisePropertyChanged(nameof(this.Rating));

                // Update Rating in the database
                this.metadataService.UpdateTrackRatingAsync(this.Data.Path, value);
            }
        }

        public bool Love
        {
            get { return this.Data.Love.HasValue && this.Data.Love.Value != 0 ? true : false; }
            set
            {
                // Update the UI
                this.Data.Love = value ? 1 : 0;
                this.RaisePropertyChanged(nameof(this.Love));

                // Update Love in the database
                this.metadataService.UpdateTrackLoveAsync(this.Data.Path, value);

                // Send Love/Unlove to the scrobbling service
                this.scrobblingService.SendTrackLoveAsync(this, value);
            }
        }

        public bool IsPlaying
        {
            get { return this.isPlaying; }
            set { SetProperty<bool>(ref this.isPlaying, value); }
        }

        public bool IsPaused
        {
            get { return this.isPaused; }
            set { SetProperty<bool>(ref this.isPaused, value); }
        }

        public bool ShowTrackNumber
        {
            get { return this.showTrackNumber; }
            set { SetProperty<bool>(ref this.showTrackNumber, value); }
        }

        public void UpdateVisibleRating(int rating)
        {
            this.Data.Rating = (long?)rating;
            this.RaisePropertyChanged(nameof(this.Rating));
        }

        public void UpdateVisibleLove(bool love)
        {
            this.Data.Love = love ? 1 : 0;
            this.RaisePropertyChanged(nameof(this.Love));
        }

        public void UpdateVisibleCounters(PlaybackCounter counters)
        {
            this.Data.PlayCount = counters.PlayCount;
            this.Data.SkipCount = counters.SkipCount;
            this.Data.DateLastPlayed = counters.DateLastPlayed;
            this.RaisePropertyChanged(nameof(this.PlayCount));
            this.RaisePropertyChanged(nameof(this.SkipCount));
            this.RaisePropertyChanged(nameof(this.DateLastPlayed));
            this.RaisePropertyChanged(nameof(this.SortDateLastPlayed));
        }

        public override string ToString()
        {
            return this.TrackTitle;
        }

        public TrackViewModel DeepCopy()
        {
            return new TrackViewModel(this.metadataService, this.scrobblingService, this.Data);
        }

        public void UpdateTrack(TrackV track)
        {
            if(track == null)
            {
                return;
            }

            this.Data = track;

            this.RaisePropertyChanged();
        }

        public void Refresh()
        {
            this.RaisePropertyChanged();
        }
    }
}
