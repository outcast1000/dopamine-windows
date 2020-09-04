using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Digimezzo.Foundation.Core.Logging;

namespace Dopamine.Data.Repositories
{
    public class SQLiteGenreVRepository: IGenreVRepository
    {
        private ISQLiteConnectionFactory factory;

        public SQLiteGenreVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        public List<GenreV> GetGenres()
        {
            return GetGenresInternal();
        }

        public List<GenreV> GetGenresByAlbumId(long albumId)
        {
            return GetGenresInternal("Albums.Id=" + albumId);
        }

        public List<GenreV> GetGenresByArtistId(long artistId)
        {
            throw new NotImplementedException();
        }

        private List<GenreV> GetGenresInternal(string whereClause = "", QueryOptions queryOptions = null)
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        string sql = RepositoryCommon.CreateSQL(GetSQLTemplate(), "", whereClause, "", queryOptions);
                        return conn.Query<GenreV>(sql);
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

        private string GetSQLTemplate()
        {
            return @"SELECT 
Genres.ID as Id, 
Genres.name as Name, 
COUNT(t.id) as TrackCount, 
COUNT(DISTINCT Albums.id) as AlbumCount, 
COUNT(DISTINCT Artists.id) as ArtistCount, 
GROUP_CONCAT(DISTINCT Artists.name ) as Artists,
MIN(t.year) as YearFrom,
MAX(t.year) as YearTo,
GenreThumbnail.key as Thumbnail
from Tracks t
LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
LEFT JOIN Albums ON Albums.id = TrackAlbums.album_id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
LEFT JOIN Genres ON TrackGenres.genre_id = Genres.id 
LEFT JOIN GenreThumbnail ON Genres.id = GenreThumbnail.genre_id 
LEFT JOIN TrackArtists ON TrackArtists.track_id = t.id
LEFT JOIN Artists ON Artists.id = TrackArtists.artist_id
LEFT JOIN Folders ON Folders.id = t.folder_id
#WHERE# 
GROUP BY Genres.id
ORDER BY Genres.name
#LIMIT#";
        }    
    }



}
