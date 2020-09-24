using Dopamine.Data.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public class SQLiteGeneralRepository: IGeneralRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ISQLiteConnectionFactory factory;
        private const string keyDBVersion = "DB_VERSION";


        public SQLiteGeneralRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        private string GetValue(string key)
        {
            using (var conn = factory.GetConnection())
            {
                try
                {
                    IList<General> items = conn.Query<General>("SELECT * from general where key=?", key);
                    if (items.Count > 0)
                        return items[0].Value;
                }
                catch (Exception ex)
                {
                    Logger.Error("Query Failed. Exception: {0}", ex.Message);
                }
            }
            return null;
        }

        private void SetValue(string key, string value)
        {
            using (var conn = factory.GetConnection())
            {
                try
                {
                    int rows = conn.InsertOrReplace(new General() { Key = key, Value = value });
                    Debug.Assert(rows > 0);
                }
                catch (Exception ex)
                {
                    Logger.Error("Query Failed. Exception: {0}", ex.Message);
                }
            }
        }

        public string GetValue(GeneralRepositoryKeys key, string def = null)
        {
            string val = GetValue(key.ToString());
            return val == null ? null : def;
        }

        public void SetValue(GeneralRepositoryKeys key, string value)
        {
            SetValue(key.ToString(), value);
        }


    }
}
