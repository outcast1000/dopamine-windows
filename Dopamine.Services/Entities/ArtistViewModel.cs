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

        public ArtistV Data { get { return this.data; } }

        public String ArtistItemInfo
        {
            get
            {
                string info = "";

                if (!string.IsNullOrEmpty(data.Genres))
                    info += string.Format("\n{0}", data.Genres);
                if (!data.MinYear.HasValue)
                {

                }
                else if (data.MinYear == data.MaxYear)
                    info += string.Format("\nYear: {0}", data.MinYear);
                else
                    info += string.Format("\nYears: {0} - {1}", data.MinYear, data.MaxYear);
                info += string.Format("\n{0} tracks", data.TrackCount);
                info += string.Format("\n{0} albums", data.AlbumCount);
                return info.Trim();
            }
        }


        public long TrackCount { get { return data.TrackCount; } }

        public string Genres { get { return data.Genres; } }


        public bool HasCover
        {
            get { return !string.IsNullOrEmpty(data.Thumbnail); }
        }

        public string Thumbnail { get {
                if (Data.ArtistImage == null)
                {
                    Task.Run(async () =>
                    {
                        _indexingService.ArtistInfoDownloaded += indexingService_ArtistInfoDownloaded;
                        bool bAccepted = await _indexingService.RequestArtistInfoAsync(Data, false, false);
                        if (!bAccepted)
                            _indexingService.ArtistInfoDownloaded -= indexingService_ArtistInfoDownloaded;
                    });
                }

                return data.Thumbnail; 
            }
            private set { SetProperty<string>(ref _thumbnail, value); }
        }

        private string _thumbnail;
        private void indexingService_ArtistInfoDownloaded(ArtistV requestedArtist, bool success)
        {
            if (requestedArtist.Id != Data.Id)
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

        public DateTime DateCreated { get { return data.DateFileCreated; } }

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
