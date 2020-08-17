using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Services.Utils;
using Prism.Mvvm;
using System;

namespace Dopamine.Services.Entities
{
    public class ArtistViewModel : BindableBase, ISemanticZoomable
    {
        private ArtistV data;
        private bool isHeader;

        public ArtistViewModel(ArtistV data)
        {
            this.data = data;
            this.isHeader = false;
        }

        public long Id { get;}

        public string Name {
            get { return data.Name; }
            set
            {
                data.Name = value;
                //RaisePropertyChanged(nameof(this.HasTitle));
            }
        }

        public long TrackCount { get { return data.TrackCount; } }

        public string Genres { get { return data.Genres; } }

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
