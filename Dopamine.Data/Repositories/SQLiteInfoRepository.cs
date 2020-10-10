using Digimezzo.Foundation.Core.Logging;
using Dopamine.Data.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class SQLiteInfoRepository:IInfoRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ISQLiteConnectionFactory _sQLiteConnectionFactory;
        private SQLiteConnection _SQLiteConnection;

        public SQLiteInfoRepository(ISQLiteConnectionFactory sQLiteConnectionFactory)
        {
            this._sQLiteConnectionFactory = sQLiteConnectionFactory;
        }

        public void SetSQLiteConnection(SQLiteConnection sQLiteConnection)
        {
            this._SQLiteConnection = sQLiteConnection;
        }

        public IList<AlbumImage> GetAlbumImages()
        {
            return GetInternal<AlbumImage>(@"SELECT 
                album_id, 
                location, 
                source, 
                date_added
                from AlbumImages
                ");
        }

        public AlbumImage GetAlbumImage(long albumId)
        {
            IList<AlbumImage> images = GetInternal<AlbumImage>(@"SELECT 
                album_id, 
                location, 
                source, 
                date_added
                from AlbumImages
                WHERE album_id=?
                ", albumId);
            Debug.Assert(images.Count <= 1);
            if (images.Count > 0)
                return images[0];
            return null;
        }

        public AlbumImage GetAlbumImageForTrackWithPath(string path)
        {
            IList<AlbumImage> images = GetInternal<AlbumImage>(@" 
                        SELECT
                        AlbumImages.album_id, 
                        AlbumImages.location, 
                        AlbumImages.source, 
                        AlbumImages.date_added
                        FROM AlbumImages
                        INNER JOIN TrackAlbums ON TrackAlbums.album_id = AlbumImages.album_id
                        INNER JOIN Tracks ON TrackAlbums.track_id = Tracks.id
                        WHERE Tracks.path =?", path);
            Debug.Assert(images.Count <= 1);
            if (images.Count > 0)
                return images[0];
            return null;
        }

        private IList<T> GetInternal<T>(string sql, params object[] sqlParams) where T : new()
        {
            if (_SQLiteConnection != null)
                return GetInternalWithConnection<T>(_SQLiteConnection, sql, sqlParams);
            Debug.Assert(_sQLiteConnectionFactory != null);
            using (var conn = _sQLiteConnectionFactory.GetConnection())
            {
                return GetInternalWithConnection<T>(conn, sql, sqlParams);
            }
        }

        private IList<T> GetInternalWithConnection<T>(SQLiteConnection connection, string sql, params object[] sqlParams) where T : new()
        {
            try
            {
                return connection.Query<T>(sql, sqlParams);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Query Failed {sql}");
            }
            return null;
        }

        private int ExecuteInternal(string sql, params object[] sqlParams)
        {
            if (_SQLiteConnection != null)
                return ExecuteInternalWithConnection(_SQLiteConnection, sql, sqlParams);
            Debug.Assert(_sQLiteConnectionFactory != null);
            using (var conn = _sQLiteConnectionFactory.GetConnection())
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

        public IList<ArtistImage> GetArtistImages()
        {
            return GetInternal<ArtistImage>(@"SELECT 
                artist_id, 
                location, 
                source, 
                date_added
                from ArtistImages
                ");
        }

        public IList<GenreImage> GetGenreImages()
        {
            return GetInternal<GenreImage>(@"SELECT 
                genre_id, 
                location, 
                source, 
                date_added
                from GenreImages
                ");
        }

        public IList<string> GetAllImagePaths()
        {
            string sql = @"SELECT location FROM GenreImages UNION SELECT location FROM AlbumImages UNION SELECT location FROM ArtistImages";

            if (_SQLiteConnection != null)
            {
                return _SQLiteConnection.QueryScalars<string>(sql);
            }
            Debug.Assert(_sQLiteConnectionFactory != null);
            using (var conn = _sQLiteConnectionFactory.GetConnection())
            {
                try
                {
                    return conn.QueryScalars<string>(sql);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Query Failed {sql}");
                }
            }
            return null;
        }

        public bool SetAlbumImage(AlbumImage image)
        {
            Debug.Assert(image.AlbumId > 0);
            Debug.Assert(image.Location.Length > 10);
            Logger.Debug("SetAlbumImage {0}", image.Location);
            return ExecuteInternal("INSERT OR REPLACE INTO AlbumImages (album_id, location, source, date_added) VALUES (?,?,?,?)", image.AlbumId, image.Location, image.Source, DateTime.Now.Ticks) > 0;
        }
        public bool SetArtistImage(ArtistImage image)
        {
            Debug.Assert(image.ArtistId > 0);
            Debug.Assert(image.Location.Length > 10);
            Logger.Debug("SetArtistImage {0}", image.Location);
            return ExecuteInternal("INSERT OR REPLACE INTO ArtistImages (artist_id, location, source, date_added) VALUES (?,?,?,?)", image.ArtistId, image.Location, image.Source, DateTime.Now.Ticks) > 0;
        }

        public bool SetAlbumImageFailed(AlbumV album)
        {
            return ExecuteInternal("INSERT OR REPLACE INTO AlbumImageFailed(album_id, date_added) VALUES (?,?)", album.Id, DateTime.Now.Ticks) > 0;
        }
        public bool SetArtistImageFailed(ArtistV artist)
        {
            return ExecuteInternal("INSERT OR REPLACE INTO ArtistImageFailed(artist_id, date_added) VALUES (?,?)", artist.Id, DateTime.Now.Ticks) > 0;

        }

        public bool HasArtistImageFailed(ArtistV artist)
        {
            IList<long> ids = GetInternal<long>("SELECT artist_id from ArtistImageFailed WHERE artist_id=?", artist.Id);
            return ids?.Count > 0;
        }

        public bool HasAlbumImageFailed(AlbumV album)
        {
            IList<long> ids = GetInternal<long>("SELECT album_id from AlbumImageFailed WHERE album_id=?", album.Id);
            return ids?.Count > 0;
        }


        public bool ClearAlbumImageFailed(AlbumV album)
        {
            return ExecuteInternal("DELETE FROM AlbumImageFailed WHERE album_id=?", album.Id) > 0;
        }
        public bool ClearArtistImageFailed(ArtistV artist)
        {
            return ExecuteInternal("DELETE FROM ArtistImageFailed WHERE artist_id=?", artist.Id) > 0;
        }

        public bool SetAlbumReview(AlbumReview albumReview)
        {
            Debug.Assert(albumReview.AlbumId > 0);
            Debug.Assert(albumReview.Review.Length > 0);
            Logger.Debug($"SetAlbumReview albumID:{albumReview.AlbumId}");
            // ALEX TODO. Check what happens with language = null
            return ExecuteInternal("INSERT OR REPLACE INTO AlbumReviews (album_id, review, source, language, date_added) VALUES (?,?,?,?,?)",
                albumReview.AlbumId, albumReview.Review, albumReview.Source, albumReview.Language, albumReview.DateAdded) > 0;
        }

        public bool SetArtistBiography(ArtistBiography artistBiography)
        {
            Debug.Assert(artistBiography.ArtistId > 0);
            Debug.Assert(artistBiography.Biography.Length > 0);
            Logger.Debug($"SetArtistBiography artistID:{artistBiography.ArtistId}");
            // ALEX TODO. Check what happens with language = null
            return ExecuteInternal("INSERT OR REPLACE INTO ArtistBiographies (artist_id, biography, source, language, date_added) VALUES (?,?,?,?,?)", 
                artistBiography.ArtistId, artistBiography.Biography, artistBiography.Source, artistBiography.Language, artistBiography.DateAdded) > 0;

        }

        public bool SetTrackLyrics(TrackLyrics lyrics)
        {
            Debug.Assert(lyrics.TrackId > 0);
            Debug.Assert(lyrics.Lyrics.Length > 0);
            Logger.Debug($"SetTrackLyrics TrackId:{lyrics.TrackId}");
            // ALEX TODO. Check what happens with language = null
            return ExecuteInternal("INSERT OR REPLACE INTO TrackLyrics (track_id, lyrics, source, language, date_added) VALUES (?,?,?,?,?)",
                lyrics.TrackId, lyrics.Lyrics, lyrics.Source, lyrics.Language, lyrics.DateAdded) > 0;
        }
    }
}
