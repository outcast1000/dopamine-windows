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
    public class SQLiteUpdateFolderUnitOfWork : IUpdateFolderUnitOfWork
    {

        private SQLiteConnection conn;
        public SQLiteUpdateFolderUnitOfWork(SQLiteConnection conn)
        {
            this.conn = conn;
            this.conn.BeginTransaction();
        }

        public void UpdateFolders(IList<FolderV> folders)
        {
            try
            {
                foreach (FolderV fol in folders)
                {
                    var dbFolder = conn.Table<Folder2>().Select((f) => f).Where((f) => f.Path.Equals(fol.Path)).FirstOrDefault();

                    if (dbFolder != null)
                    {
                        dbFolder.Show = fol.Show ? 1 : 0;
                        conn.Update(dbFolder);
                    }
                }
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
