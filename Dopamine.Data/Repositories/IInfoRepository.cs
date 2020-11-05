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
        bool RemoveAlbumImage(long album_id);

        bool SetAlbumImageFailed(AlbumV album);
        bool HasAlbumImageFailed(AlbumV album);
        bool RemoveAlbumImageFailed(AlbumV album);

        bool SetAlbumReview(AlbumReview albumReview);
        bool RemoveAlbumReview(long album_id);

        // ARTIST
        IList<ArtistImage> GetArtistImages();
        bool SetArtistImage(ArtistImage image);
        bool RemoveArtistImage(long artist_id);
        bool SetArtistImageFailed(ArtistV artist);
        bool HasArtistImageFailed(ArtistV artist);
        bool ClearArtistImageFailed(ArtistV artist);

        ArtistBiography GetArtistBiography(long artist_id);
        bool SetArtistBiography(ArtistBiography artistBiography);
        bool RemoveArtistBiography(long artist_id);

        // GENRE
        //IList<GenreImage> GetGenreImages(long artist_id);

        // TRACK
        TrackLyrics GetTrackLyrics(long track_id);
        bool SetTrackLyrics(TrackLyrics lyrics, bool bReplaceMode);
        bool RemoveTrackLyrics(long track_id);

        // GENERAL
        IList<string> GetAllImagePaths();
    }
}
