﻿using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Alex; //Digimezzo.Foundation.Core.Settings
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Core.Prism;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using Dopamine.Services.Collection;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.Extensions;
using Dopamine.Services.I18n;
using Dopamine.Services.Metadata;
using Dopamine.Services.Playback;
using Dopamine.Services.Playlist;
using Dopamine.Services.Provider;
using Dopamine.Services.Search;
using Dopamine.Services.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using NLog;

namespace Dopamine.ViewModels.Common.Base
{
    public abstract class TracksViewModelBase : CommonViewModelBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IContainerProvider container;
        private IDialogService dialogService;
        private ITrackVRepository trackRepository;
        private ISearchService searchService;
        private IPlaybackService playbackService;
        private ICollectionService collectionService;
        private II18nService i18nService;
        private IEventAggregator eventAggregator;
        private IProviderService providerService;
        private IPlaylistService playlistService;
        private IMetadataService metadataService;
        private ObservableCollection<TrackViewModel> tracks;
        private CollectionViewSource tracksCvs;
        private IList<TrackViewModel> selectedTracks;
        private IList<ArtistViewModel> selectedArtists;
        private IList<AlbumViewModel> selectedAlbums;
        private string _searchText;

        public TrackViewModel PreviousPlayingTrack { get; set; }

        public bool ShowRemoveFromDisk => SettingsClient.Get<bool>("Behaviour", "ShowRemoveFromDisk");

        public ObservableCollection<TrackViewModel> Tracks
        {
            get { return this.tracks; }
            set { SetProperty<ObservableCollection<TrackViewModel>>(ref this.tracks, value); }
        }

        public CollectionViewSource TracksCvs
        {
            get { return this.tracksCvs; }
            set { SetProperty<CollectionViewSource>(ref this.tracksCvs, value); }
        }

        public IList<TrackViewModel> SelectedTracks
        {
            get { return this.selectedTracks; }
            set { SetProperty<IList<TrackViewModel>>(ref this.selectedTracks, value); }
        }

