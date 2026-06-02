using System.Collections.Generic;
using Bocage.Sensors;
using Bocage.SimulationCore;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for <see cref="PiezometerReader"/> (chantier E6 /
    /// ADR #53): noise envelope, history wraparound, ground-truth pairing
    /// inside the rolling buffer, deterministic re-play for a given seed.
    /// Mirrors the test style of <c>WeatherStationReaderTests</c> /
    /// <c>EddyTowerSensorReaderTests</c>.
    /// </summary>
    public sealed class PiezometerReaderTests
    {
        [Test]
        public void ReadingMeanConvergesToTruthUnderManyDraws()
        {
            // σ = 0.05 m → empirical mean over many draws should sit
            // very close to the true depth (Gaussian, zero bias).
            var reader = new PiezometerReader(new SeededRandom(1UL));
            const double truth = 3.0;
            const int n = 5000;
            double sum = 0.0;
            for (int i = 0; i < n; i++) sum += reader.Read(truth);
            double mean = sum / n;
            Assert.That(mean, Is.EqualTo(truth).Within(0.01),
                "Mean of 5000 noisy draws should land within 0.01 m of the true depth. Got " + mean);
        }

        [Test]
        public void ReadingNeverGoesBelowZero()
        {
            // Truth at 0 with σ = 0.05 means roughly half the raw draws
            // would be negative; the reader clamps them at 0.
            var reader = new PiezometerReader(new SeededRandom(2UL));
            for (int i = 0; i < 500; i++)
            {
                double observed = reader.Read(0.0);
                Assert.GreaterOrEqual(observed, 0.0,
                    "Piezometer readings must clamp at 0 (water table above ground is not representable).");
            }
        }

        [Test]
        public void HistoryFillsSlidingWindowAndStoresPairedTruth()
        {
            var reader = new PiezometerReader(new SeededRandom(3UL));
            // Push capacity + 50 days at varying depth so the oldest 50 are evicted.
            for (int day = 0; day < PiezometerReader.HistoryWindowDays + 50; day++)
            {
                double truth = 2.0 + day * 0.001;
                reader.ReadAndRecord(truth);
            }
            Assert.AreEqual(PiezometerReader.HistoryWindowDays, reader.HistoryCount);

            var snapshot = new List<SensorSample<double>>();
            int copied = reader.CopyHistoryTo(snapshot);
            Assert.AreEqual(PiezometerReader.HistoryWindowDays, copied);

            // Oldest surviving sample should be day 50 (truth = 2.0 + 0.05 = 2.05).
            Assert.That(snapshot[0].Truth, Is.EqualTo(2.05).Within(1e-9),
                "Oldest sample's stored truth should be from day 50 (first 50 evicted).");
            // The matching noisy measurement must be within ~3σ = 0.15 m of the truth.
            Assert.That(snapshot[0].Measured, Is.EqualTo(2.05).Within(0.20),
                "Noisy measurement at oldest sample should be near its truth.");
        }

        [Test]
        public void TryGetLatestReturnsMostRecentRecordedSample()
        {
            var reader = new PiezometerReader(new SeededRandom(4UL));
            Assert.IsFalse(reader.TryGetLatest(out _), "No samples yet → false.");

            reader.ReadAndRecord(3.0);
            reader.ReadAndRecord(3.5);
            Assert.IsTrue(reader.TryGetLatest(out SensorSample<double> latest));
            Assert.That(latest.Truth, Is.EqualTo(3.5).Within(1e-9));
        }

        [Test]
        public void DeterministicForSameSeed()
        {
            var readerA = new PiezometerReader(new SeededRandom(42UL));
            var readerB = new PiezometerReader(new SeededRandom(42UL));
            for (int i = 0; i < 100; i++)
            {
                double a = readerA.Read(2.5 + i * 0.01);
                double b = readerB.Read(2.5 + i * 0.01);
                Assert.AreEqual(a, b, "Same seed must produce bit-identical noise sequences.");
            }
        }
    }
}
