using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{

    public enum TracksHistoryLogMode
    {
        All,
        Played,
        Explicit,
        Skipped
    };

    public interface ITrackVRepository
    {

        List<TrackV> GetTracks(QueryOptions options = null);

        List<TrackV> GetTracksWithText(string text, QueryOptions options = null);

        List<TrackV> GetTracksOfArtists(IList<long> artistIds, QueryOptions options = null);

        List<TrackV> GetTracksOfAlbums(IList<long> albumIds, QueryOptions options = null);

        List<TrackV> GetTracksWithGenres(IList<long> genreIds, QueryOptions options = null);

        List<TrackV> GetTracksOfFolders(IList<long> folderIds, QueryOptions options = null);

        //List<TrackV> GetTracksWithPaths(IList<string> paths, QueryOptions options = null);

        List<TrackV> GetTracksHistoryLog(TracksHistoryLogMode tracksHistoryLogMode, string searchText);

        TrackV GetTrackWithPath(string path, QueryOptions options = null);

        List<TrackV> GetTracksWithWhereClause(string searchText, QueryOptions options = null);

        TrackV SelectAutoPlayTrack(TrackV baseTrack);

        Dictionary<long, long> GetRanking();

        RemoveTracksResult RemoveTracks(IList<long> tracksIds);

        bool UpdateTrack(TrackV track);


        bool UpdateFolderIdValue(long trackId, long? newFolderId);
        bool UpdateIgnoreValue(long trackId, bool Ignore);
        bool UpdateDeleteValue(long trackId, bool Delete);
        bool UpdateRating(long trackId, long? Rating);
        bool UpdateLove(long trackId, long? Love);
        bool UpdateLocation(long trackId, string location);


        void ClearRemovedTrack();

        PlaybackCounter GetPlaybackCounters(string path);

        void UpdatePlaybackCounters(PlaybackCounter counters);


        //=== PLAYLIST
        List<TrackV> GetPlaylistTracks();
        void SavePlaylistTracks(IList<TrackV> tracks);
        TrackV GetPlaylistCurrentTrack();

        //=== PLAYLIST END

    }
}
