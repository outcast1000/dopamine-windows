using Digimezzo.Foundation.Core.Logging;
using Dopamine.Data.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class SQLiteImageRepository:IImageRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ISQLiteConnectionFactory _sQLiteConnectionFactory;
        private SQLiteConnection _SQLiteConnection;

        public SQLiteImageRepository(ISQLiteConnectionFactory sQLiteConnectionFactory)
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
            {
                return _SQLiteConnection.Query<T>(sql, sqlParams);
            }
            Debug.Assert(_sQLiteConnectionFactory != null);
            using (var conn = _sQLiteConnectionFactory.GetConnection())
            {
                try
                {
                    return conn.Query<T>(sql, sqlParams);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Query Failed {sql}");
                }
            }
            return null;
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

        //AlbumImage GetAlbumArtworkForPath(string path);
        /*
        Task DeleteAlbumArtworkAsync(string albumKey);

        Task<long> DeleteUnusedAlbumArtworkAsync();

        Task UpdateAlbumArtworkAsync(string albumKey, string artworkId);
        */

    }
}
