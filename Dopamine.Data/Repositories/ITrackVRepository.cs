using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface ITrackVRepository
    {
        List<TrackV> GetTracks();

        List<TrackV> GetTracksOfArtists(IList<long> artistIds);

        List<TrackV> GetTracksOfAlbums(IList<long> albumIds);

        List<TrackV> GetTracksWithGenres(IList<long> genreIds);

        List<TrackV> GetTracksWithPaths(IList<string> paths);

        TrackV GetTrackWithPath(string path);

        List<TrackV> GetTracksBySearch(string searchString);

        TrackV GetTrack(string path);

        RemoveTracksResult RemoveTracks(IList<long> tracksIds);

        bool UpdateTrack(long trackId);

        bool UpdateTrackFileInformation(string path);

        void ClearRemovedTrack();

        void UpdateRating(string path, int rating);

        void UpdateLove(string path, int love);

        PlaybackCounter GetPlaybackCounters(string path);

        void UpdatePlaybackCounters(PlaybackCounter counters);

    }
}
