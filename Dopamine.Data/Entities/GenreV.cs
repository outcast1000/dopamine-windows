using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    public class GenreV
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public long TrackCount { get; set; }

        public long AlbumCount { get; set; }

        public long ArtistCount { get; set; }

        public string Artists { get; set; }

        public long? MinYean { get; set; }

        public long? MaxYear { get; set; }

        public string Thumbnail { get; set; }



    }
}
