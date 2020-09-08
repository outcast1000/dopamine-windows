using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("ArtistDownloadFailed")]
    public class ArtistDownloadFailed
    {

        [Column("artist_id"), NotNull()]
        public long ArtistId { get; set; }

        [Column("provider"), NotNull()]
        public string Provider { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
