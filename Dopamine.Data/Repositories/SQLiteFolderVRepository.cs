using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Extensions;
using Dopamine.Data.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class SQLiteFolderVRepository : IFolderVRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ISQLiteConnectionFactory factory;

        public SQLiteFolderVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        public List<FolderV> GetFolders()
        {
            return GetFoldersInternal();
        }

        public bool SetFolderIndexing(FolderIndexing folderIndexing)
        {
            Debug.Assert(folderIndexing.FolderId > 0);
            return ExecuteInternal("INSERT OR REPLACE INTO FolderIndexing (folder_id, date_indexed, max_file_date_modified, hash, total_files) VALUES (?,?,?,?,?)",
                folderIndexing.FolderId,
                folderIndexing.DateIndexed,
                folderIndexing.MaxFileDateModified,
                folderIndexing.Hash,
                folderIndexing.TotalFiles) > 0;
        }


        private int ExecuteInternal(string sql, params object[] sqlParams)
        {
            using (var conn = factory.GetConnection())
            {
                return ExecuteInternalWithConnection(conn, sql, sqlParams);
            }
        }
        private int ExecuteInternalWithConnection(SQLiteConnection connection, string sql, params object[] sqlParams)
        {
            try
            {
                return connection.Execute(sql, sqlParams);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Execute Failed {sql}: {ex.Message}");
            }
            return 0;
        }


        private List<FolderV> GetFoldersInternal()
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        QueryOptions qo = new QueryOptions();
                        qo.ResetToIncludeAll();
                        qo.GetHistory = false;
                        return RepositoryCommon.Query<FolderV>(conn, GetSQLTemplate(), qo);
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
COUNT(DISTINCT TrackGenres.genre_id) as GenreCount,
COUNT(DISTINCT TrackAlbums.album_id) as AlbumCount,
COUNT(DISTINCT TrackArtists.artist_id) as ArtistsCount,
MIN(t.year) as MinYear,
MAX(t.year) as MaxYear,
SUM(t.duration) as TotalDuration,
SUM(t.fileSize) as TotalFileSize,
AVG(t.bitrate) as AverageBitrate,
FolderIndexing.date_indexed as DateIndexed,
FolderIndexing.total_files as TotalFiles,
FolderIndexing.max_file_date_modified as MaxFileDateModified,
FolderIndexing.hash as Hash
FROM Folders
LEFT JOIN Tracks t ON Folders.id = t.folder_id
LEFT JOIN TrackArtists  ON TrackArtists.track_id = t.id
LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
LEFT JOIN FolderIndexing ON FolderIndexing.folder_id=Folders.id
GROUP BY Folders.id
";
        }


    }
}
