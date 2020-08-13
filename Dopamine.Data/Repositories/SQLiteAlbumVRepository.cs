using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Digimezzo.Foundation.Core.Logging;

namespace Dopamine.Data.Repositories
{
    public class SQLiteAlbumVRepository: IAlbumVRepository
    {
        private ISQLiteConnectionFactory factory;

        public SQLiteAlbumVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }


        public List<AlbumV> GetAlbums()
        {
            return GetAlbumsPriv(0);
        }

        public List<AlbumV> GetAlbumsByArtist(long artistId)
        {
            return GetAlbumsPriv(artistId);
        }


        private List<AlbumV> GetAlbumsPriv(long artistId)
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        string sql = GetSQL();
                        if (artistId > 0)
                            sql = sql.Replace("#WHERE#", "AND t.artist_id = " + artistId.ToString());
                        else
                            sql = sql.Replace("#WHERE#", "");
                        return conn.Query<AlbumV>(sql);
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
            return @"
SELECT Albums.ID as Id, Albums.name as Name, Artists1.name as AlbumArtist, COUNT(t.id) as TrackCount, GROUP_CONCAT(DISTINCT Genres.name ) as Genres, GROUP_CONCAT(DISTINCT Artists2.name ) as Artists
from Tracks t
LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
LEFT JOIN Albums ON Albums.id = TrackAlbums.album_id
LEFT JOIN Artists Artists1 ON Artists1.id = Albums.artist_id
LEFT JOIN TrackIndexing ON TrackIndexing.track_id = t.id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
INNER JOIN Genres ON TrackGenres.genre_id = Genres.id 
LEFT JOIN TrackArtists ON TrackArtists.track_id = t.id
INNER JOIN Artists Artists2 ON Artists2.id = TrackArtists.artist_id
INNER JOIN Folders ON Folders.id = t.folder_id
WHERE Folders.show = 1 AND TrackIndexing.indexing_success is null AND TrackIndexing.needs_indexing is null #WHERE#
GROUP BY Albums.id
ORDER BY Albums.name ";
        }    
    }



}
