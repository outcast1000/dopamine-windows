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

        public string Genres { get; set; }

        public string Thumbnail { get; set; }

        public DateTime DateAdded { get; set; }

        public DateTime DateFileCreated { get; set; }


    }
}
