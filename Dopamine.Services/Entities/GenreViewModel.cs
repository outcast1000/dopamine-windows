using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Services.Utils;
using Prism.Mvvm;
using System;

namespace Dopamine.Services.Entities
{
    public class GenreViewModel : BindableBase, ISemanticZoomable
    {
        private GenreV data;
        private bool isHeader;

        public GenreViewModel(GenreV genre)
        {
            this.data = genre;// DataUtils.TrimColumnValue(genre);
            this.isHeader = false;
        }


        public long Id
        {
            get { return data.Id; }
        }
        public GenreV Data
        {
            get { return data; }
            set
            {
                //SetProperty<string>(ref genre.Name, value);
            }
        }

        public string Name
        {
            get { return data.Name; }
            set
            {
                //SetProperty<string>(ref genre.Name, value);
            }
        }

        public string Thumbnail
        {
            get { return data.Thumbnail; }
            set
            {
                //SetProperty<string>(ref genre.Name, value);
            }
        }

        public String GenreItemInfo
        {
            get
            {
                string info = "";

                if (!string.IsNullOrEmpty(data.Artists))
                    info += string.Format("\n{0} artists ({1})", data.ArtistCount, data.Artists);
                info += string.Format("\n{0} albums", data.AlbumCount);
                info += string.Format("\n{0} tracks", data.TrackCount);
                /*
                if (data.YearFrom == data.YearTo)
                    info += string.Format("\nYear: {0}", data.YearFrom);
                else
                    info += string.Format("\nFrom {0} to {1}", data.YearFrom, data.YearTo);
                */
                return info;
            }
        }

        public string SortGenreName => FormatUtils.GetSortableString(data.Name, true);

        public string Header => SemanticZoomUtils.GetGroupHeader(data.Name);

        public bool IsHeader
        {
            get { return this.isHeader; }
            set { SetProperty<bool>(ref this.isHeader, value); }
        }
     
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

            return string.Equals(this.data.Name, ((GenreViewModel)obj).data.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return this.data.Name.GetHashCode();
        }
    }
}
