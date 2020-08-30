using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.UnitOfWorks
{
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
        public string AlbumImage { get; set; }
        public string Lyrics { get; set; }

    }
    public interface IUpdateCollectionUnitOfWork: IDisposable
    {
        bool AddMediaFile(MediaFileData mediaFileData, long folderId);
        bool UpdateMediaFile(TrackV trackV, MediaFileData mediaFileData);
        TrackV GetTrackWithPath(string path);
    }
}