        public TracksViewModelBase(IContainerProvider container) : base(container)
        {
            // Dependency injection
            this.container = container;
            this.trackRepository = container.Resolve<ITrackVRepository>();
            this.dialogService = container.Resolve<IDialogService>();
            this.searchService = container.Resolve<ISearchService>();
            this.playbackService = container.Resolve<IPlaybackService>();
            this.collectionService = container.Resolve<ICollectionService>();
            this.i18nService = container.Resolve<II18nService>();
            this.eventAggregator = container.Resolve<IEventAggregator>();
            this.providerService = container.Resolve<IProviderService>();
            this.playlistService = container.Resolve<IPlaylistService>();
            this.metadataService = container.Resolve<IMetadataService>();

            // Events
            this.metadataService.MetadataChanged += MetadataChangedHandlerAsync;

            // Commands
            this.ToggleTrackOrderCommand = new DelegateCommand(() => this.ToggleTrackOrder());
            this.AddTracksToPlaylistCommand = new DelegateCommand<string>(async (playlistName) => await this.AddTracksToPlaylistAsync(playlistName, this.SelectedTracks));
            this.PlaySelectedCommand = new DelegateCommand(async () => await this.PlaySelectedAsync());
            this.PlayNextCommand = new DelegateCommand(async () => await this.PlayNextAsync());
            this.AddTracksToNowPlayingCommand = new DelegateCommand(async () => await this.AddTracksToNowPlayingAsync());
            this.RemoveSelectedTracksFromDiskCommand = new DelegateCommand(async () => await this.RemoveTracksFromDiskAsync(this.SelectedTracks), () => !this.IsIndexing);

            // Settings changed
            Digimezzo.Foundation.Core.Settings.SettingsClient.SettingChanged += (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "ShowRemoveFromDisk"))
                {
                    RaisePropertyChanged(nameof(this.ShowRemoveFromDisk));
                }
            };

            // Events
            this.i18nService.LanguageChanged += (_, __) =>
            {
                RaisePropertyChanged(nameof(this.TotalDurationInformation));
                RaisePropertyChanged(nameof(this.TotalSizeInformation));
                this.RefreshLanguage();
            };

            this.playbackService.TrackHistoryChanged += PlaybackService_TrackHistoryChanged;
        }

        protected virtual async void MetadataChangedHandlerAsync(MetadataChangedEventArgs e)
        {
            await this.FillListsAsync();
        }

        private async void PlaybackService_TrackHistoryChanged(TrackViewModel track)
        {
            if (this.Tracks == null)
            {
                return;
            }



            await Task.Run(() =>
            {
                Logger.Warn("PlaybackService_TrackHistoryChanged ALEX TODO. This need work / test");
                if (Tracks.Contains(track))
                    Application.Current.Dispatcher.Invoke(() => track.UpdateVisibleCounters(null));
                /*
                foreach (TrackViewModel vm in this.Tracks)
                {
                    if (counters.Select(c => c.SafePath).Contains(vm.SafePath))
                    {
                        // The UI is only updated if PropertyChanged is fired on the UI thread
                        PlaybackCounter counter = counters.Where(c => c.SafePath.Equals(vm.SafePath)).FirstOrDefault();
                        Application.Current.Dispatcher.Invoke(() => vm.UpdateVisibleCounters(counter));
                    }
                }
                */
            });
        }

        protected void SetTrackOrder(string settingName)
        {
            TrackOrder savedTrackOrder = (TrackOrder)SettingsClient.Get<int>("Ordering", settingName);

            if ((!this.EnableRating & savedTrackOrder == TrackOrder.ByRating))
            {
                this.TrackOrder = TrackOrder.Alphabetical;
            }
            else
            {
                // Only change the TrackOrder if it is not correct
                if (this.TrackOrder != savedTrackOrder) this.TrackOrder = savedTrackOrder;
            }
        }




        /*
        protected async Task GetTracksAsync(IList<string> artists, IList<string> genres, IList<AlbumViewModel> albumViewModels, TrackOrder trackOrder)
        {
            IList<Track> tracks = null;

            if (albumViewModels != null && albumViewModels.Count > 0)
            {
                // First, check Albums. They topmost have priority.
                tracks = await this.trackRepository.GetAlbumTracksAsync(albumViewModels.Select(x => x.Thumbnail).ToList());
            }
            else if (!artists.IsNullOrEmpty())
            {
                // Artists and Genres have the same priority
                tracks = await this.trackRepository.GetArtistTracksAsync(artists.Select(x => x.Replace(ResourceUtils.GetString("Language_Unknown_Artist"), string.Empty)).ToList());
            }
            else if (!genres.IsNullOrEmpty())
            {
                // Artists and Genres have the same priority
                tracks = await this.trackRepository.GetGenreTracksAsync(genres.Select(x => x.Replace(ResourceUtils.GetString("Language_Unknown_Genre"), string.Empty)).ToList());
            }
            else
            {
                // Tracks have lowest priority
                tracks = await this.trackRepository.GetTracksAsync();
            }

            await this.GetTracksCommonAsync(await this.container.ResolveTrackViewModelsAsync(tracks), trackOrder);
        }
        */

        protected async Task GetTracksAsync(IList<ArtistViewModel> artists, IList<GenreViewModel> genres, IList<AlbumViewModel> albums, TrackOrder trackOrder)
        {
            /*
            selectedArtists = artists;
            selectedAlbums = albums;
            if (Tracks == null || Tracks.Count == 0)
            {
                IList<TrackV> tracks = await Task.Run(()=> trackRepository.GetTracks());
                await this.GetTracksCommonAsync(await this.container.ResolveTrackViewModelsAsync(tracks), trackOrder);
            }
            else
                RefreshView();
            */
            IList<TrackV> tracks = null;
            if (albums != null && albums.Count > 0)
            {
                // First, check Albums. They topmost have priority.
                tracks = trackRepository.GetTracksOfAlbums(albums.Select(x => x.Id).ToList());
            }
            else if (!artists.IsNullOrEmpty())
            {
                // Artists and Genres have the same priority
                tracks = trackRepository.GetTracksOfArtists(artists.Select(x => x.Id).ToList());
            }
            else if (!genres.IsNullOrEmpty())
            {
                // Artists and Genres have the same priority
                tracks = trackRepository.GetTracksWithGenres(genres.Select(x => x.Id).ToList());
            }
            else
            {
                // Tracks have lowest priority
                tracks = trackRepository.GetTracks();
            }
            await this.GetTracksCommonAsync(await this.container.ResolveTrackViewModelsAsync(tracks), trackOrder);


        }

        protected async Task GetFilteredTracksAsync(string searchFilter, TrackOrder trackOrder)
        {
            IList<TrackV> tracks = trackRepository.GetTracksWithText(searchFilter);
            await this.GetTracksCommonAsync(await this.container.ResolveTrackViewModelsAsync(tracks), trackOrder);
        }

        protected void ClearTracks()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.TracksCvs = null;
            });

            this.Tracks = null;
        }

        protected virtual async Task GetTracksCommonAsync(IList<TrackViewModel> tracks, TrackOrder trackOrder)
        {
            try
            {
                // Do we need to show the TrackNumber?
                bool showTracknumber = this.TrackOrder == TrackOrder.ByAlbum;

                await Task.Run(() =>
                {
                    foreach (TrackViewModel vm in tracks)
                    {
                        vm.ShowTrackNumber = showTracknumber;
                    }
                });

                // Order the Tracks
                List<TrackViewModel> orderedTrackViewModels = await EntityUtils.OrderTracksAsync(tracks, trackOrder);

                // Unbind to improve UI performance
                this.ClearTracks();

                // Populate ObservableCollection
                this.Tracks = new ObservableCollection<TrackViewModel>(orderedTrackViewModels);
            }
            catch (Exception ex)
            {
                LogClient.Error("An error occurred while getting Tracks. Exception: {0}", ex.Message);

                // Failed getting Tracks. Create empty ObservableCollection.
                this.Tracks = new ObservableCollection<TrackViewModel>();
            }
            RefreshView();


        }

        private void RefreshView()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Populate CollectionViewSource

                this.TracksCvs = new CollectionViewSource { Source = this.Tracks };
                // Update count
                this.TracksCount = this.TracksCvs.View.Cast<TrackViewModel>().Count();

                // Group by Album if needed
                if (this.TrackOrder == TrackOrder.ByAlbum)
                {
                    this.TracksCvs.GroupDescriptions.Add(new PropertyGroupDescription("GroupHeader"));
                }
            });

            // Update duration and size
            this.CalculateSizeInformationAsync(this.TracksCvs);

            // Show playing Track
            this.ShowPlayingTrackAsync();
        }

        protected async Task RemoveTracksFromCollectionAsync(IList<TrackViewModel> selectedTracks)
        {
            string title = ResourceUtils.GetString("Language_Remove");
            string body = ResourceUtils.GetString("Language_Are_You_Sure_To_Remove_Song");

            if (selectedTracks != null && selectedTracks.Count > 1)
            {
                body = ResourceUtils.GetString("Language_Are_You_Sure_To_Remove_Songs");
            }

            if (this.dialogService.ShowConfirmation(0xe11b, 16, title, body, ResourceUtils.GetString("Language_Yes"), ResourceUtils.GetString("Language_No")))
            {
                RemoveTracksResult result = await this.collectionService.RemoveTracksFromCollectionAsync(selectedTracks, false);

                if (result == RemoveTracksResult.Error)
                {
                    this.dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetString("Language_Error"), ResourceUtils.GetString("Language_Error_Removing_Songs"), ResourceUtils.GetString("Language_Ok"), true, ResourceUtils.GetString("Language_Log_File"));
                }
                else
                {
                    await this.playbackService.RemoveTracks(selectedTracks);
                }
            }
        }

        protected async Task RemoveTracksFromDiskAsync(IList<TrackViewModel> selectedTracks)
        {
            string title = ResourceUtils.GetString("Language_Remove_From_Disk");
            string body = ResourceUtils.GetString("Language_Are_You_Sure_To_Remove_Song_From_Disk");

            if (selectedTracks != null && selectedTracks.Count > 1)
            {
                body = ResourceUtils.GetString("Language_Are_You_Sure_To_Remove_Songs_From_Disk");
            }

            if (this.dialogService.ShowConfirmation(0xe11b, 16, title, body, ResourceUtils.GetString("Language_Yes"), ResourceUtils.GetString("Language_No")))
            {
                RemoveTracksResult result = await this.collectionService.RemoveTracksFromCollectionAsync(selectedTracks, true);

                if (result == RemoveTracksResult.Error)
                {
                    this.dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetString("Language_Error"), ResourceUtils.GetString("Language_Error_Removing_Songs_From_Disk"), ResourceUtils.GetString("Language_Ok"), true, ResourceUtils.GetString("Language_Log_File"));
                }
                else
                {
                    await this.playbackService.RemoveTracks(selectedTracks);
                }
            }
        }

        protected async void CalculateSizeInformationAsync(CollectionViewSource source)
        {
            // Reset duration and size
            this.SetSizeInformation(0, 0);

            CollectionView viewCopy = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (source != null)
                {
                    // Create copy of CollectionViewSource because only STA can access it
                    // ALEX TODO. Here is where the warning for CollectionView comes from
                    viewCopy = new CollectionView(source.View);
                }
            });

            if (viewCopy != null)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        long totalDuration = 0;
                        long totalSize = 0;

                        foreach (TrackViewModel vm in viewCopy)
                        {
                            totalDuration += vm.Data.Duration.HasValue ? vm.Data.Duration.Value : 0;
                            totalSize += vm.Data.FileSize.HasValue ? vm.Data.FileSize.Value : 0;
                        }

                        this.SetSizeInformation(totalDuration, totalSize);
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("An error occurred while setting size information. Exception: {0}", ex.Message);
                    }

                });
            }

            RaisePropertyChanged(nameof(this.TotalDurationInformation));
            RaisePropertyChanged(nameof(this.TotalSizeInformation));
        }

        protected async Task PlaySelectedAsync()
        {
            await this.playbackService.PlayTracksAsync(SelectedTracks, PlaylistMode.Play);
        }

        protected async Task PlayNextAsync()
        {
            await this.playbackService.PlayTracksAsync(SelectedTracks, PlaylistMode.EnqueuNext);
        }

        protected async Task AddTracksToNowPlayingAsync()
        {
            await this.playbackService.PlayTracksAsync(SelectedTracks, PlaylistMode.Enqueue);
        }

        protected override void FilterLists(string searchText)
        {
            _searchText = searchText;
            GetFilteredTracksAsync(_searchText, TrackOrder);

            this.CalculateSizeInformationAsync(this.TracksCvs);
            this.ShowPlayingTrackAsync();
        }

        protected override void ConditionalScrollToPlayingTrack()
        {
            // Trigger ScrollToPlayingTrack only if set in the settings
            if (SettingsClient.Get<bool>("Behaviour", "FollowTrack"))
            {
                if (this.Tracks != null && this.Tracks.Count > 0)
                {
                    this.eventAggregator.GetEvent<ScrollToPlayingTrack>().Publish(null);
                }
            }
        }

        protected override async Task ShowPlayingTrackAsync()
        {
            await Task.Run(() =>
            {
                if (this.PreviousPlayingTrack != null)
                {
                    this.PreviousPlayingTrack.IsPlaying = false;
                    this.PreviousPlayingTrack.IsPaused = true;
                }

                if (!this.playbackService.HasCurrentTrack)
                {
                    return;
                }

                if (this.Tracks == null)
                {
                    return;
                }

                var safePath = this.playbackService.CurrentTrack.SafePath;

                // First, find the correct track by reference.
                TrackViewModel currentPlayingTrack = this.Tracks.FirstOrDefault(x => x.Equals(this.playbackService.CurrentTrack));

                // Then, if there is no reference match, find a track with the same path.
                if (currentPlayingTrack == null)
                {
                    currentPlayingTrack = this.Tracks.FirstOrDefault(x => x.SafePath.Equals(this.playbackService.CurrentTrack.SafePath));
                }

                if (!this.playbackService.IsStopped && currentPlayingTrack != null)
                {
                    currentPlayingTrack.IsPlaying = true;
                    currentPlayingTrack.IsPaused = !this.playbackService.IsPlaying;
                }

                this.PreviousPlayingTrack = currentPlayingTrack;
            });

            this.ConditionalScrollToPlayingTrack();
        }

        protected async override void MetadataService_RatingChangedAsync(RatingChangedEventArgs e)
        {
            if (this.Tracks == null) return;

            await Task.Run(() =>
            {
                foreach (TrackViewModel vm in this.Tracks)
                {
                    if (vm.SafePath.Equals(e.SafePath))
                    {
                        // The UI is only updated if PropertyChanged is fired on the UI thread
                        Application.Current.Dispatcher.Invoke(() => vm.UpdateVisibleRating(e.Rating));
                    }
                }
            });
        }

        protected async override void MetadataService_LoveChangedAsync(LoveChangedEventArgs e)
        {
            if (this.Tracks == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                foreach (TrackViewModel vm in this.Tracks)
                {
                    if (vm.SafePath.Equals(e.SafePath))
                    {
                        // The UI is only updated if PropertyChanged is fired on the UI thread
                        Application.Current.Dispatcher.Invoke(() => vm.UpdateVisibleLove(e.Love));
                    }
                }
            });
        }

        protected override void ShowSelectedTrackInformation()
        {
            // Don't try to show the file information when nothing is selected
            if (this.SelectedTracks == null || this.SelectedTracks.Count == 0) return;

            this.ShowFileInformation(this.SelectedTracks.Select(t => t.Path).ToList());
        }

        protected async override Task LoadedCommandAsync()
        {
            await Task.Delay(Constants.CommonListLoadDelay);  // Wait for the UI to slide in
            await this.FillListsAsync(); // Fill all the lists
        }

        protected async override Task UnloadedCommandAsync()
        {
            this.EmptyListsAsync(); // Empty all the lists
            GC.Collect(); // For the memory maniacs
        }

        protected override void EditSelectedTracks()
        {
            if (this.SelectedTracks == null || this.SelectedTracks.Count == 0) return;

            this.EditFiles(this.SelectedTracks.Select(t => t.Path).ToList());
        }

        protected override void SelectedTracksHandler(object parameter)
        {
            if (parameter != null)
            {
                this.SelectedTracks = new List<TrackViewModel>();

                foreach (TrackViewModel item in (IList)parameter)
                {
                    this.SelectedTracks.Add(item);
                }
            }
        }

        protected override void SearchOnline(string id)
        {
            if (this.SelectedTracks != null && this.SelectedTracks.Count > 0)
            {
                this.providerService.SearchOnline(id, new string[] { this.SelectedTracks.First().ArtistName, this.SelectedTracks.First().TrackTitle });
            }
        }

        protected virtual void ToggleTrackOrder()
        {
            switch (this.TrackOrder)
            {
                case TrackOrder.Alphabetical:
                    this.TrackOrder = TrackOrder.ReverseAlphabetical;
                    break;
                case TrackOrder.ReverseAlphabetical:
                    this.TrackOrder = TrackOrder.ByAlbum;
                    break;
                case TrackOrder.ByAlbum:
                    if (SettingsClient.Get<bool>("Behaviour", "EnableRating"))
                    {
                        this.TrackOrder = TrackOrder.ByRating;
                    }
                    else
                    {
                        this.TrackOrder = TrackOrder.Alphabetical;
                    }

                    break;
                case TrackOrder.ByRating:
                    this.TrackOrder = TrackOrder.Alphabetical;
                    break;
                default:
                    // Cannot happen, but just in case.
                    this.TrackOrder = TrackOrder.ByAlbum;
                    break;
            }
        }

        protected virtual void RefreshLanguage()
        {
            // Make sure that unknown artist, genre and album are translated correctly.
            this.FillListsAsync();
        }
    }
}
