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
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        return conn.Query<ArtistV>(GetSQL());
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
GROUP_CONCAT(DISTINCT Genres.name ) as Genres,
ArtistThumbnail.key as Thumbnail ,
MIN(t.date_added) as DateAdded,
MIN(t.date_file_created) as DateFileCreated
from Tracks t
LEFT JOIN TrackArtists  ON TrackArtists.track_id = t.id
LEFT JOIN Artists ON Artists.id = TrackArtists.artist_id
LEFT JOIN ArtistThumbnail ON Artists.id = ArtistThumbnail.artist_id
LEFT JOIN TrackIndexing ON TrackIndexing.track_id = t.id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
LEFT JOIN Genres ON TrackGenres.genre_id = Genres.id
INNER JOIN Folders ON Folders.id = t.folder_id
WHERE Folders.show = 1 AND TrackIndexing.indexing_success is null AND TrackIndexing.needs_indexing is null
GROUP BY Artists.id
ORDER BY Artists.name;
";
        }    
    }



}
