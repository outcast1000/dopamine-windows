using Dopamine.Data.Entities;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dopamine.Data.UnitOfWorks;
using Dopamine.Core.Alex;
using Dopamine.Core.IO;
using Dopamine.Data.Repositories;

namespace Dopamine.Data
{
    public class DbMigrator
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        protected sealed class DatabaseVersionAttribute : Attribute
        {
            private int version;

            public DatabaseVersionAttribute(int version)
            {
                this.version = version;
            }

            public int Version
            {
                get { return this.version; }
            }
        }

        // NOTE: whenever there is a change in the database schema,
        // this version MUST be incremented and a migration method
        // MUST be supplied to match the new version number
        protected const int CURRENT_VERSION = 27;
        private ISQLiteConnectionFactory factory;
        private int userDatabaseVersion;

        public DbMigrator(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        private void CreateTablesAndIndexes()
        {
            using (var conn = this.factory.GetConnection())
            {
                Logger.Debug("CreateTablesAndIndexes_v2: DROPPING TABLES");

                conn.Execute("DROP TABLE IF EXISTS General;");

                conn.Execute("DROP TABLE IF EXISTS PlaylistTracks;");

                conn.Execute("DROP TABLE IF EXISTS History;");
                conn.Execute("DROP TABLE IF EXISTS HistoryActions;");

                conn.Execute("DROP TABLE IF EXISTS TrackArtists;");
                conn.Execute("DROP TABLE IF EXISTS TrackAlbums;");
                conn.Execute("DROP TABLE IF EXISTS TrackLyrics;");
                conn.Execute("DROP TABLE IF EXISTS TrackGenres;");
                conn.Execute("DROP TABLE IF EXISTS Tracks;");

                conn.Execute("DROP TABLE IF EXISTS GenreImages;");
                conn.Execute("DROP TABLE IF EXISTS GenreDownloadFailed;");
                conn.Execute("DROP TABLE IF EXISTS Genres;");

                conn.Execute("DROP TABLE IF EXISTS AlbumReviews;");
                conn.Execute("DROP TABLE IF EXISTS AlbumImages;");
                conn.Execute("DROP TABLE IF EXISTS AlbumImageFailed;");
                conn.Execute("DROP TABLE IF EXISTS Albums;");

                conn.Execute("DROP TABLE IF EXISTS ArtistCollectionsArtists;");
                conn.Execute("DROP TABLE IF EXISTS ArtistCollections;");
                conn.Execute("DROP TABLE IF EXISTS ArtistBiographies;");
                conn.Execute("DROP TABLE IF EXISTS ArtistImages;");
                conn.Execute("DROP TABLE IF EXISTS ArtistImageFailed;");
                conn.Execute("DROP TABLE IF EXISTS Artists;");
                conn.Execute("DROP TABLE IF EXISTS ArtistRoles;");

                conn.Execute("DROP TABLE IF EXISTS Folders;");

                Logger.Debug("CreateTablesAndIndexes_v2: CREATING TABLES");

                //=== Configuration:
                conn.Execute("CREATE TABLE General (" +
                    "key                TEXT PRIMARY KEY," +
                    "value              TEXT)");

                conn.Execute("INSERT INTO General (key, value) VALUES (?, ?);", GeneralRepositoryKeys.DBVersion.ToString(), CURRENT_VERSION.ToString());

                //=== Artists:
                conn.Execute("CREATE TABLE Artists (" +
                            "id                 INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "name               TEXT NOT NULL COLLATE NOCASE);");

                conn.Execute("CREATE UNIQUE INDEX ArtistsNameIndex ON Artists(name);");

                //=== ArtistBiographies: (One 2 One) Each Artist may have only one biographies
                conn.Execute("CREATE TABLE ArtistBiographies (" +
                            "artist_id          INTEGER PRIMARY KEY," +
                            "biography          TEXT NOT NULL," +
                            "source             TEXT," +
                            "language           TEXT," +
                            "date_added         INTEGER NOT NULL," +
                            "FOREIGN KEY (artist_id) REFERENCES Artists(id));");

                //=== ArtistImages: (One 2 many) Each artist may have multiple images (but only one primary)
                conn.Execute("CREATE TABLE ArtistImages (" +
                            "artist_id          INTEGER NOT NULL PRIMARY KEY," +
                            "location           TEXT NOT NULL," + //=== May be cache://
                            "source             TEXT," +
                            "date_added         INTEGER NOT NULL," +
                            "FOREIGN KEY(artist_id) REFERENCES Artists(id));");

                //conn.Execute("CREATE INDEX AlbumImagesIsPrimaryIndex ON AlbumImages(is_primary);"); //=== ALEX: FOR SOME REASON WHEN THIS IS ENABLED many queries that have is_primary=1 becomes 1000 times more slow

                //=== ArtistImageFailed: (One 2 Many) Each Album may have only one failed indexing record
                conn.Execute("CREATE TABLE ArtistImageFailed (" +
                            "artist_id         INTEGER NOT NULL PRIMARY KEY," +
                            "date_added        INTEGER NOT NULL," +
                            "FOREIGN KEY (artist_id) REFERENCES Artists(id));");

                //=== ArtistCollections: 
                conn.Execute("CREATE TABLE ArtistCollections (" +
                            "id                 INTEGER PRIMARY KEY AUTOINCREMENT);");

                //=== ArtistCollectionArtists: (Many to Many) Each Artist may be in many ArtistCollections. Each ArtistCollection may have many artists
                conn.Execute("CREATE TABLE ArtistCollectionsArtists (" +
                            "id                     INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "artist_collection_id   INTEGER NOT NULL," +
                            "artist_id              INTEGER NOT NULL," +
                            "FOREIGN KEY (artist_collection_id) REFERENCES ArtistCollections(id), " +
                            "FOREIGN KEY (artist_id) REFERENCES Artists(id)); ");

                conn.Execute("CREATE INDEX ArtistCollectionsArtistsArtistCollectionIDIndex ON ArtistCollectionsArtists(artist_collection_id);");
                conn.Execute("CREATE INDEX ArtistCollectionsArtistsArtistIDIndex ON ArtistCollectionsArtists(artist_id);");


                //=== Albums:
                conn.Execute("CREATE TABLE Albums (" +
                            "id                     INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "artist_collection_id   INTEGER," +
                            "name                   TEXT NOT NULL COLLATE NOCASE," +
                            "FOREIGN KEY (artist_collection_id) REFERENCES ArtistCollections(id)); ");

                conn.Execute("CREATE INDEX AlbumsArtistCollectionIdIndex ON Albums(artist_collection_id);");
                conn.Execute("CREATE UNIQUE INDEX AlbumsUniqueIndex ON Albums(name, artist_collection_id);");

                //=== AlbumReviews: (One 2 One) Each album may have one review
                conn.Execute("CREATE TABLE AlbumReviews (" +
                            "album_id           INTEGER PRIMARY KEY," +
                            "review             TEXT NOT NULL," +
                            "source             TEXT," +
                            "language           TEXT," +
                            "date_added         INTEGER NOT NULL," +
                            "FOREIGN KEY(album_id) REFERENCES Albums(id));");

                //=== AlbumImages: (One 2 many) Each album may have multiple images
                conn.Execute("CREATE TABLE AlbumImages (" +
                            "album_id           INTEGER PRIMARY KEY," +
                            "location           TEXT NOT NULL," + //=== May be cache://
                            "source             TEXT," +
                            "date_added         INTEGER NOT NULL," +
                            "FOREIGN KEY(album_id) REFERENCES Albums(id));");

                //=== AlbumImageFailed: (One 2 many) Each Album may have mutltipe failed indexing record (one per provider)
                conn.Execute("CREATE TABLE AlbumImageFailed (" +
                            "album_id          INTEGER PRIMARY KEY," +
                            "date_added        INTEGER NOT NULL," +
                            "FOREIGN KEY (album_id) REFERENCES Albums(id));");

                //=== Genres:
                conn.Execute("CREATE TABLE Genres (" +
                            "id                 INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "name               TEXT NOT NULL COLLATE NOCASE);");

                conn.Execute("CREATE UNIQUE INDEX GenresNameIndex ON Genres(name);");

                //=== GenreImages: (Many 2 many) Each genre may have multiple images
                conn.Execute("CREATE TABLE GenreImages (" +
                            "genre_id           INTEGER PRIMARY KEY," +
                            "location           TEXT NOT NULL," +
                            "source             TEXT," +
                            "date_added         INTEGER NOT NULL," +
                            "FOREIGN KEY (genre_id) REFERENCES Genres(id));");

                //=== Folders:
                conn.Execute("CREATE TABLE Folders (" +
                            "id                     INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "path                   TEXT," +
                            "date_added             INTEGER NOT NULL," +
                            "show                   INTEGER DEFAULT 1);");

                conn.Execute("CREATE UNIQUE INDEX FoldersPath ON Folders(path);");

                //=== Tracks:
                conn.Execute("CREATE TABLE Tracks (" +
                            "id	                INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "name	            TEXT COLLATE NOCASE," +
                            "path               TEXT NOT NULL COLLATE NOCASE," +
                            "folder_id          INTEGER NOT NULL," +
                            "filesize           INTEGER," +
                            "bitrate            INTEGER," +
                            "samplerate	        INTEGER," +
                            "duration           INTEGER," +
                            "year	            INTEGER," +
                            "language           TEXT," +
                            "date_added	        INTEGER," +
                            "date_ignored       INTEGER," +
                            "rating	            INTEGER," +
                            "love	            INTEGER," +
                            "date_file_created  INTEGER," +
                            "date_file_modified INTEGER," +
                            "date_file_deleted	INTEGER," +
                            "FOREIGN KEY (folder_id) REFERENCES Folders(id));");

                conn.Execute("CREATE INDEX TracksNameIndex ON Tracks(name);");
                conn.Execute("CREATE UNIQUE INDEX TracksPathIndex ON Tracks(path);");
                conn.Execute("CREATE INDEX TracksFolderIDIndex ON Tracks(folder_id);");
                conn.Execute("CREATE INDEX TracksDateFileDeletedIndex ON Tracks(date_file_deleted);");
                conn.Execute("CREATE INDEX TracksDateIgnoredIndex ON Tracks(date_ignored);");

                //=== ArtistRoles:
                conn.Execute("CREATE TABLE ArtistRoles (" +
                            "id                 INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "name               TEXT NOT NULL);");

                conn.Execute("INSERT INTO ArtistRoles (name) VALUES ('Composer'), ('Producer'), ('Mixing');");

                //=== TrackArtists: (Many 2 Many) Many Tracks may belong to the same Artist. Many Artists may have collaborate to the same track
                conn.Execute("CREATE TABLE TrackArtists (" +
                            "id                 INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "track_id           INTEGER NOT NULL," +
                            "artist_id          INTEGER NOT NULL," +
                            "artist_role_id     INTEGER DEFAULT 1," +
                            "FOREIGN KEY (track_id) REFERENCES Tracks(id)," +
                            "FOREIGN KEY (artist_id) REFERENCES Artists(id)," +
                            "FOREIGN KEY (artist_role_id) REFERENCES ArtistRoles(id)); ");

                conn.Execute("CREATE INDEX TrackArtistsTrackIDIndex ON TrackArtists(track_id);");
                conn.Execute("CREATE INDEX TrackArtistsArtistIDIndex ON TrackArtists(artist_id);");
                conn.Execute("CREATE INDEX TrackArtistsArtistRoleIDIndex ON TrackArtists(artist_role_id);");
                conn.Execute("CREATE UNIQUE INDEX TrackArtistsCombinedIndex ON TrackArtists(track_id, artist_id, artist_role_id);");

                //=== TrackArtists: (Many 2 Many) Many Tracks may belong to the same Artist. Many Artists may have collaborate to the same track
                conn.Execute("CREATE TABLE TrackGenres (" +
                            "id                 INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "track_id           INTEGER NOT NULL," +
                            "genre_id           INTEGER NOT NULL," +
                            "FOREIGN KEY (track_id) REFERENCES Tracks(id)," +
                            "FOREIGN KEY (genre_id) REFERENCES Genres(id)); ");

                conn.Execute("CREATE INDEX TrackGenresTrackIDIndex ON TrackGenres(track_id);");
                conn.Execute("CREATE INDEX TrackGenresArtistIDIndex ON TrackGenres(genre_id);");
                conn.Execute("CREATE UNIQUE INDEX TrackGenresCombinedIndex ON TrackGenres(track_id, genre_id);");

                //=== TrackAlbum: (One 2 One) Each Track may have zero or one TrackAlbum record 
                conn.Execute("CREATE TABLE TrackAlbums (" +
                            "track_id           INTEGER PRIMARY KEY," +
                            "album_id           INTEGER NOT NULL," +
                            "track_number       INTEGER," +
                            "disc_number        INTEGER," +
                            "track_count        INTEGER," +
                            "disc_count         INTEGER," +
                            "FOREIGN KEY (track_id) REFERENCES Tracks(id)," +
                            "FOREIGN KEY (album_id) REFERENCES Albums(id)); ");

                conn.Execute("CREATE UNIQUE INDEX TrackAlbumTrackIDIndex ON TrackAlbums(track_id);");
                conn.Execute("CREATE INDEX TrackAlbumAlbumIDIndex ON TrackAlbums(album_id);");

                //=== TrackLyrics: (One 2 One) Each Track may have zero or one Lyrics record 
                conn.Execute("CREATE TABLE TrackLyrics (" +
                            "track_id           INTEGER PRIMARY KEY," +
                            "lyrics             TEXT NOT NULL COLLATE NOCASE," +
                            "source             TEXT," +
                            "language           TEXT," +
                            "date_added         INTEGER NOT NULL," +
                            "FOREIGN KEY (track_id) REFERENCES Tracks(id));");


                //=== HistoryActions: Should include Added, Deleted, Modified, Played, AutoPlayed, Skipped, Rated, Loved
                conn.Execute("CREATE TABLE HistoryActions (" +
                            "id                 INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "name               TEXT NOT NULL);");

                conn.Execute("INSERT INTO HistoryActions (name) VALUES ('Added'), ('Removed'), ('Modified'), ('Played'), ('Skipped'), ('Rated'), ('Loved');");


                //=== History:
                conn.Execute("CREATE TABLE History (" +
                            "id                     INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "track_id               INTEGER NOT NULL," +
                            "history_action_id      INTEGER NOT NULL," +
                            "history_action_extra   TEXT," +
                            "date_happened          INTEGER NOT NULL," +
                            "FOREIGN KEY (track_id) REFERENCES Tracks(id)," +
                            "FOREIGN KEY (history_action_id) REFERENCES HistoryActions(id));");

                conn.Execute("CREATE INDEX HistoryTrackIDIndex ON History(track_id);");
                conn.Execute("CREATE INDEX HistoryHistoryActionIDIndex ON History(history_action_id);");

                conn.Execute("CREATE TABLE PlaylistTracks (" +
                            "id                 INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "track_id           INTEGER," +
                            "FOREIGN KEY (track_id) REFERENCES Tracks(id));");





                // ==== START MIGRATING DATA
                try
                {
                    int count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Track'");
                    if (count == 0)
                    {
                        Logger.Debug("CreateTablesAndIndexes: There is no older tables present. No migration needed");
                        return;
                    }
                }
                catch (Exception)
                {
                    //=== old tABLES DO not elxst
                }
                Logger.Debug("CreateTablesAndIndexes: START DATA MIGRATION");

                conn.BeginTransaction();
                SQLiteImageRepository imageRepository = new SQLiteImageRepository(null);
                imageRepository.SetSQLiteConnection(conn);
                using (SQLiteUpdateCollectionUnitOfWork uc = new SQLiteUpdateCollectionUnitOfWork(conn, true))
                {
                    List<Folder> folders = conn.Table<Folder>().ToList();
                    Dictionary<long, long> folderMapID = new Dictionary<long, long>();
                    foreach (Folder folder in folders)
                    {
                        Console.WriteLine(String.Format("Migrating Folder: {0}", folder.Path));
                        conn.Insert(new Folder2() { Path = folder.Path, Show = folder.ShowInCollection });
                        folderMapID[folder.FolderID] = GetLastInsertRowID(conn);
                    }

                    // Get all the items from "track" table. Add them to the new structure

                    string query = @"SELECT DISTINCT t.TrackID, t.Artists, t.Genres, t.AlbumTitle, t.AlbumArtists, t.AlbumKey,
                        t.Path, t.SafePath, t.FileName, t.MimeType, t.FileSize, t.BitRate, 
                        t.SampleRate, t.TrackTitle, t.TrackNumber, t.TrackCount, t.DiscNumber,
                        t.DiscCount, t.Duration, t.Year, t.HasLyrics, t.DateAdded, t.DateFileCreated,
                        t.DateLastSynced, t.DateFileModified, t.NeedsIndexing, t.NeedsAlbumArtworkIndexing, t.IndexingSuccess,
                        t.IndexingFailureReason, t.Rating, t.Love, t.PlayCount, t.SkipCount, t.DateLastPlayed,
                        AlbumArtwork.ArtworkID as AlbumImage
                        FROM Track t
                        INNER JOIN FolderTrack ft ON ft.TrackID = t.TrackID
                        INNER JOIN Folder f ON ft.FolderID = f.FolderID
                        LEFT JOIN AlbumArtwork ON AlbumArtwork.AlbumKey=t.AlbumKey";
                    //WHERE f.ShowInCollection = 1 AND t.IndexingSuccess = 1 AND t.NeedsIndexing = 0";

                    //var tracks = new List<Track>();
                    IFileStorage fileStorage = new FileStorage();
                    List<Track> tracks = conn.Query<Track>(query);
                    int tracksMigrated = 0;
                    int timeStarted = Environment.TickCount;
                    string coverArtCacheFolderPath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder, ApplicationPaths.CoverArtCacheFolder);
                    foreach (Track track in tracks)
                    {
                        Logger.Info("Migrating File {0}", track.Path);

                        if (track.TrackTitle == null)
                            Logger.Warn("*** TrackTitle is null");
                        long folderTrackID = conn.ExecuteScalar<long>(@"SELECT FolderID FROM FolderTrack WHERE TrackID=?", track.TrackID);
                        string imagePath = System.IO.Path.Combine(coverArtCacheFolderPath, track.AlbumImage + ".jpg");
                        AddMediaFileResult addMediaFileResult = uc.AddMediaFile(new MediaFileData()
                        {
                            Name = track.TrackTitle,
                            Path = track.Path,
                            Filesize = track.FileSize,
                            Bitrate = track.BitRate,
                            Samplerate = track.SampleRate,
                            Duration = track.Duration,
                            Year = track.Year > 0 ? track.Year : null,
                            Language = null,
                            DateAdded = track.DateAdded,
                            Rating = track.Rating > 0 ? track.Rating : null,
                            Love = track.Love,
                            DateFileCreated = track.DateFileCreated,
                            DateFileModified = track.DateFileModified,
                            AlbumArtists = string.IsNullOrEmpty(track.AlbumArtists) ? null : track.AlbumArtists.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray(),
                            Artists = string.IsNullOrEmpty(track.Artists) ? null : track.Artists.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray(),
                            Genres = string.IsNullOrEmpty(track.Genres) ? null : track.Genres.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray(),
                            Album = track.AlbumTitle,
                            DateFileDeleted = null,
                            DateIgnored = null,
                            DiscCount = track.DiscCount,
                            DiscNumber = track.DiscNumber,
                            TrackCount = track.TrackCount,
                            TrackNumber = track.TrackNumber
                        }, folderMapID[folderTrackID]);



                        if (!string.IsNullOrEmpty(track.AlbumImage) && addMediaFileResult.AlbumId.HasValue)
                        {
                            IList<AlbumImage> images = imageRepository.GetAlbumImages();
                            AlbumImage ai = imageRepository.GetAlbumImage((long) addMediaFileResult.AlbumId);
                            if (ai == null)
                            {
                                string realPath = System.IO.Path.Combine(coverArtCacheFolderPath, track.AlbumImage + ".jpg");
                                FileInfo fi = new System.IO.FileInfo(realPath);
                                if (fi.Exists)
                                {
                                    Logger.Info($" --> Adding AlbumImage {realPath}");
                                    byte[] bytes = File.ReadAllBytes(realPath);
                                    string location = fileStorage.SaveImageToCache(bytes, FileStorageItemType.Album);
                                    AlbumImage albumImage = new AlbumImage()
                                    {
                                        AlbumId = (long)addMediaFileResult.AlbumId,
                                        DateAdded = DateTime.Now.Ticks,
                                        Location = location,
                                        Source = "[MIGRATION]"
                                    };
                                    uc.SetAlbumImage(albumImage, false);
                                }
                                else
                                {
                                    Logger.Warn($" --> Image {realPath} not found!");
                                }
                            }
                        }
                        Logger.Debug("Stats: {0} files/sec", (1000.0 * ++tracksMigrated / (Environment.TickCount - timeStarted)));
                    }


                }
                conn.Commit();
                conn.Execute("VACUUM;");
            }
        }

