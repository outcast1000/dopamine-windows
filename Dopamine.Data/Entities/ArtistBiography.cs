using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("ArtistBiographies")]
    public class ArtistBiography
    {
        [Column("artist_id"), PrimaryKey(), NotNull()]
        public long ArtistId { get; set; }

        [Column("biography"), NotNull()]
        public string Biography { get; set; }

        [Column("origin")]
        public string Origin { get; set; }

        [Column("origin_type_id")]
        public OriginType OriginType { get; set; }

        [Column("language")]
        public string Language { get; set; }

        [Column("date_added")]
        public long DateAdded { get; set; }

    }
}
