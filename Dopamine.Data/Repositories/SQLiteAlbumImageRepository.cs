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
        private ISQLiteConnectionFactory factory;
        private SQLiteConnection connection;

        public SQLiteAlbumImageRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        public void SetSQLiteConnection(SQLiteConnection connection)
        {
            this.connection = connection;
        }

        public IList<AlbumImage> GetAlbumImages()
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        return conn.Query<AlbumImage>(@"SELECT 
                            id, 
                            album_id, 
                            location, 
                            source, 
                            date_added
                            from AlbumImages
                            ");
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

        public IList<AlbumImage> GetAlbumImages(long albumId, string provider = null)
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(provider))
                        {
                            return conn.Query<AlbumImage>(@"SELECT 
                            id, 
                            album_id, 
                            location, 
                            source, 
                            date_added
                            from AlbumImages
                            WHERE album_id=?
                            ", albumId);
                        }
                        else
                        {
                            return conn.Query<AlbumImage>(@"SELECT 
                            id, 
                            album_id, 
                            location, 
                            source, 
                            date_added
                            from AlbumImages
                            WHERE album_id=? AND provider=?
                            ", albumId, provider);
                        }
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

        public IList<AlbumImage> GetAlbumImageForTrackWithPath(string location)
        {
            using (var conn = factory.GetConnection())
            {
                try
                {
                    return conn.Query<AlbumImage>(@" 
                        SELECT
                        AlbumImages.id, 
                        AlbumImages.album_id, 
                        AlbumImages.location, 
                        AlbumImages.source, 
                        AlbumImages.date_added
                        from AlbumImages
                        LEFT JOIN TrackAlbums ON TrackAlbums.album_id = AlbumImages.album_id
                        LEFT JOIN Tracks ON tracks.id = TrackAlbums.track_id
                        WHERE Tracks.location = ?", location);
                }
                catch (Exception ex)
                {
                    LogClient.Error("Query Failed. Exception: {0}", ex.Message);
                }
            }
            return null;
        }

        public AlbumImage GetPrimaryAlbumImage(long albumId)
        {
            using (var conn = factory.GetConnection())
            {
                try
                {
                    IList<AlbumImage> images = conn.Query<AlbumImage>(@" 
                        SELECT
                        AlbumImages.id, 
                        AlbumImages.album_id, 
                        AlbumImages.location, 
                        AlbumImages.source, 
                        AlbumImages.date_added
                        from AlbumImages
                        LEFT JOIN TrackAlbums ON TrackAlbums.album_id = AlbumImages.album_id
                        LEFT JOIN Tracks ON tracks.id = TrackAlbums.track_id
                        WHERE AlbumImages.album_id = ? AND is_primary=1", albumId);
                    if (images.Count > 0)
                    {
                        return images[0];
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("Query Failed. Exception: {0}", ex.Message);
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
