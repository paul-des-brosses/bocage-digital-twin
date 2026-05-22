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
            int appended = detector.Detect(model, log);
            Assert.AreEqual(0, appended);
            Assert.AreEqual(0, log.Count);
        }

        // ---------------- Hedge chalara ----------------

        [Test]
        public void Detect_low_hedge_density_emits_chalara_event()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            // 50 < 60 threshold → chalara detected.
            var model = new EcosystemModel(initialHedgerowDensity: 50.0);
            detector.Detect(model, log);
            Assert.AreEqual(1, log.Count);
            Assert.IsInstanceOf<HedgeChalaraEvent>(log.Events[0]);
            var e = (HedgeChalaraEvent)log.Events[0];
            Assert.AreEqual(50.0, e.HedgerowDensityAtDetection, 1e-9);
            Assert.AreEqual(EventSeverity.Warning, e.Severity);
        }

        [Test]
        public void Detect_chalara_respects_cooldown()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel(initialHedgerowDensity: 50.0);

            // Day 0: first detection.
            detector.Detect(model, log);
            Assert.AreEqual(1, log.Count);

            // Subsequent ticks within cooldown should not append.
            // We can't advance the model day directly without ticking
            // the engine, so we just call Detect on the same model
            // 29 times (all "day 0"). Cooldown is "current - last < 30
            // days", so calling at the same day is in cooldown.
            for (int i = 0; i < 50; i++) detector.Detect(model, log);
            Assert.AreEqual(1, log.Count, "Cooldown should prevent re-emission at the same day.");
        }

        // ---------------- Prolonged drought ----------------

        [Test]
        public void Detect_drought_requires_consecutive_days_threshold()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel(initialWaterTableDepth: 6.0);

            // Less than 30 consecutive dry days: no event.
            for (int i = 0; i < 29; i++) detector.Detect(model, log);
            Assert.AreEqual(0, log.Count,
                "29 consecutive dry days should not yet trigger drought event.");

            // 30th consecutive: should trigger.
            detector.Detect(model, log);
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
            for (int i = 0; i < 20; i++) detector.Detect(dry, log);
            detector.Detect(wet, log);

            // 29 more dry days should NOT trigger (counter was reset).
            for (int i = 0; i < 29; i++) detector.Detect(dry, log);
            Assert.AreEqual(0, log.Count,
                "After a wet break, the consecutive counter should restart from zero.");
        }

        // ---------------- Fauna acoustic anomaly ----------------

        [Test]
        public void Detect_low_fauna_emits_acoustic_anomaly_event()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            // 0.3 < 0.5 threshold → anomaly detected.
            var model = new EcosystemModel(initialFaunaPopulation: 0.3);
            detector.Detect(model, log);
            Assert.AreEqual(1, log.Count);
            Assert.IsInstanceOf<FaunaAcousticAnomalyEvent>(log.Events[0]);
            var e = (FaunaAcousticAnomalyEvent)log.Events[0];
            Assert.AreEqual(0.3, e.FaunaPopulationAtDetection, 1e-9);
        }

        [Test]
        public void Detect_fauna_at_threshold_does_not_emit()
        {
            // The detector uses strict inequality: fauna == 0.5 should
            // NOT trigger. Guards against an off-by-one rounding bug.
            var detector = new EventDetector();
            var log = new EventLog();
            var model = new EcosystemModel(initialFaunaPopulation: 0.5);
            detector.Detect(model, log);
            Assert.AreEqual(0, log.Count);
        }

        // ---------------- Cross-type independence ----------------

        [Test]
        public void Detect_simultaneous_triggers_emit_all_three()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            // Worst case: collapsed hedges + drought + fauna anomaly.
            var model = new EcosystemModel(
                initialHedgerowDensity: 30.0,
                initialWaterTableDepth: 7.0,
                initialFaunaPopulation: 0.2);

            // Drive the drought counter to threshold first.
            for (int i = 0; i < 30; i++) detector.Detect(model, log);

            // Expect 3 events: drought (day 0, after 30 ticks),
            // chalara (also day 0, in cooldown after first), fauna
            // (also day 0, in cooldown after first). Cooldown after
            // first detection prevents re-emission.
            // So actually we expect exactly 3 unique-type events total.
            int hedgeChalara = 0, drought = 0, fauna = 0;
            foreach (var e in log.Events)
            {
                if (e is HedgeChalaraEvent) hedgeChalara++;
                if (e is DroughtProlongedEvent) drought++;
                if (e is FaunaAcousticAnomalyEvent) fauna++;
            }
            Assert.AreEqual(1, hedgeChalara, "Exactly one chalara event under cooldown.");
            Assert.AreEqual(1, drought, "Exactly one drought event under cooldown.");
            Assert.AreEqual(1, fauna, "Exactly one fauna event under cooldown.");
        }

        // ---------------- EventLog helpers ----------------

        [Test]
        public void LatestOfType_returns_most_recent_match()
        {
            var log = new EventLog();
            log.Append(new HedgeChalaraEvent(detectedOnDay: 10, hedgerowDensityMetersPerHectare: 55.0));
            log.Append(new HedgeChalaraEvent(detectedOnDay: 100, hedgerowDensityMetersPerHectare: 40.0));
            log.Append(new FaunaAcousticAnomalyEvent(detectedOnDay: 200, faunaPopulation: 0.3));

            var latest = log.LatestOfType<HedgeChalaraEvent>();
            Assert.IsNotNull(latest);
            Assert.AreEqual(100, latest.DetectedOnDay);
        }

        [Test]
        public void LatestOfType_returns_null_when_no_match()
        {
            var log = new EventLog();
            log.Append(new HedgeChalaraEvent(detectedOnDay: 10, hedgerowDensityMetersPerHectare: 55.0));
            var latest = log.LatestOfType<FaunaAcousticAnomalyEvent>();
            Assert.IsNull(latest);
        }
    }
}
