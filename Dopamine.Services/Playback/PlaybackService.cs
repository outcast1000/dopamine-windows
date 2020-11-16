using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Alex; //Digimezzo.Foundation.Core.Settings
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Audio;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Core.Helpers;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Data.Repositories;
using Dopamine.Services.Entities;
using Dopamine.Services.Equalizer;
using Dopamine.Services.Extensions;
using Dopamine.Services.File;
using Dopamine.Services.I18n;
using Dopamine.Services.Playlist;
using Dopamine.Services.Utils;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Diagnostics;
using System.ServiceModel.Description;
using Dopamine.Services.Metadata;
using Dopamine.Services.Scrobbling;
using Dopamine.Services.Indexing;

namespace Dopamine.Services.Playback
{
    public class PlaybackService : IPlaybackService
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private QueueManager<TrackViewModel> queueManager;
        private System.Timers.Timer progressTimer = new System.Timers.Timer();
        private double progressTimeoutSeconds = 0.5;
        private double progress = 0.0;
        private float volume = 0.0f;
        private LoopMode loopMode;
        private bool mute;
        private bool isPlayingPreviousTrack;
        private IPlayer player;
        private bool hasMediaFoundationSupport = false;

        private bool isLoadingSettings;

        private bool isQueueChanged;
        private bool canGetSavedQueuedTracks = true;

        private II18nService i18nService;
        private IFileService fileService;
        private IEqualizerService equalizerService;
        private IPlaylistService playlistService;
        private IContainerProvider container;
        private EqualizerPreset desiredPreset;
        private EqualizerPreset activePreset;
        private bool isEqualizerEnabled;

        private System.Timers.Timer saveQueuedTracksTimer = new System.Timers.Timer();
        private int saveQueuedTracksTimeoutSeconds = 5;

        private bool isSavingQueuedTracks = false;

        private IPlayerFactory playerFactory;

        private ITrackVRepository trackRepository;
        private IGeneralRepository generalRepository;
        private ITrackHistoryRepository trackHistoryRepository;

        //private System.Timers.Timer savePlaybackCountersTimer = new System.Timers.Timer();
        //private int savePlaybackCountersTimeoutSeconds = 2;

        //private bool isSavingPLaybackCounters = false;
        //private Dictionary<string, PlaybackCounter> playbackCounters = new Dictionary<string, PlaybackCounter>();

        //private object playbackCountersLock = new object();

        private SynchronizationContext context;
        private bool isLoadingTrack;

        private AudioDevice audioDevice;

        public bool IsSavingQueuedTracks => this.isSavingQueuedTracks;

        //public bool IsSavingPlaybackCounters => this.isSavingPLaybackCounters;

        public bool HasMediaFoundationSupport => this.hasMediaFoundationSupport;

        public async Task SetPlaylistPositionAsync(int newPosition, bool bStartPaused)
        {
            queueManager.Position = newPosition;
            // NewPosition may be invalid
            if (queueManager.Position == newPosition)
            {
                PlaylistPositionChanged(this, new EventArgs());
                if (! await TryPlayAsync(queueManager.CurrentItem, bStartPaused, false))
                    StopPlayback();
            }
        }


