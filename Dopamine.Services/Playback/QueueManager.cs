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

        public QueueManager()
        {
            Shuffle = false;
            Loop = false;
            _position = -1;
        }

        public IList<TrackViewModel> Playlist { get { return _playList; } }

        public bool Shuffle { get; set; }

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
                int idx = _playlistOrder.Find(x => x == startAtIndex);
                TrackViewModel temp = _playList[idx];
                _playList.RemoveAt(idx);
                _playList.Insert(0, temp);
                // Modify PlayOrder to start at 0 track (even in Shuffle)
                idx = _playlistOrder.FindIndex(x => x == 0);// Find where is the 0 item
                _playlistOrder[idx] = _playlistOrder[0]; // Swap it
                _playlistOrder[0] = 0;
            }
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
            playlistOrderIndex++;
            _nextCounter++;
            if (Loop || _nextCounter <= _playList.Count - 1)
            {
                if (playlistOrderIndex > _playList.Count - 1)
                    playlistOrderIndex = 0;
            }
            else
                return false;
            _position = _playlistOrder[playlistOrderIndex];
            return true;
        }

        public bool Prev()
        {
            int playlistOrderIndex = _playlistOrder.FindIndex(x => x == _position);
            playlistOrderIndex--;
            _nextCounter--;
            if (Loop || _nextCounter > 0)
            {
                if (playlistOrderIndex < 0)
                    playlistOrderIndex = _playList.Count - 1;
            }
            else
                return false;
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
