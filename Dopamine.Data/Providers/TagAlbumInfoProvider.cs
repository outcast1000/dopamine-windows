using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class TagAlbumInfoProvider : IAlbumInfoProvider
    {
        
        public TagAlbumInfoProvider(FileMetadata fileMetadata)
        {
            Success = false;
            if (fileMetadata == null)
                return;
            Data = new AlbumInfoProviderData();
            try
            {
                Data.Images = new byte[][] { fileMetadata.ArtworkData.Value};
                Data.Year = fileMetadata.Year.Value;
            }
            catch (Exception ex)
            {
                Debug.Print("There was a problem while getting artwork data for Track with path='{0}'. Exception: {1}", fileMetadata.Path, ex.Message);
            }
        }

        public string ProviderName
        {
            get { return "TAG"; }
        }

        public AlbumInfoProviderData Data
        {
            get; private set;
        }

        public bool Success { get; private set; }
    }
}
