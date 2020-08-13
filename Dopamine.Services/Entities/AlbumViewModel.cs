using Digimezzo.Foundation.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;

namespace Dopamine.Services.Entities
{
    public class AlbumViewModel : BindableBase
    {
        private string albumTitle;
        private string albumArtist;
        private string albumArtists;
        private string year;
        private string artworkPath;
        private string mainHeader;
        private string subHeader;
        private long? dateAdded;
        private long? dateFileCreated;
        private long sortYear;

        public AlbumViewModel(AlbumV albumData)
        {
            this.albumArtist = albumData.AlbumArtist;
            this.albumTitle = !string.IsNullOrEmpty(albumData.Name) ? albumData.Name : ResourceUtils.GetString("Language_Unknown_Album");
            this.albumArtists = albumData.Artists;// this.GetAlbumArtists(albumData.Artists);
            this.year = albumData.Year.HasValue && albumData.Year.Value > 0 ? albumData.Year.Value.ToString() : string.Empty;
            this.SortYear = albumData.Year.HasValue ? albumData.Year.Value : 0;
            this.AlbumKey = albumData.Name + albumData.AlbumArtist + albumData.Artists;
            //this.DateAdded = albumData.DateAdded;
            //this.DateFileCreated = albumData.DateFileCreated;
        }

        private string GetAlbumArtist(AlbumData albumData)
        {
            if (!string.IsNullOrEmpty(albumData.AlbumTitle))
            {
                if (!string.IsNullOrEmpty(albumData.AlbumArtists))
                {
                    return DataUtils.GetCommaSeparatedColumnMultiValue(albumData.AlbumArtists);
                }
                else if (!string.IsNullOrEmpty(albumData.Artists))
                {
                    return DataUtils.GetCommaSeparatedColumnMultiValue(albumData.Artists);
                }
            }

            return ResourceUtils.GetString("Language_Unknown_Artist");
        }

        public List<string> GetAlbumArtists(AlbumData albumData)
        {
            if (!string.IsNullOrEmpty(albumData.AlbumArtists))
            {
                return DataUtils.SplitAndTrimColumnMultiValue(albumData.AlbumArtists).ToList();
            }
            else if (!string.IsNullOrEmpty(albumData.Artists))
            {
                return DataUtils.SplitAndTrimColumnMultiValue(albumData.Artists).ToList();
            }

            return new List<string>();
        }

        public string AlbumKey { get; set; }

        public long? DateAdded
        {
            get { return this.dateAdded; }
            set
            {
                SetProperty<long?>(ref this.dateAdded, value);
            }
        }

        public long? DateFileCreated
        {
            get { return this.dateFileCreated; }
            set
            {
                SetProperty<long?>(ref this.dateFileCreated, value);
            }
        }

        public double Opacity { get; set; }

        public bool HasCover
        {
            get { return !string.IsNullOrEmpty(this.artworkPath); }
        }

        public bool HasTitle
        {
            get { return !string.IsNullOrEmpty(this.AlbumTitle); }
        }

        public string ToolTipYear
        {
            get { return !string.IsNullOrEmpty(this.year) ? "(" + this.year + ")" : string.Empty; }
        }

        public string Year
        {
            get { return this.year; }
            set
            {
                SetProperty<string>(ref this.year, value);
                RaisePropertyChanged(nameof(this.ToolTipYear));
            }
        }

        public long SortYear
        {
            get { return this.sortYear; }
            set
            {
                SetProperty<long>(ref this.sortYear, value);
            }
        }

        public string AlbumTitle
        {
            get { return this.albumTitle; }
            set
            {
                SetProperty<string>(ref this.albumTitle, value);
                RaisePropertyChanged(nameof(this.HasTitle));
            }
        }

        public string AlbumArtist
        {
            get { return this.albumArtist; }
            set { SetProperty<string>(ref this.albumArtist, value); }
        }

        public string AlbumArtists
        {
            get { return this.albumArtists; }
            set { SetProperty<string>(ref this.albumArtists, value); }
        }

        public string ArtworkPath
        {
            get { return this.artworkPath; }
            set
            {
                SetProperty<string>(ref this.artworkPath, value);
                RaisePropertyChanged(nameof(this.HasCover));
            }
        }

        public string MainHeader
        {
            get { return this.mainHeader; }
            set { SetProperty<string>(ref this.mainHeader, value); }
        }

        public string SubHeader
        {
            get { return this.subHeader; }
            set { SetProperty<string>(ref this.subHeader, value); }
        }

        public override string ToString()
        {
            return this.albumTitle;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return this.AlbumKey.Equals(((AlbumViewModel)obj).AlbumKey);
        }

        public override int GetHashCode()
        {
            return this.AlbumKey.GetHashCode();
        }
    }
}
