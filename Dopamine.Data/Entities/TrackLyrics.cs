using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("TrackLyrics")]
    public class TrackLyrics
    {
        [Column("track_id"), Indexed(), NotNull()]
        public long TrackId { get; set; }

        [Column("lyrics"), NotNull()]
        public string Lyrics { get; set; }

        [Column("source")]
        public string Source { get; set; }

        [Column("language")]
        public string Language { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
