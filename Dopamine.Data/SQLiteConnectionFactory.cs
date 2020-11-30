using Dopamine.Core.Base;
using Dopamine.Core.Alex;
using SQLite;
using System;
using System.IO;
using System.Diagnostics;
using SQLitePCL;
using System.Text.RegularExpressions;
using System.Text;

namespace Dopamine.Data
{
    public class SQLiteConnectionFactory : ISQLiteConnectionFactory
    {
        public string DatabaseFile
        {
            get
            {
                String appFolder = Dopamine.Core.Alex.SettingsClient.ApplicationFolder();
                string path = Path.Combine(appFolder, ProductInformation.ApplicationName + ".db");
                return path;
            }
        }

        public SQLiteConnection GetConnection()
        {
            SQLiteConnection con = new SQLiteConnection(this.DatabaseFile) { BusyTimeout = new TimeSpan(0, 0, 0, 10) };
            SQLitePCL.raw.sqlite3_create_function(con.Handle, "REGEXP", 2, null, MatchRegex);
            SQLitePCL.raw.sqlite3_create_function(con.Handle, "LOWER_I", 1, null, LowerInternational);
            return con;

        }

        private void LowerInternational(sqlite3_context ctx, object user_data, sqlite3_value[] args)
        {
            utf8z utf8 = SQLitePCL.raw.sqlite3_value_text(args[0]);
            string s = utf8.utf8_to_string();
            if (s != null)
                SQLitePCL.raw.sqlite3_result_text(ctx, s.ToLower());
        }

        private void MatchRegex(sqlite3_context ctx, object user_data, sqlite3_value[] args)
        {
            bool isMatched = System.Text.RegularExpressions.Regex.IsMatch(SQLitePCL.raw.sqlite3_value_text(args[1]).utf8_to_string(), SQLitePCL.raw.sqlite3_value_text(args[0]).utf8_to_string(), RegexOptions.IgnoreCase);
            if (isMatched)
                SQLitePCL.raw.sqlite3_result_int(ctx, 1);
            else
                SQLitePCL.raw.sqlite3_result_int(ctx, 0);
        }
    }
}
