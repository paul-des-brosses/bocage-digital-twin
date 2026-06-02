using System;
using System.Collections.Generic;
using Bocage.SimulationCore;

namespace Bocage.Sensors
{
    /// <summary>
    /// Couche 2 reader for the on-site Piezometer sprite. Returns the
    /// noisy depth-to-water-table observation a field piezometer would
    /// record, derived from <see cref="Bocage.SimulationCore.Model.EcosystemModel.WaterTableDepth"/>
    /// plus a Gaussian noise term (σ = 0.05 m, envelope typical of
    /// pressure-transducer piezometers logging hourly + daily-averaged).
    /// No event detection, no recommendation, no impact on the model —
    /// indicators and rules keep reading the ground truth from the model
    /// (same pattern as <see cref="WeatherStationReader"/> and
    /// <see cref="EddyTowerSensorReader"/>).
    /// <para>
    /// Each call to <see cref="ReadAndRecord"/> appends a
    /// <see cref="SensorSample{T}"/> (noisy + ground truth) to a 365-day
    /// circular buffer so the future inspection panel (chantier E6 /
    /// ADR #53) can plot the measured series against the true depth and
    /// the alert thresholds. The buffer is pre-allocated at construction
    /// (no runtime allocation — CLAUDE.md §6) and the oldest entry is
    /// overwritten in O(1) as the window slides.
    /// </para>
    /// <para>
    /// The reader owns a derived sub-stream (<c>"piezometer"</c>) so its
    /// noise sequence is reproducible from the master seed and isolated
    /// from every other sub-system.
    /// </para>
    /// </summary>
    public sealed class PiezometerReader : ISensorHistory<SensorSample<double>>
    {
        public const int HistoryWindowDays = 365;
        public const double NoiseSigmaMeters = 0.05;

        private readonly SeededRandom _rng;
        private readonly RollingSensorHistory<SensorSample<double>> _history =
            new RollingSensorHistory<SensorSample<double>>(HistoryWindowDays);

        public PiezometerReader(SeededRandom masterRng)
        {
            if (masterRng == null) throw new ArgumentNullException(nameof(masterRng));
            _rng = masterRng.DeriveSubStream("piezometer");
        }

        /// <inheritdoc />
        public int HistoryCount => _history.HistoryCount;

        /// <inheritdoc />
        public int Capacity => _history.Capacity;

        /// <summary>Gets the most recent paired sample, or <c>false</c> if none recorded yet.</summary>
        public bool TryGetLatest(out SensorSample<double> value) => _history.TryGetLatest(out value);

        /// <inheritdoc />
        public int CopyHistoryTo(IList<SensorSample<double>> destination) => _history.CopyHistoryTo(destination);

        /// <summary>
        /// Returns a noisy depth (m) without touching the history buffer.
        /// Clamped at 0 to match real piezometers, which cannot report a
        /// water table above ground.
        /// </summary>
        public double Read(double trueWaterTableDepthMeters)
        {
            double noise = _rng.NextGaussian(0.0, NoiseSigmaMeters);
            double observed = trueWaterTableDepthMeters + noise;
            return observed < 0.0 ? 0.0 : observed;
        }

        /// <summary>
        /// Reads and appends the paired (noisy, truth) sample to the
        /// rolling 365-day buffer. Returns the noisy observation.
        /// </summary>
        public double ReadAndRecord(double trueWaterTableDepthMeters)
        {
            double observed = Read(trueWaterTableDepthMeters);
            _history.Record(new SensorSample<double>(observed, trueWaterTableDepthMeters));
            return observed;
        }
    }
}
