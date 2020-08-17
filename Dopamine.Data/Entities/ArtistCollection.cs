using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("ArtistCollection")]
    public class ArtistCollection
    {
        public ArtistCollection() { }

        [Column("id"), PrimaryKey(), AutoIncrement()]
        public long Id { get; set; }

    }
}
