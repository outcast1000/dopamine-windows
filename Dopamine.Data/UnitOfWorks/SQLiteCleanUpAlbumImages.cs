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
        private SQLiteConnection conn;
        public SQLiteCleanUpImagesUnitOfWork(SQLiteConnection conn)
        {
            this.conn = conn;
            this.conn.BeginTransaction();
        }

        public long CleanUp()
        {
            string selectAlbumIdWithoutTracks = @"
                SELECT 
                TrackAlbums.album_id as Id
                from Tracks t
                LEFT JOIN TrackAlbums ON TrackAlbums.track_id = t.id
                LEFT JOIN Albums ON Albums.id = TrackAlbums.album_id
                WHERE t.date_file_deleted is null AND t.date_ignored is null
                GROUP BY Albums.id
                HAVING Count(t.id)=0
                ORDER BY Albums.name
                ";
            long deletions = conn.Execute(String.Format("DELETE FROM AlbumImages WHERE album_id in ({0});", selectAlbumIdWithoutTracks));
            long total = deletions;
            Debug.Print("SQLiteCleanUpImagesUnitOfWork: DELETING {0} Album Images", deletions);
            string selectArtistIdWithoutTracks = @"
                SELECT 
                TrackArtists.artist_id as Id
                from Tracks t
                LEFT JOIN TrackArtists ON TrackArtists.track_id = t.id
                WHERE t.date_file_deleted is null AND t.date_ignored is null
                GROUP BY TrackArtists.artist_id
                HAVING Count(t.id)=0
                ";
            deletions = conn.Execute(String.Format("DELETE FROM ArtistImages WHERE artist_id in ({0});", selectArtistIdWithoutTracks));
            total += deletions;
            Debug.Print("SQLiteCleanUpImagesUnitOfWork: DELETING {0} Artist Images", deletions);
            string selectGenreIdWithoutTracks = @"
                SELECT 
                TrackGenres.genre_id as Id
                from Tracks t
                LEFT JOIN TrackGenres ON TrackGenres.track_id = t.id
                WHERE t.date_file_deleted is null AND t.date_ignored is null
                GROUP BY TrackGenres.genre_id
                HAVING Count(t.id)=0
                ";
            deletions = conn.Execute(String.Format("DELETE FROM GenreImages WHERE genre_id in ({0});", selectGenreIdWithoutTracks));
            total += deletions;
            Debug.Print("SQLiteCleanUpImagesUnitOfWork: DELETING {0} Genre Images", deletions);
            return total;
        }

        public void Dispose()
        {
            conn.Commit();
            conn.Dispose();
        }
    }
}
