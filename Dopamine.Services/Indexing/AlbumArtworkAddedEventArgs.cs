using Dopamine.Data.Entities;
using System;
using System.Collections.Generic;

namespace Dopamine.Services.Indexing
{
    public class AlbumArtworkAddedEventArgs : EventArgs
    {
        public IList<AlbumV> Albums { get; set; }
    }

    public class ArtistImagesAddedEventArgs : EventArgs
    {
        public IList<ArtistV> Artists { get; set; }
    }


    
}