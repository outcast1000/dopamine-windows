using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using Dopamine.Services.Cache;
using Dopamine.Services.Indexing;
using Dopamine.Services.Utils;
using Prism.Mvvm;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Services.Entities
{
    public class ArtistViewModel : BindableBase, ISemanticZoomable
    {
        private ArtistV data;
        private bool isHeader;
        private IIndexingService _indexingService;
        private IArtistVRepository _artistVRepository;
		
        public ArtistViewModel(IIndexingService indexingService, IArtistVRepository artistVRepository, ArtistV data)
        {
            this.data = data;
            this.isHeader = false;
            _indexingService = indexingService;
            _artistVRepository = artistVRepository;
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

        public ArtistV Data { get { return data; } }

        public String ArtistItemInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(data.Genres))
                    sb.AppendLine(ResourceUtils.GetString("Language_Genres") + ": " + data.Genres.Replace(",", ", "));
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
                if (Data.PlayCount.HasValue && Data.PlayCount.Value > 0)
                    sb.AppendLine(ResourceUtils.GetString("Language_Plays") + ": " + data.PlayCount.Value);
                if (Data.SkipCount.HasValue && Data.SkipCount.Value > 0)
                    sb.AppendLine(ResourceUtils.GetString("Language_Skips") + ": " + data.SkipCount.Value);
                return sb.ToString();
            }
        }


        public long TrackCount { get { return data.TrackCount; } }

        public string Genres { get { return data.Genres; } }
		
		public bool HasCover
        {
            get { return !string.IsNullOrEmpty(data.Thumbnail); }
        }

        public string Thumbnail { get {
                if (Data.ArtistImage == null && !_bImageRequested)
                {
                    _bImageRequested = true;
                    RequestImageDownload(false, false);
                }

                return data.Thumbnail; 
            }
            private set { SetProperty<string>(ref _thumbnail, value); }
        }

        public async Task RequestImageDownload(bool bIgnorePreviousFailures, bool bForce)
        {
            await Task.Run(async () =>
            {
                _indexingService.ArtistInfoDownloaded += indexingService_ArtistInfoDownloaded;
                bool bAccepted = await _indexingService.RequestArtistInfoAsync(Data, bIgnorePreviousFailures, bForce);
                if (!bAccepted)
                    _indexingService.ArtistInfoDownloaded -= indexingService_ArtistInfoDownloaded;
            });
        }

        private string _thumbnail;
        private bool _bImageRequested = false;
        private void indexingService_ArtistInfoDownloaded(ArtistV request, bool success)
        {
            if (request.Id != Data.Id)
                return;// Belongs to a different view model
            _indexingService.ArtistInfoDownloaded -= indexingService_ArtistInfoDownloaded;
            if (!success)
                return;// Nothing to change
            ArtistV artist = _artistVRepository.GetArtist(Data.Id);
            if (artist == null)
                return;// Should not happen
            data = artist;
            if (artist.Thumbnail != null)
                Thumbnail = artist.Thumbnail;
        }

        public DateTime DateAdded { get { return data.DateAdded; } }

        public DateTime DateFileCreated { get { return data.DateFileCreated; } }

        public long? Year { get { return data.MinYear; } }

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
            return string.IsNullOrEmpty(data.Name) ? 0 : data.Name.GetHashCode();
        }

        public bool IsSelected { get; set; }
    }
}
