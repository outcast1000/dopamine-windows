using Dopamine.Core.Alex;  //Digimezzo.Foundation.Core.Settings
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Data;
using Dopamine.Services.Dialog;
using Dopamine.ViewModels.Common.Base;
using Dopamine.Views.FullPlayer.Collection;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dopamine.Data.Repositories;
using Dopamine.Data.Entities;
using Dopamine.Services.Scrobbling;
using Dopamine.Services.Indexing;
using Dopamine.Services.Entities;
using System.Linq;
using Dopamine.Services.Metadata;
using System.Collections.ObjectModel;
using System;
using System.Windows;
using System.Windows.Data;
using Dopamine.Core.Base;

namespace Dopamine.ViewModels.FullPlayer.Collection
{
    public class CollectionHistoryLogViewModel : TracksViewModelBase
    {
        private enum HistoryListMode
        {
            LogAll,
            LogPlayed,
            Tracks
        }


        private IContainerProvider container;
        private IDialogService dialogService;
        private IEventAggregator eventAggregator;
        private bool _rankVisible;
        private bool _happenedVisible;
        private bool _happenedExVisible;

        private bool ratingVisible;
        private bool loveVisible;
        private bool lyricsVisible;
        private bool artistVisible;
        private bool albumVisible;
        private bool genreVisible;
        private bool lengthVisible;
        private bool playCountVisible;
        private bool skipCountVisible;
        private bool dateLastPlayedVisible;
        private bool dateAddedVisible;
        private bool dateCreatedVisible;
        private bool albumArtistVisible;
        private bool trackNumberVisible;
        private bool yearVisible;
        private bool bitrateVisible;
        private string _searchText;
        private ObservableCollection<TrackViewModel> tracks;
        private CollectionViewSource tracksCvs;
        private HistoryListMode _historyListMode;
        private string _historyListModeText;
        private long _recordCount = 0;

        public DelegateCommand ToggleModeCommand { get; set; }

        public string HistoryListModeText
        {
            get { return _historyListModeText; }
            set { SetProperty<string>(ref _historyListModeText, value); }
        }

        public long RecordCount
        {
            get { return _recordCount; }
            set { SetProperty<long>(ref _recordCount, value); }
        }




        public bool RankVisible // Only when in charts mode
        {
            get { return _rankVisible; }
            set { SetProperty<bool>(ref _rankVisible, value); }
        }

        public bool HappenedVisible // Only when in history (all + played) mode
        {
            get { return _happenedVisible; }
            set { SetProperty<bool>(ref _happenedVisible, value); }
        }

        public bool HappenedExVisible // Only when in history (all + played) mode
        {
            get { return _happenedExVisible; }
            set { SetProperty<bool>(ref _happenedExVisible, value); }
        }

        public bool RatingVisible
        {
            get { return this.ratingVisible; }
            set { SetProperty<bool>(ref this.ratingVisible, value); }
        }

        public bool LoveVisible
        {
            get { return this.loveVisible; }
            set { SetProperty<bool>(ref this.loveVisible, value); }
        }

        public bool LyricsVisible
        {
            get { return this.lyricsVisible; }
            set { SetProperty<bool>(ref this.lyricsVisible, value); }
        }

        public bool ArtistVisible
        {
            get { return this.artistVisible; }
            set { SetProperty<bool>(ref this.artistVisible, value); }
        }

        public bool AlbumVisible
        {
            get { return this.albumVisible; }
            set { SetProperty<bool>(ref this.albumVisible, value); }
        }

        public bool GenreVisible
        {
            get { return this.genreVisible; }
            set { SetProperty<bool>(ref this.genreVisible, value); }
        }

        public bool LengthVisible
        {
            get { return this.lengthVisible; }
            set { SetProperty<bool>(ref this.lengthVisible, value); }
        }

