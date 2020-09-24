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
    public class SQLiteSavePlaylistUnitOfWork : ISavePlaylistUnitOfWork
    {

        private SQLiteConnection conn;
        public SQLiteSavePlaylistUnitOfWork(SQLiteConnection conn)
        {
            this.conn = conn;
            this.conn.BeginTransaction();
        }

        public void SaveTracks(IList<TrackV> tracks)
        {
            try
            {
                conn.DeleteAll<PlaylistTrack>();
                conn.InsertAll(tracks.Select(f => new PlaylistTrack() { TrackID = f.Id }));
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not update the Folders. Exception: {0}", ex.Message);
            }
        }

        public void Dispose()
        {
            conn.Commit();
            conn.Dispose();
        }


    }
}
