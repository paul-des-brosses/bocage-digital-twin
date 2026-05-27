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
        {
            int appended = 0;

            // ---- Prolonged drought: requires consecutive-day accounting ----
            if (model.WaterTableDepth > DroughtDepthThresholdMeters)
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
                    waterTableDepthMeters: model.WaterTableDepth,
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
