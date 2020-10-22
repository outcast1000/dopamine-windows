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
                origin, 
                origin_type_id, 
                date_added
                from AlbumImages
                ");
        }

        public AlbumImage GetAlbumImage(long albumId)
        {
            IList<AlbumImage> images = GetInternal<AlbumImage>(@"SELECT 
                album_id, 
                location, 
                origin, 
                origin_type_id, 
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
                        AlbumImages.origin, 
                        AlbumImages.origin_type_id, 
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
                origin, 
                origin_type_id, 
                date_added
                from ArtistImages
                ");
        }

        public IList<GenreImage> GetGenreImages()
        {
            return GetInternal<GenreImage>(@"SELECT 
                genre_id, 
                location, 
                origin, 
                origin_type_id, 
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

        public bool SetAlbumImage(AlbumImage image, bool bReplaceMode)
        {
            Debug.Assert(image.AlbumId > 0);
            Debug.Assert(image.Location.Length > 10);
            Logger.Debug("SetAlbumImage {0}", image.Location);
            string insert = "INSERT";
            if (bReplaceMode)
                insert = "INSERT OR REPLACE";
            if (image.DateAdded == 0)
                image.DateAdded = DateTime.Now.Ticks;
            bool ret = ExecuteInternal(insert + " INTO AlbumImages (album_id, location, origin, origin_type_id, date_added) VALUES (?,?,?,?,?)", image.AlbumId, image.Location, image.Origin, image.OriginType, image.DateAdded) > 0;
            Debug.Assert(ret, "Insert Failed");
            return ret;
        }
        public bool SetArtistImage(ArtistImage image)
        {
            Debug.Assert(image.ArtistId > 0);
            Debug.Assert(image.Location.Length > 10);
            Logger.Debug("SetArtistImage {0}", image.Location);
            if (image.DateAdded == 0)
                image.DateAdded = DateTime.Now.Ticks;
            bool ret = ExecuteInternal("INSERT OR REPLACE INTO ArtistImages (artist_id, location, origin, origin_type_id, date_added) VALUES (?,?,?,?,?)", image.ArtistId, image.Location, image.Origin, image.OriginType, image.DateAdded) > 0;
            Debug.Assert(ret, "Insert Failed");
            return ret;
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
            bool ret = ExecuteInternal("INSERT OR REPLACE INTO AlbumReviews (album_id, review, origin, origin_type_id, language, date_added) VALUES (?,?,?,?,?,?)",
                albumReview.AlbumId, albumReview.Review, albumReview.Origin, albumReview.OriginType, albumReview.Language, albumReview.DateAdded) > 0;
            Debug.Assert(ret, "Insert Failed");
            return ret;

        }

        public bool SetArtistBiography(ArtistBiography artistBiography)
        {
            Debug.Assert(artistBiography.ArtistId > 0);
            Debug.Assert(artistBiography.Biography.Length > 0);
            Logger.Debug($"SetArtistBiography artistID:{artistBiography.ArtistId}");
            bool ret = ExecuteInternal("INSERT OR REPLACE INTO ArtistBiographies (artist_id, biography, origin, origin_type_id, language, date_added) VALUES (?,?,?,?,?,?)", 
                artistBiography.ArtistId, artistBiography.Biography, artistBiography.Origin, artistBiography.OriginType, artistBiography.Language, artistBiography.DateAdded) > 0;
            Debug.Assert(ret, "Insert Failed");
            return ret;

        }


        public TrackLyrics GetTrackLyrics(long track_id)
        {
            IList<TrackLyrics> list = GetInternal<TrackLyrics>(@"
SELECT 
track_id, 
lyrics, 
origin,
origin_type_id, 
language,
date_added
from TrackLyrics
WHERE track_id = ?
                ", track_id);
            if (list.Count == 1)
                return list[0];
            return null;
        }

        public bool SetTrackLyrics(TrackLyrics lyrics, bool bReplaceMode)
        {
            Debug.Assert(lyrics.TrackId > 0);
            Debug.Assert(lyrics.Lyrics.Length > 0);
            Logger.Debug($"SetTrackLyrics TrackId:{lyrics.TrackId}");
            string insert = "INSERT";
            if (bReplaceMode)
                insert = "INSERT OR REPLACE";
            if (lyrics.DateAdded == 0)
                lyrics.DateAdded = DateTime.Now.Ticks;
            bool ret = ExecuteInternal(insert + " INTO TrackLyrics (track_id, lyrics, origin, origin_type_id, language, date_added) VALUES (?,?,?,?,?,?)",
                lyrics.TrackId, lyrics.Lyrics, lyrics.Origin, lyrics.OriginType, lyrics.Language, lyrics.DateAdded) > 0;
            Debug.Assert(ret, "Insert Failed");
            return ret;
        }


        public bool RemoveArtistImage(long artist_id)
        {
            Debug.Assert(artist_id > 0);
            return ExecuteInternal("DELETE FROM ArtistImages WHERE artist_id=?", artist_id) > 0;
        }

        public bool RemoveTrackLyrics(long track_id)
        {
            Debug.Assert(track_id > 0);
            return ExecuteInternal("DELETE FROM TrackLyrics WHERE track_id=?", track_id) > 0;
        }
    }
}
