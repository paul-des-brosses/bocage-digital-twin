using Bocage.Sensors.Events;
using Bocage.SimulationCore.Model;

namespace Bocage.Sensors
{
    /// <summary>
    /// Couche 2 detector that reads <see cref="EcosystemModel"/> once per
    /// simulated day and appends events to the given
    /// <see cref="EventLog"/> whenever the state crosses a documented
    /// threshold. Stateful only in the minimal sense required by the
    /// "prolonged drought" detection (it counts consecutive dry days) —
    /// no copy of the model is kept, no UnityEngine reference.
    /// <para>
    /// Per-type cooldown of <see cref="CooldownDays"/> prevents the
    /// same event being re-emitted every tick while the underlying
    /// state remains in the alert window. The cooldown reads the log
    /// (via <see cref="EventLog.LatestOfType{T}"/>) — no hidden state.
    /// </para>
    /// <para>
    /// Calibration sources are documented on each event class
    /// (DroughtProlongedEvent, FaunaAcousticAnomalyEvent).
    /// </para>
    /// </summary>
    public sealed class EventDetector
    {
        // Thresholds tuned 2026-05-21 to fire under moderate scenarios
        // (RCP4.5 alone) rather than only at catastrophic ones. Earlier
        // values (5 / 0.5) only triggered under combined climate +
        // intensification stress, leaving the digital twin's decision
        // path unobservable in the canonical "Trajectoire RCP4.5"
        // preset which is the most likely user demo path.
        //
        // Drought depth at 3.5 m = root-zone alarm level (Chambre
        // Normandie agronomic alert). Below this, deep-rooted crops
        // and most hedge species lose access to capillary water.
        // Fauna at 0.7 = −30 % from baseline, aligned with the
        // Vigie-Nature farmland bird decline observed over 1989-2017
        // in intensified zones.
        public const double DroughtDepthThresholdMeters = 3.5;
        public const int DroughtConsecutiveDaysThreshold = 30;
        public const double FaunaAcousticAnomalyThreshold = 0.7;
        // Soil carbon stock (tC/ha) below this is flagged as degrading. The
        // default stock is 50; under low organic inputs (no cover crops, low
        // residue restitution) it drifts well below. Source: INRAE 4 pour 1000 /
        // BDAT reference stocks.
        public const double SoilCarbonLowThresholdTonnesPerHectare = 45.0;
        // Profitability (EUR/ha/yr) below this is "abnormally low": the farm is in
        // real tension (real Perche margins run 100-400; neutral year ~335). Only
        // then does the engine offer an economy-for-ecology trade-off.
        public const double ProfitLowThresholdEurosPerHectare = 50.0;
        public const int CooldownDays = 30;

        private int _consecutiveDryDays;

        /// <summary>
        /// Run one detection pass for the current model state, appending
        /// any newly-detected events to <paramref name="log"/>. Returns
        /// the number of events appended this pass (0 in the steady
        /// state). Safe to call every tick.
        /// <para>
        /// <paramref name="measuredFaunaPopulation"/> is the noisy
        /// estimate produced by <see cref="FaunaSensorReader"/> (combining
        /// the acoustic recorder and the camera trap) — NOT the model's
        /// ground truth. Routing the sensor reading through the detector
        /// preserves the "primauté du capteur" invariant: an algorithmic
        /// alert reflects what was measured, not what the simulation
        /// knows internally.
        /// </para>
        /// </summary>
        public int Detect(EcosystemModel model, EventLog log, double measuredFaunaPopulation)
            => Detect(model, log, measuredFaunaPopulation, double.MaxValue, 1.0);

        /// <summary>
        /// Convenience pass that thresholds the model's GROUND TRUTH for drought
        /// and soil carbon (test / back-compat path). The real run uses the
        /// sensor-routed overload below.
        /// </summary>
        public int Detect(EcosystemModel model, EventLog log, double measuredFaunaPopulation,
            double currentProfitEurosPerHa, double currentBiodiversity01)
            => Detect(model, log, measuredFaunaPopulation, model.WaterTableDepth, model.SoilCarbonStock,
                currentProfitEurosPerHa, currentBiodiversity01);