         private long GetLastInsertRowID(SQLiteConnection conn)
        {
            SQLiteCommand cmdLastRow = conn.CreateCommand(@"select last_insert_rowid()");
            return cmdLastRow.ExecuteScalar<long>();
        }



        //=== My Update ====
        [DatabaseVersion(27)]
        private void Migrate27()
        {
            try
            {
                CreateTablesAndIndexes();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Migration(27) Failed");
            }
        }

        public void Migrate()
        {
            try
            {
                if (!this.IsDatabaseValid())
                {
                    Logger.Info("Creating a new database at {0}", this.factory.DatabaseFile);
                    CreateTablesAndIndexes();
                }
                else
                {
                    // Upgrade the database if it is not the latest version
                    if (this.IsMigrationNeeded())
                    {
                        Logger.Info("Creating a backup of the database");
                        this.BackupDatabase();
                        Logger.Info("Upgrading database");
                        this.MigrateDatabase();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "There was a problem initializing the database. Exception: {0}", ex.Message);
            }
        }

        private bool IsDatabaseValid()
        {
            int count = 0;

            using (var conn = this.factory.GetConnection())
            {
                // HACK: in database version 11, the table Configurations was renamed to Configuration. When migrating from version 10 to 11, 
                // we still need to get the version from the original table as the new Configuration doesn't exist yet and is not found. 
                // At some later point in time, this try catch can be removed.
                count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Tracks'");
            }

            return count > 0;
        }

        public bool IsMigrationNeeded()
        {
            if (!this.IsDatabaseValid())
            {
                return true;
            }

            using (var conn = this.factory.GetConnection())
            {
                try
                {
                    this.userDatabaseVersion = Convert.ToInt32(conn.ExecuteScalar<string>("SELECT Value FROM General WHERE key = ?", GeneralRepositoryKeys.DBVersion.ToString()));
                    //=== ALEX DEBUG. USE "26" to force the update. "27" to avoid it. Reenable the Execute scalar
                    //userDatabaseVersion = 26;
                }
                catch (Exception)
                {
                    this.userDatabaseVersion = 0;
                }
            }

            return this.userDatabaseVersion < CURRENT_VERSION;
        }

        private void MigrateDatabase()
        {
            for (int i = this.userDatabaseVersion + 1; i <= CURRENT_VERSION; i++)
            {
                MethodInfo method = typeof(DbMigrator).GetTypeInfo().GetDeclaredMethod("Migrate" + i);
                if (method != null) method.Invoke(this, null);
            }

            using (var conn = this.factory.GetConnection())
            {
                conn.Execute("INSERT OR REPLACE INTO General (key, value) VALUES (?,?)", GeneralRepositoryKeys.DBVersion.ToString(), CURRENT_VERSION.ToString());
            }

            Logger.Info("Upgraded from database version {0} to {1}", this.userDatabaseVersion.ToString(), CURRENT_VERSION.ToString());
        }

        private void BackupDatabase()
        {
            try
            {
                string databaseFileCopy = String.Format($"{factory.DatabaseFile}.{userDatabaseVersion}.{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.old");
                if (File.Exists(databaseFileCopy)) 
                    File.Delete(databaseFileCopy);
                File.Copy(this.factory.DatabaseFile, databaseFileCopy);
            }
            catch (Exception ex)
            {
                Logger.Info("Could not create a copy of the database file. Exception: {0}", ex.Message);
            }
        }
    }
}
