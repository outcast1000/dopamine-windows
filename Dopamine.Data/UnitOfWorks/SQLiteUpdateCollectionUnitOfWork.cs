﻿using Dopamine.Core.Alex;
using Dopamine.Core.Extensions;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Data.Repositories;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.UnitOfWorks
{
    public class SQLiteUpdateCollectionUnitOfWork: IUpdateCollectionUnitOfWork
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private SQLiteConnection conn;
        private SQLiteTrackVRepository sQLiteTrackVRepository;
        private SQLiteInfoRepository sQLiteImageRepository;
        private bool bSharedConnection;
        public SQLiteUpdateCollectionUnitOfWork(SQLiteConnection conn, bool bSharedConnection)
        {
            this.bSharedConnection = bSharedConnection;
            this.conn = conn;
            if (!bSharedConnection)
                this.conn.BeginTransaction();
            sQLiteTrackVRepository = new SQLiteTrackVRepository(null);
            sQLiteImageRepository = new SQLiteInfoRepository(null);
            sQLiteTrackVRepository.SetSQLiteConnection(conn);
            sQLiteImageRepository.SetSQLiteConnection(conn);

        }

        public void Dispose()
        {
            if (!bSharedConnection)
            {
                conn.Commit();
                conn.Dispose();
            }
        }

        public AddMediaFileResult AddMediaFile(MediaFileData mediaFileData, long? folderId)
        {
            AddMediaFileResult result = new AddMediaFileResult() { Success=false };
            try
            {
                int added = conn.Insert(new Track2()
                {
                    Name = mediaFileData.Name.Trim(),
                    Path = mediaFileData.Path.Trim(),
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
                    if (string.IsNullOrWhiteSpace(artist))
                        continue;
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
                    if (string.IsNullOrWhiteSpace(artist))
                        continue;
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

            if (!string.IsNullOrWhiteSpace(mediaFileData.Album))
            {
                if (artistCollection.IsNullOrEmpty())
                {
                    result.AlbumId = GetAlbumID(mediaFileData.Album, null);
                }
                else
                {
                    long artistCollectionID = GetArtistCollectionID(artistCollection);
                    result.AlbumId = GetAlbumID(mediaFileData.Album, artistCollectionID);
                }
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
                    if (string.IsNullOrWhiteSpace(genre))
                        continue;
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

            if (mediaFileData.Lyrics != null && mediaFileData.Lyrics.Text != null && mediaFileData.Lyrics.Text.Length > 0)
            {
                try
                {
                    int ret = conn.Insert(new TrackLyrics()
                    {
                        TrackId = (long)result.TrackId,
                        Lyrics = mediaFileData.Lyrics.Text,
                        Origin = mediaFileData.Lyrics.Source,
                        OriginType = OriginType.File,
                        DateAdded = DateTime.Now.Ticks,
                        Language = mediaFileData.Lyrics.Language
                    });
                    Debug.Assert(ret > 0, "Insert Failed");

                }
                catch (SQLiteException ex)
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
            long? folder_id = trackV.FolderID;
            int success = conn.Update(new Track2()
            {
                Id = track_id,
                Name = mediaFileData.Name.Trim(),
                Path = mediaFileData.Path.Trim(),
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
                    if (string.IsNullOrWhiteSpace(artist))
                        continue;
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
                    if (string.IsNullOrWhiteSpace(artist))
                        continue;
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
            if (!string.IsNullOrWhiteSpace(mediaFileData.Album))
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
                    if (string.IsNullOrWhiteSpace(genre))
                        continue;
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
            if (mediaFileData.Lyrics != null && mediaFileData.Lyrics.Text != null && mediaFileData.Lyrics.Text.Length > 0)
            {
                try
                {
                    int ret = conn.Insert(new TrackLyrics()
                    {
                        TrackId = track_id,
                        Lyrics = mediaFileData.Lyrics.Text,
                        Origin = mediaFileData.Lyrics.Source,
                        OriginType = OriginType.File,
                        DateAdded = DateTime.Now.Ticks,
                        Language = mediaFileData.Lyrics.Language
                    });
                    Debug.Assert(ret > 0, "Insert Failed");
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
            return sQLiteTrackVRepository.GetTrackWithPath(path, QueryOptions.IncludeAll());
        }

        private long GetArtistID(String entry)
        {
            //=== Normalization. Clean up "the " from artists
            if (entry.ToLower().StartsWith("the "))
                entry = entry.Substring(4);
            entry = entry.Trim();
            //=== END Normalization
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

        private long GetAlbumID(String entry, long? artist_collection_id)
        {
            //=== Normalization
            entry = entry.Trim();
            //=== Normalization End
            long? id;
            if (artist_collection_id.HasValue)
                id = conn.ExecuteScalar<long?>("SELECT id FROM Albums WHERE name=? AND artist_collection_ID=?", entry, artist_collection_id);
            else
                id = conn.ExecuteScalar<long?>("SELECT id FROM Albums WHERE name=? AND artist_collection_ID is null", entry);

            if (id.HasValue)
                return id.Value;
            try
            {
                if (artist_collection_id.HasValue)
                    conn.Insert(new Album() { Name = entry, ArtistCollectionId = artist_collection_id.Value });
                else
                    conn.Insert(new Album() { Name = entry });

                return GetLastInsertRowID();
            }
            catch (SQLite.SQLiteException ex)
            {
                Debug.WriteLine(String.Format("SQLiteException (GetAlbumID) {0}", ex.Message));
                throw new Exception(String.Format("SQLiteException (GetAlbumID) '{0}' ex:{1}", entry, ex.Message));
            }

        }
        private long GetGenreID(String entry)
        {
            //=== Normalization
            entry = entry.Trim();
            //=== Normalization End
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

    }


}
