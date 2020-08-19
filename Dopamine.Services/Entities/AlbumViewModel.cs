using Digimezzo.Foundation.Core.Settings;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.IO;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Services.Cache;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
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

        public long Id { get { return data.Id; } }
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(data.Name))
                    return ResourceUtils.GetString("Language_Unknown_Album");
                return data.Name;
            }
            set
            {
                data.Name = value;
                //RaisePropertyChanged(nameof(this.HasTitle));
            }
        }

        public String AlbumItemInfo
        {
            get
            {
                string info = "";
                if (AlbumArtists.Equals(Artists))
                    info += AlbumArtists;
                else
                    info += string.Format("{0} ({1})", AlbumArtists, Artists);
                if (data.Year.HasValue && data.Year > 0)
                    info += string.Format("\n{0}", data.Year);
                if (!string.IsNullOrEmpty(data.Genres))
                    info += string.Format("\n{0}", data.Genres);
                info += string.Format("\n{0} tracks", data.TrackCount);
                //info += string.Format("\n{0}", data.DateAdded);
                //info += string.Format("\n{0}", data.DateFileCreated);
                return info;
            }
        }

        public string AlbumArtists
        {
            get
            {
                if (string.IsNullOrEmpty(data.AlbumArtists))
                    return ResourceUtils.GetString("Language_Unknown_Artist");
                return data.AlbumArtists;
            }
        }

        public string Artists
        {
            get
            {
                if (string.IsNullOrEmpty(data.Artists))
                    return ResourceUtils.GetString("Language_Unknown_Artist");
                return data.Artists;
            }
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
            if (Name == null)
                return ((AlbumViewModel)obj).Name == null;

            return Name.Equals(((AlbumViewModel)obj).Name) && Name.Equals(((AlbumViewModel)obj).Name);
        }

        public override int GetHashCode()
        {
            return (Name + Artists + AlbumArtists).GetHashCode();
        }
    }
}
