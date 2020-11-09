using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    public class GenreV
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public long TrackCount { get; set; }

        public long AlbumCount { get; set; }

        public long ArtistCount { get; set; }

        public string Thumbnail { get; set; }

        public string Artists { get; set; }

        public long? MinYear { get; set; }

        public long? MaxYear { get; set; }

        public DateTime MinDateAdded { get; set; }

        public DateTime MaxDateAdded { get; set; }

        public DateTime MinDateFileCreated { get; set; }

        public DateTime MaxDateFileCreated { get; set; }

        // History
        public long? PlayCount { get; set; }
        public long? SkipCount { get; set; }// This is expensive
        public long? DateLastPlayed { get; set; }
        public long? DateFirstPlayed { get; set; }
        // History END


    }
}
