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

        private void AddAction(HistoryRepositoryActions action, long trackId, string jsonExtra = null)
        {
            using (var conn = this.factory.GetConnection())
            {
                try
                {
                    conn.Insert(new TrackHistory { TrackId = trackId, DateHappened = DateTime.Now.Ticks, HistoryActionId = (int) action, HistoryActionExtra = jsonExtra });
                }
                catch (Exception ex)
                {
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
        public void AddSkippedAction(long trackId, long position, long percentage, string reason)
        {
            AddAction(HistoryRepositoryActions.Skipped, trackId, $"{{\"position\":{position}, \"percentage\":{percentage}, \"reason\":\"{reason}\"}}");
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
