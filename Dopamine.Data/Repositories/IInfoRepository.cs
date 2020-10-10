using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IInfoRepository
    {
        // ALBUM
        IList<AlbumImage> GetAlbumImages();
        AlbumImage GetAlbumImage(long albumId);
        AlbumImage GetAlbumImageForTrackWithPath(string path);
        bool SetAlbumImage(AlbumImage image, bool bReplaceMode);
        bool SetAlbumImageFailed(AlbumV album);
        bool HasAlbumImageFailed(AlbumV album);
        bool ClearAlbumImageFailed(AlbumV album);
        bool SetAlbumReview(AlbumReview albumReview);

        // ARTIST
        IList<ArtistImage> GetArtistImages();
        bool SetArtistImage(ArtistImage image);
        bool RemoveArtistImage(long artist_id);
        bool SetArtistImageFailed(ArtistV artist);
        bool HasArtistImageFailed(ArtistV artist);
        bool ClearArtistImageFailed(ArtistV artist);
        bool SetArtistBiography(ArtistBiography artistBiography);

        // GENRE
        IList<GenreImage> GetGenreImages();

        // TRACK
        bool SetTrackLyrics(TrackLyrics lyrics, bool bReplaceMode);
        bool RemoveTrackLyrics(long track_id);

        // GENERAL
        IList<string> GetAllImagePaths();
    }
}
