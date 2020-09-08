using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("AlbumImages")]
    public class AlbumImage
    {
        [Column("id"), PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }

        [Column("album_id"), NotNull()]
        public long AlbumId { get; set; }

        [Column("location"), Unique(), NotNull()]
        public string Location { get; set; }

        [Column("source")]
        public string Source { get; set; }

        [Column("is_primary")]
        public bool? IsPrimary { get; set; }

        [Column("date_added")]
        public long DateAdded { get; set; }

    }
}
