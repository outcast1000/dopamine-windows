using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;

namespace Dopamine.Core.Helpers
{
    /// <summary>
    /// A folder watcher that is not too nervous when notifying of changes
    /// </summary>
    public class GentleFolderWatcher : IDisposable
    {
        private FileSystemWatcher watcher = new FileSystemWatcher();
        private Timer changeNotificationTimer = new Timer();

        public event EventHandler FolderChanged = delegate { };
        public event EventHandler<List<FileSystemEventArgs>> FilesChanged = delegate { };
        public event EventHandler<List<RenamedEventArgs>> FilesRenamed = delegate { };

        private List<FileSystemEventArgs> _pendingChangedFilePaths = new List<FileSystemEventArgs>();
        private List<RenamedEventArgs> _pendingRenamedItems = new List<RenamedEventArgs>();

        public GentleFolderWatcher(string folderPath, bool includeSubdirectories, int intervalMilliSeconds = 200)
        {
            // Timer
            this.changeNotificationTimer.Interval = intervalMilliSeconds;
            this.changeNotificationTimer.Elapsed += new ElapsedEventHandler(ChangeNotificationTimerElapsed);

            // Set the folder to watch
            this.watcher.Path = folderPath;

            // Watch subdirectories or not
            this.watcher.IncludeSubdirectories = includeSubdirectories;

            // Add event handlers
            this.watcher.Changed += new FileSystemEventHandler(OnChanged);
            this.watcher.Created += new FileSystemEventHandler(OnChanged);
            this.watcher.Deleted += new FileSystemEventHandler(OnChanged);
            this.watcher.Renamed += new RenamedEventHandler(OnRenamed);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            _pendingRenamedItems.Add(e);
            this.changeNotificationTimer.Stop();
            this.changeNotificationTimer.Start();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            _pendingChangedFilePaths.Add(e);
            this.changeNotificationTimer.Stop();
            this.changeNotificationTimer.Start();
          
        }

        private void ChangeNotificationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.changeNotificationTimer.Stop();

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_pendingChangedFilePaths.Count > 0)
                {
                    this.FilesChanged(this, _pendingChangedFilePaths.Select(x => x).ToList());
                    _pendingChangedFilePaths.Clear();
                }
                if (_pendingRenamedItems.Count > 0)
                {
                    this.FilesRenamed(this, _pendingRenamedItems.Select(x => x).ToList());
                    _pendingRenamedItems.Clear();
                }
                this.FolderChanged(this, new EventArgs());
            });
        }

        public bool IsSuspended { get => this.watcher.EnableRaisingEvents == false; }

        public void Suspend()
        {
            this.watcher.EnableRaisingEvents = false;
            this.changeNotificationTimer.Stop();
            _pendingChangedFilePaths.Clear();
            _pendingRenamedItems.Clear();
        }

        public void Resume()
        {
            this.watcher.EnableRaisingEvents = true;
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                   if(this.watcher != null)
                    {
                        this.watcher.EnableRaisingEvents = false;
                        this.changeNotificationTimer.Stop();
                        this.watcher.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}