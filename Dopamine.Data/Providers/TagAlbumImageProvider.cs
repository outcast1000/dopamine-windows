using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Providers
{
    public class TagAlbumImageProvider : IAlbumImageProvider
    {
        private int _counter = 0;
        private FileMetadata _fileMetadata = null;
        private Byte[] _AlbumImage = null;
        private string _AlbumImageUniqueID = null;

        public TagAlbumImageProvider(FileMetadata fileMetadata)
        {
            if (_fileMetadata == null)
                return;
            try
            {
                _AlbumImage = _fileMetadata.ArtworkData.Value;
                _AlbumImageUniqueID = System.Convert.ToBase64String(new System.Security.Cryptography.SHA1Cng().ComputeHash(_AlbumImage));
            }
            catch (Exception ex)
            {
                Debug.Print("There was a problem while getting artwork data for Track with path='{0}'. Exception: {1}", _fileMetadata.Path, ex.Message);
            }
        }

        public Byte[] AlbumImage { get {
                if (_counter != 0)//== Only one image is supported
                    return null;
                return _AlbumImage;
            } }
        public string AlbumImageUniqueID
        {
            get
            {
                if (_counter != 0)//== Only one image is supported
                    return null;
                return _AlbumImageUniqueID;
            }
        }
        public void next()
        {
            _counter++;
        }
        public string ProviderName
        {
            get { return "TAG"; }
        }
    }
}
