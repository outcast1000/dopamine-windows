using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Services.Utils;
using Prism.Mvvm;
using System;
using System.Text;

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

        public long Id { get { return data.Id; } }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(data.Name))
                    return ResourceUtils.GetString("Language_Unknown_Genre");
                return data.Name;
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

        public GenreV Data { get { return data; } }
        public String GenreItemInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(data.Artists))
                    sb.AppendLine(ResourceUtils.GetString("Language_Artists") + ": " + data.Artists);
                if (!data.MinYear.HasValue)
                {
                    // Do nothing 
                }
                else if (data.MinYear == data.MaxYear)
                    sb.AppendLine(ResourceUtils.GetString("Language_Year") + ": " + data.MinYear);
                else
                    sb.AppendLine(ResourceUtils.GetString("Language_Year") + ": " + data.MinYear + " - " + data.MaxYear);

                sb.AppendLine(ResourceUtils.GetString("Language_Songs") + ": " + data.TrackCount);
                sb.AppendLine(ResourceUtils.GetString("Language_Albums") + ": " + data.AlbumCount);

                /*
                if (Data.PlayCount.HasValue && Data.PlayCount.Value > 0)
                    sb.AppendLine(ResourceUtils.GetString("Language_Plays") + ": " + data.PlayCount.Value);
                if (Data.SkipCount.HasValue && Data.SkipCount.Value > 0)
                    sb.AppendLine(ResourceUtils.GetString("Language_Skips") + ": " + data.SkipCount.Value);
                */
                return sb.ToString();
            }
        }

        public long TrackCount => data.TrackCount;

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
            return string.IsNullOrEmpty(data.Name) ? 0 : data.Name.GetHashCode();
        }

        public bool IsSelected { get; set; }
    }
}
