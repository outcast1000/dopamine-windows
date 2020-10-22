using Dopamine.Core.Extensions;
using Dopamine.Data.Providers;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("AlbumImages")]
    public class AlbumImage
    {
        [Column("album_id"), PrimaryKey()]
        public long AlbumId { get; set; }

        [Column("location"), NotNull()]
        public string Location { get; set; }

        [Column("origin")]
        public string Origin { get; set; }

        [Column("origin_type_id")]
        public OriginType OriginType { get; set; }

        [Column("date_added")]
        public long DateAdded { get; set; }

    }
}
