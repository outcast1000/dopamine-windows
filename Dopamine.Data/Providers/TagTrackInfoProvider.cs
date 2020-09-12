using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class TagTrackInfoProvider : ITrackInfoProvider
    {
        
        public TagTrackInfoProvider(FileMetadata fileMetadata)
        {
            Success = false;
            if (fileMetadata == null)
                return;
            Data = new TrackInfoProviderData();
            Success = true;
            try
            {
                if (fileMetadata.ArtworkData != null && fileMetadata.ArtworkData.Value != null)
                {
                    Data.Images = new byte[][] { fileMetadata.ArtworkData.Value };
                }
                if (fileMetadata.Lyrics != null && !string.IsNullOrEmpty(fileMetadata.Lyrics.Value))
                {
                    Data.Lyrics = new string[] { fileMetadata.Lyrics.Value };
                }
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

        public TrackInfoProviderData Data
        {
            get; private set;
        }

        public bool Success { get; private set; }
    }
}
