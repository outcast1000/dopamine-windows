using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("ArtistCollectionsArtists")]
    public class ArtistCollectionsArtist
    {
        public ArtistCollectionsArtist() { }

        [Column("artist_collection_id")]
        public long ArtistCollectionId { get; set; }

        [Column("artist_id")]
        public long ArtistId { get; set; }

    }
}
