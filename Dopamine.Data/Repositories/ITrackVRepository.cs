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

        List<TrackV> GetTracks(bool bGetHistory, QueryOptions options);

        List<TrackV> GetTracksWithText(string text, bool bGetHistory, QueryOptions options = null);

        List<TrackV> GetTracksOfArtists(IList<long> artistIds, bool bGetHistory);

        List<TrackV> GetTracksOfAlbums(IList<long> albumIds, bool bGetHistory);

        List<TrackV> GetTracksWithGenres(IList<long> genreIds, bool bGetHistory);

        List<TrackV> GetTracksOfFolders(IList<long> folderIds, QueryOptions options = null);

        List<TrackV> GetTracksWithPaths(IList<string> paths, bool bGetHistory);

        List<TrackV> GetTracksHistoryLog(TracksHistoryLogMode tracksHistoryLogMode, string searchText = null);

        TrackV GetTrackWithPath(string path, QueryOptions options = null);

        List<TrackV> GetTracksWithWhereClause(string searchText, bool bGetHistory);

        Dictionary<long, long> GetRanking();

        RemoveTracksResult RemoveTracks(IList<long> tracksIds);

        bool UpdateTrack(TrackV track);


        bool UpdateFolderIdValue(long trackId, long? newFolderId);
        bool UpdateIgnoreValue(long trackId, bool Ignore);
        bool UpdateDeleteValue(long trackId, bool Delete);
        bool UpdateRating(long trackId, long? Rating);
        bool UpdateLove(long trackId, long? Love);



        bool UpdateTrackFileInformation(string path);

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
