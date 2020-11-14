using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Digimezzo.Foundation.Core.Logging;
using System.Windows.Documents;
using System.Diagnostics;

namespace Dopamine.Data.Repositories
{
    public class SQLiteAlbumVRepository: IAlbumVRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ISQLiteConnectionFactory factory;

        public SQLiteAlbumVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        public List<AlbumV> GetAlbums(QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            return GetAlbumsInternal(qo);
        }

        public List<AlbumV> GetAlbumsWithText(string text, QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            if (!string.IsNullOrEmpty(text))
            {
                qo.extraWhereClause.Add("(Albums.Name like ? OR Artists.Name like ?)");
                qo.extraWhereParams.Add("%" + text + "%");
                qo.extraWhereParams.Add("%" + text + "%");
            }
            return GetAlbumsInternal(qo);
        }


        public List<AlbumV> GetAlbumsWithoutImages(bool incudeFailedDownloads)
        {
            QueryOptions qo = new QueryOptions();
            qo.extraWhereClause.Add("Thumbnail is null");
            if (incudeFailedDownloads == false)
            {
                qo.extraJoinClause.Add("LEFT JOIN AlbumImageFailed ON AlbumImageFailed.album_id=Albums.id");
                qo.extraWhereClause.Add("AlbumImageFailed.album_id is null");
            }
            return GetAlbumsInternal(qo);
        }

        public List<AlbumV> GetAlbumsWithArtists(List<long> artistIds, QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            qo.extraWhereClause.Add("Artists.id in (" + string.Join(",", artistIds) + ")");
            return GetAlbumsInternal(qo);
        }

        public List<AlbumV> GetAlbumsWithGenres(List<long> genreIds, QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            qo.extraWhereClause.Add("Genres.id in (" + string.Join(",", genreIds) + ")");
            return GetAlbumsInternal(qo);
        }

        public AlbumV GetAlbum(long albumId, QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            qo.extraWhereClause.Add("Albums.id=?");
            qo.extraWhereParams.Add(albumId);
            List<AlbumV> result = GetAlbumsInternal(qo);
            if (result.Count == 0)
                return null;
            Debug.Assert(result.Count == 1);
            return result[0];
        }
        public AlbumV GetAlbumOfTrackId(long trackId, QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            qo.extraWhereClause.Add("Albums.id=(SELECT album_id FROM TrackAlbums WHERE track_id=?)");
            qo.extraWhereParams.Add(trackId);
            List<AlbumV> result = GetAlbumsInternal(qo);
            if (result.Count == 0)
                return null;
            Debug.Assert(result.Count == 1);
            return result[0];
        }

        private List<AlbumV> GetAlbumsInternal(QueryOptions queryOptions)
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        return RepositoryCommon.Query<AlbumV>(conn, GetSQLTemplate(), queryOptions);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Query Failed");
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
            return @"
SELECT
Albums.ID as Id, 
Albums.name as Name, 
COUNT(DISTINCT t.id) as TrackCount,
COUNT(DISTINCT AlbumArtists.id) as AlbumArtistCount, 
GROUP_CONCAT(DISTINCT AlbumArtists.name) as AlbumArtists,
COUNT(DISTINCT Genres.id) as GenreCount,
GROUP_CONCAT(DISTINCT Genres.name) as Genres,
COUNT(DISTINCT Artists.id) as ArtistCount, 
GROUP_CONCAT(DISTINCT Artists.name) as Artists,
MIN(t.year) as MinYear,
MAX(t.year) as MaxYear,
AlbumImages.Location as Thumbnail,
MIN(t.date_added) as MinDateAdded,
MAX(t.date_added) as MaxDateAdded,
MIN(t.date_file_created) as MinDateFileCreated,
MIN(t.date_file_created) as MaxDateFileCreated #SELECT#
from Tracks t
LEFT JOIN TrackArtists  ON TrackArtists.track_id = t.id
LEFT JOIN Artists ON Artists.id = TrackArtists.artist_id
LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
LEFT JOIN Albums ON Albums.id = TrackAlbums.album_id
LEFT JOIN AlbumImages ON Albums.id = AlbumImages.album_id
LEFT JOIN ArtistCollectionsArtists ON ArtistCollectionsArtists.artist_collection_id = Albums.artist_collection_id
LEFT JOIN Artists AlbumArtists ON AlbumArtists.id = ArtistCollectionsArtists.artist_id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
LEFT JOIN Genres ON TrackGenres.genre_id = Genres.id
LEFT JOIN Folders ON Folders.id = t.folder_id #JOIN#
#WHERE#
GROUP BY Albums.id
ORDER BY Albums.name
#LIMIT#
";

        }    
    }



}
