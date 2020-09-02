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

        [Column("path"), Unique(), NotNull()]
        public string Path { get; set; }

        [Column("file_size"), NotNull()]
        public long FileSize { get; set; }

        [Column("source_hash"), NotNull()]
        public string SourceHash { get; set; }

        [Column("source")]
        public string Source { get; set; }

        [Column("date_added")]
        public long DateAdded { get; set; }

    }
}
