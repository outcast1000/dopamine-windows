using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("AlbumDownloadFailed")]
    public class AlbumDownloadFailed
    {

        [Column("album_id"), NotNull()]
        public long AlbumId { get; set; }

        [Column("provider"), NotNull()]
        public string Provider { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
