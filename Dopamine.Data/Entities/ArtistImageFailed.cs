using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("ArtistImageFailed")]
    public class ArtistImageFailed
    {
        [Column("artist_id"), PrimaryKey()]
        public long ArtistId { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
