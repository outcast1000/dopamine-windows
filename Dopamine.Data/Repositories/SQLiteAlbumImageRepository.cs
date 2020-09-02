using Digimezzo.Foundation.Core.Logging;
using Dopamine.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class SQLiteAlbumImageRepository:IAlbumImageRepository
    {
        private ISQLiteConnectionFactory factory;

        public SQLiteAlbumImageRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
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
path, 
source, 
date_added, 
file_size,
source_hash
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

        public IList<AlbumImage> GetAlbumImages(long albumId)
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
path, 
source, 
date_added, 
file_size,
source_hash
from AlbumImages
WHERE album_id=?
", albumId);
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

        //AlbumImage GetAlbumArtworkForPath(string path);
        /*
        Task DeleteAlbumArtworkAsync(string albumKey);

        Task<long> DeleteUnusedAlbumArtworkAsync();

        Task UpdateAlbumArtworkAsync(string albumKey, string artworkId);
        */

    }
}
