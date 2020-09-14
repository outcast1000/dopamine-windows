using Digimezzo.Foundation.Core.Logging;
using Dopamine.Data.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class SQLiteAlbumImageRepository:IAlbumImageRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ISQLiteConnectionFactory _sQLiteConnectionFactory;
        private SQLiteConnection _SQLiteConnection;

        public SQLiteAlbumImageRepository(ISQLiteConnectionFactory sQLiteConnectionFactory)
        {
            this._sQLiteConnectionFactory = sQLiteConnectionFactory;
        }

        public void SetSQLiteConnection(SQLiteConnection sQLiteConnection)
        {
            this._SQLiteConnection = sQLiteConnection;
        }

        public IList<AlbumImage> GetAlbumImages()
        {
            return GetInternal(@"SELECT 
                album_id, 
                location, 
                source, 
                date_added
                from AlbumImages
                ");

        }

        public AlbumImage GetAlbumImage(long albumId)
        {
            IList<AlbumImage> images = GetInternal(@"SELECT 
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
            IList<AlbumImage> images = GetInternal(@" 
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

        public AlbumImage GetPrimaryAlbumImage(long albumId)
        {
            IList<AlbumImage> images = GetInternal(@" 
                        SELECT
                        album_id, 
                        location, 
                        source, 
                        date_added
                        from AlbumImages
                        WHERE album_id = ?", albumId);
            Debug.Assert(images.Count < 2);
            return images.Count > 0 ? images[0] : null;
        }

        private IList<AlbumImage> GetInternal(string sql, params object[] sqlParams)
        {
            if (_SQLiteConnection != null)
            {
                return _SQLiteConnection.Query<AlbumImage>(sql, sqlParams);
            }
            Debug.Assert(_sQLiteConnectionFactory != null);
            using (var conn = _sQLiteConnectionFactory.GetConnection())
            {
                try
                {
                    return conn.Query<AlbumImage>(sql, sqlParams);
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