        public bool PlayCountVisible
        {
            get { return this.playCountVisible; }
            set { SetProperty<bool>(ref this.playCountVisible, value); }
        }

        public bool SkipCountVisible
        {
            get { return this.skipCountVisible; }
            set { SetProperty<bool>(ref this.skipCountVisible, value); }
        }

        public bool DateLastPlayedVisible
        {
            get { return this.dateLastPlayedVisible; }
            set { SetProperty<bool>(ref this.dateLastPlayedVisible, value); }
        }

        public bool DateAddedVisible
        {
            get { return this.dateAddedVisible; }
            set { SetProperty<bool>(ref this.dateAddedVisible, value); }
        }

        public bool DateCreatedVisible
        {
            get { return this.dateCreatedVisible; }
            set { SetProperty<bool>(ref this.dateCreatedVisible, value); }
        }

        public bool AlbumArtistVisible
        {
            get { return this.albumArtistVisible; }
            set { SetProperty<bool>(ref this.albumArtistVisible, value); }
        }

        public bool TrackNumberVisible
        {
            get { return this.trackNumberVisible; }
            set { SetProperty<bool>(ref this.trackNumberVisible, value); }
        }

        public bool YearVisible
        {
            get { return this.yearVisible; }
            set { SetProperty<bool>(ref this.yearVisible, value); }
        }

        public bool BitrateVisible
        {
            get { return this.bitrateVisible; }
            set { SetProperty<bool>(ref this.bitrateVisible, value); }
        }

        public DelegateCommand ChooseColumnsCommand { get; set; }

        public CollectionHistoryLogViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            this.container = container;
            this.dialogService = container.Resolve<IDialogService>();
            this.eventAggregator = container.Resolve<IEventAggregator>();

            // Settings


            this.ToggleModeCommand = new DelegateCommand(async () => await this.ToggleHistoryListMode());
            _historyListMode = HistoryListMode.LogAll;
            Task unAwaitedTask = UpdateHistoryListMode();

            // Commands
            this.ChooseColumnsCommand = new DelegateCommand(this.ChooseColumns);
            //this.RemoveSelectedTracksCommand = new DelegateCommand(async () => await this.RemoveTracksFromCollectionAsync(this.SelectedTracks), () => !this.IsIndexing);
            
            // Show only the columns which are visible
            

        }

        protected override void OnLoad()
        {
            base.OnLoad();
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += SettingsClient_SettingChanged;
            this.GetVisibleColumns();

        }

        private void SettingsClient_SettingChanged(object sender, Digimezzo.Foundation.Core.Settings.SettingChangedEventArgs e)
        {
            if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
            {
                this.EnableRating = (bool)e.Entry.Value;
                this.GetVisibleColumns();
            }

            if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
            {
                this.EnableLove = (bool)e.Entry.Value;
                this.GetVisibleColumns();
            }
        }

        protected override void OnUnLoad()
        {
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged -= SettingsClient_SettingChanged;
            base.OnUnLoad();
        }

        private void ChooseColumns()
        {
            CollectionTracksColumns view = this.container.Resolve<CollectionTracksColumns>();
            view.DataContext = this.container.Resolve<CollectionTracksColumnsViewModel>();

            this.dialogService.ShowCustomDialog(
                0xe73e,
                16,
                ResourceUtils.GetString("Language_Columns"),
                view,
                400,
                0,
                false,
                true,
                true,
                true,
                ResourceUtils.GetString("Language_Ok"),
                ResourceUtils.GetString("Language_Cancel"),
                ((CollectionTracksColumnsViewModel)view.DataContext).SetVisibleColumns);

            // When the dialog is closed, update the columns
            this.GetVisibleColumns();
        }

