using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Extensions;
using Dopamine.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class SQLiteFolderVRepository : IFolderVRepository
    {
        private readonly ISQLiteConnectionFactory factory;

        public SQLiteFolderVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        public List<FolderV> GetFolders()
        {
            return GetFoldersInternal();
        }


        private List<FolderV> GetFoldersInternal()
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        return RepositoryCommon.Query<FolderV>(conn, GetSQLTemplate());
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("Query Failed. Exception: {0}", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
            }


            return null;
        }
        private string GetSQLTemplate()
        {
            return @"
SELECT
Folders.ID as Id,
Folders.path as Path,
Folders.show as Show,
Folders.date_added as DateAdded,
COUNT(DISTINCT t.id) as TrackCount,
COUNT(DISTINCT Genres.id) as GenreCount,
COUNT(DISTINCT Albums.id) as AlbumCount,
COUNT(DISTINCT Artists.id) as ArtistsCount,
MIN(t.year) as MinYear,
MAX(t.year) as MaxYear,
SUM(t.duration) as TotalDuration,
SUM(t.fileSize) as TotalFileSize,
AVG(t.bitrate) as AverageBitrate
FROM Folders
LEFT JOIN Tracks t ON Folders.id = t.folder_id
LEFT JOIN TrackArtists  ON TrackArtists.track_id = t.id
LEFT JOIN Artists ON Artists.id = TrackArtists.artist_id
LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
LEFT JOIN Albums ON Albums.id = TrackAlbums.album_id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
LEFT JOIN Genres ON TrackGenres.genre_id = Genres.id
GROUP BY Folders.id
";
        }


    }
}
