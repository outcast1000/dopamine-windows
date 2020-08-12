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

    }
}
