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

namespace Dopamine.Data.Repositories
{
    public class SQLiteTrackVRepository : ITrackVRepository
    {
        private ISQLiteConnectionFactory factory;
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
            return GetTracksInternal("", options);
        }

        public List<TrackV> GetTracksOfFolders(IList<long> folderIds, QueryOptions options = null)
        {
            return GetTracksInternal("Folders.id in (" + string.Join(",", folderIds) + ")", options);
        }

        private List<TrackV> GetTracksInternal(string whereClause, QueryOptions queryOptions = null)
        {
            if (connection != null)
                return GetTracksInternal(connection, whereClause, queryOptions);
            try
            {
                using (var conn = factory.GetConnection())
                {
                    return GetTracksInternal(conn, whereClause, queryOptions);
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
            }

            return null;
        }

        private List<TrackV> GetTracksInternal(SQLiteConnection connection, string whereClause, QueryOptions queryOptions = null)
        {
            try
            {
                string sql = RepositoryCommon.CreateSQL(GetSQLTemplate(), "", whereClause, "", queryOptions);
                return connection.Query<TrackV>(sql);
            }
            catch (Exception ex)
            {
                LogClient.Error("Query Failed. Exception: {0}", ex.Message);
            }
            return null;
        }

        private TrackV GetTrackInternal(string whereClause, QueryOptions queryOptions = null)
        {
            if (connection != null)
                return GetTrackInternal(connection, whereClause, queryOptions);
            try
            {
                using (var conn = factory.GetConnection())
                {
                    return GetTrackInternal(conn, whereClause, queryOptions);
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
            }
            return null;
        }

        private TrackV GetTrackInternal(SQLiteConnection connection, string whereClause, QueryOptions queryOptions = null)
        {
            try
            {
                string sql = RepositoryCommon.CreateSQL(GetSQLTemplate(), "", whereClause, "", queryOptions);
                return connection.FindWithQuery<TrackV>(sql);
            }
            catch (Exception ex)
            {
                LogClient.Error("Query Failed. Exception: {0}", ex.Message);
            }
            return null;
        }

        private string GetSQLTemplate()
        {
            return @"SELECT DISTINCT t.id as Id, 
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
0 as PlayCount, 
0 as SkipCount, 
0 as DateLastPlayed,
t.folder_id as FolderID
FROM Tracks t
LEFT JOIN TrackArtists ON TrackArtists.track_id =t.id 
LEFT JOIN Artists ON Artists.id =TrackArtists.artist_id  
LEFT JOIN TrackAlbums ON TrackAlbums.track_id =t.id 
LEFT JOIN Albums ON Albums.id =TrackAlbums.album_id  
LEFT JOIN ArtistCollectionsArtists ON ArtistCollectionsArtists.artist_collection_id = Albums.artist_collection_id 
LEFT JOIN Artists as Artists2 ON Artists2.id = ArtistCollectionsArtists.artist_id 
LEFT JOIN TrackGenres ON TrackGenres.track_id =t.id 
LEFT JOIN Genres ON Genres.id = TrackGenres.genre_id  
INNER JOIN Folders ON Folders.id = t.folder_id
#WHERE#
GROUP BY t.id
#LIMIT#";
        }

        public List<TrackV> GetTracksBySearch(string whereClause)
        {
            return GetTracksInternal(whereClause);
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

        public async Task ClearRemovedTrackAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    try
                    {
                        using (var conn = this.factory.GetConnection())
                        {
                            conn.Execute("DELETE FROM RemovedTrack;");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("Could not clear removed tracks. Exception: {0}", ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });
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
  
        public async Task<TrackV> GetLastModifiedTrackForAlbumKeyAsync(AlbumV album)
        {
            TrackV lastModifiedTrack = null;

            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            Debug.Assert(false, "TODO");
                            //lastModifiedTrack = conn.Table<TrackV>().Where((t) => t.Equals(albumKey)).Select((t) => t).OrderByDescending((t) => t.DateFileModified).FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not get the last modified track for the given albumKey. Exception: {0}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });

            return lastModifiedTrack;
        }

        public async Task DisableNeedsAlbumArtworkIndexingAsync(AlbumV album)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            Debug.Assert(false, "TODO");
                            //conn.Execute($"UPDATE Track SET NeedsAlbumArtworkIndexing=0 WHERE AlbumKey=?;", albumKey);
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not disable NeedsAlbumArtworkIndexing for the given albumKey. Exception: {0}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });
        }

        public async Task DisableNeedsAlbumArtworkIndexingForAllTracksAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            conn.Execute($"UPDATE Track SET NeedsAlbumArtworkIndexing=0;");
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not disable NeedsAlbumArtworkIndexing for all tracks. Exception: {0}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });
        }

        public async Task EnableNeedsAlbumArtworkIndexingForAllTracksAsync(bool onlyWhenHasNoCover)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            if (onlyWhenHasNoCover)
                            {
                                conn.Execute($"UPDATE Track SET NeedsAlbumArtworkIndexing=1 WHERE AlbumKey NOT IN (SELECT AlbumKey FROM AlbumArtwork);");
                            }
                            else
                            {
                                conn.Execute($"UPDATE Track SET NeedsAlbumArtworkIndexing=1;");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error($"Could not disable NeedsAlbumArtworkIndexing for all tracks. {nameof(onlyWhenHasNoCover)}={onlyWhenHasNoCover}. Exception: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });
        }

        public async Task UpdateRatingAsync(string path, int rating)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            conn.Execute("UPDATE Track SET Rating=? WHERE SafePath=?", rating, path.ToSafePath());
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not update rating for path='{0}'. Exception: {1}", ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });
        }
        public async Task UpdateLoveAsync(string path, int love)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            conn.Execute("UPDATE Track SET Love=? WHERE SafePath=?", love, path.ToSafePath());
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not update love for path='{0}'. Exception: {1}", path, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });
        }

        public async Task UpdatePlaybackCountersAsync(PlaybackCounter counters)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            conn.Execute("UPDATE Track SET PlayCount=?, SkipCount=?, DateLastPlayed=? WHERE SafePath=?", counters.PlayCount, counters.SkipCount, counters.DateLastPlayed, counters.Path.ToSafePath());
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not update statistics for path='{0}'. Exception: {1}", counters.Path, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });
        }

        public async Task<PlaybackCounter> GetPlaybackCountersAsync(string path)
        {
            PlaybackCounter counters = null;

            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            counters = conn.Query<PlaybackCounter>("SELECT Path, SafePath, PlayCount, SkipCount, DateLastPlayed FROM Track WHERE SafePath=?", path.ToSafePath()).FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not get PlaybackCounters for path='{0}'. Exception: {1}", path, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });

            return counters;
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

        public List<TrackV> GetTracksOfArtists(IList<long> artistIds)
        {
            return GetTracksInternal("Artists.id in (" + string.Join(",", artistIds) + ")");
        }

        public List<TrackV> GetTracksOfAlbums(IList<long> albumIds)
        {
            return GetTracksInternal("Albums.id in (" + string.Join(",", albumIds) + ")");
        }

        public List<TrackV> GetTracksWithGenres(IList<long> genreIds)
        {
            return GetTracksInternal("Genres.id in (" + string.Join(",", genreIds) + ")");
        }

        public List<TrackV> GetTracksWithPaths(IList<string> paths)
        {
            return GetTracksInternal("t.path in (" + string.Join(",", paths.Select(x => String.Format("\"{0}\"", x))) + ")");
        }

        public TrackV GetTrackWithPath(string path, QueryOptions options = null)
        {
            return GetTrackInternal(String.Format("t.path='{0}'", path.Replace("'", "''")), options);
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
            throw new NotImplementedException();
        }

        public void UpdatePlaybackCounters(PlaybackCounter counters)
        {
            throw new NotImplementedException();
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
    }
}
