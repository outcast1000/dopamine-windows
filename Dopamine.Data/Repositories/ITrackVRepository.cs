using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface ITrackVRepository
    {
        List<TrackV> GetTracksByArtistId(long artistId);

        List<TrackV> GetTracksByAlbumId(long albumId);

        List<TrackV> GetTracksByGenreId(long genreId);

        List<TrackV> GetTracksBySearch(string searchString);

        TrackV GetTrack(string path);

        RemoveTracksResult RemoveTracks(IList<TrackV> tracks);

        bool UpdateTrack(TrackV track);

        bool UpdateTrackFileInformation(string path);

        void ClearRemovedTrack();

        void UpdateRating(string path, int rating);

        void UpdateLove(string path, int love);

    }
}
