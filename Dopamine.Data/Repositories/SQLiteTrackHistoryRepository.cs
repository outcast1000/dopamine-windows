using Dopamine.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public enum HistoryRepositoryActions
    {
        ExplicitSelected = 1,
        Played = 2,
        Skipped = 3,
        Rated = 4,
        Loved = 5
    };
    public class SQLiteTrackHistoryRepository: ITrackHistoryRepository
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ISQLiteConnectionFactory factory;

        public SQLiteTrackHistoryRepository(ISQLiteConnectionFactory factory)
        {
            this.factory = factory;
        }

        private class ActionStats
        {
            public long Actions {get; set;}
            public long LastDate { get; set; }
            public long FirstDate { get; set; }
    }

        private void AddAction(HistoryRepositoryActions action, long trackId, string jsonExtra = null)
        {
            using (var conn = this.factory.GetConnection())
            {
                try
                {
                    conn.BeginTransaction();
                    conn.Insert(new TrackHistory { TrackId = trackId, DateHappened = DateTime.Now.Ticks, HistoryActionId = (int) action, HistoryActionExtra = jsonExtra });
                    switch (action)
                    {
                        case HistoryRepositoryActions.ExplicitSelected:
                        case HistoryRepositoryActions.Played:
                        case HistoryRepositoryActions.Skipped:
                            ActionStats actionStats = conn.FindWithQuery<ActionStats>("SELECT COUNT(*) as Actions, MAX(date_happened) as LastDate, MIN(date_happened) as FirstDate FROM TrackHistory WHERE track_id=? AND history_action_id=? GROUP BY track_id", trackId, (int)action);
                            if (action == HistoryRepositoryActions.Played)
                            {
                                conn.Execute(@"
INSERT INTO TrackHistoryStats 
(track_id,plays,first_played,last_played) 
VALUES (?,?,?,?)
ON CONFLICT (track_id) DO UPDATE SET plays=excluded.plays, first_played=excluded.first_played, last_played=excluded.last_played"
                                , trackId, actionStats.Actions, actionStats.FirstDate, actionStats.LastDate);
                            }
                            else if (action == HistoryRepositoryActions.Skipped)
                            {
                                conn.Execute(@"
INSERT INTO TrackHistoryStats 
(track_id,skips) 
VALUES (?,?)
ON CONFLICT (track_id) DO UPDATE SET skips=excluded.skips"
                                , trackId, actionStats.Actions);
                            }
                            else if (action == HistoryRepositoryActions.ExplicitSelected)
                            {
                                conn.Execute(@"
INSERT OR IGNORE INTO TrackHistoryStats 
(track_id,explicits) 
VALUES (?,?)
ON CONFLICT (track_id) DO UPDATE SET skips=excluded.explicits"
                                , trackId, actionStats.Actions, actionStats.Actions, trackId);
                            }
                            break;
                        default:
                            break;
                    }
                    conn.Commit();

                }
                catch (Exception ex)
                {
                    conn.Rollback();
                    Logger.Error(ex, "HistoryRepositoryActions. Message {0}", ex.Message);
                }
            }
        }


        public void AddExplicitSelected(long trackId)
        {
            AddAction(HistoryRepositoryActions.ExplicitSelected, trackId);
        }
        public void AddPlayedAction(long trackId)
        {
            AddAction(HistoryRepositoryActions.Played, trackId, null);
        }
        public void AddSkippedAction(long trackId, string reason)
        {
            AddAction(HistoryRepositoryActions.Skipped, trackId, $"{{\"reason\":\"{reason}\"}}");
        }

        public void AddRateAction(long trackId, long rate)
        {
            AddAction(HistoryRepositoryActions.Rated, trackId, $"{{\"rate\":{rate}}}");
        }
        public void AddLoveAction(long trackId, bool love)
        {
            AddAction(HistoryRepositoryActions.Loved, trackId, $"{{\"love\":{love}}}");
        }


    }
}
