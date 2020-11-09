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
        public bool GetHistory = false;
        public long Limit = 0;
        public long Offset = 0;
        public QueryOptionsBool WhereVisibleFolders = QueryOptionsBool.True;
        public QueryOptionsBool WhereIgnored = QueryOptionsBool.False;
        public QueryOptionsBool WhereDeleted = QueryOptionsBool.False;
        public QueryOptionsBool WhereInACollection = QueryOptionsBool.True;
        public List<string> extraSelectClause = new List<string>();
        public List<string> extraJoinClause = new List<string>();
        public List<object> extraJoinParams = new List<object>();
        public List<string> extraWhereClause = new List<string>();
        public List<object> extraWhereParams = new List<object>();
        public string OrderClause = String.Empty;
        public void ResetToIncludeAll()
        {
            WhereVisibleFolders = QueryOptionsBool.Ignore;
            WhereIgnored = QueryOptionsBool.Ignore;
            WhereDeleted = QueryOptionsBool.Ignore;
            WhereInACollection = QueryOptionsBool.Ignore;
        }
        public static QueryOptions IncludeAll()
        {
            QueryOptions qo = new QueryOptions();
            qo.ResetToIncludeAll();
            return qo;
        }
    }

    class RepositoryCommon
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static long sQueryCounter = 0;
        public static List<T> Query<T>(SQLiteConnection connection, string sqlTemplate, QueryOptions queryOptions = null) where T : new()
        {
            if (queryOptions == null)
                queryOptions = new QueryOptions();
            //=== History
            if (queryOptions.GetHistory == true)
            {
                queryOptions.extraSelectClause.Add("SUM(TrackHistoryStats.plays) as PlayCount");
                queryOptions.extraSelectClause.Add("SUM(TrackHistoryStats.skips) as SkipCount");
                queryOptions.extraSelectClause.Add("MIN(TrackHistoryStats.first_played) as DateFirstPlayed");
                queryOptions.extraSelectClause.Add("MAX(TrackHistoryStats.last_played) as DateLastPlayed");
                queryOptions.extraJoinClause.Add("LEFT JOIN TrackHistoryStats on t.id=TrackHistoryStats.track_id");
            }
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
            //=== WhereNotInACollection
            if (queryOptions.WhereInACollection == QueryOptionsBool.True)
                queryOptions.extraWhereClause.Add("t.folder_id is not null");
            else if (queryOptions.WhereInACollection == QueryOptionsBool.False)
                queryOptions.extraWhereClause.Add("t.folder_id is null");
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
                sbJoin.Append("\n");
                sbJoin.Append(join);
            }

            StringBuilder sbSelect = new StringBuilder();
            foreach (string item in queryOptions.extraSelectClause)
            {
                sbSelect.Append(",\n");
                sbSelect.Append(item);
            }

            string limit = "";
            if (queryOptions.Limit > 0)
            {
                limit = String.Format("LIMIT {0},{1}", queryOptions.Offset, queryOptions.Limit);
            }

            string sql = sqlTemplate.Replace("#WHERE#", sbWhere.ToString());
            sql = sql.Replace("#SELECT#", sbSelect.ToString());
            sql = sql.Replace("#JOIN#", sbJoin.ToString());
            sql = sql.Replace("#ORDER#", queryOptions.OrderClause);
            sql = sql.Replace("#LIMIT#", limit);

            List<object> allParams = new List<object>();
            if (queryOptions?.extraJoinParams?.Count > 0)
                allParams.AddRange(queryOptions.extraJoinParams);
            if (queryOptions?.extraWhereParams?.Count > 0)
                allParams.AddRange(queryOptions.extraWhereParams);
            return RawQuery<T>(connection, sql, allParams.ToArray());
        }

        public static List<T> RawQuery<T>(SQLiteConnection connection, string sql, params object[] args) where T : new()
        {
            try
            {
                List<T> list = connection.Query<T>(sql, args);
                return list;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Query Failed. {++sQueryCounter} Message:{ex.Message} Type: {typeof(T).ToString()} SQL:\n{sql.Replace("\n", " ")}");
            }
            return null;
        }

    }
}
