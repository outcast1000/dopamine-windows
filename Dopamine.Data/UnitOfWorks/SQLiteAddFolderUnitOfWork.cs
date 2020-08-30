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
    public class SQLiteAddFolderUnitOfWork : IAddFolderUnitOfWork
    {

        private SQLiteConnection conn;
        public SQLiteAddFolderUnitOfWork(SQLiteConnection conn)
        {
            this.conn = conn;
            this.conn.BeginTransaction();
        }

        public AddFolderResult AddFolder(string path)
        {
            AddFolderResult result = AddFolderResult.Success;
            try
            {
                if (!conn.Table<Folder2>().Select((f) => f).ToList().Select((f) => f.Path).Contains(path))
                {
                    conn.Insert(new Folder2 { Path = path, Show = 1, DateAdded = DateTime.Now.Ticks });
                    LogClient.Info("Added the Folder {0}", path);
                }
                else
                {
                    LogClient.Info("Didn't add the Folder {0} because it is already in the database", path);
                    result = AddFolderResult.Duplicate;
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not add the Folder {0}. Exception: {1}", path, ex.Message);
                result = AddFolderResult.Error;
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
