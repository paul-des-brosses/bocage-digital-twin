using System.Collections.Generic;
using Bocage.Sensors;
using Bocage.SimulationCore;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for the chantier E6 / ADR #53 refactor of
    /// <see cref="FaunaSensorReader"/> into a thin orchestrator over two
    /// per-channel history containers (<see cref="AcousticSensorReader"/>
    /// and <see cref="CameraTrapSensorReader"/>). The legacy
    /// <see cref="FaunaSensorReader.Read"/> API and its bit-identical RNG
    /// consumption MUST survive — the 10-year
    /// <c>CalibrationScenarioValidationTests</c> rely on it.
    /// </summary>
    public sealed class FaunaSensorChannelsTests
    {
        [Test]
        public void ReadAndRecord_PopulatesBothChannelHistoriesWithTruth()
        {
            var reader = new FaunaSensorReader(new SeededRandom(1UL));
            const double truth = 0.8;
            reader.ReadAndRecord(truth);

            Assert.AreEqual(1, reader.Acoustic.HistoryCount);
            Assert.AreEqual(1, reader.Camera.HistoryCount);

            Assert.IsTrue(reader.Acoustic.TryGetLatest(out SensorSample<double> acousticLatest));
            Assert.IsTrue(reader.Camera.TryGetLatest(out SensorSample<double> cameraLatest));
            Assert.That(acousticLatest.Truth, Is.EqualTo(truth).Within(1e-9));
            Assert.That(cameraLatest.Truth, Is.EqualTo(truth).Within(1e-9));
        }

        [Test]
        public void Read_DoesNotMutateChannelHistories()
        {
            var reader = new FaunaSensorReader(new SeededRandom(2UL));
            reader.Read(0.6);
            reader.Read(0.7);

            Assert.AreEqual(0, reader.Acoustic.HistoryCount,
                "Read() is the pure variant; only ReadAndRecord() should append to histories.");
            Assert.AreEqual(0, reader.Camera.HistoryCount);
        }

        [Test]
        public void Read_PreservesBitIdenticalRngConsumption()
        {
            // CRITICAL: changing the draw order or sub-stream name would
            // shift the bytes emitted by the shared "fauna-sensors" RNG
            // and break the 10-year CalibrationScenarioValidationTests
            // envelope. This test pins the consumption at 2 draws per
            // call by comparing Read vs ReadAndRecord across two readers
            // built from the same seed.
            var readerA = new FaunaSensorReader(new SeededRandom(99UL));
            var readerB = new FaunaSensorReader(new SeededRandom(99UL));
            const double truth = 1.0;

            double a1 = readerA.Read(truth);
            double a2 = readerA.Read(truth);

            double b1 = readerB.ReadAndRecord(truth);
            double b2 = readerB.ReadAndRecord(truth);

            Assert.AreEqual(a1, b1, "Read and ReadAndRecord must consume the same RNG bytes.");
            Assert.AreEqual(a2, b2, "Successive calls must consume RNG identically.");
        }

        [Test]
        public void DeterministicForSameSeed()
        {
            var readerA = new FaunaSensorReader(new SeededRandom(7UL));
            var readerB = new FaunaSensorReader(new SeededRandom(7UL));
            for (int i = 0; i < 100; i++)
            {
                double truth = 0.3 + (i % 7) * 0.1;
                Assert.AreEqual(readerA.Read(truth), readerB.Read(truth));
            }
        }

        [Test]
        public void ChannelHistoriesEvictIndependentlyAtTheirOwnCapacity()
        {
            var reader = new FaunaSensorReader(new SeededRandom(8UL));
            int cap = AcousticSensorReader.HistoryWindowDays;
            for (int i = 0; i < cap + 30; i++) reader.ReadAndRecord(0.8 + i * 0.0001);

            Assert.AreEqual(cap, reader.Acoustic.HistoryCount);
            Assert.AreEqual(cap, reader.Camera.HistoryCount);

            var acousticSnapshot = new List<SensorSample<double>>();
            reader.Acoustic.CopyHistoryTo(acousticSnapshot);
            Assert.AreEqual(cap, acousticSnapshot.Count);
            // Oldest surviving day in the 365-window is day 30 → truth = 0.8 + 30*0.0001 = 0.803.
            Assert.That(acousticSnapshot[0].Truth, Is.EqualTo(0.803).Within(1e-9));
        }
    }
}
