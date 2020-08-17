using Digimezzo.Foundation.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dopamine.Services.Entities
{
    public class AlbumViewModel : BindableBase
    {
        private AlbumV data;

        public AlbumViewModel(AlbumV data)
        {
            this.data = data;
        }

        private string GetAlbumArtist()
        {
            if (!string.IsNullOrEmpty(data.AlbumArtist))
            {
                return data.AlbumArtist;
            }

            return ResourceUtils.GetString("Language_Unknown_Artist");
        }

        public string GetArtists()
        {
            return data.Artists;
        }

        public bool HasCover
        {
            get { return !string.IsNullOrEmpty(data.Thumbnail); }
        }

        public bool HasTitle
        {
            get { return !string.IsNullOrEmpty(data.Name); }
        }

        public string ToolTipYear
        {
            get { return !data.Year.HasValue ? "(" + data.Year + ")" : string.Empty; }
        }

        public string Year
        {
            get { return data.Year.HasValue ? data.Year.ToString() : ""; }
        }

 
        public string AlbumTitle
        {
            get { return data.Name; }
            set
            {
                data.Name = value;
                //RaisePropertyChanged(nameof(this.HasTitle));
            }
        }

        public string AlbumArtist
        {
            get { return data.AlbumArtist; }
            set { data.AlbumArtist = value; }
        }

        public string Thumbnail { get { return data.Thumbnail; } }

        public DateTime DateAdded { get { return data.DateAdded; } }

        public DateTime DateFileCreated { get { return data.DateFileCreated; } }

 
        public override string ToString()
        {
            return data.Name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return AlbumTitle.Equals(((AlbumViewModel)obj).AlbumTitle) && AlbumArtist.Equals(((AlbumViewModel)obj).AlbumArtist);
        }

        public override int GetHashCode()
        {
            return (AlbumTitle + AlbumArtist).GetHashCode();
        }
    }
}
