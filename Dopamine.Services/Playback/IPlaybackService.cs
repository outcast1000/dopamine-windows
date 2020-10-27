using Dopamine.Core.Audio;
using Dopamine.Core.Base;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Services.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Services.Playback
{
    public delegate void PlaybackFailedEventHandler(object sender, PlaybackFailedEventArgs e);
    public delegate void PlaybackSuccessEventHandler(object sender, PlaybackSuccessEventArgs e);
    public delegate void PlaybackPausedEventHandler(object sender, PlaybackPausedEventArgs e);
    //public delegate void PlaybackCountersChangedEventHandler(IList<PlaybackCounter> counters);
    public delegate void TrackHistoryChangedEventHandler(TrackViewModel track);
    public delegate void PlaybackVolumeChangedEventhandler(object sender, PlaybackVolumeChangedEventArgs e);


    public enum PlaylistMode
    {
        Play,
        Enqueue,
        EnqueuNext
    }

    public interface IPlaybackService
    {
        IPlayer Player { get; }

        TrackViewModel CurrentTrack { get; }
        int CurrentPlaylistPosition { get; }

        bool HasQueue { get; }

        bool HasCurrentTrack { get; }

        bool IsSavingQueuedTracks { get; }

        //bool IsSavingPlaybackCounters { get; }

        bool HasMediaFoundationSupport { get; }

        IList<TrackViewModel> Queue { get; }

        bool Shuffle { get; set; }

        Task SetPlaylistPositionAsync(int newPosition);

        bool Mute { get; }

        bool IsStopped { get; }

        bool IsPlaying { get; }

        TimeSpan GetCurrentTime { get; }

        TimeSpan GetTotalTime { get; }

        double Progress { get; set; }

        float Volume { get; set; }

        LoopMode LoopMode { get; set; }

        bool UseAllAvailableChannels { get; set; }

        int Latency { get; set; }

        bool EventMode { get; set; }

        bool ExclusiveMode { get; set; }

        void Stop();

        void SkipProgress(double progress);

        void SkipSeconds(int jumpSeconds);

        void SetMute(bool mute);

        Task PlayNextAsync();

        Task PlayPreviousAsync();

        Task PlayOrPauseAsync();

        Task PlayTracksAndStartOnTrack(IList<TrackViewModel> tracks, TrackViewModel track, PlaylistMode mode, bool? shuffle = null, LoopMode? loopMode = null);

        Task PlayTracksAsync(IList<TrackViewModel> tracks, PlaylistMode mode, bool? shuffle = null, LoopMode? loopMode = null);

        Task PlayAllTracksAsync(PlaylistMode mode, bool? shuffle = null, LoopMode? loopMode = null);//bool shuffle, bool unshuffle);

        Task PlayArtistsAsync(IList<ArtistViewModel> artists, PlaylistMode mode, bool? shuffle = null, LoopMode? loopMode = null);//, bool shuffle, bool unshuffle);

        Task PlayGenresAsync(IList<GenreViewModel> genres, PlaylistMode mode, bool? shuffle = null, LoopMode? loopMode = null);//, bool shuffle, bool unshuffle);

        Task PlayAlbumsAsync(IList<AlbumViewModel> albumViewModels, PlaylistMode mode, bool? shuffle = null, LoopMode? loopMode = null);//, bool shuffle, bool unshuffle);

        Task PlayPlaylistsAsync(IList<PlaylistViewModel> playlistViewModels, PlaylistMode mode, bool? shuffle = null, LoopMode? loopMode = null);//, bool shuffle, bool unshuffle);

        Task StopIfPlayingAsync(TrackViewModel track);

        Task RandomizePlaylistAsync();

        Task<bool> RemoveTracks(IList<TrackViewModel> tracks);

        Task SavePlaylistAsync();

        void ApplyPreset(EqualizerPreset preset);

        Task SetIsEqualizerEnabledAsync(bool isEnabled);

        Task UpdateQueueMetadataAsync(IList<FileMetadata> fileMetadatas);

        Task UpdateQueueOrderAsync(IList<TrackViewModel> tracks);

        Task<IList<AudioDevice>> GetAllAudioDevicesAsync();

        Task SwitchAudioDeviceAsync(AudioDevice audioDevice);

        Task<AudioDevice> GetSavedAudioDeviceAsync();

        event PlaybackSuccessEventHandler PlaybackSuccess;
        event PlaybackFailedEventHandler PlaybackFailed;
        event PlaybackPausedEventHandler PlaybackPaused;
        event EventHandler PlaybackSkipped;
        event EventHandler PlaybackStopped;
        event EventHandler PlaybackResumed;
        event EventHandler PlaybackProgressChanged;
        event PlaybackVolumeChangedEventhandler PlaybackVolumeChanged;
        event EventHandler PlaybackMuteChanged;
        event EventHandler PlaybackLoopChanged;
        event EventHandler PlaybackShuffleChanged;
        event Action<int> AddedTracksToQueue;
        //event PlaybackCountersChangedEventHandler PlaybackCountersChanged;
        event TrackHistoryChangedEventHandler TrackHistoryChanged;
        event Action<bool> LoadingTrack;
        event EventHandler PlayingTrackChanged;
        event EventHandler PlaylistChanged;
        event EventHandler PlaylistPositionChanged;

    }
}
