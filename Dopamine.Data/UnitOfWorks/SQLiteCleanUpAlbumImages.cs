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
    public class SQLiteCleanUpAlbumImagesUnitOfWork : ICleanUpAlbumImagesUnitOfWork
    {
        private SQLiteConnection conn;
        public SQLiteCleanUpAlbumImagesUnitOfWork(SQLiteConnection conn)
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
            int ret = conn.Execute(String.Format("DELETE FROM AlbumThumbnail WHERE album_id in ({0});", selectAlbumIdWithoutTracks));
            Debug.Print("SQLiteCleanUpAlbumImagesUnitOfWork: DELETING {0} Thumbnails", ret);
            ret = conn.Execute(String.Format("DELETE FROM AlbumThumbnail WHERE album_id in ({0});", selectAlbumIdWithoutTracks));
            Debug.Print("SQLiteCleanUpAlbumImagesUnitOfWork: DELETING {0} Album Images", ret);
            return ret;
        }

        public void Dispose()
        {
            conn.Commit();
            conn.Dispose();
        }
    }
}