        public bool IsStopped
        {
            get
            {
                if (this.player != null)
                {
                    return !this.player.CanStop;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool IsPlaying
        {
            get
            {
                if (this.player != null)
                {
                    return this.player.CanPause;
                }
                else
                {
                    return false;
                }
            }
        }

        public IList<TrackViewModel> Playlist => queueManager.Playlist;

        public IList<PlaylistItem> PlaylistItems
        {
            get
            {
                List<PlaylistItem> items = new List<PlaylistItem>();
                int i = 0;
                foreach (TrackViewModel vm in queueManager.Playlist)
                {
                    items.Add(new PlaylistItem(i, i == queueManager.Position, vm));
                    i++;
                }
                return items;
            }
        }
        public TrackViewModel CurrentTrack => this.queueManager.CurrentItem;

        public bool HasQueue => !this.queueManager.Playlist.IsNullOrEmpty();

        public bool HasCurrentTrack => this.queueManager.CurrentItem != null;

        public double Progress
        {
            get { return this.progress; }
            set { this.progress = value; }
        }

        public float Volume
        {
            get { return this.volume; }

            set
            {
                if (value > 1)
                {
                    value = 1;
                }

                if (value < 0)
                {
                    value = 0;
                }

                this.volume = value;

                if (this.player != null && !this.mute) this.player.SetVolume(value);

                SettingsClient.Set<double>("Playback", "Volume", Math.Round(value, 2));
                this.PlaybackVolumeChanged(this, new PlaybackVolumeChangedEventArgs(isLoadingSettings));
            }
        }

        public LoopMode LoopMode
        {
            get { return this.loopMode; }
            set
            {
                this.loopMode = value;
                queueManager.Loop = (value == LoopMode.All);
                this.PlaybackLoopChanged(this, new EventArgs());
            }
        }

        public bool Shuffle
        {
            get { return queueManager.Shuffle; } set { 
                queueManager.Shuffle = value;
                this.PlaybackShuffleChanged(this, new EventArgs());
            }
        }

        public bool Mute
        {
            get { return this.mute; }
        }

        public bool UseAllAvailableChannels { get; set; }

        public int Latency { get; set; }

        public bool EventMode { get; set; }

        public bool ExclusiveMode { get; set; }

        public TimeSpan GetCurrentTime
        {
            get
            {
                try
                {
                    // Check if there is a Track playing
                    if (this.player != null && this.player.CanStop)
                    {
                        // This prevents displaying a current time which is larger than the total time
                        if (this.player.GetCurrentTime() <= this.player.GetTotalTime())
                        {
                            return this.player.GetCurrentTime();
                        }
                        else
                        {
                            return this.player.GetTotalTime();
                        }
                    }
                    else
                    {
                        return new TimeSpan(0);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Failed to get current time. Returning 00:00. Exception: {0}", ex.Message);
                    return new TimeSpan(0);
                }

            }
        }

        public TimeSpan GetTotalTime
        {
            get
            {
                try
                {
                    // Check if there is a Track playing
                    if (this.player != null && this.player.CanStop && this.HasCurrentTrack && this.CurrentTrack.Duration != null)
                    {
                        // In some cases, the duration reported by TagLib is 1 second longer than the duration reported by IPlayer.
                        if (this.CurrentTrack.Data.Duration > this.player.GetTotalTime().TotalMilliseconds)
                        {
                            // To show the same duration everywhere, we report the TagLib duration here instead of the IPlayer duration.
                            return new TimeSpan(0, 0, 0, 0, Convert.ToInt32(this.CurrentTrack.Data.Duration));
                        }
                        else
                        {
                            // Unless the TagLib duration is incorrect. In rare cases it is 0, even if 
                            // IPlayer reports a correct duration. In such cases, report the IPlayer duration.
                            return this.player.GetTotalTime();
                        }
                    }
                    else
                    {
                        return new TimeSpan(0);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to get total time. Returning 00:00. Exception: {0}", ex.Message);
                    return new TimeSpan(0);
                }

            }
        }

        public IPlayer Player
        {
            get { return this.player; }
        }

        public int? CurrentPlaylistPosition => queueManager.Position;

        public PlaybackService(IFileService fileService, II18nService i18nService, ITrackVRepository trackRepository,
            IEqualizerService equalizerService, IGeneralRepository generalRepository, IContainerProvider container, 
            IPlaylistService playlistService, ITrackHistoryRepository trackHistoryRepository)
        {
            this.fileService = fileService;
            this.i18nService = i18nService;
            this.trackRepository = trackRepository;
            this.generalRepository = generalRepository;
            this.trackHistoryRepository = trackHistoryRepository;
            this.equalizerService = equalizerService;
            this.playlistService = playlistService;
            this.container = container;

            this.context = SynchronizationContext.Current;

            this.queueManager = new QueueManager<TrackViewModel>();

            // Event handlers
            this.fileService.ImportingTracks += (_, __) => this.canGetSavedQueuedTracks = false;
            this.fileService.TracksImported += async (tracks, track) => await this.PlayTracksAndStartOnTrack(tracks, track);
            this.i18nService.LanguageChanged += (_, __) => this.UpdateQueueLanguageAsync();

            // Set up timers
            this.progressTimer.Interval = TimeSpan.FromSeconds(this.progressTimeoutSeconds).TotalMilliseconds;
            this.progressTimer.Elapsed += new ElapsedEventHandler(this.ProgressTimeoutHandler);

            this.saveQueuedTracksTimer.Interval = TimeSpan.FromSeconds(this.saveQueuedTracksTimeoutSeconds).TotalMilliseconds;
            this.saveQueuedTracksTimer.Elapsed += new ElapsedEventHandler(this.SaveQueuedTracksTimeoutHandler);

            //this.savePlaybackCountersTimer.Interval = TimeSpan.FromSeconds(this.savePlaybackCountersTimeoutSeconds).TotalMilliseconds;
            //this.savePlaybackCountersTimer.Elapsed += new ElapsedEventHandler(this.SavePlaybackCountersHandler);

            this.Initialize();
        }

        public event PlaybackSuccessEventHandler PlaybackSuccess = delegate { };
        public event PlaybackPausedEventHandler PlaybackPaused = delegate { };
        public event PlaybackFailedEventHandler PlaybackFailed = delegate { };
        public event EventHandler PlaybackProgressChanged = delegate { };
        public event EventHandler PlaybackResumed = delegate { };
        public event EventHandler PlaybackStopped = delegate { };
        public event PlaybackVolumeChangedEventhandler PlaybackVolumeChanged = delegate { };
        public event EventHandler PlaybackMuteChanged = delegate { };
        public event EventHandler PlaybackLoopChanged = delegate { };
        public event EventHandler PlaybackShuffleChanged = delegate { };
        public event Action<int> AddedTracksToQueue = delegate { };
        //public event PlaybackCountersChangedEventHandler PlaybackCountersChanged = delegate { };
        public event TrackHistoryChangedEventHandler TrackHistoryChanged = delegate { };
        public event Action<bool> LoadingTrack = delegate { };
        public event EventHandler PlayingTrackChanged = delegate { };
        public event EventHandler PlaylistChanged = delegate { };
        public event EventHandler PlaylistPositionChanged = delegate { };
        public event EventHandler PlaybackSkipped = delegate { };

        private AudioDevice CreateDefaultAudioDevice()
        {
            return new AudioDevice(ResourceUtils.GetString("Language_Default_Audio_Device"), string.Empty);
        }

        public async Task<AudioDevice> GetSavedAudioDeviceAsync()
        {
            
            string savedAudioDeviceId = SettingsClient.Get<string>("Playback", "AudioDevice");

            IList<AudioDevice> audioDevices = await this.GetAllAudioDevicesAsync();
            AudioDevice savedDevice = audioDevices.Where(x => x.DeviceId.Equals(savedAudioDeviceId)).FirstOrDefault();

            if (savedDevice == null)
            {
                LogClient.Warning($"Audio device with deviceId={savedAudioDeviceId} could not be found. Using default device instead.");
                savedDevice = this.CreateDefaultAudioDevice();
            }

            return savedDevice;
        }

        public async Task<IList<AudioDevice>> GetAllAudioDevicesAsync()
        {
            var audioDevices = new List<AudioDevice>();

            await Task.Run(() =>
            {
                if (this.player != null)
                {
                    audioDevices.Add(this.CreateDefaultAudioDevice());
                    audioDevices.AddRange(this.player.GetAllAudioDevices());
                }
            });

            return audioDevices;
        }

        public async Task SwitchAudioDeviceAsync(AudioDevice device)
        {
            this.audioDevice = device;

            await Task.Run(() =>
            {
                if (this.player != null)
                {
                    this.player.SwitchAudioDevice(this.audioDevice);
                }
            });
        }

        public async Task StopIfPlayingAsync(TrackViewModel track)
        {
            if (track.SafePath.Equals(this.CurrentTrack.SafePath))
            {
                if (this.Playlist.Count == 1)
                {
                    this.Stop();
                }
                else
                {
                    await this.PlayNextAsync();
                }
            }
        }

        public async Task UpdateQueueOrderAsync(IList<PlaylistItem> items)
        {
            await Task.Run(() =>
            {
                IList<TrackViewModel> vms = items.Select(x => x.TrackViewModel).ToList();
                if (queueManager.ReorderTracks(vms))
                {
                    PlaylistChanged(this, new EventArgs());
                }
            });
        }


        public async Task UpdatePlaylistMetadataAsync(IList<FileMetadata> fileMetadatas)
        {
            //=== Need to refresh
            await RefreshPlaylistInfo();
            PlayingTrackChanged(this, new EventArgs());
            PlaylistChanged(this, new EventArgs());
        }

        private async void UpdateQueueLanguageAsync()
        {
            Logger.Warn("UpdateQueueLanguageAsync. Needs testing");
            await RefreshPlaylistInfo();
            this.PlayingTrackChanged(this, new EventArgs());
            this.PlaylistChanged(this, new EventArgs());
        }

        public async Task SetIsEqualizerEnabledAsync(bool isEnabled)
        {
            this.isEqualizerEnabled = isEnabled;

            this.desiredPreset = await this.equalizerService.GetSelectedPresetAsync();
            this.activePreset = isEnabled ? this.desiredPreset : new EqualizerPreset();

            if (this.player != null)
            {
                this.player.ApplyFilter(this.activePreset.Bands);
            }
        }

        public void ApplyPreset(EqualizerPreset preset)
        {
            this.desiredPreset = preset;

            if (this.isEqualizerEnabled)
            {
                this.activePreset = desiredPreset;

                if (this.player != null)
                {
                    this.player.ApplyFilter(this.activePreset.Bands);
                }
            }
        }

        private async Task RefreshPlaylistInfo()
        {
            await SavePlaylistAsync();
            IList<TrackV> existingTracks = trackRepository.GetPlaylistTracks();
            queueManager.UpdatePlaylistTrackInfo(await this.container.ResolveTrackViewModelsAsync(existingTracks));
        }

        public async Task SavePlaylistAsync()
        {
            if (!this.isQueueChanged)
            {
                return;
            }

            this.saveQueuedTracksTimer.Stop();
            this.isSavingQueuedTracks = true;

            await Task.Run(() =>
            {
                try
                {
                    IList<TrackV> tracks = queueManager.Playlist.Select(x => x.Data).ToList();
                    trackRepository.SavePlaylistTracks(tracks);
                    if (queueManager.CurrentItem != null)
                    {
                        TrackV currentTrackPath = this.CurrentTrack.Data;
                        long progressSeconds = Convert.ToInt64(this.GetCurrentTime.TotalSeconds);
                        generalRepository.SetValue(GeneralRepositoryKeys.PlayListPosition, queueManager.Position.ToString());
                        generalRepository.SetValue(GeneralRepositoryKeys.PlayListPositionInTrack, progressSeconds.ToString());
                        Logger.Info($"Saved {tracks.Count} tracks in playlist. (Position: {queueManager.Position} ProgressSeconds: {progressSeconds})");
                    }
                    else
                        Logger.Info($"Saved {tracks.Count} tracks in playlist. (No current track)");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not save queued tracks. Exception: {0}", ex.Message);
                }
            });

            this.isSavingQueuedTracks = false;
        }

        public async Task PlayOrPauseAsync()
        {
            if (!this.IsStopped)
            {
                if (this.IsPlaying)
                {
                    await this.PauseAsync();
                }
                else
                {
                    await this.ResumeAsync();
                }
            }
            else
            {
                if (this.Playlist != null && this.Playlist.Count > 0)
                {
                    // There are already tracks enqueued. Start playing immediately.
                    queueManager.Position = 0;
                    await this.TryPlayAsync(this.queueManager.CurrentItem, false, false);
                }
                else
                {
                    LoopMode = LoopMode.AutoPlay;
                    await TryPlayNextAsync(false);
                }
            }
        }

        public void SetMute(bool mute)
        {
            this.mute = mute;

            if (this.player != null)
            {
                this.player.SetVolume(mute ? 0.0f : this.Volume);
            }

            SettingsClient.Set<bool>("Playback", "Mute", this.mute);
            this.PlaybackMuteChanged(this, new EventArgs());
        }

        public void SkipProgress(double progress)
        {
            if (this.player != null && this.player.CanStop)
            {
                this.Progress = progress;
                int newSeconds = Convert.ToInt32(progress * this.player.GetTotalTime().TotalSeconds);
                this.player.Skip(newSeconds);
                this.PlaybackSkipped(this, new EventArgs());
            }
            else
            {
                this.Progress = 0.0;
            }

            this.PlaybackProgressChanged(this, new EventArgs());
        }

        public void SkipSeconds(int seconds)
        {
            if (this.player != null && this.player.CanStop)
            {
                double totalSeconds = this.GetCurrentTime.TotalSeconds;

                if (seconds < 0 && totalSeconds <= Math.Abs(seconds))
                {
                    this.player.Skip(0);
                }
                else
                {
                    this.player.Skip(Convert.ToInt32(this.GetCurrentTime.TotalSeconds + seconds));
                }

                this.PlaybackSkipped(this, new EventArgs());
                this.PlaybackProgressChanged(this, new EventArgs());
            }
        }

        public void Stop()
        {
            if (this.player != null && this.player.CanStop)
            {
                this.player.Stop();
            }

            this.PlayingTrackChanged(this, new EventArgs());

            this.progressTimer.Stop();
            this.Progress = 0.0;
            this.PlaybackStopped(this, new EventArgs());
        }

        public async Task PlayNextAsync()
        {
            LogClient.Info("Request to play the next track.");
            if (CurrentTrack != null)
                trackHistoryRepository.AddSkippedAction(CurrentTrack.Id, "PlayNext");
            // We don't want interruptions when trying to play the next Track.
            // If the next Track cannot be played, keep skipping to the 
            // following Track until a working Track is found.
            bool playSuccess = false;
            int numberSkips = 0;

            while (!playSuccess)
            {
                // We skip maximum 3 times. This prevents an infinite 
                // loop if shuffledTracks only contains broken Tracks.
                if (numberSkips < 3)
                {
                    numberSkips += 1;
                    playSuccess = await this.TryPlayNextAsync(true);
                }
                else
                {
                    this.Stop();
                    playSuccess = true; // Otherwise we never get out of this While loop
                }
            }
        }

        public async Task PlayPreviousAsync()
        {
            LogClient.Info("Request to play the previous track.");

            // We don't want interruptions when trying to play the previous Track. 
            // If the previous Track cannot be played, keep skipping to the
            // preceding Track until a working Track is found.
            bool playSuccess = false;
            int numberSkips = 0;

            while (!playSuccess)
            {
                // We skip maximum 3 times. This prevents an infinite 
                // loop if shuffledTracks only contains broken Tracks.
                if (numberSkips < 3)
                {
                    numberSkips += 1;
                    playSuccess = await this.TryPlayPreviousAsync(true);
                }
                else
                {
                    this.Stop();
                    playSuccess = true; // Otherwise we never get out of this While loop
                }
            }
        }

        /*
        public async Task PlayAllTracksAsync(PlaylistMode mode, TrackOrder trackOrder = TrackOrder.None)
        {
            IList<TrackV> tracks = this.trackRepository.GetTracks(new QueryOptions(DataRichnessEnum.History));
            await this.PlayTracksAsync(await this.container.ResolveTrackViewModelsAsync(tracks), mode, trackOrder);
        }
        */

        public async Task PlayTracksAndStartOnTrack(IList<TrackViewModel> tracks, TrackViewModel track)
        {
            if (tracks == null || track == null)
                return;
            await Task.Run(() =>
            {
                int idx = tracks.IndexOf(track);
                queueManager.Play(tracks, idx);
                trackHistoryRepository.AddExecuted(track.Id);
            });
            await TryPlayAsync(track, false, false);
        }

        public async Task PlayArtistsAsync(IList<ArtistViewModel> artists, PlaylistMode mode, TrackOrder trackOrder = TrackOrder.ByAlbum)
        {
            if (artists == null)
                return;
            IList<TrackV> tracks = trackRepository.GetTracksOfArtists(artists.Select(x => x.Id).ToList(), new QueryOptions(DataRichnessEnum.History));
            await this.PlayTracksAsync(await this.container.ResolveTrackViewModelsAsync(tracks), mode, trackOrder);
        }

        public async Task PlayGenresAsync(IList<GenreViewModel> genres, PlaylistMode mode, TrackOrder trackOrder = TrackOrder.ByAlbum)
        {
            if (genres == null)
                return;
            List<TrackV> tracks = trackRepository.GetTracksWithGenres(genres.Select(x => x.Id).ToList(), new QueryOptions(DataRichnessEnum.History));
            await this.PlayTracksAsync(await this.container.ResolveTrackViewModelsAsync(tracks), mode, trackOrder);
        }

        public async Task PlayAlbumsAsync(IList<AlbumViewModel> albums, PlaylistMode mode, TrackOrder trackOrder = TrackOrder.ByFileName)
        {
            if (albums == null)
                return;
            List<TrackV> tracks = trackRepository.GetTracksOfAlbums(albums.Select(x => x.Id).ToList(), new QueryOptions(DataRichnessEnum.History));
            await this.PlayTracksAsync(await this.container.ResolveTrackViewModelsAsync(tracks), mode, trackOrder);

        }

        public async Task PlayPlaylistsAsync(IList<PlaylistViewModel> playlistViewModels, PlaylistMode mode, TrackOrder trackOrder = TrackOrder.None)
        {
            if (playlistViewModels == null)
                return;
            IList<TrackViewModel> tracks = await this.playlistService.GetTracksAsync(playlistViewModels.First());
            await this.PlayTracksAsync(tracks, mode, trackOrder);

        }

        private async Task<bool> RemovePlaylistItems(IList<int> positions)
        {

            bool bRet = false;
            //=== We need to check if we are removing the current item in order to stop playing it (if it is playing)
            bool bAreWeRemovingTheCurrentTrack = false;
            int? currentPosition = queueManager.Position;// == null ? -1 : queueManager.CurrentItem.Id;

            //=== Remove all the needed files
            await Task.Run(async () =>
            {
                if (currentPosition.HasValue)
                    bAreWeRemovingTheCurrentTrack = positions.Contains(currentPosition.Value);
                if (queueManager.Remove(positions))
                {
                    PlaylistChanged(this, new EventArgs());
                    await SavePlaylistAsync();
                    bRet = true;
                }
            });
            if (bAreWeRemovingTheCurrentTrack)
            {
                if (IsPlaying)
                    await TryPlayAsync(queueManager.CurrentItem, false, false);
                else
                    StopPlayback();
            }
            return bRet;

        }

        public async Task<bool> RemovePlaylistItems(IList<PlaylistItem> items)
        {
            if (items?.Count == 0)
                return true;
            bool bRet = false;
            //=== Remove all the needed files
            await Task.Run(async () =>
            {
                IList<int> positionsToRemove = items.Select(x => x.Position).ToList();
                bRet = await RemovePlaylistItems(positionsToRemove);
            });
            return bRet;
        }

        private class TrackViewModelEqualityComparer : IEqualityComparer<TrackViewModel>
        {
            public bool Equals(TrackViewModel x, TrackViewModel y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(TrackViewModel obj)
            {
                return (int) obj.Id;
            }
        }

        public async Task<bool> RemovePlaylistItems(IList<TrackViewModel> tracks)
        {
            await Task.Run(async () =>
            {
                IList<int> positions = new List<int>();
                int i = 0;
                TrackViewModelEqualityComparer eq = new TrackViewModelEqualityComparer();
                foreach (TrackViewModel vm in queueManager.Playlist)
                {
                    i++;
                    if (tracks.Contains(vm, eq))
                    {
                        positions.Add(i);
                    }
                }
                return await RemovePlaylistItems(positions);
            });
            return false;
        }

        public async Task<EnqueueResult> AddToQueueAsync(IList<TrackViewModel> tracks)
        {
            await Task.Run(() =>
            {
                queueManager.Enqueue(tracks);
                AddedTracksToQueue(tracks.Count);
                PlaylistChanged(this, new EventArgs());
                ResetSaveQueuedTracksTimer();
            });
            return new EnqueueResult() { EnqueuedTracks = tracks, IsSuccess = true };
        }

        public async Task<EnqueueResult> AddToQueueNextAsync(IList<TrackViewModel> tracks)
        {
            await Task.Run(() =>
            {
                queueManager.EnqueueNext(tracks);
                PlaylistChanged(this, new EventArgs());
                AddedTracksToQueue(tracks.Count);
                ResetSaveQueuedTracksTimer();
            });
            return new EnqueueResult() { EnqueuedTracks = tracks, IsSuccess = true };
        }

        public async Task<EnqueueResult> AddArtistsToQueueAsync(IList<ArtistViewModel> artists)
        {
            IList<TrackV> tracks = trackRepository.GetTracksOfArtists(artists.Select(x=>x.Id).ToList(), new QueryOptions(DataRichnessEnum.History));
            List<TrackViewModel> orederedTracks = await EntityUtils.OrderTracksAsync(await this.container.ResolveTrackViewModelsAsync(tracks), TrackOrder.ByAlbum);
            return await this.AddToQueueAsync(orederedTracks);
        }

        public async Task<EnqueueResult> AddGenresToQueueAsync(IList<GenreViewModel> genres)
        {
            IList<TrackV> tracks = trackRepository.GetTracksWithGenres(genres.Select(x => x.Id).ToList(), new QueryOptions(DataRichnessEnum.History));
            List<TrackViewModel> orederedTracks = await EntityUtils.OrderTracksAsync(await this.container.ResolveTrackViewModelsAsync(tracks), TrackOrder.ByAlbum);
            return await this.AddToQueueAsync(orederedTracks);
        }

        public async Task<EnqueueResult> AddAlbumsToQueueAsync(IList<AlbumViewModel> albums)
        {
            IList<TrackV> tracks = trackRepository.GetTracksOfAlbums(albums.Select(x => x.Id).ToList(), new QueryOptions(DataRichnessEnum.History));
            List<TrackViewModel> orederedTracks = await EntityUtils.OrderTracksAsync(await this.container.ResolveTrackViewModelsAsync(tracks), TrackOrder.ByAlbum);
            return await this.AddToQueueAsync(orederedTracks);
        }

        private async void Initialize()
        {
            // Media Foundation
            this.hasMediaFoundationSupport = MediaFoundationHelper.HasMediaFoundationSupport();

            // Settings
            this.SetPlaybackSettings();

            // PlayerFactory
            this.playerFactory = new PlayerFactory();

            // Player (default for now, can be changed later when playing a file)
            this.player = this.playerFactory.Create(this.hasMediaFoundationSupport);

            // Audio device
            await this.SetAudioDeviceAsync();

            // Equalizer
            await this.SetIsEqualizerEnabledAsync(SettingsClient.Get<bool>("Equalizer", "IsEnabled"));

            // Queued tracks
            this.LoadPlaylistAsync();
        }



        private async Task PauseAsync(bool isSilent = false)
        {
            try
            {
                if (this.player != null)
                {
                    await Task.Run(() => this.player.Pause());
                    this.PlaybackPaused(this, new PlaybackPausedEventArgs() { IsSilent = isSilent });
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not pause track with path='{0}'. Exception: {1}", this.CurrentTrack.Path, ex.Message);
            }
        }

        private async Task ResumeAsync()
        {
            try
            {
                if (this.player != null)
                {
                    bool isResumed = false;
                    await Task.Run(() => isResumed = this.player.Resume());

                    if (isResumed)
                    {
                        this.PlaybackResumed(this, new EventArgs());
                    }
                    else
                    {
                        this.PlaybackStopped(this, new EventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not resume track with path='{0}'. Exception: {1}", this.CurrentTrack.Path, ex.Message);
            }
        }

        private void StopPlayback()
        {
            if (this.player != null)
            {
                this.PlaybackStopped(this, new EventArgs());
                // Remove the previous Stopped handler (not sure this is needed)
                this.player.PlaybackInterrupted -= this.PlaybackInterruptedHandler;
                this.player.PlaybackFinished -= this.PlaybackFinishedHandler;

                this.player.Stop();
                this.player.Dispose();
                this.player = null;
            }
        }

        private async Task StartPlaybackAsync(TrackViewModel track, bool bStartPaused, bool silent)
        {
            // If we start playing a track, we need to make sure that
            // queued tracks are saved when the application is closed.
            this.isQueueChanged = true;

            // Settings
            this.SetPlaybackSettings();

            // Play the Track from its runtime path (current or temporary)
            this.player = this.playerFactory.Create(this.hasMediaFoundationSupport);

            this.player.SetPlaybackSettings(this.Latency, this.EventMode, this.ExclusiveMode, this.activePreset.Bands, this.UseAllAvailableChannels);
            this.player.SetVolume(silent | this.Mute ? 0.0f : this.Volume);

            // We need to set PlayingTrack before trying to play the Track.
            // So if we go into the Catch when trying to play the Track,
            // at least, the next time TryPlayNext is called, it will know that 
            // we already tried to play this track and it can find the next Track.
            //this.queueManager.SetCurrentTrack(track.Path);
            //queueManager.Play(new List<TrackViewModel>() { track });

            // Play the Track
            await Task.Run(() => this.player.Play(track.Path, this.audioDevice, bStartPaused));
            if (bStartPaused)
                this.PlaybackPaused(this, new PlaybackPausedEventArgs() { IsSilent = false });

            // Start reporting progress
            this.progressTimer.Start();

            // Hook up the Stopped event
            this.player.PlaybackInterrupted += this.PlaybackInterruptedHandler;
            this.player.PlaybackFinished += this.PlaybackFinishedHandler;
        }


        /*
        private async Task LogTrackHistoryAsync(string reason)
        {
            if (this.HasCurrentTrack)
            {
                try
                {
                    double currentTime = this.GetCurrentTime.TotalSeconds;
                    double totalTime = this.GetTotalTime.TotalSeconds;
                    double percentage = 100 * currentTime / totalTime;
                    await Task.Run(() =>
                    {
                        if (currentTime < 5)
                        {
                            // Do not log anything
                        }
                        if (percentage > 95)
                        {
                            trackHistoryRepository.AddSkippedAction(CurrentTrack.Id, (long)currentTime, (long)percentage, reason);
                        }
                        else
                        {
                            trackHistoryRepository.AddPlayedAction(CurrentTrack.Id);
                        }
                    });

                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not get time information for Track with path='{0}'. Exception: {1}", this.CurrentTrack.Path, ex.Message);
                }
            }
        }
        */

        private async Task<bool> TryPlayAsync(TrackViewModel track, bool bStartPaused, bool isSilent)
        {
            if (track == null)
            {
                // Should happen if
                //  1. We clear the playlist
                //  2. Delete the last track from the playlist
                //  3. When we first start the application
                Stop();
                return true;
            }

            if (this.isLoadingTrack)
            {
                // Only load 1 track at a time (just in case)
                return true;
            }

            this.OnLoadingTrack(true);
            bool isPlaybackSuccess = true;
            PlaybackFailedEventArgs playbackFailedEventArgs = null;

            try
            {
                // If a Track was playing, make sure it is now stopped.
                this.StopPlayback();

                // Check that the file exists
                if (!System.IO.File.Exists(track.Path))
                {
                    throw new FileNotFoundException(string.Format("File '{0}' was not found", track.Path));
                }

                // Start playing
                await this.StartPlaybackAsync(track, bStartPaused, isSilent);

                // Playing was successful
                this.PlaybackSuccess(this, new PlaybackSuccessEventArgs()
                {
                    IsPlayingPreviousTrack = this.isPlayingPreviousTrack,
                    IsSilent = isSilent
                });

                // Set this to false again after raising the event. It is important to have a correct slide 
                // direction for cover art when the next Track is a file from double click in Windows.
                this.isPlayingPreviousTrack = false;
                Logger.Info("Playing the file {0}. EventMode={1}, ExclusiveMode={2}, LoopMode={3}, Shuffle={4}", track.Path, this.EventMode, this.ExclusiveMode, this.LoopMode, queueManager.Shuffle);
            }
            catch (FileNotFoundException fnfex)
            {
                playbackFailedEventArgs = new PlaybackFailedEventArgs { FailureReason = PlaybackFailureReason.FileNotFound, Message = fnfex.Message, StackTrace = fnfex.StackTrace };
                isPlaybackSuccess = false;
            }
            catch (Exception ex)
            {
                playbackFailedEventArgs = new PlaybackFailedEventArgs { FailureReason = PlaybackFailureReason.Unknown, Message = ex.Message, StackTrace = ex.StackTrace };
                isPlaybackSuccess = false;
            }

            if (!isPlaybackSuccess)
            {
                try
                {
                    if (this.player != null)
                    {
                        this.player.Stop();
                    }
                }
                catch (Exception)
                {
                    LogClient.Error("Could not stop the Player");
                }

                LogClient.Error("Could not play the file {0}. EventMode={1}, ExclusiveMode={2}, LoopMode={3}, Shuffle={4}. Exception: {5}. StackTrace: {6}", track.Path, this.EventMode, this.ExclusiveMode, this.LoopMode, this.Shuffle, playbackFailedEventArgs.Message, playbackFailedEventArgs.StackTrace);

                this.PlaybackFailed(this, playbackFailedEventArgs);
            }

            this.OnLoadingTrack(false);

            return isPlaybackSuccess;
        }

        private void OnLoadingTrack(bool isLoadingTrack)
        {
            this.isLoadingTrack = isLoadingTrack;
            this.LoadingTrack(isLoadingTrack);
        }

        private async Task<bool> TryPlayPreviousAsync(bool ignoreLoopOne)
        {
            this.isPlayingPreviousTrack = true;

            if (this.GetCurrentTime.Seconds > 3)
            {
                // If we're more than 3 seconds into the Track, try to
                // jump to the beginning of the current Track.
                Logger.Info("TryPlayPreviousAsync. We're more than 3 seconds into the Track. We will jump to the beginning of the current Track.");
                this.player.Skip(0);
                return true;
            }

            // When "loop one" is enabled and ignoreLoopOne is true, act like "loop all".
            LoopMode loopMode = this.LoopMode == LoopMode.One && ignoreLoopOne ? LoopMode.All : this.LoopMode;
            if (loopMode == LoopMode.One)
                return await this.TryPlayAsync(queueManager.CurrentItem, false, false);

            if (!queueManager.Prev())
            {
                //this.Stop();
                return true;
            }
            PlaylistPositionChanged(this, new EventArgs());
            return await this.TryPlayAsync(queueManager.CurrentItem, false, false);
        }

        private async Task<bool> TryPlayNextAsync(bool ignoreLoopOne)
        {
            this.isPlayingPreviousTrack = false;

            // When "loop one" is enabled and ignoreLoopOne is true, act like "loop all".
            LoopMode loopMode = this.LoopMode == LoopMode.One && ignoreLoopOne ? LoopMode.All : this.LoopMode;
            if (loopMode == LoopMode.One)
                return await this.TryPlayAsync(queueManager.CurrentItem, false, false);

            //TrackViewModel nextTrack = await this.queueManager.NextTrackAsync(loopMode, returnToStart);

            if (!queueManager.Next())
            {
                //this.Stop();
                if (loopMode == LoopMode.AutoPlay)
                {
                    // Select a new track
                    TrackV track = null;
                    await Task.Run(() =>
                    {
                        track = trackRepository.SelectAutoPlayTrack(CurrentTrack?.Data);
                    });
                    if (track == null)
                        return false;
                    // Add it to the list as the last one
                    await PlayTracksAsync(new List<TrackViewModel>() { new TrackViewModel(container.Resolve<IMetadataService>(), container.Resolve<IScrobblingService>(), container.Resolve<IAlbumVRepository>(), container.Resolve<IIndexingService>(), track) }, PlaylistMode.Enqueue, TrackOrder.None);
                    if (!queueManager.Next())
                        return false;// On failed
                    // play it
                }
                else
                {
                    bool returnToStart = SettingsClient.Get<bool>("Playback", "LoopWhenShuffle") & queueManager.Shuffle;
                    if (returnToStart)
                    {
                        await SetPlaylistPositionAsync(0, true);
                        //await PauseAsync();
                    }
                    Logger.Warn("ALEX TODO. Make it send an event that we found the end of the playlist");
                    return true;
                }
            }
            PlaylistPositionChanged(this, new EventArgs());
            return await this.TryPlayAsync(queueManager.CurrentItem, false, false);
        }

        private void ProgressTimeoutHandler(object sender, ElapsedEventArgs e)
        {
            this.HandleProgress();
        }

        private void PlaybackInterruptedHandler(Object sender, PlaybackInterruptedEventArgs e)
        {
            // Playback was interrupted for some reason. Make sure we are in a correct state.
            // Use our context to trigger the work, because this event is fired on the Player's Playback thread.
            this.context.Post(new SendOrPostCallback((state) =>
            {
                LogClient.Info("Track interrupted: {0}", this.CurrentTrack.Path);
                this.Stop();
            }), null);
        }

        private void PlaybackFinishedHandler(Object sender, EventArgs e)
        {
            // Try to play the next Track from the list automatically
            // Use our context to trigger the work, because this event is fired on the Player's Playback thread.
            this.context.Post(new SendOrPostCallback(async (state) =>
            {
                LogClient.Info("Track finished: {0}", this.CurrentTrack.Path);
                //await this.UpdatePlaybackCountersAsync(this.CurrentTrack.Path, true, false); // Increase PlayCount
                trackHistoryRepository.AddPlayedAction(CurrentTrack.Id);
                await this.TryPlayNextAsync(false);
            }), null);
        }

        private async void SaveQueuedTracksTimeoutHandler(object sender, ElapsedEventArgs e)
        {
            await this.SavePlaylistAsync();
        }

        private async void LoadPlaylistAsync()
        {
            if (!this.canGetSavedQueuedTracks)
            {
                LogClient.Info("Aborting getting of saved queued tracks");
                return;
            }

            try
            {
                Logger.Info("Getting saved queued tracks");
                IList<TrackV> existingTracks = trackRepository.GetPlaylistTracks();
                if (existingTracks == null)
                    return;
                int playListPosition = int.Parse(generalRepository.GetValue(GeneralRepositoryKeys.PlayListPosition, "-1"));
                IList<TrackViewModel> existingTrackViewModels = await this.container.ResolveTrackViewModelsAsync(existingTracks);
                
                await this.PlayTracksAsync(existingTrackViewModels, PlaylistMode.Enqueue, TrackOrder.None);
                if (playListPosition >= 0 && playListPosition < existingTrackViewModels.Count)
                {
                    queueManager.Position = playListPosition;
                }

                if (!SettingsClient.Get<bool>("Startup", "RememberLastPlayedTrack"))
                {
                    return;
                }

                if (!this.canGetSavedQueuedTracks)
                {
                    LogClient.Info("Aborting getting of saved queued tracks");
                    return;
                }


                TrackViewModel playingTrackViewModel = queueManager.CurrentItem;

                if (playingTrackViewModel == null)
                {
                    return;
                }

                int progressSeconds = int.Parse(generalRepository.GetValue(GeneralRepositoryKeys.PlayListPositionInTrack));



                try
                {
                    Logger.Info("Starting track {0} paused", playingTrackViewModel.Path);
                    await this.StartTrackPausedAsync(playingTrackViewModel, progressSeconds);
                }
                catch (Exception ex)
                {
                    Logger.Error("Could not set the playing track. Exception: {0}", ex.Message);
                    this.Stop(); // Should not be required, but just in case.
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not get saved queued tracks. Exception: {0}", ex.Message);
            }
        }

        private async Task StartTrackPausedAsync(TrackViewModel track, int progressSeconds)
        {
            if (await this.TryPlayAsync(track, true, false))
            {
                //await this.PauseAsync(true);
                this.player.Skip(progressSeconds);
                //await Task.Delay(200); // Small delay before unmuting

                if (!this.mute)
                {
                    this.player.SetVolume(this.Volume);
                }

                PlaybackProgressChanged(this, new EventArgs());
            }
        }

        private void HandleProgress()
        {
            if (this.player != null && this.player.CanStop)
            {
                TimeSpan totalTime = this.player.GetTotalTime();
                TimeSpan currentTime = this.player.GetCurrentTime();

                this.Progress = currentTime.TotalMilliseconds / totalTime.TotalMilliseconds;
            }
            else
            {
                this.Progress = 0.0;
            }

            PlaybackProgressChanged(this, new EventArgs());
        }

        public async Task PlayTracksAsync(IList<TrackViewModel> tracks, PlaylistMode mode, TrackOrder trackOrder)
        {
            if (mode == PlaylistMode.Shuffle)
            {
                tracks = await EntityUtils.OrderTracksAsync(tracks, TrackOrder.Random);
                mode = PlaylistMode.Play;
            }
            else if (trackOrder != TrackOrder.None)
                tracks = await EntityUtils.OrderTracksAsync(tracks, trackOrder);
            await Task.Run(() =>
            {
                switch (mode)
                {
                    case PlaylistMode.Play:
                        queueManager.Play(tracks);
                        break;
                    case PlaylistMode.Enqueue:
                        queueManager.Enqueue(tracks);
                        break;
                    case PlaylistMode.EnqueuNext:
                        queueManager.EnqueueNext(tracks);
                        break;
                }
            });
            if (mode == PlaylistMode.Play)
                await TryPlayAsync(queueManager.CurrentItem, false, false);
            this.PlaylistChanged(this, new EventArgs());
            this.ResetSaveQueuedTracksTimer(); // Save queued tracks to the database
        }

        public async Task RandomizePlaylistAsync()
        {
            await Task.Run(() =>
            {
                queueManager.Randomize();
                PlaylistChanged(this, new EventArgs());
            });
            await SetPlaylistPositionAsync(0, false);
        }

        private void ResetSaveQueuedTracksTimer()
        {
            this.saveQueuedTracksTimer.Stop();
            this.isQueueChanged = true;
            this.saveQueuedTracksTimer.Start();
        }


        private void SetPlaybackSettings()
        {
            this.isLoadingSettings = true;
            this.UseAllAvailableChannels = SettingsClient.Get<bool>("Playback", "WasapiUseAllAvailableChannels");
            this.LoopMode = (LoopMode)SettingsClient.Get<int>("Playback", "LoopMode");
            this.Latency = SettingsClient.Get<int>("Playback", "AudioLatency");
            this.Volume = SettingsClient.Get<float>("Playback", "Volume");
            this.mute = SettingsClient.Get<bool>("Playback", "Mute");
            queueManager.Shuffle = SettingsClient.Get<bool>("Playback", "Shuffle");
            this.EventMode = false;
            //this.EventMode = SettingsClient.Get<bool>("Playback", "WasapiEventMode");
            //this.ExclusiveMode = false;
            this.ExclusiveMode = SettingsClient.Get<bool>("Playback", "WasapiExclusiveMode");
            this.isLoadingSettings = false;
        }

        private async Task SetAudioDeviceAsync()
        {
            Logger.Trace("SetAudioDeviceAsync");
            this.audioDevice = await this.GetSavedAudioDeviceAsync();
            Logger.Trace("SetAudioDeviceAsync (END)");
        }

        public void ClearPlaylist()
        {
            Stop();
            queueManager.Clear();
            PlaylistChanged(this, new EventArgs());
        }
    }
}
