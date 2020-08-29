using Dopamine.Core.Extensions;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
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
        public SQLiteUpdateCollectionUnitOfWork(SQLiteConnection conn)
        {
            this.conn = conn;
            this.conn.BeginTransaction();
        }

        public void Dispose()
        {
            conn.Commit();
            conn.Dispose();
        }

        public bool AddMediaFile(MediaFileData mediaFileData, long folderId)
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
                return false;
            long track_id = GetLastInsertRowID();
            //Add the (Album) artists in an artistCollection list
            List<long> artistCollection = new List<long>();
            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.AlbumArtists))
            {
                foreach (string artist in mediaFileData.AlbumArtists)
                {
                    artistCollection.Add(GetArtistID(artist));
                }
            }
            //Add the artists
            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.Artists))
            {
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

            if (!string.IsNullOrEmpty(mediaFileData.Album))
            {
                long artistCollectionID = GetArtistCollectionID(artistCollection);
                long albumID = GetAlbumID(mediaFileData.Album, artistCollectionID, mediaFileData.AlbumImage);
                try
                {
                    conn.Insert(new TrackAlbum()
                    {
                        TrackId = track_id,
                        AlbumId = albumID,
                        TrackNumber = mediaFileData.TrackNumber,
                        DiscNumber = mediaFileData.DiscNumber,
                        TrackCount = mediaFileData.TrackCount,
                        DiscCount = mediaFileData.DiscCount
                    });
                }
                catch (SQLite.SQLiteException ex)
                {
                    Console.WriteLine("SQLiteException (Genres) {0}", ex.Message);
                }
            }


            if (!ListExtensions.IsNullOrEmpty<string>(mediaFileData.Genres))
            {
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
                        Console.WriteLine("SQLiteException (Genres) {0}", ex.Message);
                    }

                }
            }
            return true;
        }

        public bool AddIndexFailedMediaFile(string path, long folderId, string reason)
        {
            int added = conn.Insert(new Track2()
            {
                Path = path,
                FolderId = folderId
                
            });
            if (added == 0)
                return false;
            long track_id = GetLastInsertRowID();
            conn.Insert(new TrackIndexFailed()
            {
                TrackId = track_id,
                IndexingFailureReason = reason,
                DateHappened = DateTime.Now.Ticks
            });
            return true;
        }

        public bool UpdateMediaFile(TrackV trackV, MediaFileData mediaFileData)
        {
            throw new NotImplementedException();
        }



        private long GetArtistID(String entry)
        {
            List<long> ids = conn.QueryScalars<long>("SELECT * FROM Artists WHERE name=?", entry);
            if (ids.Count == 0)
            {
                try
                {
                    conn.Insert(new Artist() { Name = entry });
                }
                catch (SQLite.SQLiteException ex)
                {
                    Console.WriteLine("SQLiteException (GetArtistID) {0}", ex.Message);
                }
                return GetLastInsertRowID();
            }
            return ids[0];
        }

        private long GetArtistCollectionID(List<long> artistIDs)
        {
            string inString = string.Join(",", artistIDs);
            List<long> ids = conn.QueryScalars<long>(@"
SELECT DISTINCT artist_collection_id from ArtistCollectionsArtists 
INNER JOIN (
SELECT artist_collection_id as id, count(*) as c FROM ArtistCollectionsArtists
GROUP BY artist_collection_id) AGROUP ON ArtistCollectionsArtists.artist_collection_id = AGROUP.id
WHERE artist_id IN (" + inString + ") AND AGROUP.C=" + artistIDs.Count.ToString());

            if (ids.Count == 0)
            {
                conn.Insert(new ArtistCollection() { });
                long artist_collection_id = GetLastInsertRowID();
                foreach (long artistID in artistIDs)
                {
                    conn.Insert(new ArtistCollectionsArtist() { ArtistCollectionId = artist_collection_id, ArtistId = artistID }); ;
                }
                return artist_collection_id;
            }
            return ids[0];
        }




        private long GetAlbumID(String entry, long artist_collection_id, string albumImage)
        {
            List<long> ids;
            ids = conn.QueryScalars<long>("SELECT * FROM Albums WHERE name=? AND artist_collection_ID=?", entry, artist_collection_id);
            if (ids.Count == 0)
            {
                conn.Insert(new Album() { Name = entry, ArtistCollectionId = artist_collection_id });
                long albumID = GetLastInsertRowID();
                if (!string.IsNullOrEmpty(albumImage))
                {
                    conn.Insert(new AlbumThumbnail()
                    {
                        AlbumId = albumID,
                        Key = albumImage
                    });
                    conn.Insert(new AlbumImage()
                    {
                        AlbumId = albumID,
                        Key = albumImage,
                        Source = "Migration",
                        DateAdded = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });

                }
                return albumID;
            }
            return ids[0];
        }
        private long GetGenreID(String entry)
        {
            List<long> ids = conn.QueryScalars<long>("SELECT * FROM Genres WHERE name=?", entry);
            if (ids.Count == 0)
            {
                conn.Insert(new Genre() { Name = entry });
                return GetLastInsertRowID();
            }
            return ids[0];
        }

        private long GetLastInsertRowID()
        {
            SQLiteCommand cmdLastRow = conn.CreateCommand(@"select last_insert_rowid()");
            return cmdLastRow.ExecuteScalar<long>();
        }
    }
}
