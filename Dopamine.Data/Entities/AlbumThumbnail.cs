using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("AlbumThumbnail")]
    public class AlbumThumbnail
    {
        [Column("album_id"), Unique()]
        public long AlbumId { get; set; }

        [Column("album_image_id"), Indexed(), NotNull()]
        public long AlbumIndexId { get; set; }

    }
}
