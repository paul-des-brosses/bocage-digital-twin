using System;
using Bocage.SimulationCore.Model;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily rain generator for the seasonal weather model (chantier E2 /
    /// ADR #52). For each simulated day the model draws a Bernoulli with
    /// the month's wet-day probability; if the day is wet, intensity in mm
    /// is drawn from a log-normal with the month's <c>(mu, sigma)</c>. Dry
    /// days return zero precipitation.
    /// <para>
    /// The two random draws (wet/dry then intensity) consume the same RNG
    /// sub-stream passed in by the caller — typically a sub-stream named
    /// <c>"markov-rain"</c> derived inside
    /// <see cref="WeatherUpdateRule"/> from the rule's own RNG. Keeping
    /// the two draws on the same sub-stream is intentional: it guarantees
    /// that for a given seed, the same wet/dry decision is always paired
    /// with the same intensity draw, so changes to the dry-path code (or
    /// adding new draws elsewhere) do not shift the sequence.
    /// </para>
    /// </summary>
    public static class MarkovRainModel
    {
        /// <summary>
        /// Draws (isWetDay, precipitationMillimeters) for the given month.
        /// Returned mm is &gt;= 0 by construction.
        /// </summary>
        public static (bool isWet, double precipitationMillimeters) Draw(
            in MonthlyClimate month, SeededRandom rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            bool isWet = rng.NextDouble() < month.ProbabilityWetDay;
            if (!isWet) return (false, 0.0);

            double z = SampleStandardNormal(rng);
            double precipitation = Math.Exp(month.LogNormalMu + month.LogNormalSigma * z);
            if (double.IsNaN(precipitation) || double.IsInfinity(precipitation)) precipitation = 0.0;
            if (precipitation < 0.0) precipitation = 0.0;
            return (true, precipitation);
        }

        /// <summary>
        /// Box-Muller draw from N(0, 1). Duplicated from
        /// <see cref="SeededRandom.NextGaussian"/> so the rain generator
        /// can construct a log-normal sample directly without paying for
        /// the (mean + stdDev * z) recomposition inside NextGaussian and
        /// without consuming an extra random draw on the dry path.
        /// </summary>
        private static double SampleStandardNormal(SeededRandom rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
