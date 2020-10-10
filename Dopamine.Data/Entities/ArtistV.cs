using Dopamine.Core.Extensions;
using SQLite;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Dopamine.Data.Entities
{
    public class ArtistV
    {
        public ArtistV() { }

        public long Id { get; set; }

        public string Name { get; set; }

        public long TrackCount { get; set; }

        public long GenreCount { get; set; }

        public string Genres { get; set; }

        public long AlbumCount { get; set; }

        public string Albums { get; set; }

        public string Thumbnail { get; set; }

        public string ArtistImage { get; set; }

        public long? MinYear { get; set; }

        public long? MaxYear { get; set; }

        public DateTime DateAdded { get; set; }

        public DateTime DateFileCreated { get; set; }


    }
}
