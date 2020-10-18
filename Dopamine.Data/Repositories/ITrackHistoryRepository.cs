using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface ITrackHistoryRepository
    {
        void AddExecuted(long trackId);
        void AddPlayedAction(long trackId);
        void AddSkippedAction(long trackId, string reason);
        void AddRateAction(long trackId, long rate);
        void AddLoveAction(long trackId, bool love);

    }
}
