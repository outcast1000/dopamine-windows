using Digimezzo.Foundation.Core.Logging;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Services.Entities;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Dopamine.Services.Playback
{
    public class QueueManager<T> where T : new()
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<T> _playList = new List<T>();
        private List<int> _playlistOrder = new List<int>();
        private int? _position = null;
        private int _nextCounter = 0;
        private bool _shuffle = false;

        public QueueManager()
        {
            Shuffle = false;
            Loop = false;
        }

        public IList<T> Playlist { get { return _playList; } }

        public bool Shuffle { get { return _shuffle; } set
            {
                if (value != Shuffle)
                {
                    _shuffle = value;
                    _playlistOrder = CreatePlayListOrder();
                }
            }
        }

        public bool Loop { get; set; }

        public int? Position
        {
            get { return _position; }
            set
            {
                if (!_playList.IsNullOrEmpty())
                {
                    if (value >= 0 && value < _playList.Count())
                    {
                        _position = value;
                    }
                    else
                    {
                        Debug.Assert(false, "Not a legal position value");
                    }
                }
                else
                    Debug.Assert(false, "Not a legal position value (playlist is empty)");
            }
        }

        public T CurrentItem { get
            {
                if (_position.HasValue && _position.Value < _playList.Count)
                {
                    return _playList[_position.Value];
                }
                Logger.Info("CurrentItem is null");
                return default;
            }
        }

        public void Play(IList<T> items, int startAtIndex = -1)
        {
            Clear();
            if (items.IsNullOrEmpty())
                return;
            Enqueue(items);
            if (startAtIndex != -1)
            {
                if (Shuffle)
                {
                    // Modify PlayOrder to start at startAtIndex track as the first one
                    int idx = _playlistOrder.FindIndex(x => x == startAtIndex);// Find where is the startAtIndex item
                    int temp = _playlistOrder[0];// Swap it with the first one
                    _playlistOrder[0] = _playlistOrder[idx];
                    _playlistOrder[idx] = temp;
                    _position = _playlistOrder[0];
                }
                else 
                    _position = _playlistOrder[startAtIndex];
            }
            else 
                _position = _playlistOrder[0];
            
        }

        public void Enqueue(IList<T> items)
        {
            _playList.AddRange(items);
            _nextCounter = 0;
            _playlistOrder = CreatePlayListOrder();
        }

        public void EnqueueNext(IList<T> items)
        {
            if (!_position.HasValue)
                Enqueue(items);
            else
            {
                _playList.InsertRange(_position.HasValue ? 0 : _position.Value + 1, items);
                _nextCounter = 0;
                _playlistOrder = CreatePlayListOrder();
            }

        }

        public void Randomize()
        {
            _playList = _playList.Randomize();
            _playlistOrder = CreatePlayListOrder();
            _position = 0;
            _nextCounter = 0;
        }


        public void Clear()
        {
            _playList = new List<T>();
            _playlistOrder = new List<int>();
            _position = null;
            _nextCounter = 0;
        }

        public bool Next()
        {
            if (_playList.Count <= 0)
                return false;
            if (!_position.HasValue)
            {
                _position = 0;
                return true;
            }

            int playlistOrderIndex = _playlistOrder.FindIndex(x => x == _position);
            bool bHasMoreToPlay = Shuffle ? _nextCounter < _playList.Count - 1 : _position < _playList.Count - 1;

            if (Loop || bHasMoreToPlay)
            {
                if (playlistOrderIndex >= _playList.Count - 1)
                    playlistOrderIndex = 0;
                else
                    playlistOrderIndex++;
            }
            else
                return false;
            _nextCounter++;
            _position = _playlistOrder[playlistOrderIndex];
            return true;
        }

        public bool Prev()
        {
            int playlistOrderIndex = _playlistOrder.FindIndex(x => x == _position);
            bool bHasMoreToPlay = Shuffle ? _nextCounter > 0 : _position > 0;
            if (Loop || bHasMoreToPlay)
            {
                if (playlistOrderIndex <= 0)
                    playlistOrderIndex = _playList.Count - 1;
                else
                    playlistOrderIndex--;
            }
            else
                return false;
            _nextCounter--;
            _position = _playlistOrder[playlistOrderIndex];
            return true;

        }

        private List<int> CreatePlayListOrder()
        {
            if (_playList.IsNullOrEmpty())
                return null;
            if (Shuffle)
                return Enumerable.Range(0, _playList.Count).ToList().Randomize();
            return Enumerable.Range(0, _playList.Count).ToList();
        }


        public bool Reorder(IList<int> reorderedPositions)
        {
            if (reorderedPositions.Count != _playList.Count)
            {
                Logger.Warn($"ReorderTracks (count: {reorderedPositions.Count}) failed. It should have the same numbers of tracks as the original (count: {_playList.Count})");
                return false;
            }
            /*
            if (CurrentTrack != null)
            {
                // The playing track may have changed position. Find the new position and change it internally.
                // * ALEX TODO: This code may have issues if there the playlist have the same (current)track multiple times.
                //              It should be refactored to use IList<int> reorderedTracks
                int newPosition = reorderedPositions.IndexOf(CurrentTrack);
                if (newPosition == -1)
                {
                    Logger.Warn($"ReorderPlaylist (count: {reorderedTracks.Count}) failed. The current track does not exist in the reordered list");
                    return false;
                }
                _position = newPosition;
            }
            _playList.Clear();
            _playList.InsertRange(0, reorderedTracks);
            */
            return true;
        }
        // ALEX TODO: It should be refactored to use IList<int> reorderedTracks
        public bool ReorderTracks(IList<T> reorderedTracks)
        {
            if (reorderedTracks.Count != _playList.Count)
            {
                Logger.Warn($"ReorderTracks (count: {reorderedTracks.Count}) failed. It should have the same numbers of tracks as the original (count: {_playList.Count})");
                return false;
            }
            if (CurrentItem != null)
            {
                // The playing track may have changed position. Find the new position and change it internally.
                // * ALEX TODO: This code may have issues if there the playlist have the same (current)track multiple times.
                //              It should be refactored to use IList<int> reorderedTracks
                int newPosition = reorderedTracks.IndexOf(CurrentItem);
                if (newPosition == -1)
                {
                    Logger.Warn($"ReorderPlaylist (count: {reorderedTracks.Count}) failed. The current track does not exist in the reordered list");
                    return false;
                }
                _position = newPosition;
            }
            _playList.Clear();
            _playList.InsertRange(0, reorderedTracks);
            return true;
        }

        // Remove all tracks. Continue playing if active track is not affected. Next track if it is deleted. Nothing if the list is empty
        public bool Remove(IList<int> positions)
        {
            if (positions.Count > _playList.Count)
            {
                Logger.Warn($"RemoveTracks (count: {positions.Count}) failed. It should have at most the same numbers of tracks as the original (count: {_playList.Count})");
                return false;
            }
            T current = CurrentItem;
            positions = positions.OrderByDescending(p => p).ToList();
            foreach (int position in positions)
            {
                _playList.RemoveAt(position);
            }
            int idxCurrent = _playList.IndexOf(current);
            if (idxCurrent != -1)
            {
                _position = idxCurrent;
            }
            if (_playList.IsNullOrEmpty())
            {
                Clear();
                return true;
            }
            if (_position > _playList.Count - 1)
                _position = _playList.Count - 1;
            _playlistOrder = CreatePlayListOrder();
            return true;

        }
        public bool RemoveTracks(IList<T> removedTracks)
        {
            if (removedTracks.Count > _playList.Count)
            {
                Logger.Warn($"RemoveTracks (count: {removedTracks.Count}) failed. It should have at most the same numbers of tracks as the original (count: {_playList.Count})");
                return false;
            }
            T current = CurrentItem;
            foreach (var tvm in removedTracks)
            {
                bool ret = _playList.Remove(tvm);
                Debug.Assert(ret, "Track does not exist?. We will ignore this error for this version");
            }
            int idxCurrent = _playList.IndexOf(current);
            if (idxCurrent != -1)
            {
                _position = idxCurrent;
            }
            if (_position > _playList.Count - 1)
                _position = _playList.Count - 1;
            _playlistOrder = CreatePlayListOrder();
            return true;

        }

        public void UpdatePlaylistTrackInfo(IList<T> tracks)
        {
            Debug.Assert(tracks.Count == _playList.Count);
            _playList.Clear();
            _playList.InsertRange(0, tracks);
        }



    }
}
