using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    public class FolderV
    {
        public long Id { get; set; }

        public string Path { get; set; }

        public bool Show { get; set; }

        public DateTime DateAdded { get; set; }

        public long TrackCount { get; set; }
        public long GenreCount { get; set; }
        public long AlbumCount { get; set; }
        public long ArtistsCount { get; set; }
        public long? MinYear { get; set; }
        public long? MaxYear { get; set; }
        public long TotalDuration { get; set; }
        public long TotalFileSize { get; set; }
        public long? AverageBitrate { get; set; }

        // Indexing
        public long? DateIndexed { get; set; }
        public long? TotalFiles { get; set; }
        public long? MaxFileDateModified { get; set; }
        public string Hash { get; set; }



    }
}
