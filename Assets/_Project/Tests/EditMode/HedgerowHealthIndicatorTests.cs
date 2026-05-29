using Bocage.Indicators.Hero;
using Bocage.Sensors;
using Bocage.Sensors.Events;
using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Sub-étape 9β — derived hedgerow health proxy.
    /// Pins the formula's headline properties: monotone in density,
    /// monotone (decreasing) in active stress events, never escapes
    /// [0,1], and tolerates a null event log (EditMode bootstrap path).
    /// </summary>
    public sealed class HedgerowHealthIndicatorTests
    {
        // Build a model with a known current day; the indicator looks
        // at (currentDay - event.DetectedOnDay) so we need an explicit
        // value to assert recent vs old events.
        private static EcosystemModel BuildModel(double hedgerowDensity, int currentDay)
        {
            var model = new EcosystemModel(initialHedgerowDensity: hedgerowDensity);
            for (int i = 0; i < currentDay; i++) model.AdvanceDay();
            return model;
        }

        [Test]
        public void Compute_with_null_log_returns_density_baseline()
        {
            var model = BuildModel(hedgerowDensity: 90.0, currentDay: 0);
            double baseline = HedgerowDensityIndicator.Normalize(model.HedgerowDensity);
            Assert.AreEqual(baseline, HedgerowHealthIndicator.Compute(model, null), 1e-9);
        }

        [Test]
        public void Compute_with_empty_log_returns_density_baseline()
        {
            var model = BuildModel(hedgerowDensity: 90.0, currentDay: 0);
            var log = new EventLog();
            double baseline = HedgerowDensityIndicator.Normalize(model.HedgerowDensity);
            Assert.AreEqual(baseline, HedgerowHealthIndicator.Compute(model, log), 1e-9);
        }

        [Test]
        public void Recent_drought_applies_penalty()
        {
            var model = BuildModel(hedgerowDensity: 90.0, currentDay: 30);
            var log = new EventLog();
            log.Append(new DroughtProlongedEvent(detectedOnDay: 20, waterTableDepthMeters: 4.0, consecutiveDryDays: 35));

            double baseline = HedgerowDensityIndicator.Normalize(model.HedgerowDensity);
            double withEvent = HedgerowHealthIndicator.Compute(model, log);

            Assert.AreEqual(baseline - HedgerowHealthIndicator.DroughtPenalty, withEvent, 1e-9);
        }

        [Test]
        public void Old_event_outside_window_does_not_apply_penalty()
        {
            // Window is 60 days; an event detected on day 5 of a model
            // that is now on day 200 is well outside.
            var model = BuildModel(hedgerowDensity: 90.0, currentDay: 200);
            var log = new EventLog();
            log.Append(new DroughtProlongedEvent(detectedOnDay: 5, waterTableDepthMeters: 4.0, consecutiveDryDays: 35));

            double baseline = HedgerowDensityIndicator.Normalize(model.HedgerowDensity);
            double withOldEvent = HedgerowHealthIndicator.Compute(model, log);

            Assert.AreEqual(baseline, withOldEvent, 1e-9);
        }

        [Test]
        public void Drought_event_clamps_to_zero_on_sparse_hedges()
        {
            // Density baseline near 0 + drought penalty pushes the raw
            // result negative; the indicator must clamp.
            var model = BuildModel(hedgerowDensity: HedgerowDensityIndicator.MinMetersPerHectare, currentDay: 30);
            var log = new EventLog();
            log.Append(new DroughtProlongedEvent(detectedOnDay: 27, waterTableDepthMeters: 4.5, consecutiveDryDays: 30));

            double health = HedgerowHealthIndicator.Compute(model, log);
            Assert.GreaterOrEqual(health, 0.0);
            Assert.LessOrEqual(health, 1.0);
        }

        [Test]
        public void Compute_is_monotone_increasing_in_density()
        {
            var sparse = BuildModel(hedgerowDensity: 50.0, currentDay: 0);
            var medium = BuildModel(hedgerowDensity: 90.0, currentDay: 0);
            var dense  = BuildModel(hedgerowDensity: 130.0, currentDay: 0);

            double a = HedgerowHealthIndicator.Compute(sparse, null);
            double b = HedgerowHealthIndicator.Compute(medium, null);
            double c = HedgerowHealthIndicator.Compute(dense, null);

            Assert.Less(a, b);
            Assert.LessOrEqual(b, c); // monotone non-decreasing (may hit the 1.0 cap)
        }
    }
}
