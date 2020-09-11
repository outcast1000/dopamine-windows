using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    [Table("AlbumReviews")]
    public class AlbumReview
    {
        [Column("album_id"), PrimaryKey(), NotNull()]
        public long AlbumId { get; set; }

        [Column("review"), NotNull()]
        public string Review { get; set; }

        [Column("source")]
        public string Source { get; set; }
        
        [Column("language")]
        public string Language { get; set; }

        [Column("date_added"), NotNull()]
        public long DateAdded { get; set; }

    }
}
