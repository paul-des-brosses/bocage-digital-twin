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
    /// Refactored in chantier E6 / ADR #53 to expose the two channels
    /// individually via <see cref="Acoustic"/> and <see cref="Camera"/>,
    /// each owning its own 365-day rolling history of paired
    /// (noisy, ground-truth) samples. The inspection panel plots these so
    /// the user can SEE per-sensor uncertainty (« acoustic fragile à
    /// faible densité ») and the fusion behaviour. The single derived
    /// sub-stream (<c>"fauna-sensors"</c>) and the two-draw order
    /// (acoustic, then camera) are preserved bit-for-bit, so existing
    /// EventDetector and 10-year calibration tests keep producing the
    /// same sequences.
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
        /// Per-channel rolling history (365 days) for the acoustic sensor.
        /// Empty until <see cref="ReadAndRecord"/> has been called at
        /// least once. Read by the inspection panel binding (chantier E6 /
        /// ADR #53).
        /// </summary>
        public AcousticSensorReader Acoustic { get; } = new AcousticSensorReader();

        /// <summary>
        /// Per-channel rolling history (365 days) for the camera trap.
        /// Empty until <see cref="ReadAndRecord"/> has been called at
        /// least once. Read by the inspection panel binding (chantier E6 /
        /// ADR #53).
        /// </summary>
        public CameraTrapSensorReader Camera { get; } = new CameraTrapSensorReader();

        /// <summary>
        /// Returns the combined fauna abundance estimate (mean of two
        /// independent noisy readings) WITHOUT touching either channel's
        /// history. Each reading is clamped at 0 to match real sensors
        /// that cannot return negative counts. Kept for callers that
        /// want a pure read (and to mirror the read/record split used by
        /// <see cref="WeatherStationReader"/> and
        /// <see cref="EddyTowerSensorReader"/>).
        /// </summary>
        public double Read(double trueFaunaPopulation)
        {
            DrawBothChannels(trueFaunaPopulation, out double acoustic, out double cameraTrap);
            return (acoustic + cameraTrap) / 2.0;
        }

        /// <summary>
        /// Returns the combined fauna abundance estimate AND records the
        /// two per-channel paired (noisy, truth) samples into
        /// <see cref="Acoustic"/> and <see cref="Camera"/>. Use this from
        /// the tick loop so the inspection panel has fresh history; the
        /// pure <see cref="Read"/> variant is for callers that must not
        /// mutate state (e.g. some unit tests).
        /// </summary>
        public double ReadAndRecord(double trueFaunaPopulation)
        {
            DrawBothChannels(trueFaunaPopulation, out double acoustic, out double cameraTrap);
            Acoustic.RecordInternal(new SensorSample<double>(acoustic, trueFaunaPopulation));
            Camera.RecordInternal(new SensorSample<double>(cameraTrap, trueFaunaPopulation));
            return (acoustic + cameraTrap) / 2.0;
        }

        // Acoustic-then-camera draw order is load-bearing: changing it
        // shifts the bytes the shared sub-stream emits and breaks the
        // 10-year CalibrationScenarioValidationTests envelope. Both
        // Read and ReadAndRecord MUST go through this helper.
        private void DrawBothChannels(double trueFaunaPopulation, out double acoustic, out double cameraTrap)
        {
            double sigma = 0.20 / Math.Sqrt(Math.Max(trueFaunaPopulation, 0.01));
            acoustic = Math.Max(0.0, _rng.NextGaussian(trueFaunaPopulation, sigma));
            cameraTrap = Math.Max(0.0, _rng.NextGaussian(trueFaunaPopulation, sigma));
        }
    }
}
