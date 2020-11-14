using Digimezzo.Foundation.Core.Settings;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.IO;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using Dopamine.Services.Cache;
using Dopamine.Services.Indexing;
using Dopamine.Services.Utils;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Services.Entities
{
    public delegate void Notify();  // delegate

    public class AlbumViewModel : BindableBase, ISemanticZoomable
    {
        private AlbumV data;
        private bool isHeader;
        private IIndexingService _indexingService;
        private IAlbumVRepository _albumVRepository;

        public event Notify ImageRequestCompleted; // event

        public AlbumViewModel(IIndexingService indexingService, IAlbumVRepository albumVRepository, AlbumV data)
        {
            this.data = data;
            this.isHeader = false;
            _indexingService = indexingService;
            _albumVRepository = albumVRepository;
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

        public AlbumV Data { get { return data; } }

        public String AlbumItemInfo
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
                if (Data.Thumbnail == null && !_bImageRequested)
                {
                    _bImageRequested = true;
                    Task unAwaitedTask = RequestImageDownload(false, false);
                }

                return data.Thumbnail; 
            }
            private set { SetProperty<string>(ref _thumbnail, value); }
        }

        public async Task RequestImageDownload(bool bIgnorePreviousFailures, bool bForce)
        {
            await Task.Run(async () =>
            {
                _indexingService.AlbumInfoDownloaded += indexingService_InfoDownloaded;
                bool bAccepted = await _indexingService.RequestAlbumInfoAsync(Data, bIgnorePreviousFailures, bForce);
                if (!bAccepted)
                    _indexingService.AlbumInfoDownloaded -= indexingService_InfoDownloaded;
            });
        }

        private string _thumbnail;
        private bool _bImageRequested = false;
        private void indexingService_InfoDownloaded(AlbumV request, bool success)
        {
            if (request.Id != Data.Id)
                return;// Belongs to a different view model
            _indexingService.AlbumInfoDownloaded -= indexingService_InfoDownloaded;
            if (!success)
            {
                ImageRequestCompleted?.Invoke();
                return;// Nothing to change
            }
            AlbumV album = _albumVRepository.GetAlbum(Data.Id, new QueryOptions(DataRichnessEnum.History));
            if (album == null)
                return;// Should not happen
            data = album;
            if (album.Thumbnail != null)
                Thumbnail = album.Thumbnail;
            ImageRequestCompleted?.Invoke();
        }

        public string AlbumArtistComplete
        {
            get
            {
                if (AlbumArtists.Equals(Artists))
                    return AlbumArtists;
                return string.Format("{0} ({1})", AlbumArtists, Artists);
            }
        }

        public string Header => SemanticZoomUtils.GetGroupHeader(Data.Name, true);

        public bool IsHeader
        {
            get { return this.isHeader; }
            set { SetProperty<bool>(ref this.isHeader, value); }
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


        public bool HasTitle
        {
            get { return !string.IsNullOrEmpty(data.Name); }
        }

        public string ToolTipYear
        {
            get { return !data.MinYear.HasValue ? "(" + data.MinYear + ")" : string.Empty; }
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
            if (Name == null)
                return ((AlbumViewModel)obj).Name == null;
            return Name.Equals(((AlbumViewModel)obj).Name) && Name.Equals(((AlbumViewModel)obj).Name);
        }

        public override int GetHashCode()
        {
            return (Name + Artists + AlbumArtists).GetHashCode();
        }

        public bool IsSelected { get; set; }
    }
}