        private void GetVisibleColumns()
        {
            bool columnRatingVisible = false;
            bool columnLoveVisible = false;

            CollectionUtils.GetVisibleSongsColumns(
                ref columnRatingVisible,
                ref columnLoveVisible,
                ref this.lyricsVisible,
                ref this.artistVisible,
                ref this.albumVisible,
                ref this.genreVisible,
                ref this.lengthVisible,
                ref this.playCountVisible,
                ref this.skipCountVisible,
                ref this.dateLastPlayedVisible,
                ref this.dateAddedVisible,
                ref this.dateCreatedVisible,
                ref this.albumArtistVisible,
                ref this.trackNumberVisible,
                ref this.yearVisible,
                ref this.bitrateVisible);

            RaisePropertyChanged(nameof(this.LyricsVisible));
            RaisePropertyChanged(nameof(this.ArtistVisible));
            RaisePropertyChanged(nameof(this.AlbumVisible));
            RaisePropertyChanged(nameof(this.GenreVisible));
            RaisePropertyChanged(nameof(this.LengthVisible));
            RaisePropertyChanged(nameof(this.PlayCountVisible));
            RaisePropertyChanged(nameof(this.SkipCountVisible));
            RaisePropertyChanged(nameof(this.DateLastPlayedVisible));
            RaisePropertyChanged(nameof(this.DateAddedVisible));
            RaisePropertyChanged(nameof(this.DateCreatedVisible));
            RaisePropertyChanged(nameof(this.AlbumArtistVisible));
            RaisePropertyChanged(nameof(this.TrackNumberVisible));
            RaisePropertyChanged(nameof(this.YearVisible));
            RaisePropertyChanged(nameof(this.BitrateVisible));


            this.RatingVisible = this.EnableRating && columnRatingVisible;
            this.LoveVisible = this.EnableLove && columnLoveVisible;
        }

        protected async override Task FillListsAsync()
        {
            await this.GetTracksAsync();
        }

        protected async Task GetTracksAsync()
        {
            await Task.Run(() => {
                IScrobblingService scrobblingService = container.Resolve<IScrobblingService>();
                IAlbumVRepository albumVRepository = container.Resolve<IAlbumVRepository>();
                ITrackVRepository trackVRepository = container.Resolve<ITrackVRepository>();
                IIndexingService indexingService = container.Resolve<IIndexingService>();
                IMetadataService metadataService = container.Resolve<IMetadataService>();

                IList<TrackViewModel> trackViewModels;
                if (_historyListMode == HistoryListMode.Tracks)
                {
                    Dictionary<long, long> ranking = trackVRepository.GetRanking();
                    // Get the tracks
                    QueryOptions qo = new QueryOptions();
                    qo.extraWhereClause.Add("plays>0");
                    IList<TrackV> tracks = trackVRepository.GetTracksWithText(_searchText, new QueryOptions(DataRichnessEnum.History));
                    trackViewModels = tracks.OrderByDescending(x => x.PlayCount).
                                                                    Select(x => new TrackViewModel(metadataService, scrobblingService, albumVRepository, indexingService, x)
                                                                    {
                                                                        Rank = (x.PlayCount.HasValue && (long)x.PlayCount.Value > 0) ? ranking[(long)x.PlayCount.Value] : (long?)null
                                                                    }).ToList();
                }
                else
                {
                    TracksHistoryLogMode tracksHistoryLogMode = TracksHistoryLogMode.All;
                    if (_historyListMode == HistoryListMode.LogPlayed)
                        tracksHistoryLogMode = TracksHistoryLogMode.Played;
                    IList<TrackV> tracks = trackVRepository.GetTracksHistoryLog(tracksHistoryLogMode, _searchText);// (_searchText, true, qo);
                    RecordCount = tracks.Count();
                    trackViewModels = tracks.Select(x => new TrackViewModel(metadataService, scrobblingService, albumVRepository, indexingService, x)).ToList();
                }

                try
                {
                    // Do we need to show the TrackNumber?


                    // Order the Tracks
                    //List<TrackViewModel> orderedTrackViewModels = await EntityUtils.OrderTracksAsync(tracks, trackOrder);

                    // Unbind to improve UI performance
                    //this.ClearTracks();

                    // Populate ObservableCollection
                    this.tracks = new ObservableCollection<TrackViewModel>(trackViewModels);
                }
                catch (Exception)
                {
                    //LogClient.Error("An error occurred while getting Tracks. Exception: {0}", ex.Message);

                    // Failed getting Tracks. Create empty ObservableCollection.
                    this.tracks = new ObservableCollection<TrackViewModel>();
                }
                RefreshView();
            });

        }

