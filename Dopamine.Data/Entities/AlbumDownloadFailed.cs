using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("AlbumDownloadFailed")]
    public class AlbumDownloadFailed
    {
        [Column("id"), PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }

        [Column("album_id"), NotNull(), Indexed()]
        public long AlbumId { get; set; }

        [Column("provider"), NotNull(), Indexed()]
        public string Provider { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
