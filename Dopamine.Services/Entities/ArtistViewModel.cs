using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Services.Cache;
using Dopamine.Services.Utils;
using Prism.Mvvm;
using System;

namespace Dopamine.Services.Entities
{
    public class ArtistViewModel : BindableBase, ISemanticZoomable
    {
        private ArtistV data;
        private bool isHeader;
        private ICacheService cacheService;
        public ArtistViewModel(ArtistV data, ICacheService cacheService)
        {
            this.data = data;
            this.cacheService = cacheService;
            this.isHeader = false;

        }

        public long Id { get { return data.Id; } }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(data.Name))
                    return ResourceUtils.GetString("Language_Unknown_Artist");
                return data.Name;
            }
            set
            {
                data.Name = value;
                //RaisePropertyChanged(nameof(this.HasTitle));
            }
        }

        public String ArtistItemInfo
        {
            get
            {
                string info = "";

                if (!string.IsNullOrEmpty(data.Genres))
                    info += string.Format("\n{0}", data.Genres);
                info += string.Format("\n{0} tracks", data.TrackCount);
                //info += string.Format("\n{0}", data.DateAdded);
                //info += string.Format("\n{0}", data.DateFileCreated);
                return info;
            }
        }


        public long TrackCount { get { return data.TrackCount; } }

        public string Genres { get { return data.Genres; } }


        public bool HasCover
        {
            get { return !string.IsNullOrEmpty(data.Thumbnail); }
        }

        public string Thumbnail { get { return data.Thumbnail; } }

        public DateTime DateAdded { get { return data.DateAdded; } }

        public string Header => SemanticZoomUtils.GetGroupHeader(Name, true);

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

            return string.Equals(data.Name, ((ArtistViewModel)obj).data.Name, StringComparison.CurrentCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return data.Name.GetHashCode();
        }
    }
}
