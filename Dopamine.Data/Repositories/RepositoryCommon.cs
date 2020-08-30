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
    }
    class RepositoryCommon
    {
        public static string CreateSQL(string template, string additionalWhere, QueryOptions queryOptions)
        {
            QueryOptions usedOptions = queryOptions != null ? queryOptions : new QueryOptions();
            string where = "";
            //=== WhereVisibleFolders
            if (usedOptions.WhereVisibleFolders == QueryOptionsBool.True)
                where += "AND Folders.show = 1 ";
            else if (usedOptions.WhereVisibleFolders == QueryOptionsBool.False)
                where += "AND Folders.show = 0 ";
            //=== WhereIndexingFailed
            /*
            if (usedOptions.WhereIndexingFailed == QueryOptionsBool.True)
                where += "AND TrackIndexFailed.track_id is not null ";
            else if (usedOptions.WhereIndexingFailed == QueryOptionsBool.False)
                where += "AND TrackIndexFailed.track_id is null ";
            */
            //=== WhereIgnored
            if (usedOptions.WhereIgnored == QueryOptionsBool.True)
                where += "AND t.date_ignored is not null ";
            else if (usedOptions.WhereIgnored == QueryOptionsBool.False)
                where += "AND t.date_ignored is null ";
            //=== WhereDeleted
            if (usedOptions.WhereDeleted == QueryOptionsBool.True)
                where += "AND t.date_file_deleted is not null ";
            else if (usedOptions.WhereIgnored == QueryOptionsBool.False)
                where += "AND t.date_file_deleted is null ";
            //=== AdditionalWhere
            if (!string.IsNullOrEmpty(additionalWhere))
                where += "AND " + additionalWhere;
            if (where != "")
                where = "WHERE" + where.Substring(3);

            string limit = "";
            if (usedOptions.Limit > 0)
            {
                limit = String.Format("LIMIT {0},{1}", usedOptions.Offset, usedOptions.Limit);
            }

            string sql = template.Replace("#WHERE#", where);
            sql = sql.Replace("#LIMIT#", limit);

            return sql;
        }
    }
}
