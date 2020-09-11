using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("GenreImages")]
    public class GenreImages
    {
        [Column("id"), PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }

        [Column("genre_id"), NotNull(), Indexed()]
        public long ArtistId { get; set; }

        [Column("location"), NotNull()]
        public string Location { get; set; }

        [Column("is_primary")]
        public bool? IsPrimary { get; set; }

        [Column("source")]
        public string Source { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
