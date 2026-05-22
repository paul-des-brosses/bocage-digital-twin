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
    /// (HedgeChalaraEvent, DroughtProlongedEvent,
    /// FaunaAcousticAnomalyEvent).
    /// </para>
    /// </summary>
    public sealed class EventDetector
    {
        public const double HedgeAlertThresholdMetersPerHectare = 60.0;
        public const double DroughtDepthThresholdMeters = 5.0;
        public const int DroughtConsecutiveDaysThreshold = 30;
        public const double FaunaAcousticAnomalyThreshold = 0.5;
        public const int CooldownDays = 30;

        private int _consecutiveDryDays;

        /// <summary>
        /// Run one detection pass for the current model state, appending
        /// any newly-detected events to <paramref name="log"/>. Returns
        /// the number of events appended this pass (0 in the steady
        /// state). Safe to call every tick.
        /// </summary>
        public int Detect(EcosystemModel model, EventLog log)
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

            // ---- Chalara dieback: simple threshold on hedge density ----
            if (model.HedgerowDensity < HedgeAlertThresholdMetersPerHectare
                && InCooldown<HedgeChalaraEvent>(log, model.CurrentDay) == false)
            {
                log.Append(new HedgeChalaraEvent(
                    detectedOnDay: model.CurrentDay,
                    hedgerowDensityMetersPerHectare: model.HedgerowDensity));
                appended++;
            }

            // ---- Fauna acoustic anomaly: threshold on composite index ----
            if (model.FaunaPopulation < FaunaAcousticAnomalyThreshold
                && InCooldown<FaunaAcousticAnomalyEvent>(log, model.CurrentDay) == false)
            {
                log.Append(new FaunaAcousticAnomalyEvent(
                    detectedOnDay: model.CurrentDay,
                    faunaPopulation: model.FaunaPopulation));
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
