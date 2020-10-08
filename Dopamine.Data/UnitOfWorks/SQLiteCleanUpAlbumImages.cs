using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.UnitOfWorks
{
    public class SQLiteCleanUpImagesUnitOfWork : ICleanUpImagesUnitOfWork
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private SQLiteConnection conn;
        public SQLiteCleanUpImagesUnitOfWork(SQLiteConnection conn)
        {
            this.conn = conn;
            this.conn.BeginTransaction();
        }

        public long CleanUp()
        {
            // Clean Up Album Images (from DB) from Album that have no tracks
            string selectAlbumIdWithoutTracks = @"
                SELECT 
                TrackAlbums.album_id as Id
                from Tracks t
                LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
                INNER JOIN AlbumImages on TrackAlbums.album_id = AlbumImages.album_id
                GROUP BY TrackAlbums.album_id
                HAVING Count(t.id)=0
                ";
            long deletions = conn.Execute(String.Format("DELETE FROM AlbumImages WHERE album_id in ({0});", selectAlbumIdWithoutTracks));
            long total = deletions;
            Logger.Debug($"DELETING {deletions} Album Image DB Records that have no tracks");
            // Clean Up Album Images (from DB) from Album that have no tracks
            string selectArtistIdWithoutTracks = @"
              	SELECT 
                TrackArtists.artist_id as Id
                from Tracks t
                LEFT JOIN TrackArtists ON TrackArtists.track_id = t.id
                INNER JOIN ArtistImages on TrackArtists.track_id = ArtistImages.artist_id
                GROUP BY TrackArtists.artist_id
                HAVING Count(t.id)=0
                ";
            deletions = conn.Execute(String.Format("DELETE FROM ArtistImages WHERE artist_id in ({0});", selectArtistIdWithoutTracks));
            total += deletions;
            Debug.Print($"DELETING {deletions} Artist Image DB Records that have no tracks");
            string selectGenreIdWithoutTracks = @"
              	SELECT 
                TrackGenres.genre_id as Id
                from Tracks t
                LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
                INNER JOIN GenreImages on TrackGenres.track_id = GenreImages.genre_id
                GROUP BY TrackGenres.genre_id 
                HAVING Count(t.id)=0
                ";
            deletions = conn.Execute(String.Format("DELETE FROM GenreImages WHERE genre_id in ({0});", selectGenreIdWithoutTracks));
            total += deletions;
            Debug.Print($"DELETING {deletions} Genre Image DB Records from Genres that have no tracks");
            return total;
        }

        public void Dispose()
        {
            conn.Commit();
            conn.Dispose();
        }
    }
}
