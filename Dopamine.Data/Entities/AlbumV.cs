using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    public class AlbumV
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public long TrackCount { get; set; }

        public long AlbumArtistCount { get; set; }

        public string AlbumArtists { get; set; }

        public long ArtistCount { get; set; }

        public string Artists { get; set; }

        public long GenreCount { get; set; }

        public string Genres { get; set; }

        public long? MinYear { get; set; }

        public long? MaxYear { get; set; }

        public string Thumbnail { get; set; }

        public DateTime DateAdded { get; set; }

        public DateTime DateFileCreated { get; set; }


    }
}
