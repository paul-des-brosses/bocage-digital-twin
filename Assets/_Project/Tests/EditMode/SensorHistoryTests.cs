using System;
using System.Collections.Generic;
using Bocage.Sensors;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the reusable sliding-window container
    /// <see cref="RollingSensorHistory{T}"/> that backs every sensor reader's
    /// history (chantier E6 / ADR #53–#54). Per-reader behaviour (noise,
    /// baseline, sign convention) stays covered by <c>SeasonalWeatherTests</c>
    /// and <c>SoilCarbonTests</c>; these target the ring-buffer arithmetic the
    /// readers now delegate to.
    /// </summary>
    public sealed class RollingSensorHistoryTests
    {
        [Test]
        public void RecordsAreReadBackOldestFirst()
        {
            var history = new RollingSensorHistory<int>(4);
            history.Record(10);
            history.Record(20);
            history.Record(30);

            Assert.AreEqual(3, history.HistoryCount);
            Assert.AreEqual(4, history.Capacity);

            var snapshot = new List<int>();
            int copied = history.CopyHistoryTo(snapshot);
            Assert.AreEqual(3, copied);
            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, snapshot,
                "Samples must come back in chronological order (oldest first).");
        }

        [Test]
        public void SlidingWindowEvictsOldestWhenFull()
        {
            const int capacity = 3;
            var history = new RollingSensorHistory<int>(capacity);
            // Push capacity + 2 samples (0,1,2,3,4): the oldest two (0,1) expire.
            for (int i = 0; i < capacity + 2; i++) history.Record(i);

            Assert.AreEqual(capacity, history.HistoryCount, "Count must cap at capacity once full.");

            var snapshot = new List<int>();
            history.CopyHistoryTo(snapshot);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, snapshot,
                "Only the three most recent samples should survive, oldest first.");
        }

        [Test]
        public void SlidingWindowAtThreeSixtyFiveCapacityRetainsMostRecentYear()
        {
            // Mirrors the readers' 365-day window: push a full year + 50 extra
            // days and confirm the oldest 50 expired and the count caps at 365.
            const int capacity = 365;
            var history = new RollingSensorHistory<int>(capacity);
            for (int i = 0; i < capacity + 50; i++) history.Record(i);

            Assert.AreEqual(capacity, history.HistoryCount);

            var snapshot = new List<int>();
            history.CopyHistoryTo(snapshot);
            Assert.AreEqual(capacity, snapshot.Count);
            Assert.AreEqual(50, snapshot[0], "Oldest surviving sample is day 50 (0..49 evicted).");
            Assert.AreEqual(capacity + 49, snapshot[snapshot.Count - 1], "Newest sample is the last pushed.");
        }

        [Test]
        public void TryGetLatestReflectsMostRecentRecord()
        {
            var history = new RollingSensorHistory<int>(3);
            Assert.IsFalse(history.TryGetLatest(out int empty), "No sample recorded yet → false.");
            Assert.AreEqual(0, empty);

            history.Record(7);
            history.Record(9);
            Assert.IsTrue(history.TryGetLatest(out int latest));
            Assert.AreEqual(9, latest, "Latest must be the most recently recorded sample.");
        }

        [Test]
        public void ClearDropsAllSamplesButKeepsCapacity()
        {
            var history = new RollingSensorHistory<int>(3);
            history.Record(1);
            history.Record(2);
            history.Clear();

            Assert.AreEqual(0, history.HistoryCount);
            Assert.AreEqual(3, history.Capacity);
            Assert.IsFalse(history.TryGetLatest(out _));
        }

        [Test]
        public void ConstructorRejectsNonPositiveCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RollingSensorHistory<int>(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RollingSensorHistory<int>(-5));
        }
    }
}
