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

    public class AlbumInfoIndexingQueueJob
    {
        public AlbumV Album;
    }

    class AlbumInfoIndexingQueue
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Queue<AlbumInfoIndexingQueueJob> _jobs = new Queue<AlbumInfoIndexingQueueJob>();
        private bool _delegateQueuedOrRunning = false;
        private IAlbumInfoProvider _infoProvider;

        public delegate void Notify(AlbumV requestedAlbum, AlbumInfoProviderData result);
        public event Notify InfoDownloaded;

        public AlbumInfoIndexingQueue(IAlbumInfoProvider infoProvider)
        {
            _infoProvider = infoProvider;
        }

        public void Enqueue(AlbumInfoIndexingQueueJob job)
        {
            lock (_jobs)
            {
                _jobs.Enqueue(job);
                Logger.Debug($"Enqueue (#{_jobs.Count}) - {job.Album.Name}");
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
                AlbumInfoIndexingQueueJob item;
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
                    InfoDownloaded(item.Album, RetrieveInfo(item.Album));
                }
                catch
                {
                    ThreadPool.UnsafeQueueUserWorkItem(ProcessQueuedItems, null);
                    InfoDownloaded(item.Album, new AlbumInfoProviderData() { result = InfoProviderResult.Fail_Generic });
                    throw;
                }
            }
        }
        private AlbumInfoProviderData RetrieveInfo(AlbumV album)
        {
            if (string.IsNullOrEmpty(album.Name))
                return null;
            Logger.Debug($"RetrieveInfo: Getting {album.Name} - {album.AlbumArtists}");
            return _infoProvider.Get(album.Name, string.IsNullOrEmpty(album.AlbumArtists) ? null : DataUtils.SplitAndTrimColumnMultiValue(album.AlbumArtists).ToArray());
        }
    }
}
