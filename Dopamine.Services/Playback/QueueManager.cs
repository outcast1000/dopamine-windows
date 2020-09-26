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
    public class QueueManager
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private List<TrackViewModel> _playList = new List<TrackViewModel>();
        private List<int> _playlistOrder = new List<int>();
        private int _position = -1;
        private int _nextCounter = 0;
        private bool _shuffle = false;

        public QueueManager()
        {
            Shuffle = false;
            Loop = false;
            _position = -1;
        }

        public IList<TrackViewModel> Playlist { get { return _playList; } }

        public void UpdatePlaylistTrackInfo(IList<TrackViewModel> tracks)
        {
            Debug.Assert(tracks.Count == _playList.Count);
            _playList.Clear();
            _playList.InsertRange(0, tracks);
        }

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

        public int Position
        {
            get { return _position; }
            set
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
        }

        public TrackViewModel CurrentTrack { get
            {
                if (_position >= 0 && _position < _playList.Count)
                {
                    return _playList[_position];
                }
                Logger.Info("CurrentTrack is null");
                return null;
            }
        }

        public void Play(IList<TrackViewModel> tracks, int startAtIndex = -1)
        {
            Clear();
            if (tracks.IsNullOrEmpty())
                return;
            Enqueue(tracks);
            if (startAtIndex != -1)
            {
                // Modify PlayList and set it as first track
                /*
                int idx = _playlistOrder.Find(x => x == startAtIndex);
                TrackViewModel temp = _playList[idx];
                _playList.RemoveAt(idx);
                _playList.Insert(0, temp);
                */
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

        public void Enqueue(IList<TrackViewModel> tracks)
        {
            _playList.AddRange(tracks);
            _nextCounter = 0;
            _playlistOrder = CreatePlayListOrder();
        }

        public void EnqueueNext(IList<TrackViewModel> tracks)
        {
            if (_position == -1)
                Enqueue(tracks);
            else
            {
                _playList.InsertRange(_position, tracks);
                _nextCounter = 0;
                _playlistOrder = CreatePlayListOrder();
            }

        }

        public void Clear()
        {
            _playList = new List<TrackViewModel>();
            _position = -1;
        }

        public bool Next()
        {
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


    }
}
