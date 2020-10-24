using System;
using System.Threading;

namespace Dopamine.Core.Alex
{
    /* USAGE EXAMPLE
        Function Reentry protection with 'Try'. Otherwise just use lock()
        object _lockObject = new object();

        public async virtual void ProtectedFunction()
        {
            using (var tryLock = new TryLock(_lockObject))
            {
                if (!tryLock.HasLock)
                {
                    Logger.Warn("EXIT RefreshCoverArtAsync (Reentrance lock)");
                    return;
                }
                ...
            }
        }

    */

    public class TryLock : IDisposable
    {
        private object locked;
        public bool HasLock { get; private set; }
        public TryLock(object obj)
        {
            if (Monitor.TryEnter(obj))
            {
                HasLock = true;
                locked = obj;
            }

        }
        public void Dispose()
        {
            if (HasLock)
            {
                Monitor.Exit(locked);
                locked = null;
                HasLock = false;
            }
        }
    }
}
