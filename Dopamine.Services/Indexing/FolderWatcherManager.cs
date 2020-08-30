using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Helpers;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Dopamine.Services.Indexing
{
    internal class FolderWatcherManager
    {
        private IFolderVRepository folderVRepository;
        private IList<GentleFolderWatcher> watchers = new List<GentleFolderWatcher>();

        public event EventHandler FoldersChanged = delegate { };

        public FolderWatcherManager(IFolderVRepository folderVRepository)
        {
            this.folderVRepository = folderVRepository;
        }

        private void Watcher_FolderChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.FoldersChanged(this, new EventArgs());
            });
        }

        public async Task StartWatchingAsync()
        {
            await this.StopWatchingAsync();

            List<FolderV> folders = folderVRepository.GetFolders();

            foreach (FolderV fol in folders)
            {
                if (Directory.Exists(fol.Path))
                {
                    try
                    {
                        // When the folder exists, but access is denied, creating the FileSystemWatcher throws an exception.
                        var watcher = new GentleFolderWatcher(fol.Path, true, 2000);
                        watcher.FolderChanged += Watcher_FolderChanged;
                        this.watchers.Add(watcher);
                        watcher.Resume();
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error($"Could not watch folder '{fol.Path}', even though it exists. Please check folder permissions. Exception: {ex.Message}");
                    }
                }
            }
        }

        public async Task StopWatchingAsync()
        {
            if (this.watchers.Count == 0)
            {
                return;
            }

            await Task.Run(() =>
            {
                for (int i = this.watchers.Count - 1; i >= 0; i--)
                {
                    this.watchers[i].FolderChanged -= Watcher_FolderChanged;
                    this.watchers[i].Dispose();
                    this.watchers.RemoveAt(i);
                }
            });
        }
    }
}