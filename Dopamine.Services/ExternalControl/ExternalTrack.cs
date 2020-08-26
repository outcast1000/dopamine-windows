using Dopamine.Services.Entities;

namespace Dopamine.Services.ExternalControl
{
    /// <summary>
    /// ExternalControlServer can't use TrackViewModel directly, as it is not serializable.
    /// </summary>
    public class ExternalTrack
    {
        private TrackViewModel trackViewModel;

        public ExternalTrack()
        {
        }

        public ExternalTrack(TrackViewModel trackViewModel)
        {
            this.trackViewModel = trackViewModel;

            this.BitRate = this.trackViewModel?.Data?.BitRate != null ? this.trackViewModel.Data.BitRate.Value : 0;
            this.Duration = this.trackViewModel?.Data?.Duration != null ? this.trackViewModel.Data.Duration.Value : 0;
            this.FileName = !string.IsNullOrEmpty(this.trackViewModel.Data.FileName) ? this.trackViewModel.Data.FileName : "";
            this.Path = !string.IsNullOrEmpty(this.trackViewModel.Data.Path) ? this.trackViewModel.Data.Path : "";
            this.SampleRate = this.trackViewModel?.Data?.SampleRate != null ? this.trackViewModel.Data.SampleRate.Value : 0;
            this.TrackNumber = this.trackViewModel?.Data?.TrackNumber != null ? this.trackViewModel.Data.TrackNumber.Value.ToString() : "";
            this.TrackTitle = !string.IsNullOrEmpty(this.trackViewModel.TrackTitle) ? this.trackViewModel.TrackTitle : "";
            this.Year = !string.IsNullOrEmpty(this.trackViewModel.Year) ? this.trackViewModel.Year : "";
            this.AlbumArtist = !string.IsNullOrEmpty(this.trackViewModel.AlbumArtist) ? this.trackViewModel.AlbumArtist : "";
            this.AlbumTitle = !string.IsNullOrEmpty(this.trackViewModel.AlbumTitle) ? this.trackViewModel.AlbumTitle : "";
            this.ArtistName = !string.IsNullOrEmpty(this.trackViewModel.ArtistName) ? this.trackViewModel.ArtistName : "";
            this.Genre = !string.IsNullOrEmpty(this.trackViewModel.Genre) ? this.trackViewModel.Genre : "";
            this.Love = this.trackViewModel?.Data?.Love != null ? this.trackViewModel.Data.Love.Value : 0;
            this.PlayCount = this.trackViewModel?.Data?.PlayCount != null ? this.trackViewModel.Data.PlayCount.Value : 0;
            this.Rating = this.trackViewModel?.Data?.Rating != null ? this.trackViewModel.Data.Rating.Value : 0;
            this.SkipCount = this.trackViewModel?.Data?.SkipCount != null ? this.trackViewModel.Data.SkipCount.Value : 0;
        }

        public long BitRate { get; set; }

        public long Duration { get; set; }

        public string FileName { get; set; }

        public string Path { get; set; }

        public long SampleRate { get; set; }

        public string TrackNumber { get; set; }

        public string TrackTitle { get; set; }

        public string Year { get; set; }

        public string AlbumArtist { get; set; }

        public string AlbumTitle { get; set; }

        public string ArtistName { get; set; }

        public string Genre { get; set; }

        public long Love { get; set; }

        public long PlayCount { get; set; }

        public long Rating { get; set; }

        public long SkipCount { get; set; }
    }
}
