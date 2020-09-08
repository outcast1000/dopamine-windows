using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.UnitOfWorks
{

    public class MediaFileDataImage
    {
        public string Path { get; set; }
        public string Source { get; set; }
    }

    public class MediaFileDataText
    {
        public string Text { get; set; }
        public string Source { get; set; }
        public string Language { get; set; }
    }
    public class MediaFileData
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long? Filesize { get; set; }
        public long? Bitrate { get; set; }
        public long? Samplerate { get; set; }
        public long? Duration { get; set; }
        public long? Year { get; set; }
        public string Language { get; set; }
        public long DateAdded { get; set; }
        public long? DateFileDeleted { get; set; }
        public long? DateFileCreated { get; set; }
        public long? DateFileModified { get; set; }
        public long? Rating { get; set; }
        public long? Love { get; set; }
        public long? DateIgnored { get; set; }
        public IList<string> Artists { get; set; }
        public IList<string> AlbumArtists { get; set; }
        public IList<string> Genres { get; set; }
        public string Album { get; set; }
        public long? TrackNumber { get; set; }
        public long? TrackCount { get; set; }
        public long? DiscNumber { get; set; }
        public long? DiscCount { get; set; }
        //public IList<MediaFileDataImage> AlbumImages { get; set; }
        public MediaFileDataText Lyrics { get; set; }

    }

    public class AddMediaFileResult
    {
        public bool Success { get; set; }
        public long? TrackId { get; set; }
        public long? AlbumId { get; set; }
    }

    public class UpdateMediaFileResult
    {
        public bool Success { get; set; }
        public long? AlbumId { get; set; }
    }


    public interface IUpdateCollectionUnitOfWork: IDisposable
    {

        AddMediaFileResult AddMediaFile(MediaFileData mediaFileData, long folderId);
        UpdateMediaFileResult UpdateMediaFile(TrackV trackV, MediaFileData mediaFileData);
        TrackV GetTrackWithPath(string path);

        // Adds an album images
        // params
        //      album_id
        //      images
        //      bIsPrimary: If another images is primary it will be deleted. There should be only one primary image
        // return false on error
        bool AddAlbumImage(AlbumImage image);

        bool RemoveAlbumImage(long album_id, string location);
        bool RemoveAllAlbumImages(long album_id);
        bool SetAlbumImageAsPrimary(long album_image_id, bool bIsPrimary);


        bool AddArtistImage(ArtistImage image);

        bool RemoveArtistImage(long artist_id, string location);
        bool RemoveAllArtistImages(long artist_id);
        bool SetArtistImageAsPrimary(long artist_image_id, bool bIsPrimary);
    }
}
