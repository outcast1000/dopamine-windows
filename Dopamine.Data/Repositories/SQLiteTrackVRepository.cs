using Digimezzo.Foundation.Core.Utils;
using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Dopamine.Core.Alex;
using SQLite;
using Dopamine.Data.UnitOfWorks;

namespace Dopamine.Data.Repositories
{
    public class SQLiteTrackVRepository : ITrackVRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ISQLiteConnectionFactory factory;
        private SQLiteConnection connection;

        public SQLiteTrackVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }
        public void SetSQLiteConnection(SQLiteConnection connection)
        {
            this.connection = connection;
        }

        public List<TrackV> GetTracks(QueryOptions options = null)
        {
            return GetTracksInternal(options);
        }

        public List<TrackV> GetTracksWithText(string text)
        {

            QueryOptions qo = new QueryOptions();
            if (!string.IsNullOrEmpty(text))
            {
                qo.extraWhereClause.Add("(t.Name like ? OR Artists.Name like ? OR Albums.Name like ?)");
                qo.extraWhereParams.Add("%" + text + "%");
                qo.extraWhereParams.Add("%" + text + "%");
                qo.extraWhereParams.Add("%" + text + "%");
            }
            return GetTracksInternal(qo);
        }

        public List<TrackV> GetTracksOfFolders(IList<long> folderIds, QueryOptions options = null)
        {
            if (options == null)
                options = new QueryOptions();
            options.extraWhereClause.Add("Folders.id in (" + string.Join(",", folderIds) + ")");
            return GetTracksInternal(options);
        }

        private List<TrackV> GetTracksInternal(QueryOptions queryOptions = null)
        {
            if (connection != null)
                return GetTracksInternal(connection, queryOptions);
            try
            {
                using (var conn = factory.GetConnection())
                {
                    return GetTracksInternal(conn, queryOptions);
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
            }

            return null;
        }

        private List<TrackV> GetTracksInternal(SQLiteConnection connection, QueryOptions queryOptions = null)
        {
            try
            {
                return RepositoryCommon.Query<TrackV>(connection, GetSQLTemplate(), queryOptions);
            }
            catch (Exception ex)
            {
                LogClient.Error("Query Failed. Exception: {0}", ex.Message);
            }
            return null;
        }

        private string GetSQLTemplate()
        {
            return @"
SELECT DISTINCT t.id as Id, 
GROUP_CONCAT(DISTINCT Artists.name) as Artists, 
GROUP_CONCAT(DISTINCT Genres.name) as Genres, 
GROUP_CONCAT(DISTINCT Albums.name) as AlbumTitle, 
GROUP_CONCAT(DISTINCT Artists2.name) as AlbumArtists, 
t.path as Path, 
t.filesize as FileSize, 
t.bitrate as BitRate, 
t.samplerate as SampleRate, 
t.name as TrackTitle, 
TrackAlbums.track_number as TrackNumber, 
TrackAlbums.disc_number as DiscCount, 
TrackAlbums.track_count as TrackCount, 
TrackAlbums.disc_count as DiscCount, 
t.duration as Duration, 
t.year as Year, 
0 as HasLyrics, 
t.date_added as DateAdded, 
t.date_ignored as DateIgnored, 
t.date_file_created as DateFileCreated,
t.date_file_modified as DateFileModified, 
t.date_file_deleted as DateFileDeleted, 
t.rating as Rating, 
t.love as Love, 
t.folder_id as FolderID,
COALESCE(MAX(AlbumImages.location), ArtistImages.location) as Thumbnail,
AlbumImages.location as Thumbnail #SELECT#
FROM Tracks t
LEFT JOIN TrackArtists ON TrackArtists.track_id =t.id 
LEFT JOIN Artists ON Artists.id =TrackArtists.artist_id  
LEFT JOIN TrackAlbums ON TrackAlbums.track_id =t.id 
LEFT JOIN Albums ON Albums.id =TrackAlbums.album_id  
LEFT JOIN ArtistCollectionsArtists ON ArtistCollectionsArtists.artist_collection_id = Albums.artist_collection_id 
LEFT JOIN Artists as Artists2 ON Artists2.id = ArtistCollectionsArtists.artist_id 
LEFT JOIN TrackGenres ON TrackGenres.track_id =t.id 
LEFT JOIN Genres ON Genres.id = TrackGenres.genre_id  
LEFT JOIN Folders ON Folders.id = t.folder_id
LEFT JOIN AlbumImages ON Albums.id=AlbumImages.album_id
LEFT JOIN ArtistImages ON Artists.id=ArtistImages.artist_id #JOIN#
#WHERE#
GROUP BY t.id
#LIMIT#";
        }

        public List<TrackV> GetTracksBySearch(string whereClause, bool bGetHistory)
        {
            QueryOptions qo = new QueryOptions();
            qo.extraWhereClause.Add(whereClause);
            qo.GetHistory = bGetHistory;
            return GetTracksInternal(qo);
        }

 

        public async Task<RemoveTracksResult> RemoveTracksAsync(IList<TrackV> tracks)
        {
            RemoveTracksResult result = RemoveTracksResult.Success;

            await Task.Run(() =>
            {
                try
                {
                    try
                    {
                        using (var conn = this.factory.GetConnection())
                        {
                            IList<string> pathsToRemove = tracks.Select((t) => t.Path).ToList();

                            conn.Execute("BEGIN TRANSACTION");

                            foreach (string path in pathsToRemove)
                            {
                                // Add to table RemovedTrack, only if not already present.
                                conn.Execute("INSERT INTO RemovedTrack(DateRemoved, Path, SafePath) SELECT ?,?,? WHERE NOT EXISTS (SELECT 1 FROM RemovedTrack WHERE SafePath=?)", DateTime.Now.Ticks, path, path.ToSafePath(), path.ToSafePath());

                                // Remove from QueuedTrack
                                conn.Execute("DELETE FROM QueuedTrack WHERE SafePath=?", path.ToSafePath());

                                // Remove from Track
                                conn.Execute("DELETE FROM Track WHERE SafePath=?", path.ToSafePath());
                            }

                            conn.Execute("COMMIT");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("Could remove tracks from the database. Exception: {0}", ex.Message);
                        result = RemoveTracksResult.Error;
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                    result = RemoveTracksResult.Error;
                }
            });

            return result;
        }

        public async Task<bool> UpdateTrackFileInformationAsync(string path)
        {
            bool updateSuccess = false;

            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            Track dbTrack = conn.Query<Track>("SELECT * FROM Track WHERE SafePath=?", path.ToSafePath()).FirstOrDefault();

                            if (dbTrack != null)
                            {
                                dbTrack.FileSize = FileUtils.SizeInBytes(path);
                                dbTrack.DateFileModified = FileUtils.DateModifiedTicks(path);
                                dbTrack.DateLastSynced = DateTime.Now.Ticks;

                                conn.Update(dbTrack);

                                updateSuccess = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not update file information for Track with Path='{0}'. Exception: {1}", path, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });

            return updateSuccess;
        }
  
        public RemoveTracksResult RemoveTracks(IList<long> tracksIds)
        {
            throw new NotImplementedException();
        }

        public bool UpdateTrackFileInformation(string path)
        {
            throw new NotImplementedException();
        }

        public void ClearRemovedTrack()
        {
            throw new NotImplementedException();
        }

        public void UpdateRating(string path, int rating)
        {
            throw new NotImplementedException();
        }

        public void UpdateLove(string path, int love)
        {
            throw new NotImplementedException();
        }

        public List<TrackV> GetTracksOfArtists(IList<long> artistIds, bool bGetHistory)
        {
            QueryOptions qo = new QueryOptions();
            bool bHasNull = false;
            foreach(long artistId in artistIds)
            {
                if (artistId == 0)
                {
                    bHasNull = true;
                    break;
                }
            }
            if (bHasNull)
            {
                if (artistIds.Count == 1)
                    qo.extraWhereClause.Add("Artists.id is null");
                else
                {
                    artistIds.Remove(0);
                    qo.extraWhereClause.Add("(Artists.id is null OR Artists.id in (" + string.Join(", ", artistIds) + "))");
                }
            }
            else
            {
                qo.extraWhereClause.Add("Artists.id in (" + string.Join(",", artistIds) + ")");
            }
            qo.GetHistory = bGetHistory;
            return GetTracksInternal(qo);
        }

        public List<TrackV> GetTracksOfAlbums(IList<long> albumIds, bool bGetHistory)
        {
            QueryOptions qo = new QueryOptions();
            qo.extraWhereClause.Add("Albums.id in (" + string.Join(",", albumIds) + ")");
            qo.GetHistory = bGetHistory;
            return GetTracksInternal(qo);
        }

        public List<TrackV> GetTracksWithGenres(IList<long> genreIds, bool bGetHistory)
        {
            QueryOptions qo = new QueryOptions();
            qo.extraWhereClause.Add("Genres.id in (" + string.Join(",", genreIds) + ")");
            qo.GetHistory = bGetHistory;
            return GetTracksInternal(qo);
        }

        public List<TrackV> GetTracksWithPaths(IList<string> paths, bool bGetHistory)
        {
            if (paths.Count == 0)
                return new List<TrackV>();
            QueryOptions qo = new QueryOptions();
            /*
            string where = String.Empty;
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(where))
                    where += "t.path in (?";
                else
                    where += ",?";
                qo.extraWhereParams.Add(path);
            }
            qo.extraWhereClause.Add(where + ")");
            */
            qo.extraWhereClause.Add("t.path in (" + string.Join(",", paths.Select(x => String.Format("\"{0}\"", x))) + ")");
            qo.GetHistory = bGetHistory;
            return GetTracksInternal(qo);
            //return GetTracksInternal("t.path in (" + string.Join(",", paths.Select(x => String.Format("\"{0}\"", x))) + ")");
        }

        public TrackV GetTrackWithPath(string path, QueryOptions options = null)
        {
            if (options == null) 
                options = new QueryOptions();
            options.extraWhereClause.Add("t.path=?");
            options.extraWhereParams.Add(path);
            IList<TrackV> tracks = GetTracksInternal(options);
            if (tracks == null || tracks.Count == 0)
            {
                Logger.Warn($"GetTrackWithPath not found: {path}");
                return null;
            }
            Debug.Assert(tracks.Count == 1);
            return tracks[0];
        }


        public bool UpdateFolderIdValue(long trackId, long? newFolderId)
        {

            if (newFolderId.HasValue)
                return ExecuteSQL("UPDATE Tracks SET folder_id = ? WHERE id = ?;", newFolderId, trackId);
            return ExecuteSQL("UPDATE Tracks SET folder_id = NULL WHERE id = ?;", trackId);
        }

        public bool UpdateIgnoreValue(long trackId, bool Ignore)
        {
            if (Ignore)
                return ExecuteSQL("UPDATE Tracks SET date_ignored = ? WHERE id = ?;", DateTime.Now.Ticks, trackId);
            return ExecuteSQL("UPDATE Tracks SET date_ignored = NULL WHERE id = ?;", trackId);
        }

        public bool UpdateDeleteValue(long trackId, bool Delete)
        {
            if (Delete)
                return ExecuteSQL("UPDATE Tracks SET date_file_deleted = ? WHERE id = ?;", DateTime.Now.Ticks, trackId);
            return ExecuteSQL("UPDATE Tracks SET date_file_deleted = NULL WHERE id = ?;", trackId);
        }

        private bool ExecuteSQL(string sql, params object[] args)
        {
            try
            {
                using (var conn = this.factory.GetConnection())
                {
                    int ret = conn.Execute(sql, args);
                    return ret == 1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "ExecuteSQL Failed. Exception: {0} SQL: {1}", ex.Message, sql);
            }
            return false;
        }


        public bool UpdateTrack(TrackV track)
        {
            try
            {
                using (var conn = this.factory.GetConnection())
                {
                    int ret = conn.Update(new Track2()
                    {
                        Id = track.Id,
                        Name = track.TrackTitle,
                        Path = track.Path,
                        FolderId = track.FolderID,
                        Filesize = track.FileSize,
                        Bitrate = track.BitRate,
                        Samplerate = track.SampleRate,
                        Duration = track.Duration,
                        Year = track.Year > 0 ? track.Year : null,
                        Language = null,
                        DateAdded = track.DateAdded,
                        Rating = track.Rating,
                        Love = track.Love,
                        DateFileCreated = track.DateFileCreated,
                        DateFileModified = track.DateFileModified,
                        DateIgnored = track.DateIgnored,
                        DateFileDeleted = track.DateFileDeleted
                    });
                    return ret == 1;
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
            }
            return false ;
        }

        public PlaybackCounter GetPlaybackCounters(string path)
        {
            Logger.Warn("ALEX TODO - PlaybackCounter GetPlaybackCounters(string path)");
            return new PlaybackCounter();
            //throw new NotImplementedException();
        }

        public void UpdatePlaybackCounters(PlaybackCounter counters)
        {
            Logger.Warn("ALEX TODO - void UpdatePlaybackCounters(PlaybackCounter counters)");
            //throw new NotImplementedException();
        }


        public bool DeleteTrack(TrackV track)
        {
            track.DateFileDeleted = DateTime.UtcNow.Ticks;// DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return UpdateTrack(track);
        }

        public bool IgnoreTrack(TrackV track)
        {
            track.DateIgnored = DateTime.UtcNow.Ticks;
            return UpdateTrack(track);
        }


        //=== PLAYLIST
        public List<TrackV> GetPlaylistTracks()
        {
            QueryOptions qo = new QueryOptions();
            qo.extraWhereClause.Add("t.id in (SELECT track_id from PlaylistTracks)");
            qo.WhereVisibleFolders = QueryOptionsBool.Ignore;
            qo.WhereDeleted = QueryOptionsBool.Ignore;
            qo.WhereIgnored = QueryOptionsBool.Ignore;
            qo.GetHistory = true;
            return GetTracksInternal(qo);
        }
        public void SavePlaylistTracks(IList<TrackV> tracks)
        {
            try
            {
                using (SQLiteSavePlaylistUnitOfWork uow = new SQLiteSavePlaylistUnitOfWork(factory))
                {
                    uow.SaveTracks(tracks);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not connect to the database. Exception: {0}", ex.Message);
            }
        }
        public TrackV GetPlaylistCurrentTrack()
        {
            QueryOptions qo = new QueryOptions();
            qo.extraWhereClause.Add("t.id in (SELECT value from General WHERE key=?)");
            qo.extraWhereParams.Add(GeneralRepositoryKeys.PlayListPosition);
            qo.WhereVisibleFolders = QueryOptionsBool.Ignore;
            qo.WhereDeleted = QueryOptionsBool.Ignore;
            qo.WhereIgnored = QueryOptionsBool.Ignore;
            IList<TrackV> tracks = GetTracksInternal(qo);
            if (tracks.Count > 0)
                return tracks[0];
            return null;
        }

        //=== PLAYLIST END
    }
}
