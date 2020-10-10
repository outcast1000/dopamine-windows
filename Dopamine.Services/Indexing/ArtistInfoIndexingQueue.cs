using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Providers;
using Dopamine.Data.UnitOfWorks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dopamine.Services.Indexing
{

    public class ArtistInfoIndexingQueueJob
    {
        public ArtistV Artist;
    }

    class ArtistInfoIndexingQueue
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Queue<ArtistInfoIndexingQueueJob> _jobs = new Queue<ArtistInfoIndexingQueueJob>();
        private bool _delegateQueuedOrRunning = false;
        private IArtistInfoProvider _infoProvider;

        public delegate void Notify(ArtistV requestedArtist, ArtistInfoProviderData result);
        public event Notify InfoDownloaded;

        public ArtistInfoIndexingQueue(IArtistInfoProvider infoProvider)
        {
            _infoProvider = infoProvider;
        }

        public void Enqueue(ArtistInfoIndexingQueueJob job)
        {
            lock (_jobs)
            {
                _jobs.Enqueue(job);
                if (!_delegateQueuedOrRunning)
                {
                    _delegateQueuedOrRunning = true;
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
                }
            }
        }

        private void ProcessQueuedItems(object ignored)
        {
            while (true)
            {
                ArtistInfoIndexingQueueJob item;
                lock (_jobs)
                {
                    if (_jobs.Count == 0)
                    {
                        _delegateQueuedOrRunning = false;
                        break;
                    }

                    item = _jobs.Dequeue();
                }

                try
                {
                    InfoDownloaded(item.Artist, RetrieveInfo(item.Artist));
                }
                catch
                {
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
					InfoDownloaded(item.Artist, null);
                    throw;
                }
            }
        }
        private ArtistInfoProviderData RetrieveInfo(ArtistV artist)
        {
            if (string.IsNullOrEmpty(artist.Name))
                return null;
            Logger.Debug($"RetrieveInfo. Getting {artist.Name}");
			return _infoProvider.Get(artist.Name);
        }

    }
}
