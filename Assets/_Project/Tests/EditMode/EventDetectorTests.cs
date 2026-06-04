using Bocage.Sensors;
using Bocage.Sensors.Events;
using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the Couche 2 <see cref="EventDetector"/>. Tests
    /// exercise each detection path independently, plus the cooldown
    /// guard and the prolonged-drought consecutive-day accounting.
    /// <para>
    /// Sub-étape 10b: the fauna noise model lives in
    /// <see cref="FaunaSensorReader"/>; the detector itself receives a
    /// deterministic <c>measuredFaunaPopulation</c> from its caller, so
    /// these tests pass an explicit value rather than wiring an RNG.
    /// </para>
    /// </summary>
    public sealed class EventDetectorTests
    {
        // ---------------- Healthy baseline ----------------

        [Test]
        public void Detect_baseline_state_emits_no_event()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel();
            int appended = detector.Detect(model, log, 1.0);
            Assert.AreEqual(0, appended);
            Assert.AreEqual(0, log.Count);
        }

        // ---------------- Prolonged drought ----------------

        [Test]
        public void Detect_drought_requires_consecutive_days_threshold()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel(initialWaterTableDepth: 6.0);

            // Less than 30 consecutive dry days: no event.
            for (int i = 0; i < 29; i++) detector.Detect(model, log, 1.0);
            Assert.AreEqual(0, log.Count,
                "29 consecutive dry days should not yet trigger drought event.");

            // 30th consecutive: should trigger.
            detector.Detect(model, log, 1.0);
            Assert.AreEqual(1, log.Count);
            Assert.IsInstanceOf<DroughtProlongedEvent>(log.Events[0]);
            var e = (DroughtProlongedEvent)log.Events[0];
            Assert.AreEqual(30, e.ConsecutiveDryDays);
            Assert.AreEqual(EventSeverity.Critical, e.Severity);
        }

        [Test]
        public void Detect_drought_counter_resets_when_rain_arrives()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            var dry = new EcosystemModel(initialWaterTableDepth: 6.0);
            var wet = new EcosystemModel(initialWaterTableDepth: 2.0);

            // 20 dry days, then 1 wet day → counter resets to 0.
            for (int i = 0; i < 20; i++) detector.Detect(dry, log, 1.0);
            detector.Detect(wet, log, 1.0);

            // 29 more dry days should NOT trigger (counter was reset).
            for (int i = 0; i < 29; i++) detector.Detect(dry, log, 1.0);
            Assert.AreEqual(0, log.Count,
                "After a wet break, the consecutive counter should restart from zero.");
        }

        // ---------------- Fauna acoustic anomaly ----------------

        [Test]
        public void Detect_low_fauna_emits_acoustic_anomaly_event()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel();
            // 0.3 < 0.7 threshold → anomaly detected.
            detector.Detect(model, log, 0.3);
            Assert.AreEqual(1, log.Count);
            Assert.IsInstanceOf<FaunaAcousticAnomalyEvent>(log.Events[0]);
            var e = (FaunaAcousticAnomalyEvent)log.Events[0];
            Assert.AreEqual(0.3, e.FaunaPopulationAtDetection, 1e-9);
        }

        [Test]
        public void Detect_fauna_at_threshold_does_not_emit()
        {
            // The detector uses strict inequality: fauna exactly equal to
            // the threshold should NOT trigger. Guards against an
            // off-by-one rounding bug.
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel();
            detector.Detect(model, log, EventDetector.FaunaAcousticAnomalyThreshold);
            Assert.AreEqual(0, log.Count);
        }

        // ---------------- Cross-type independence ----------------

        [Test]
        public void Detect_simultaneous_triggers_emit_drought_and_fauna()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            // Worst case: drought + fauna collapse.
            var model = new EcosystemModel(
                initialWaterTableDepth: 7.0,
                initialFaunaPopulation: 0.2);

            // Drive the drought counter to threshold; the fauna check
            // fires immediately on the first tick (no consecutive-day
            // accounting), so by the time drought triggers both events
            // are in the log under cooldown.
            for (int i = 0; i < 30; i++) detector.Detect(model, log, 0.2);

            int drought = 0, fauna = 0;
            foreach (var e in log.Events)
            {
                if (e is DroughtProlongedEvent) drought++;
                if (e is FaunaAcousticAnomalyEvent) fauna++;
            }
            Assert.AreEqual(1, drought, "Exactly one drought event under cooldown.");
            Assert.AreEqual(1, fauna, "Exactly one fauna event under cooldown.");
        }

        // ---------------- EventLog helpers ----------------

        [Test]
        public void LatestOfType_returns_most_recent_match()
        {
            var log = new EventLog();
            log.Append(new DroughtProlongedEvent(detectedOnDay: 10, waterTableDepthMeters: 4.0, consecutiveDryDays: 30));
            log.Append(new DroughtProlongedEvent(detectedOnDay: 100, waterTableDepthMeters: 5.0, consecutiveDryDays: 45));
            log.Append(new FaunaAcousticAnomalyEvent(detectedOnDay: 200, faunaPopulation: 0.3));

            var latest = log.LatestOfType<DroughtProlongedEvent>();
            Assert.IsNotNull(latest);
            Assert.AreEqual(100, latest.DetectedOnDay);
        }

        [Test]
        public void LatestOfType_returns_null_when_no_match()
        {
            var log = new EventLog();
            log.Append(new DroughtProlongedEvent(detectedOnDay: 10, waterTableDepthMeters: 4.0, consecutiveDryDays: 30));
            var latest = log.LatestOfType<FaunaAcousticAnomalyEvent>();
            Assert.IsNull(latest);
        }

        // ---------------- Low profitability (economic alert) ----------------

        [Test]
        public void LowProfitability_event_fires_below_threshold()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel();
            // Profit below the 50 EUR/ha alert, fauna healthy. The detector only
            // thresholds the caller-supplied indicators.
            detector.Detect(model, log, 1.0, currentProfitEurosPerHa: 20.0, currentBiodiversity01: 0.6);
            Assert.IsNotNull(log.LatestOfType<LowProfitabilityEvent>());
        }

        [Test]
        public void LowProfitability_event_silent_above_threshold()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel();
            detector.Detect(model, log, 1.0, currentProfitEurosPerHa: 300.0, currentBiodiversity01: 0.6);
            Assert.IsNull(log.LatestOfType<LowProfitabilityEvent>());
        }

        [Test]
        public void Detect_three_arg_overload_never_fires_low_profitability()
        {
            // The convenience overload (used by the older detector tests and any
            // non-economic call site) passes a sentinel profit, so it never emits
            // a LowProfitabilityEvent.
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel();
            detector.Detect(model, log, 1.0);
            Assert.IsNull(log.LatestOfType<LowProfitabilityEvent>());
        }
    }
}
