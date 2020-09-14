using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("AlbumImageFailed")]
    public class AlbumImageFailed
    {
        [Column("album_id"), PrimaryKey()]
        public long AlbumId { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
