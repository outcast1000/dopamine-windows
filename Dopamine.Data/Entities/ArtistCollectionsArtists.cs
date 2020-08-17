using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("ArtistCollectionsArtists")]
    public class ArtistCollectionsArtists
    {
        public ArtistCollectionsArtists() { }

        [Column("artist_collection_id")]
        public long ArtistCollectionId { get; set; }

        [Column("artist__id")]
        public long ArtistId { get; set; }

    }
}
