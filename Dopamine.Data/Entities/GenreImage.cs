using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("GenreImages")]
    public class GenreImage
    {
        [Column("genre_id"), NotNull(), Indexed()]
        public long ArtistId { get; set; }

        [Column("location"), NotNull()]
        public string Location { get; set; }

        [Column("origin")]
        public string Origin { get; set; }

        [Column("origin_type_id")]
        public OriginType OriginType { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
