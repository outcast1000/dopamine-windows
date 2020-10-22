using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{

    public class TrackV
    {
        public long Id { get; set; }

        public string Genres { get; set; }

        public string AlbumTitle { get; set; }

        public string Artists { get; set; }

        public string AlbumArtists { get; set; }

        public string Path { get; set; }

        //public string MimeType { get; set; }

        public string FileName { get { return System.IO.Path.GetFileName(Path); } }

        public long? FileSize { get; set; }

        public long? BitRate { get; set; }

        public long? SampleRate { get; set; }

        public string TrackTitle { get; set; }

        public long? TrackNumber { get; set; }

        public long? TrackCount { get; set; }

        public long? DiscNumber { get; set; }

        public long? DiscCount { get; set; }

        public long? Duration { get; set; }

        public long? Year { get; set; }

        public long? HasLyrics { get; set; }

        public long DateAdded { get; set; }

        public long DateFileCreated { get; set; }

        public long DateFileModified { get; set; }

        public long DateFileDeleted { get; set; }
        /*
        public long DateLastSynced { get; set; }

        public long? NeedsIndexing { get; set; }

        public long? NeedsAlbumArtworkIndexing { get; set; }
        */
        public long? IndexingSuccess { get; set; }

        //public string IndexingFailureReason { get; set; }

        public long? Rating { get; set; }

        public long? Love { get; set; }

        public long? DateIgnored { get; set; }

        public long FolderID { get; set; }

        public string Language { get; set; }

        public string AlbumImage { get; set; }

        public string ArtistImage { get; set; }

        // History
        public long? PlayCount { get; set; }
        public long? SkipCount { get; set; }// This is expensive
        public long? DateLastPlayed { get; set; }
        public long? DateFirstPlayed { get; set; }
        // History END
        // History Log
        public long? DateHappened { get; set; }
        public HistoryActionType? HistoryActionId { get; set; }

        public static TrackV CreateDefault(string path)
        {
            var track = new TrackV()
            {
                Path = path,
                DateAdded = DateTime.Now.Ticks
            };

            return track;
        }

        public TrackV ShallowCopy()
        {
            return (TrackV)this.MemberwiseClone();
        }


        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return this.Path.Equals(((TrackV)obj).Path);
        }

        public override int GetHashCode()
        {
            return new { this.Path }.GetHashCode();
        }
    }
}
