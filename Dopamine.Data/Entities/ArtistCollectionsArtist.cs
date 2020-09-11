using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("ArtistCollectionsArtists")]
    public class ArtistCollectionsArtist
    {
        public ArtistCollectionsArtist() { }

        [Column("id"), PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }

        [Column("artist_collection_id"), NotNull(), Indexed()]
        public long ArtistCollectionId { get; set; }

        [Column("artist_id"), NotNull(), Indexed()]
        public long ArtistId { get; set; }

    }
}
