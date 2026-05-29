using System;
using System.Collections.Generic;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;

namespace Bocage.Sensors
{
    /// <summary>
    /// Couche 2 reader for the on-site EddyTower sprite. Returns the daily
    /// net CO2 flux that a real eddy-covariance tower would record above
    /// the soil + canopy ensemble, derived from the day-over-day change in
    /// <see cref="EcosystemModel.SoilCarbonStock"/> plus a Gaussian noise
    /// term (σ = 1.5 kgCO2/ha/day, envelope representative of FluxNet
    /// cropland uncertainty). No event detection, no recommendation —
    /// this is the pure measurement chain that ADR #48 requires to make
    /// EddyTower a first-class sensor instead of a decorative sprite.
    /// <para>
    /// Sign convention follows the atmospheric science / Net Ecosystem
    /// Exchange tradition: <b>positive flux = emission to atmosphere</b>,
    /// <b>negative flux = sequestration into soil</b>. So a soil that is
    /// gaining carbon (ΔC > 0) reports a negative flux, and a soil that
    /// is losing carbon (ΔC &lt; 0) reports a positive flux. The
    /// conversion factor <c>44/12 × 1000</c> turns tC/ha/day into
    /// kgCO2/ha/day.
    /// </para>
    /// <para>
    /// Each call to <see cref="ReadAndRecord"/> also stores the noisy
    /// flux in an internal 365-day circular buffer, mutualised with the
    /// future inspection panel (chantier E6 / ADR #53). The buffer is
    /// pre-allocated at construction (no runtime allocation) and the
    /// oldest entry is overwritten in O(1) as the window slides.
    /// </para>
    /// <para>
    /// The reader owns a derived sub-stream (<c>"eddy-tower"</c>) so its
    /// noise sequence is reproducible from the master seed and isolated
    /// from every other sub-system. The very first call has no previous
    /// stock to subtract, so it returns 0 ± noise and silently captures
    /// the baseline; from the second call onward the ΔC is meaningful.
    /// </para>
    /// </summary>
    public sealed class EddyTowerSensorReader
    {
        public const int HistoryWindowDays = 365;
        public const double NoiseSigmaKgCO2PerHectarePerDay = 1.5;
        public const double CarbonToCO2MassRatio = 44.0 / 12.0;
        public const double TonnesToKilograms = 1000.0;

        private readonly SeededRandom _rng;
        private readonly double[] _history = new double[HistoryWindowDays];
        private int _historyHead;
        private int _historyCount;
        private double _previousSoilCarbonStock;
        private bool _hasBaseline;

        public EddyTowerSensorReader(SeededRandom masterRng)
        {
            if (masterRng == null) throw new ArgumentNullException(nameof(masterRng));
            _rng = masterRng.DeriveSubStream("eddy-tower");
        }

        /// <summary>Total number of samples currently stored (caps at 365).</summary>
        public int HistoryCount => _historyCount;

        /// <summary>
        /// Returns a noisy daily net CO2 flux in kgCO2/ha/day without
        /// touching the baseline or the history buffer. ΔC is computed
        /// against the previously baselined stock — if no baseline has
        /// been captured yet, returns noise only (the baseline must be
        /// established by a prior <see cref="ReadAndRecord"/> call).
        /// </summary>
        public double Read(double currentSoilCarbonStock)
        {
            double deltaTcPerHaPerDay = _hasBaseline
                ? currentSoilCarbonStock - _previousSoilCarbonStock
                : 0.0;
            double netFluxKgCO2 = -deltaTcPerHaPerDay * CarbonToCO2MassRatio * TonnesToKilograms;
            double noise = _rng.NextGaussian(0.0, NoiseSigmaKgCO2PerHectarePerDay);
            return netFluxKgCO2 + noise;
        }

        /// <summary>
        /// Reads the noisy daily net CO2 flux, appends it to the rolling
        /// 365-day buffer, and updates the baseline so the next call's
        /// ΔC is measured against today's stock.
        /// </summary>
        public double ReadAndRecord(double currentSoilCarbonStock)
        {
            double observed = Read(currentSoilCarbonStock);
            _history[_historyHead] = observed;
            _historyHead = (_historyHead + 1) % HistoryWindowDays;
            if (_historyCount < HistoryWindowDays) _historyCount++;
            _previousSoilCarbonStock = currentSoilCarbonStock;
            _hasBaseline = true;
            return observed;
        }

        /// <summary>
        /// Copies the recorded fluxes into <paramref name="destination"/>
        /// in chronological order (oldest first). Returns the number of
        /// samples actually written.
        /// </summary>
        public int CopyHistoryTo(IList<double> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            int oldestIndex = _historyCount < HistoryWindowDays
                ? 0
                : _historyHead;
            for (int i = 0; i < _historyCount; i++)
            {
                int slot = (oldestIndex + i) % HistoryWindowDays;
                destination.Add(_history[slot]);
            }
            return _historyCount;
        }
    }
}
