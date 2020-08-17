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
            return @"SELECT 
Albums.ID as Id, 
Albums.name as Name, 
COUNT(t.id) as TrackCount, 
GROUP_CONCAT(DISTINCT Artists2.name ) as AlbumArtists,
GROUP_CONCAT(DISTINCT Artists2.name ) as Artists,
GROUP_CONCAT(DISTINCT Genres.name ) as Genres, 
MAX(t.year) as Year,
AlbumThumbnail.key as Thumbnail,
MIN(t.date_added) as DateAdded,
MIN(t.date_file_created) as DateFileCreated
from Tracks t
LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
LEFT JOIN Albums ON Albums.id = TrackAlbums.album_id
LEFT JOIN AlbumThumbnail ON Albums.id = AlbumThumbnail.album_id 
LEFT JOIN ArtistCollectionsArtists ON ArtistCollectionsArtists.artist_collection_id = Albums.artist_collection_id
LEFT JOIN Artists Artists1 ON Artists1.id = ArtistCollectionsArtists.artist_id
LEFT JOIN TrackIndexing ON TrackIndexing.track_id = t.id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
LEFT JOIN Genres ON TrackGenres.genre_id = Genres.id 
LEFT JOIN TrackArtists ON TrackArtists.track_id = t.id
LEFT JOIN Artists Artists2 ON Artists2.id = TrackArtists.artist_id
LEFT JOIN Folders ON Folders.id = t.folder_id
WHERE Folders.show = 1 AND TrackIndexing.indexing_success is null AND TrackIndexing.needs_indexing is null 
GROUP BY Albums.id
ORDER BY Albums.name;";
        }    
    }



}
