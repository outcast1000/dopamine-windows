using SQLite;

namespace Dopamine.Data.Entities
{
    public class PlaylistTrack
    {
        [PrimaryKey(), AutoIncrement()]
        public long ID { get; set; }
        public long TrackID { get; set; }

    }
}
