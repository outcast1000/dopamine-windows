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
    public class SQLiteIgnoreMediaFileUnitOfWork : IIgnoreMediaFileUnitOfWork
    {
        private SQLiteConnection conn;
        public SQLiteIgnoreMediaFileUnitOfWork(SQLiteConnection conn)
        {
            this.conn = conn;
            this.conn.BeginTransaction();
        }
        public bool IgnoreMediaFile(long trackId)
        {
            int ret = conn.Execute(String.Format("UPDATE tracks SET date_ignored = {0} WHERE id = {1}", DateTime.Now.Ticks, trackId));
            return ret == 1;
        }

        public void Dispose()
        {
            conn.Commit();
            conn.Dispose();
        }
    }
}
