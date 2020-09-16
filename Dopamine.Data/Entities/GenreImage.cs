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

        [Column("source")]
        public string Source { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
