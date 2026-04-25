using Bocage.SimulationCore;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class SeededRandomTests
    {
        [Test]
        public void SameSeedProducesIdenticalSequence()
        {
            var a = new SeededRandom(42UL);
            var b = new SeededRandom(42UL);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(a.NextUInt64(), b.NextUInt64(),
                    $"Divergence at step {i} — splitmix64 must be deterministic.");
            }
        }

        [Test]
        public void DifferentSeedsProduceDifferentSequences()
        {
            var a = new SeededRandom(1UL);
            var b = new SeededRandom(2UL);

            int divergences = 0;
            for (int i = 0; i < 100; i++)
            {
                if (a.NextUInt64() != b.NextUInt64()) divergences++;
            }
            Assert.Greater(divergences, 95,
                "Independent seeds should yield essentially independent streams.");
        }

        [Test]
        public void SubStreamDerivationIsDeterministic()
        {
            var master1 = new SeededRandom(123UL);
            var master2 = new SeededRandom(123UL);

            var weather1 = master1.DeriveSubStream("weather");
            var weather2 = master2.DeriveSubStream("weather");

            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(weather1.NextUInt64(), weather2.NextUInt64());
            }
        }

        [Test]
        public void DifferentSubStreamIdsProduceDifferentSequences()
        {
            var master = new SeededRandom(999UL);

            var weather = master.DeriveSubStream("weather");
            var fauna = master.DeriveSubStream("fauna");

            int divergences = 0;
            for (int i = 0; i < 100; i++)
            {
                if (weather.NextUInt64() != fauna.NextUInt64()) divergences++;
            }
            Assert.Greater(divergences, 95,
                "Distinct subsystem identifiers must yield independent streams.");
        }

        [Test]
        public void NextDoubleStaysInUnitInterval()
        {
            var rng = new SeededRandom(7UL);
            for (int i = 0; i < 10_000; i++)
            {
                double v = rng.NextDouble();
                Assert.GreaterOrEqual(v, 0.0);
                Assert.Less(v, 1.0);
            }
        }

        [Test]
        public void NextIntRespectsBounds()
        {
            var rng = new SeededRandom(7UL);
            for (int i = 0; i < 10_000; i++)
            {
                int v = rng.NextInt(-5, 5);
                Assert.GreaterOrEqual(v, -5);
                Assert.Less(v, 5);
            }
        }

        [Test]
        public void ZeroSeedIsAcceptedWithoutDegeneracy()
        {
            // splitmix64 fed with state=0 still produces non-zero output, but
            // we map seed 0 to a non-zero fallback for clarity. Ensure the
            // first value is not zero and the stream has variation.
            var rng = new SeededRandom(0UL);
            ulong first = rng.NextUInt64();
            ulong second = rng.NextUInt64();
            Assert.AreNotEqual(0UL, first);
            Assert.AreNotEqual(first, second);
        }
    }
}
