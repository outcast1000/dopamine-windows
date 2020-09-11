using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("ArtistDownloadFailed")]
    public class ArtistDownloadFailed
    {
        [Column("id"), PrimaryKey(), AutoIncrement(), NotNull()]
        public long Id { get; set; }

        [Column("artist_id"), NotNull(), Indexed()]
        public long ArtistId { get; set; }

        [Column("provider"), NotNull(), Indexed()]
        public string Provider { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
