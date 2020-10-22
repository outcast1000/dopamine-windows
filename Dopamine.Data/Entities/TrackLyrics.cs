using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("TrackLyrics")]
    public class TrackLyrics
    {
        [Column("track_id"), PrimaryKey()]
        public long TrackId { get; set; }

        [Column("lyrics"), NotNull()]
        public string Lyrics { get; set; }

        [Column("origin")]
        public string Origin { get; set; }

        [Column("origin_type_id")]
        public OriginType OriginType { get; set; }

        [Column("language")]
        public string Language { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
