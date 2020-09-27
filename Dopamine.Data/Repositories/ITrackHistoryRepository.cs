﻿using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface ITrackHistoryRepository
    {
        void AddExplicitSelected(long trackId);
        void AddPlayedAction(long trackId, long position, long percentage);
        void AddSkippedAction(long trackId, long position, long percentage);
        void AddRateAction(long trackId, long rate);
        void AddLoveAction(long trackId, bool love);

    }
}
