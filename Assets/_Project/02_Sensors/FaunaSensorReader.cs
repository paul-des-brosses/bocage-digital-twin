using System;
using Bocage.SimulationCore;

namespace Bocage.Sensors
{
    /// <summary>
    /// Couche 2 reader that synthesises an estimate of the model's
    /// <c>FaunaPopulation</c> from two independent sensors — the passive
    /// acoustic recorder (<c>sensor_acoustic01</c>) and the camera trap
    /// (<c>sensor_camera_trap_01</c>). Each reading is drawn from a
    /// Gaussian centred on the true abundance, with a standard deviation
    /// inversely proportional to the square root of that abundance.
    /// This matches field reality (Poisson sampling theory): rare species
    /// generate fewer detections per night and therefore noisier
    /// estimates than abundant ones.
    /// <para>
    /// Per-sensor formula:
    /// <c>sigma = 0.20 / sqrt(max(trueFauna, 0.01))</c>.
    /// The combined estimate is the arithmetic mean of two independent
    /// draws, which reduces the effective standard deviation by √2.
    /// </para>
    /// <para>
    /// Reference values for the σ envelope:
    /// <list type="bullet">
    ///   <item>fauna = 1.0 (baseline) — σ individual = 0.20, combined ≈ 0.14 (≈14 % relative error)</item>
    ///   <item>fauna = 0.7 (event threshold) — σ individual ≈ 0.24, combined ≈ 0.17</item>
    ///   <item>fauna = 0.3 (collapse) — σ individual ≈ 0.37, combined ≈ 0.26</item>
    /// </list>
    /// </para>
    /// <para>
    /// The reader owns a derived sub-stream (<c>"fauna-sensors"</c>) so
    /// the noise sequence is reproducible from the master seed and
    /// independent of every other simulation sub-system.
    /// </para>
    /// </summary>
    public sealed class FaunaSensorReader
    {
        private readonly SeededRandom _rng;

        public FaunaSensorReader(SeededRandom masterRng)
        {
            if (masterRng == null) throw new ArgumentNullException(nameof(masterRng));
            _rng = masterRng.DeriveSubStream("fauna-sensors");
        }

        /// <summary>
        /// Returns the combined fauna abundance estimate (mean of two
        /// independent noisy readings). Each reading is clamped at 0 to
        /// match real sensors that cannot return negative counts.
        /// </summary>
        public double Read(double trueFaunaPopulation)
        {
            double sigma = 0.20 / Math.Sqrt(Math.Max(trueFaunaPopulation, 0.01));
            double acoustic = Math.Max(0.0, _rng.NextGaussian(trueFaunaPopulation, sigma));
            double cameraTrap = Math.Max(0.0, _rng.NextGaussian(trueFaunaPopulation, sigma));
            return (acoustic + cameraTrap) / 2.0;
        }
    }
}
