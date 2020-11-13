using Dopamine.Core.Base;
using Dopamine.Core.Alex;
using SQLite;
using System;
using System.IO;
using System.Diagnostics;

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
            return new SQLiteConnection(this.DatabaseFile) { BusyTimeout = new TimeSpan(0, 0, 0, 10) };
        }
    }
}
