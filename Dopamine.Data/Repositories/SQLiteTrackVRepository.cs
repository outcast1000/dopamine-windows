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

namespace Dopamine.Data.Repositories
{
    public class SQLiteTrackVRepository : ITrackVRepository
    {
        private ISQLiteConnectionFactory factory;

        public SQLiteTrackVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        public List<TrackV> GetTracks()
        {
            return GetTracksInternal();
        }

        private List<TrackV> GetTracksInternal(string whereClause = "")
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        string sql = GetSQL();
                        sql = sql.Replace("#WHERE#", whereClause);
                        return conn.Query<TrackV>(sql);
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

        private string GetSQL()
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
0 as TrackCount, 
TrackAlbums.disc_number as DiscNumber,
0 as DiscCount, 
t.duration as Duration, 
t.year as Year, 
0 as HasLyrics, 
t.date_added as DateAdded, 
0 as DateFileCreated,
0 as DateLastSynced, 
0 as DateFileModified, 
TrackIndexing.needs_indexing as NeedsIndexing, 
TrackIndexing.needs_album_artwork_indexing as NeedsAlbumArtworkIndexing, 
TrackIndexing.indexing_success as IndexingSuccess,
TrackIndexing.indexing_failure_reason as IndexingFailureReason, 
t.rating as Rating, 
t.love as Love, 
0 as PlayCount, 
0 as SkipCount, 
0 as DateLastPlayed
FROM Tracks t
LEFT JOIN TrackArtists ON TrackArtists.track_id =t.id 
LEFT JOIN Artists ON Artists.id =TrackArtists.artist_id  
LEFT JOIN TrackAlbums ON TrackAlbums.track_id =t.id 
LEFT JOIN Albums ON Albums.id =TrackAlbums.album_id  
LEFT JOIN ArtistCollectionsArtists ON ArtistCollectionsArtists.artist_collection_id = Albums.artist_collection_id 
LEFT JOIN Artists as Artists2 ON Artists2.id = ArtistCollectionsArtists.artist_id 
LEFT JOIN TrackGenres ON TrackGenres.track_id =t.id 
LEFT JOIN Genres ON Genres.id =TrackGenres.genre_id  
INNER JOIN Folders ON Folders.id = t.folder_id
LEFT JOIN TrackIndexing ON TrackIndexing.track_id=t.id
WHERE Folders.show = 1 AND TrackIndexing.indexing_success is null AND TrackIndexing.needs_indexing is null #WHERE#
GROUP BY t.id";
        }

        public List<TrackV> GetTracksBySearch(string whereClause)
        {
            return GetTracksInternal(whereClause);
        }

 

        public TrackV GetTrack(string path)
        {
            TrackV track = null;

            try
            {
                using (var conn = this.factory.GetConnection())
                {
                    try
                    {
                        track = conn.Query<TrackV>("SELECT * FROM Track WHERE SafePath=?", path.ToSafePath()).FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("Could not get the Track with Path='{0}'. Exception: {1}", path, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
            }

            return track;
        }

        public async Task<TrackV> GetTrackAsync(string path)
        {
            TrackV track = null;

            await Task.Run(() =>
            {
                track = this.GetTrack(path);
            });

            return track;
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

        public async Task<bool> UpdateTrackAsync(TrackV track)
        {
            bool isUpdateSuccess = false;

            await Task.Run(() =>
            {
                try
                {
                    using (var conn = this.factory.GetConnection())
                    {
                        try
                        {
                            conn.Update(track);

                            isUpdateSuccess = true;
                        }
                        catch (Exception ex)
                        {
                            LogClient.Error("Could not update the Track with path='{0}'. Exception: {1}", track.Path, ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not connect to the database. Exception: {0}", ex.Message);
                }
            });

            return isUpdateSuccess;
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
            return GetTracksInternal("AND Artists.id in (" + string.Join(",", artistIds) + ")");
        }

        public List<TrackV> GetTracksOfAlbums(IList<long> albumIds)
        {
            return GetTracksInternal("AND Albums.id in (" + string.Join(",", albumIds) + ")");
        }

        public List<TrackV> GetTracksWithGenres(IList<long> genreIds)
        {
            return GetTracksInternal("AND Genres.id in (" + string.Join(",", genreIds) + ")");
        }

        public List<TrackV> GetTracksWithPaths(IList<string> paths)
        {
            return GetTracksInternal("AND t.path in (" + string.Join(",", paths.Select(x => String.Format("\"{0}\"", x))) + ")");
        }

        public TrackV GetTrackWithPath(string path)
        {
            throw new NotImplementedException();
        }

        public bool UpdateTrack(long trackId)
        {
            throw new NotImplementedException();
        }

        public PlaybackCounter GetPlaybackCounters(string path)
        {
            throw new NotImplementedException();
        }

        public void UpdatePlaybackCounters(PlaybackCounter counters)
        {
            throw new NotImplementedException();
        }
    }
}
