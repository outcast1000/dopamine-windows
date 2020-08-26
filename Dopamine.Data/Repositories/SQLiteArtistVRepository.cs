using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Digimezzo.Foundation.Core.Logging;

namespace Dopamine.Data.Repositories
{
    public class SQLiteArtistVRepository: IArtistVRepository
    {
        private ISQLiteConnectionFactory factory;

        public SQLiteArtistVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        public List<ArtistV> GetArtists()
        {
            return GetArtistsInternal();
        }

        private List<ArtistV> GetArtistsInternal(string whereClause = "")
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        string sql = GetSQL();
                        sql = sql.Replace("#WHERE#", whereClause);
                        return conn.Query<ArtistV>(sql);
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
SELECT
Artists.ID as Id,
Artists.name as Name,
COUNT(t.id) as TrackCount,
COUNT(DISTINCT Genres.id ) as GenreCount,
GROUP_CONCAT(DISTINCT Genres.name ) as Genres,
COUNT(DISTINCT Albums.id ) as AlbumCount,
GROUP_CONCAT(DISTINCT Albums.name ) as Albums,
ArtistThumbnail.key as Thumbnail ,
MIN(t.year) as MinYear,
MAX(t.year) as MaxYear,
MIN(t.date_added) as DateAdded,
MIN(t.date_file_created) as DateFileCreated
from Tracks t
LEFT JOIN TrackArtists  ON TrackArtists.track_id = t.id
LEFT JOIN Artists ON Artists.id = TrackArtists.artist_id
LEFT JOIN ArtistThumbnail ON Artists.id = ArtistThumbnail.artist_id
LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
LEFT JOIN Albums ON Albums.id = TrackAlbums.album_id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
LEFT JOIN Genres ON TrackGenres.genre_id = Genres.id
LEFT JOIN TrackIndexing ON TrackIndexing.track_id = t.id
INNER JOIN Folders ON Folders.id = t.folder_id
WHERE Folders.show = 1 AND TrackIndexing.indexing_success is null AND TrackIndexing.needs_indexing is null
#WHERE#
GROUP BY Artists.id
ORDER BY Artists.name;
";
        }    
    }



}
