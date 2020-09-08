using Dopamine.Core.Alex;
using Dopamine.Core.Extensions;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Data.Repositories;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.UnitOfWorks
{
    public class SQLiteUpdateCollectionUnitOfWork: IUpdateCollectionUnitOfWork
    {
        private SQLiteConnection conn;
        private SQLiteTrackVRepository sQLiteTrackVRepository;
        private SQLiteAlbumImageRepository sQLiteAlbumImageRepository;
        private bool bDisposeConnection;
        public SQLiteUpdateCollectionUnitOfWork(SQLiteConnection conn, bool bDisposeConnection)
        {
            this.bDisposeConnection = bDisposeConnection;
            this.conn = conn;
            this.conn.BeginTransaction();
            sQLiteTrackVRepository = new SQLiteTrackVRepository(null);
            sQLiteAlbumImageRepository = new SQLiteAlbumImageRepository(null);
            sQLiteTrackVRepository.SetSQLiteConnection(conn);
            sQLiteAlbumImageRepository.SetSQLiteConnection(conn);

        }

        public void Dispose()
        {
            conn.Commit();
            if (bDisposeConnection)
                conn.Dispose();
        }

        public AddMediaFileResult AddMediaFile(MediaFileData mediaFileData, long folderId)
        {
            AddMediaFileResult result = new AddMediaFileResult() { Success=false };
            try
            {
                int added = conn.Insert(new Track2()
                {
                    Name = mediaFileData.Name,
                    Path = mediaFileData.Path,
                    FolderId = folderId,
                    Filesize = mediaFileData.Filesize,
                    Bitrate = mediaFileData.Bitrate,
                    Samplerate = mediaFileData.Samplerate,
                    Duration = mediaFileData.Duration,
                    Year = mediaFileData.Year,
                    Language = mediaFileData.Language,
                    DateAdded = mediaFileData.DateAdded,
                    Rating = mediaFileData.Rating,//Should you take it from the file?
                    Love = mediaFileData.Love,
                    DateFileCreated = mediaFileData.DateFileCreated,
                    DateFileModified = mediaFileData.DateFileModified,
                    DateFileDeleted = mediaFileData.DateFileDeleted,
                    DateIgnored = mediaFileData.DateIgnored
                });
                if (added == 0)
                    return result;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(String.Format("AddMediaFile / Track2 Path: {0} Exce: {1}", mediaFileData.Path, ex.Message));
                return result;
            }
            result.TrackId = GetLastInsertRowID();
            //Add the (Album) artists in an artistCollection list
            List<long> artistCollection = new List<long>();
            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.AlbumArtists))
            {
                mediaFileData.AlbumArtists = mediaFileData.AlbumArtists.Distinct().ToList();
                foreach (string artist in mediaFileData.AlbumArtists)
                {
                    artistCollection.Add(GetArtistID(artist));
                }
            }
            //Add the artists
            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.Artists))
            {
                bool bUseArtistForAlbumArtistCollection = artistCollection.Count == 0;
                mediaFileData.Artists = mediaFileData.Artists.Distinct().ToList();
                foreach (string artist in mediaFileData.Artists)
                {
                    long curID = GetArtistID(artist);
                    if (bUseArtistForAlbumArtistCollection)
                        artistCollection.Add(curID);
                    conn.Insert(new TrackArtist()
                    {
                        TrackId = (long) result.TrackId,
                        ArtistId = curID,
                        ArtistRoleId = 1
                    });
                }
            }

            if (!string.IsNullOrEmpty(mediaFileData.Album))
            {
                long artistCollectionID = GetArtistCollectionID(artistCollection);
                result.AlbumId = GetAlbumID(mediaFileData.Album, artistCollectionID);
                try
                {
                    conn.Insert(new TrackAlbum()
                    {
                        TrackId = (long) result.TrackId,
                        AlbumId = (long) result.AlbumId,
                        TrackNumber = mediaFileData.TrackNumber,
                        DiscNumber = mediaFileData.DiscNumber,
                        TrackCount = mediaFileData.TrackCount,
                        DiscCount = mediaFileData.DiscCount
                    });
                }
                catch (SQLite.SQLiteException ex)
                {
                    Debug.WriteLine(String.Format("SQLiteException (Genres) {0}", ex.Message));
                }
            }


            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.Genres))
            {
                mediaFileData.Genres = mediaFileData.Genres.Distinct().ToList();
                foreach (string genre in mediaFileData.Genres)
                {
                    long curID = GetGenreID(genre);
                    try
                    {
                        conn.Insert(new TrackGenre()
                        {
                            TrackId = (long) result.TrackId,
                            GenreId = curID
                        });
                    }
                    catch (SQLite.SQLiteException ex)
                    {
                        Debug.WriteLine(String.Format("SQLiteException (Genres) {0}", ex.Message));
                    }

                }
            }

            if (mediaFileData.Lyrics != null)
            {
                try
                {
                    conn.Insert(new TrackLyrics()
                    {
                        TrackId = (long)result.TrackId,
                        Lyrics = mediaFileData.Lyrics.Text,
                        Source = mediaFileData.Lyrics.Source,
                        DateAdded = DateTime.Now.Ticks,
                        Language = mediaFileData.Lyrics.Language
                    }); ;
                }
                catch (SQLite.SQLiteException ex)
                {
                    Debug.WriteLine(String.Format("SQLiteException (TrackLyrics) {0}", ex.Message));
                }
            }
            result.Success = true;
            return result;
        }

        public UpdateMediaFileResult UpdateMediaFile(TrackV trackV, MediaFileData mediaFileData)
        {
            UpdateMediaFileResult updateMediaFileResult = new UpdateMediaFileResult() { Success = false };
            long track_id = trackV.Id;
            long folder_id = trackV.FolderID;
            int success = conn.Update(new Track2()
            {
                Id = track_id,
                Name = mediaFileData.Name,
                Path = mediaFileData.Path,
                FolderId = folder_id,
                Filesize = mediaFileData.Filesize,
                Bitrate = mediaFileData.Bitrate,
                Samplerate = mediaFileData.Samplerate,
                Duration = mediaFileData.Duration,
                Year = mediaFileData.Year,
                Language = mediaFileData.Language,
                DateAdded = mediaFileData.DateAdded,
                Rating = mediaFileData.Rating,
                Love = mediaFileData.Love,
                DateFileCreated = mediaFileData.DateFileCreated,
                DateFileModified = mediaFileData.DateFileModified,
                DateFileDeleted = mediaFileData.DateFileDeleted,
                DateIgnored = mediaFileData.DateIgnored
            });
            if (success == 0)
                return updateMediaFileResult;
            //Add the (Album) artists in an artistCollection list
            List<long> artistCollection = new List<long>();
            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.AlbumArtists))
            {
                mediaFileData.AlbumArtists = mediaFileData.AlbumArtists.Distinct().ToList();
                foreach (string artist in mediaFileData.AlbumArtists)
                {
                    artistCollection.Add(GetArtistID(artist));
                }
            }
            //Add the artists
            conn.Execute(String.Format("DELETE FROM TrackArtists WHERE track_id={0}", track_id));
            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.Artists))
            {
                mediaFileData.Artists = mediaFileData.Artists.Distinct().ToList();
                bool bUseArtistForAlbumArtistCollection = artistCollection.Count == 0;

                foreach (string artist in mediaFileData.Artists)
                {
                    long curID = GetArtistID(artist);
                    if (bUseArtistForAlbumArtistCollection)
                        artistCollection.Add(curID);
                    conn.Insert(new TrackArtist()
                    {
                        TrackId = track_id,
                        ArtistId = curID,
                        ArtistRoleId = 1
                    });
                }
            }

            conn.Execute(String.Format("DELETE FROM TrackAlbums WHERE track_id={0}", track_id));
            if (!string.IsNullOrEmpty(mediaFileData.Album))
            {
                long artistCollectionID = GetArtistCollectionID(artistCollection);
                updateMediaFileResult.AlbumId = GetAlbumID(mediaFileData.Album, artistCollectionID);
                try
                {
                    conn.Insert(new TrackAlbum()
                    {
                        TrackId = track_id,
                        AlbumId = (long) updateMediaFileResult.AlbumId,
                        TrackNumber = mediaFileData.TrackNumber,
                        DiscNumber = mediaFileData.DiscNumber,
                        TrackCount = mediaFileData.TrackCount,
                        DiscCount = mediaFileData.DiscCount
                    });
                }
                catch (SQLite.SQLiteException ex)
                {
                    Debug.WriteLine(String.Format("SQLiteException (Genres) {0}", ex.Message));
                }
            }


            conn.Execute(String.Format("DELETE FROM TrackGenres WHERE track_id={0}", track_id));
            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.Genres))
            {
                mediaFileData.Genres = mediaFileData.Genres.Distinct().ToList();
                foreach (string genre in mediaFileData.Genres)
                {
                    long curID = GetGenreID(genre);
                    try
                    {
                        conn.Insert(new TrackGenre()
                        {
                            TrackId = track_id,
                            GenreId = curID
                        });
                    }
                    catch (SQLite.SQLiteException ex)
                    {
                        Debug.WriteLine(String.Format("SQLiteException (Genres) {0}", ex.Message));
                    }

                }
            }

            conn.Execute(String.Format("DELETE FROM TrackLyrics WHERE track_id={0}", track_id));
            if (mediaFileData.Lyrics != null)
            {
                try
                {
                    conn.Insert(new TrackLyrics()
                    {
                        TrackId = track_id,
                        Lyrics = mediaFileData.Lyrics.Text,
                        Source = mediaFileData.Lyrics.Source,
                        DateAdded = DateTime.Now.Ticks,
                        Language = mediaFileData.Lyrics.Language
                    }); ;
                }
                catch (SQLite.SQLiteException ex)
                {
                    Debug.WriteLine(String.Format("SQLiteException (TrackLyrics) {0}", ex.Message));
                }
            }
            updateMediaFileResult.Success = true;
            return updateMediaFileResult;
        }

        public TrackV GetTrackWithPath(string path)
        {
            return sQLiteTrackVRepository.GetTrackWithPath(path, new QueryOptions() {WhereDeleted = QueryOptionsBool.Ignore, WhereIgnored = QueryOptionsBool.Ignore, WhereVisibleFolders = QueryOptionsBool.Ignore, UseLimit=false });
        }


        public bool AddAlbumImage(AlbumImage image)
        {
            Debug.Assert(image.Id == 0);
            Debug.Assert(image.AlbumId > 0);
            Debug.Assert(image.Location.Length > 10);
            Debug.Print("AddAlbumImage {0} - {1}", image.Id, image.Location);
            if (image.IsPrimary == true)
            {
                int ret = conn.Execute("DELETE FROM AlbumImages WHERE album_id=? AND is_primary=1", image.AlbumId);
                Debug.Print("AddAlbumImage albumImageId: Deleted preivous primary: {0} records", ret);

            }
            long albumImageId = GetAlbumImageID(image);
            Debug.Print("AddAlbumImage albumImageId: {0} albumId: {1} location: {2}", albumImageId, image.AlbumId, image.Location);
            return true;
        }

        public bool RemoveAlbumImage(long album_id, string location)
        {
            int deletions = conn.Execute("DELETE FROM AlbumImages WHERE album_id=? AND location=?", album_id, location);
            if (deletions == 0)
                return false;
            return true;
        }
        public bool RemoveAllAlbumImages(long album_id)
        {
            conn.Execute("DELETE FROM AlbumImages WHERE album_id = ?", album_id);
            return true;
        }

        public bool SetAlbumImageAsPrimary(long album_image_id, bool bSetAsPrimary)
        {
            Debug.Print("SetAlbumImageAsPrimary album_image_id: {0}", album_image_id);
            if (bSetAsPrimary)
                conn.Execute("UPDATE AlbumImages SET is_primary = 1 WHERE album_image_id = ?", album_image_id);
            else 
                conn.Execute("UPDATE AlbumImages SET is_primary = NULL WHERE album_image_id = ?", album_image_id);
            return true;
        }


        private long GetArtistID(String entry)
        {
            long? id = conn.ExecuteScalar<long?>("SELECT id FROM Artists WHERE name=?", entry);
            if (id != null)
                return (long)id;
            try
            {
                conn.Insert(new Artist() { Name = entry });
                return GetLastInsertRowID();
            }
            catch (SQLite.SQLiteException ex)
            {
                Debug.WriteLine(String.Format("SQLiteException (GetArtistID) {0}", ex.Message));
                throw new Exception(String.Format("SQLiteException (GetArtistID) '{0}' ex:{1}", entry, ex.Message));
            }
        }

        private long GetArtistCollectionID(List<long> artistIDs)
        {
            string inString = string.Join(",", artistIDs);
            long? id = conn.ExecuteScalar<long?>(@"
SELECT DISTINCT artist_collection_id from ArtistCollectionsArtists 
INNER JOIN (
SELECT artist_collection_id as id, count(*) as c FROM ArtistCollectionsArtists
GROUP BY artist_collection_id) AGROUP ON ArtistCollectionsArtists.artist_collection_id = AGROUP.id
WHERE artist_id IN (" + inString + ") AND AGROUP.C=" + artistIDs.Count.ToString());

            if (id != null)
                return (long)id;
            conn.Insert(new ArtistCollection() { });
            long artist_collection_id = GetLastInsertRowID();
            foreach (long artistID in artistIDs)
            {
                conn.Insert(new ArtistCollectionsArtist() { ArtistCollectionId = artist_collection_id, ArtistId = artistID }); ;
            }
            return artist_collection_id;
        }




        private long GetAlbumID(String entry, long artist_collection_id)
        {
            long? id = conn.ExecuteScalar<long?>("SELECT id FROM Albums WHERE name=? AND artist_collection_ID=?", entry, artist_collection_id);
            if (id != null)
                return (long) id;
            try
            {
                conn.Insert(new Album() { Name = entry, ArtistCollectionId = artist_collection_id });
                return GetLastInsertRowID();
            }
            catch (SQLite.SQLiteException ex)
            {
                Debug.WriteLine(String.Format("SQLiteException (GetAlbumID) {0}", ex.Message));
                throw new Exception(String.Format("SQLiteException (GetAlbumID) '{0}' ex:{1}", entry, ex.Message));
            }

        }

        private long GetAlbumImageID(AlbumImage image)
        {
            long? id = conn.ExecuteScalar<long?>("SELECT id FROM AlbumImages WHERE album_id=? AND location=?", image.AlbumId, image.Location);
            //long? id = conn.ExecuteScalar<long?>("SELECT id FROM AlbumImages WHERE path=?", image.Path);
            if (id != null)
                return (long) id;
            try
            {
                image.DateAdded = DateTime.Now.Ticks;
                conn.Insert(image);
                return GetLastInsertRowID();
            }
            catch (SQLite.SQLiteException ex)
            {
                string err = String.Format("SQLiteException (GetAlbumImageID) '{0}' ex:{1}", image.Location, ex.Message);
                Debug.WriteLine(err);
                throw new Exception(err);
            }
        }

        private long GetArtistImageID(ArtistImage image)
        {
            long? id = conn.ExecuteScalar<long?>("SELECT id FROM ArtistImages WHERE artist_id=? AND location=?", image.ArtistId, image.Location);
            //long? id = conn.ExecuteScalar<long?>("SELECT id FROM AlbumImages WHERE path=?", image.Path);
            if (id != null)
                return (long)id;
            try
            {
                image.DateAdded = DateTime.Now.Ticks;
                conn.Insert(image);
                return GetLastInsertRowID();
            }
            catch (SQLite.SQLiteException ex)
            {
                string err = String.Format("SQLiteException (GetArtistImageID) '{0}' ex:{1}", image.Location, ex.Message);
                Debug.WriteLine(err);
                throw new Exception(err);
            }
        }

        private long GetGenreID(String entry)
        {
            long? id = conn.ExecuteScalar<long?>("SELECT id FROM Genres WHERE name=?", entry);
            if (id != null)
                return (long) id;
            try
            {
                conn.Insert(new Genre() { Name = entry });
                return GetLastInsertRowID();
            }
            catch (SQLite.SQLiteException ex)
            {
                string err = String.Format("SQLiteException (GetGenreID) '{0}' ex:{1}", entry, ex.Message);
                Debug.WriteLine(err);
                throw new Exception(err);
            }
        }

        private long GetLastInsertRowID()
        {
            SQLiteCommand cmdLastRow = conn.CreateCommand(@"select last_insert_rowid()");
            return cmdLastRow.ExecuteScalar<long>();
        }


        public bool AddArtistImage(ArtistImage image)
        {
            Debug.Assert(image.Id == 0);
            Debug.Assert(image.ArtistId > 0);
            Debug.Assert(image.Location.Length > 10);
            Debug.Print("AddArtistImage {0} - {1}", image.Id, image.Location);
            if (image.IsPrimary == true)
            {
                int ret = conn.Execute("DELETE FROM ArtistImages WHERE artist_id=? AND is_primary=1", image.ArtistId);
                Debug.Print("AddArtistImage Deleted previous primary: {0} records", ret);

            }
            long imageId = GetArtistImageID(image);
            Debug.Print("AddAlbumImage imageId: {0} artistId: {1} location: {2}", imageId, image.ArtistId, image.Location);
            return true;
        }

        public bool RemoveArtistImage(long artist_id, string location)
        {
            int deletions = conn.Execute("DELETE FROM ArtistImages WHERE artist_id=? AND location=?", artist_id, location);
            if (deletions == 0)
                return false;
            return true;
        }

        public bool RemoveAllArtistImages(long artist_id)
        {
            conn.Execute("DELETE FROM ArtistImages WHERE artist_id = ?", artist_id);
            return true;
        }

        public bool SetArtistImageAsPrimary(long image_id, bool bIsPrimary)
        {
            Debug.Print("SetArtistImageAsPrimary image_id: {0}", image_id);
            if (bIsPrimary)
                conn.Execute("UPDATE ArtistImages SET is_primary = 1 WHERE artist_image_id = ?", image_id);
            else
                conn.Execute("UPDATE ArtistImages SET is_primary = NULL WHERE artist_image_id = ?", image_id);
            return true;
        }
    }


}
