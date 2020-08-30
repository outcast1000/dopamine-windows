using Digimezzo.Foundation.Core.Logging;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.UnitOfWorks
{
    public class SQLiteRemoveFolderUnitOfWork : IRemoveFolderUnitOfWork
    {
        private SQLiteConnection conn;
        public SQLiteRemoveFolderUnitOfWork(SQLiteConnection conn)
        {
            this.conn = conn;
            this.conn.BeginTransaction();
        }

        public RemoveFolderResult RemoveFolder(long folderId)
        {
            RemoveFolderResult result = RemoveFolderResult.Success;

            try
            {
                conn.Execute($"DELETE FROM TrackArtists WHERE track_id in (SELECT id from tracks where folder_id={folderId};");
                conn.Execute($"DELETE FROM TrackAlbums WHERE track_id in (SELECT id from tracks where folder_id={folderId};");
                conn.Execute($"DELETE FROM TrackGenres WHERE track_id in (SELECT id from tracks where folder_id={folderId};");
                conn.Execute($"DELETE FROM TrackLyrics WHERE track_id in (SELECT id from tracks where folder_id={folderId};");
                conn.Execute($"DELETE FROM Tracks WHERE folder_id={folderId};");
                conn.Execute($"DELETE FROM Folders WHERE id={folderId};");
                LogClient.Info("Removed the Folder with FolderID={0}", folderId);
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not remove the Folder with FolderID={0}. Exception: {1}", folderId, ex.Message);
                result = RemoveFolderResult.Error;
            }

            return result;
        }

        public void Dispose()
        {
            conn.Commit();
            conn.Dispose();
        }
    }
}