        public CollectionViewSource TracksCvs
        {
            get { return this.tracksCvs; }
            set { SetProperty<CollectionViewSource>(ref this.tracksCvs, value); }
        }

        private void RefreshView()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Populate CollectionViewSource

                this.TracksCvs = new CollectionViewSource { Source = this.tracks };
                // Update count
                //this.TracksCount = this.TracksCvs.View.Cast<TrackViewModel>().Count();

                //this.CalculateSizeInformationAsync(this.TracksCvs);
                //this.ShowPlayingTrackAsync();
            });

            // Update duration and size

            // Show playing Track
        }

        protected async override Task EmptyListsAsync()
        {
            TracksCvs = null;
            tracks = null;
        }

        protected override async void FilterListsAsync(string searchText)
        {
            _searchText = searchText;
            await GetTracksAsync();
            /*
            GetFilteredTracksAsync(_searchText, TrackOrder);
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.CalculateSizeInformationAsync(this.TracksCvs);
                this.ShowPlayingTrackAsync();
            });
            */
        }

        protected override void ConditionalScrollToPlayingTrack()
        {
            throw new System.NotImplementedException();
        }

        protected override void MetadataService_RatingChangedAsync(RatingChangedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        protected override void MetadataService_LoveChangedAsync(LoveChangedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        protected override void ShowSelectedTrackInformation()
        {
            throw new System.NotImplementedException();
        }

        protected async override Task LoadedCommandAsync()
        {
            await Task.Delay(Constants.CommonListLoadDelay);  // Wait for the UI to slide in
            await this.FillListsAsync(); // Fill all the lists
        }

        protected async override Task UnloadedCommandAsync()
        {
            await this.EmptyListsAsync(); // Empty all the lists
            GC.Collect(); // For the memory maniacs
        }


        private async Task ToggleHistoryListMode()
        {
            
            await EmptyListsAsync();
            switch (_historyListMode)
            {
                case HistoryListMode.LogAll:
                    _historyListMode = HistoryListMode.LogPlayed;
                    break;
                case HistoryListMode.LogPlayed:
                    _historyListMode = HistoryListMode.Tracks;
                    break;
                case HistoryListMode.Tracks:
                    _historyListMode = HistoryListMode.LogAll;
                    break;
                default:
                    _historyListMode = HistoryListMode.LogAll;
                    break;
            }
            await UpdateHistoryListMode();
            await FillListsAsync();
        }


        private async Task UpdateHistoryListMode()
        {
            switch (_historyListMode)
            {
                case HistoryListMode.LogAll:
                    HistoryListModeText = ResourceUtils.GetString("Language_History");
                    HappenedExVisible = true;
                    HappenedVisible = false;
                    RankVisible = false;
                    break;
                case HistoryListMode.LogPlayed:
                    HistoryListModeText = String.Format($"{ResourceUtils.GetString("Language_History")} / {ResourceUtils.GetString("Language_Played")}");
                    HappenedExVisible = false;
                    HappenedVisible = true;
                    RankVisible = false;
                    break;
                case HistoryListMode.Tracks:
                    HistoryListModeText = ResourceUtils.GetString("Language_Rank");
                    HappenedExVisible = false;
                    HappenedVisible = false;
                    RankVisible = true;
                    break;
                default:
                    HistoryListModeText = "???";
                    break;
            }
        }

    }
}