        /// <summary>
        /// Full, sensor-routed detection pass (primauté du capteur, §9). Drought
        /// thresholds the PIEZOMETER's measured depth
        /// (<paramref name="measuredWaterTableDepthMeters"/>) and the carbon alert
        /// thresholds the EddyTower's INTEGRATED stock estimate
        /// (<paramref name="estimatedSoilCarbonStock"/>) — not the model's hidden
        /// truth — exactly as the fauna path thresholds the sensor-measured fauna
        /// index. The economic <see cref="Bocage.Sensors.Events.LowProfitabilityEvent"/>
        /// thresholds the Couche 04 profitability + biodiversity indicators the
        /// caller supplies.
        /// </summary>
        public int Detect(EcosystemModel model, EventLog log,
            double measuredFaunaPopulation, double measuredWaterTableDepthMeters, double estimatedSoilCarbonStock,
            double currentProfitEurosPerHa, double currentBiodiversity01)
        {
            int appended = 0;

            // ---- Prolonged drought: thresholds the PIEZOMETER-measured depth
            // (not model truth), with consecutive-day accounting. ----
            if (measuredWaterTableDepthMeters > DroughtDepthThresholdMeters)
            {
                _consecutiveDryDays++;
            }
            else
            {
                _consecutiveDryDays = 0;
            }

            if (_consecutiveDryDays >= DroughtConsecutiveDaysThreshold
                && InCooldown<DroughtProlongedEvent>(log, model.CurrentDay) == false)
            {
                log.Append(new DroughtProlongedEvent(
                    detectedOnDay: model.CurrentDay,
                    waterTableDepthMeters: measuredWaterTableDepthMeters,
                    consecutiveDryDays: _consecutiveDryDays));
                appended++;
            }

            // ---- Fauna acoustic anomaly: threshold on the SENSOR-MEASURED
            // composite index, not the ground truth from the model. ----
            if (measuredFaunaPopulation < FaunaAcousticAnomalyThreshold
                && InCooldown<FaunaAcousticAnomalyEvent>(log, model.CurrentDay) == false)
            {
                log.Append(new FaunaAcousticAnomalyEvent(
                    detectedOnDay: model.CurrentDay,
                    faunaPopulation: measuredFaunaPopulation));
                appended++;
            }

            // ---- Soil carbon low: thresholds the EddyTower's INTEGRATED stock
            // estimate (baseline + integral of the measured noisy fluxes), not the
            // model's hidden truth — same sensor-routing as the fauna and drought
            // paths above (primauté du capteur, §9). ----
            if (estimatedSoilCarbonStock < SoilCarbonLowThresholdTonnesPerHectare
                && InCooldown<SoilCarbonLowEvent>(log, model.CurrentDay) == false)
            {
                log.Append(new SoilCarbonLowEvent(
                    detectedOnDay: model.CurrentDay,
                    soilCarbon: estimatedSoilCarbonStock));
                appended++;
            }

            // ---- Low profitability: the economic alert. Profit + biodiversity
            // are computed by Couche 04 and passed in (the detector only
            // thresholds them). Drives the economic counter-recommendations. ----
            if (currentProfitEurosPerHa < ProfitLowThresholdEurosPerHectare
                && InCooldown<LowProfitabilityEvent>(log, model.CurrentDay) == false)
            {
                log.Append(new LowProfitabilityEvent(
                    detectedOnDay: model.CurrentDay,
                    profitEurosPerHectare: currentProfitEurosPerHa,
                    biodiversity: currentBiodiversity01));
                appended++;
            }

            return appended;
        }

        /// <summary>
        /// Resets the internal counters. Used by the test suite to
        /// reuse a detector across scenarios.
        /// </summary>
        public void Reset()
        {
            _consecutiveDryDays = 0;
        }

        private static bool InCooldown<T>(EventLog log, int currentDay) where T : class, IEvent
        {
            var last = log.LatestOfType<T>();
            if (last == null) return false;
            return currentDay - last.DetectedOnDay < CooldownDays;
        }
    }
}
