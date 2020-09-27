using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("TrackHistory")]
    public class TrackHistory
    {
        public TrackHistory() { }

        [Column("id"), PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }

        [Column("track_id"), Indexed(), NotNull()]
        public long TrackId { get; set; }

        [Column("history_action_id"), Indexed(), NotNull()]
        public long HistoryActionId { get; set; }

        [Column("history_action_extra")]
        public string HistoryActionExtra{ get; set; }

        [Column("date_happened"), NotNull()]
        public long DateHappened { get; set; }


    }
}
