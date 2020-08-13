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

        public ArtistV Data
        {
            get { return data; }
            set
            {
                SetProperty<ArtistV>(ref this.data, value);
            }
        }


        public string SortArtistName => FormatUtils.GetSortableString(data.Name, true);

        public string Header => SemanticZoomUtils.GetGroupHeader(data.Name, true);

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
