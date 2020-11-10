using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("Albums")]
    public class Album
    {
        [Column("id"), PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }

        [Column("name"), Indexed(), Collation("NOCASE"), NotNull()]
        public string Name { get; set; }

        [Column("artist_collection_id"), Indexed()]
        public long? ArtistCollectionId { get; set; }

    }
}
