using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("TrackArtists")]
    public class TrackIndexFailed
    {
        [Column("track_id"), Indexed(), NotNull()]
        public long TrackId { get; set; }

        [Column("indexing_failure_reason")]
        public string IndexingFailureReason { get; set; }

        [Column("date_happened")]
        public long DateHappened { get; set; }

    }
}
