using Dopamine.Core.Api.Lastfm;
using Dopamine.Data.Entities;
using Dopamine.Services.Entities;
using Dopamine.Services.Playback;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dopamine.Tests
{



    [TestClass]
    public class QueueManagerTest
    {

        [TestMethod(), TestCategory(TestCategories.QueueManager)]
        public void MainTest()
        {
            IList<TrackViewModel> l1 = new List<TrackViewModel>();
            IList<TrackViewModel> l2 = new List<TrackViewModel>();
            for (int i = 0; i < 20; i++)
            {
                if (i < 5)
                    l1.Add(new TrackViewModel(null, null, null, null, new TrackV() { Id = i + 1, TrackTitle = "L1 - " + i.ToString()}));
                if (i < 10)
                    l2.Add(new TrackViewModel(null, null, null, null, new TrackV() { Id = i + 11, TrackTitle = "L2 - " + i.ToString() }));
            }


            QueueManager<TrackViewModel> qm = new QueueManager<TrackViewModel>();
            Test(qm, l1, l2, false, false);
            Test(qm, l1, l2, true, false);
            Test(qm, l1, l2, false, true);
            Test(qm, l1, l2, true, true);


            //Assert.IsTrue(!string.IsNullOrEmpty(sessionKey));
        }

        private void Test(QueueManager<TrackViewModel> qm, IList<TrackViewModel> l1, IList<TrackViewModel> l2, bool shuffle, bool loop)
        {
            qm.Shuffle = shuffle;
            qm.Loop = loop;
            Debug.Print($"Testing Shuffle: {qm.Shuffle} Loop: {qm.Loop}");
            Debug.Print($"Play l1");
            qm.Play(l1);
            TestNextPrev(qm);
            Debug.Print($"Enq l2");
            qm.Enqueue(l2);
            TestNextPrev(qm);
            Debug.Print($"Play l2");
            qm.Play(l2, 2);
            TestNextPrev(qm);
        }

        private void TestNextPrev(QueueManager<TrackViewModel> qm)
        {
            Debug.Print($"Testing NEXT");
            for (int i = 0; i < 20; i++)
            {
                Debug.Print($"i: {i} Position: {qm.Position} Current: {qm.CurrentTrack.TrackTitle}");
                if (!qm.Next())
                    break;
            }
            Debug.Print("Testing PREV");
            for (int i = 0; i < 20; i++)
            {
                Debug.Print($"i: {i} Position: {qm.Position} Current: {qm.CurrentTrack.TrackTitle}");
                if (!qm.Prev())
                    break;
            }
        }


    }
}
