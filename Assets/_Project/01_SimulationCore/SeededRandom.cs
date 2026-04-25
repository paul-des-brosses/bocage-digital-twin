using System;

namespace Bocage.SimulationCore
{
    /// <summary>
    /// Deterministic pseudo-random generator for the simulation core.
    /// Implements splitmix64 — small, fast, repeatable across platforms.
    /// Sub-streams are derived from a master seed by mixing with a 64-bit
    /// FNV-1a hash of an identifier so each subsystem (weather, fauna,
    /// sensors, events, ...) has an independent, reproducible stream.
    /// </summary>
    public sealed class SeededRandom
    {
        private const ulong FnvOffset = 0xCBF29CE484222325UL;
        private const ulong FnvPrime = 0x100000001B3UL;

        private const ulong SplitmixGamma = 0x9E3779B97F4A7C15UL;
        private const ulong SplitmixC1 = 0xBF58476D1CE4E5B9UL;
        private const ulong SplitmixC2 = 0x94D049BB133111EBUL;

        private const ulong NonZeroFallback = 0xDEADBEEFCAFEBABEUL;

        public ulong MasterSeed { get; }
        private ulong _state;

        public SeededRandom(ulong masterSeed)
        {
            MasterSeed = masterSeed;
            _state = masterSeed == 0UL ? NonZeroFallback : masterSeed;
        }

        public SeededRandom DeriveSubStream(string subsystemId)
        {
            if (subsystemId == null) throw new ArgumentNullException(nameof(subsystemId));
            ulong h = Fnv1a64(subsystemId);
            ulong derived = MasterSeed ^ h ^ (h << 32);
            return new SeededRandom(derived);
        }

        public ulong NextUInt64()
        {
            _state += SplitmixGamma;
            ulong z = _state;
            z = (z ^ (z >> 30)) * SplitmixC1;
            z = (z ^ (z >> 27)) * SplitmixC2;
            return z ^ (z >> 31);
        }

        public double NextDouble()
        {
            // 53-bit mantissa, scaled to [0, 1).
            return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
        }

        public float NextFloat()
        {
            return (float)NextDouble();
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentException("maxExclusive must be greater than minInclusive.");
            ulong span = (ulong)((long)maxExclusive - minInclusive);
            ulong r = NextUInt64() % span;
            return (int)((long)minInclusive + (long)r);
        }

        public double NextRange(double minInclusive, double maxExclusive)
        {
            return minInclusive + (maxExclusive - minInclusive) * NextDouble();
        }

        public double NextGaussian(double mean, double stdDev)
        {
            // Box-Muller, single sample. Adequate for sensor noise; not
            // optimized for tight loops — call sites should cache when needed.
            double u1 = 1.0 - NextDouble();
            double u2 = 1.0 - NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return mean + stdDev * z;
        }

        private static ulong Fnv1a64(string s)
        {
            ulong h = FnvOffset;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= FnvPrime;
            }
            return h;
        }
    }
}
