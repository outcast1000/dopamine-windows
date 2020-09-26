using SQLite;

namespace Dopamine.Data.Entities
{
    [Table("PlaylistTracks")]
    public class PlaylistTrack
    {
        [Column("id"), PrimaryKey(), AutoIncrement()]
        public long ID { get; set; }
        [Column("track_id"), Indexed()]
        public long TrackID { get; set; }

    }
}
