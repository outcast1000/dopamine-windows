using Dopamine.Core.Extensions;
using SQLite;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Dopamine.Data.Entities
{
    public class ArtistV
    {
        public ArtistV() { }

        public long Id { get; set; }

        public string Name { get; set; }

        public long TrackCount { get; set; }

        public long GenreCount { get; set; }

        public string Genres { get; set; }

        public long AlbumCount { get; set; }

        public string Albums { get; set; }

        public string Thumbnail { get; set; }

        public string ArtistImage { get; set; }

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
