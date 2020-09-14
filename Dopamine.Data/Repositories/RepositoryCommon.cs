using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public enum QueryOptionsBool
    {
        True = 0,
        False = 1,
        Ignore = 2
    }

    public class QueryOptions
    {
        public bool UseLimit = false;
        public long Limit = 0;
        public long Offset = 0;
        public QueryOptionsBool WhereVisibleFolders = QueryOptionsBool.True;
        public QueryOptionsBool WhereIgnored = QueryOptionsBool.False;
        public QueryOptionsBool WhereDeleted = QueryOptionsBool.False;
        public List<string> extraJoinClause = new List<string>();
        public List<object> extraJoinParams = new List<object>();
        public List<string> extraWhereClause = new List<string>();
        public List<object> extraWhereParams = new List<object>();
        public string OrderClause = String.Empty;
    }

    class RepositoryCommon
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static long sQueryCounter = 0;
        public static List<T> Query<T>(SQLiteConnection connection, string sqlTemplate, QueryOptions queryOptions = null) where T : new()
        {
            if (queryOptions == null)
                queryOptions = new QueryOptions();
            //=== WhereVisibleFolders
            if (queryOptions.WhereVisibleFolders == QueryOptionsBool.True)
                queryOptions.extraWhereClause.Add("Folders.show = 1");
            else if (queryOptions.WhereVisibleFolders == QueryOptionsBool.False)
                queryOptions.extraWhereClause.Add("Folders.show = 0");
            //=== WhereIndexingFailed
            /*
            if (usedOptions.WhereIndexingFailed == QueryOptionsBool.True)
                where += "AND TrackIndexFailed.track_id is not null ";
            else if (usedOptions.WhereIndexingFailed == QueryOptionsBool.False)
                where += "AND TrackIndexFailed.track_id is null ";
            */
            //=== WhereIgnored
            if (queryOptions.WhereIgnored == QueryOptionsBool.True)
                queryOptions.extraWhereClause.Add("t.date_ignored is not null");
            else if (queryOptions.WhereIgnored == QueryOptionsBool.False)
                queryOptions.extraWhereClause.Add("t.date_ignored is null");
            //=== WhereDeleted
            if (queryOptions.WhereDeleted == QueryOptionsBool.True)
                queryOptions.extraWhereClause.Add("t.date_file_deleted is not null");
            else if (queryOptions.WhereIgnored == QueryOptionsBool.False)
                queryOptions.extraWhereClause.Add("t.date_file_deleted is null");
            //=== AdditionalWhere
            StringBuilder sbWhere = new StringBuilder();
            foreach (string condition in queryOptions.extraWhereClause)
            {
                sbWhere.Append(sbWhere.Length == 0 ? " WHERE " : " AND ");
                sbWhere.Append(condition);
            }

            StringBuilder sbJoin = new StringBuilder();
            foreach (string join in queryOptions.extraJoinClause)
            {
                sbJoin.Append(join);
                sbJoin.Append("\n");
            }

            string limit = "";
            if (queryOptions.Limit > 0)
            {
                limit = String.Format("LIMIT {0},{1}", queryOptions.Offset, queryOptions.Limit);
            }

            string sql = sqlTemplate.Replace("#WHERE#", sbWhere.ToString());
            sql = sql.Replace("#JOIN#", sbJoin.ToString());
            sql = sql.Replace("#ORDER#", queryOptions.OrderClause);
            sql = sql.Replace("#LIMIT#", limit);

            List<object> allParams = new List<object>();
            if (queryOptions?.extraJoinParams?.Count > 0)
                allParams.AddRange(queryOptions.extraJoinParams);
            if (queryOptions?.extraWhereParams?.Count > 0)
                allParams.AddRange(queryOptions.extraWhereParams);
            try
            {
                List<T> list = connection.Query<T>(sql, allParams.ToArray());
                Logger.Trace($"Query ({++sQueryCounter}) {typeof(T).ToString()} {list.Count} records");
                return list;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Query Failed. {++sQueryCounter} Message:{ex.Message} Type: {typeof(T).ToString()} SQL:\n{sql.Replace("\n","")}");
            }
            return null;
            
                

        }

    }
}
