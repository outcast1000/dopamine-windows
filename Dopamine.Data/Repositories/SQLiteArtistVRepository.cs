﻿using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Digimezzo.Foundation.Core.Logging;
using System.Diagnostics;

namespace Dopamine.Data.Repositories
{
    public class SQLiteArtistVRepository: IArtistVRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ISQLiteConnectionFactory factory;

        public SQLiteArtistVRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        public List<ArtistV> GetArtists(QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            return GetArtistsInternal(qo);
        }


        public List<ArtistV> GetArtistsWithText(string text, QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.ToLower();
                qo.extraWhereClause.Add("LOWER_I(Artists.Name) like ?");
                qo.extraWhereParams.Add("%" + text + "%");
            }
            return GetArtistsInternal(qo);
        }

        public ArtistV GetArtist(long artistID, QueryOptions qo = null)
        {
            Debug.Assert(artistID > 0);
            if (qo == null)
                qo = new QueryOptions();
            qo.extraWhereClause.Add("Artists.id=?");
            qo.extraWhereParams.Add(artistID);
            IList<ArtistV> artists = GetArtistsInternal(qo);
            Debug.Assert(artists.Count == 1);
            if (artists.Count == 1)
                return artists[0];
            return null;
        }


        public List<ArtistV> GetArtistsWithoutImages(bool incudeFailedDownloads)
        {
            QueryOptions qo = new QueryOptions();
            qo.extraWhereClause.Add("ArtistImages.artist_id is null");
            if (!incudeFailedDownloads)
            {
                qo.extraJoinClause.Add("LEFT JOIN ArtistImageFailed ON ArtistImageFailed.artist_id=Artists.id");
                qo.extraWhereClause.Add("ArtistImageFailed.artist_id is null");
            }
            return GetArtistsInternal(qo);
        }

        public List<ArtistV> GetArtistsOfTrack(long track_id, QueryOptions qo = null)
        {
            if (qo == null)
                qo = new QueryOptions();
            qo.extraWhereClause.Add("t.id=?");
            qo.extraWhereParams.Add(track_id);
            return GetArtistsInternal(qo);
        }

        private List<ArtistV> GetArtistsInternal(QueryOptions queryOptions)
        {
            try
            {
                using (var conn = factory.GetConnection())
                {
                    try
                    {
                        return RepositoryCommon.Query<ArtistV>(conn, GetSQLTemplate(), queryOptions);
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
            return @"
SELECT
Artists.ID as Id,
Artists.name as Name,
COUNT(DISTINCT t.id) as TrackCount,
COUNT(DISTINCT Genres.id) as GenreCount,
GROUP_CONCAT(DISTINCT Genres.name) as Genres,
COUNT(DISTINCT Albums.id) as AlbumCount,
GROUP_CONCAT(DISTINCT Albums.name) as Albums,
MIN(t.year) as MinYear,
MAX(t.year) as MaxYear,
COALESCE(ArtistImages.location, MAX(AlbumImages.location)) as Thumbnail,
ArtistImages.location as ArtistImage,
MIN(t.date_added) as MinDateAdded,
MAX(t.date_added) as MaxDateAdded,
MIN(t.date_file_created) as MinDateFileCreated,
MIN(t.date_file_created) as MaxDateFileCreated #SELECT#
from Tracks t
LEFT JOIN TrackArtists  ON TrackArtists.track_id = t.id
LEFT JOIN Artists ON Artists.id = TrackArtists.artist_id
LEFT JOIN ArtistImages ON Artists.id = ArtistImages.artist_id
LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
LEFT JOIN Albums ON Albums.id = TrackAlbums.album_id
LEFT JOIN AlbumImages ON Albums.id = AlbumImages.album_id
LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
LEFT JOIN Genres ON TrackGenres.genre_id = Genres.id
LEFT JOIN Folders ON Folders.id = t.folder_id #JOIN#
#WHERE#
GROUP BY Artists.id
ORDER BY Artists.name
#LIMIT#
";
        }    
    }



}
