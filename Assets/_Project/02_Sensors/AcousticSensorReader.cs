using System.Collections.Generic;

namespace Bocage.Sensors
{
    /// <summary>
    /// Couche 2 history container for the passive acoustic recorder
    /// (<c>sensor_acoustic01</c>). One of the two channels owned by
    /// <see cref="FaunaSensorReader"/>: the parent reader makes the noisy
    /// draws (so the sub-stream and draw order remain bit-identical to
    /// the pre-refactor behaviour — preserving 10-year calibration tests)
    /// and pushes each paired (noisy, truth) sample here via
    /// <see cref="RecordInternal"/>. The inspection panel
    /// (chantier E6 / ADR #53) plots the rolling 365-day series to surface
    /// per-sensor uncertainty (« acoustic fragile à faible densité »
    /// pedagogy point).
    /// </summary>
    public sealed class AcousticSensorReader : ISensorHistory<SensorSample<double>>
    {
        public const int HistoryWindowDays = 365;

        private readonly RollingSensorHistory<SensorSample<double>> _history =
            new RollingSensorHistory<SensorSample<double>>(HistoryWindowDays);

        /// <inheritdoc />
        public int HistoryCount => _history.HistoryCount;

        /// <inheritdoc />
        public int Capacity => _history.Capacity;

        /// <summary>Gets the most recent paired sample, or <c>false</c> if none recorded yet.</summary>
        public bool TryGetLatest(out SensorSample<double> value) => _history.TryGetLatest(out value);

        /// <inheritdoc />
        public int CopyHistoryTo(IList<SensorSample<double>> destination) => _history.CopyHistoryTo(destination);

        /// <summary>
        /// Appends a paired (noisy, truth) sample. Intended for the parent
        /// <see cref="FaunaSensorReader"/> only — kept public because
        /// asmdef `internal` boundaries make it inconvenient to enforce
        /// here, but the only legitimate caller is the parent reader's
        /// <c>ReadAndRecord</c>.
        /// </summary>
        public void RecordInternal(SensorSample<double> sample) => _history.Record(sample);
    }
}
