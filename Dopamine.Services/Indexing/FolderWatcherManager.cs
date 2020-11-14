using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Helpers;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Dopamine.Services.Indexing
{
    internal class FolderWatcherManager
    {
        private IFolderVRepository folderVRepository;
        private Dictionary<GentleFolderWatcher, long> _watchers = new Dictionary<GentleFolderWatcher, long>();

        public event EventHandler<long> FoldersChanged = delegate { };
        public delegate void FolderWatcherManagerHandler<TEventArgs>(object sender, long id, TEventArgs e);
        public event FolderWatcherManagerHandler<List<FileSystemEventArgs>> FilesChanged = delegate { };
        public event FolderWatcherManagerHandler<List<RenamedEventArgs>> FilesRenamed = delegate { };

        public FolderWatcherManager(IFolderVRepository folderVRepository)
        {
            this.folderVRepository = folderVRepository;
        }

        private void Watcher_FolderChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.FoldersChanged(this, _watchers[(GentleFolderWatcher)sender]);
            });
        }

        public bool IsSuspended
        {
            get { return _watchers.Count == 0 ? true : _watchers.First().Key.IsSuspended; }
        }

        public void Suspend()
        {
            foreach (var watcherKV in _watchers)
            {
                watcherKV.Key.Suspend();
            }
        }

        public void Resume()
        {
            foreach (var watcherKV in _watchers)
            {
                watcherKV.Key.Resume();
            }
        }



        public async Task StartWatchingAsync()
        {
            await this.StopWatchingAsync();
            List<FolderV> folders = folderVRepository.GetShownFolders();
            foreach (FolderV fol in folders)
            {
                if (Directory.Exists(fol.Path))
                {
                    try
                    {
                        // When the folder exists, but access is denied, creating the FileSystemWatcher throws an exception.
                        var watcher = new GentleFolderWatcher(fol.Path, true, 2000);
                        watcher.FolderChanged += Watcher_FolderChanged;
                        watcher.FilesRenamed += Watcher_FilesRenamed;
                        watcher.FilesChanged += Watcher_FilesChanged;
                        _watchers[watcher] = fol.Id;
                        watcher.Resume();
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error($"Could not watch folder '{fol.Path}', even though it exists. Please check folder permissions. Exception: {ex.Message}");
                    }
                }
            }
        }

        private void Watcher_FilesChanged(object sender, List<FileSystemEventArgs> e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.FilesChanged(this, _watchers[(GentleFolderWatcher)sender], e);
            });
        }

        private void Watcher_FilesRenamed(object sender, List<RenamedEventArgs> e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.FilesRenamed(this, _watchers[(GentleFolderWatcher)sender], e);
            });
        }

        public async Task StopWatchingAsync()
        {
            if (this._watchers.Count == 0)
                return;
            await Task.Run(() =>
            {
                foreach (var watcherKV in _watchers)
                {
                    watcherKV.Key.FolderChanged -= Watcher_FolderChanged;
                    watcherKV.Key.Dispose();
                }
                _watchers.Clear();
            });
        }
    }
}