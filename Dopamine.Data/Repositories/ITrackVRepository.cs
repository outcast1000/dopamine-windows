using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface ITrackVRepository
    {

        List<TrackV> GetTracks(QueryOptions options = null);

        List<TrackV> GetTracksOfArtists(IList<long> artistIds);

        List<TrackV> GetTracksOfAlbums(IList<long> albumIds);

        List<TrackV> GetTracksWithGenres(IList<long> genreIds);

        List<TrackV> GetTracksOfFolders(IList<long> folderIds, QueryOptions options = null);

        List<TrackV> GetTracksWithPaths(IList<string> paths);

        TrackV GetTrackWithPath(string path, QueryOptions options = null);

        List<TrackV> GetTracksBySearch(string searchText);

        RemoveTracksResult RemoveTracks(IList<long> tracksIds);

        bool UpdateTrack(TrackV track);


        bool UpdateFolderIdValue(long trackId, long? newFolderId);
        bool UpdateIgnoreValue(long trackId, bool Ignore);
        bool UpdateDeleteValue(long trackId, bool Delete);



        bool UpdateTrackFileInformation(string path);

        void ClearRemovedTrack();

        void UpdateRating(string path, int rating);

        void UpdateLove(string path, int love);

        PlaybackCounter GetPlaybackCounters(string path);

        void UpdatePlaybackCounters(PlaybackCounter counters);


        //=== PLAYLIST
        List<TrackV> GetPlaylistTracks();
        void SavePlaylistTracks(IList<TrackV> tracks);
        TrackV GetPlaylistCurrentTrack();

        //=== PLAYLIST END

    }
}
