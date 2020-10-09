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

        public AlbumV Data { get { return data; } }

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
                //info += string.Format("\n{0} tracks", data.TrackCount);
                if (!data.MinYear.HasValue)
                {

                }
                else if (data.MinYear == data.MaxYear)
                    info += string.Format("\nYear: {0}", data.MinYear);
                else
                    info += string.Format("\nYears: {0} - {1}", data.MinYear, data.MaxYear);
                if (!string.IsNullOrEmpty(data.Genres))
                    info += string.Format("\n{0}", data.Genres);
                //info += string.Format("\n{0}", data.DateAdded);
                //info += string.Format("\n{0}", data.DateFileCreated);
                return info.Trim();
            }
        }

        public string AlbumArtistComplete
        {
            get
            {
                string result = "";
                if (AlbumArtists.Equals(Artists))
                    result += AlbumArtists;
                else
                    result += string.Format("{0} ({1})", AlbumArtists, Artists);
                return result;
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
            get { return !data.MinYear.HasValue ? "(" + data.MinYear + ")" : string.Empty; }
        }

        public string MinYear
        {
            get { return data.MinYear.HasValue ? data.MinYear.ToString() : ""; }
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
