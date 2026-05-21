using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class CropYieldDynamicsRuleTests
    {
        private static SeededRandom RngFor(string subStream) =>
            new SeededRandom(42UL).DeriveSubStream(subStream);

        [Test]
        public void Yield_converges_to_baseline_at_ideal_conditions()
        {
            // After the 2026-05-21 calibration refactor, the bell curve no
            // longer boosts yield at the ideal density — it only penalises
            // deviations. Ideal hedge density aligned on Perche departmental
            // average (90 m/ha) per INRAE so the default initial state sits
            // exactly at the bell peak. Target = baseline = 5.5 t/ha.
            var rule = new CropYieldDynamicsRule();
            var model = new EcosystemModel(
                initialHedgerowDensity: 90.0,
                initialWaterTableDepth: 2.0,
                initialCropYield: 5.0);
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 1000; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.CropYield, Is.EqualTo(5.5).Within(0.05));
        }

        [Test]
        public void Yield_drops_when_hedgerow_density_far_from_ideal()
        {
            // No-hedge farm should yield slightly less than ideal-hedge.
            var ruleIdeal = new CropYieldDynamicsRule();
            var modelIdeal = new EcosystemModel(initialHedgerowDensity: 90.0, initialCropYield: 5.0);
            var ctx = new ScenarioContext();
            var rng1 = RngFor(ruleIdeal.SubStreamId);
            for (int i = 0; i < 1000; i++) ruleIdeal.Apply(modelIdeal, ctx, rng1);

            var ruleNoHedge = new CropYieldDynamicsRule();
            var modelNoHedge = new EcosystemModel(initialHedgerowDensity: 0.0, initialCropYield: 5.0);
            var rng2 = RngFor(ruleNoHedge.SubStreamId);
            for (int i = 0; i < 1000; i++) ruleNoHedge.Apply(modelNoHedge, ctx, rng2);

            Assert.Less(modelNoHedge.CropYield, modelIdeal.CropYield);
        }

        [Test]
        public void Yield_drops_under_positive_temperature_anomaly()
        {
            var rule = new CropYieldDynamicsRule();
            var model = new EcosystemModel(initialCropYield: 5.5);
            var ctx = new ScenarioContext(initialTemperatureAnomalyC: 5.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 1000; i++) rule.Apply(model, ctx, rng);

            Assert.Less(model.CropYield, 5.5);
        }

        [Test]
        public void Yield_drops_under_negative_precipitation_anomaly()
        {
            var rule = new CropYieldDynamicsRule();
            var model = new EcosystemModel(initialCropYield: 5.5);
            var ctx = new ScenarioContext(initialPrecipitationAnomalyPercent: -60.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 1000; i++) rule.Apply(model, ctx, rng);

            Assert.Less(model.CropYield, 5.5);
        }

        [Test]
        public void Yield_collapses_when_water_table_collapses()
        {
            var rule = new CropYieldDynamicsRule();
            var model = new EcosystemModel(
                initialHedgerowDensity: 0.0,
                initialWaterTableDepth: 10.0,
                initialCropYield: 5.5);
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 1500; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.CropYield, Is.LessThan(0.2));
        }

        [Test]
        public void Yield_is_deterministic_under_same_seed()
        {
            var rule = new CropYieldDynamicsRule();
            var ctx = new ScenarioContext(initialInputIntensityFactor: 1.5);

            var modelA = new EcosystemModel(initialCropYield: 5.5);
            var rngA = RngFor(rule.SubStreamId);
            for (int i = 0; i < 200; i++) rule.Apply(modelA, ctx, rngA);

            var modelB = new EcosystemModel(initialCropYield: 5.5);
            var rngB = RngFor(rule.SubStreamId);
            for (int i = 0; i < 200; i++) rule.Apply(modelB, ctx, rngB);

            Assert.AreEqual(modelA.CropYield, modelB.CropYield, 1e-9);
        }
    }

    public sealed class InputCostDynamicsRuleTests
    {
        private static SeededRandom RngFor(string subStream) =>
            new SeededRandom(42UL).DeriveSubStream(subStream);

        [Test]
        public void InputCost_converges_to_baseline_at_neutral_scenario()
        {
            // Baseline = 1200 €/ha/yr (CIVAM grandes cultures bocage).
            var rule = new InputCostDynamicsRule();
            var model = new EcosystemModel(initialInputCost: 1200.0);
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 600; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.InputCost, Is.EqualTo(1200.0).Within(2.0));
        }

        [Test]
        public void InputCost_doubles_when_intensity_doubles()
        {
            var rule = new InputCostDynamicsRule();
            var model = new EcosystemModel(initialInputCost: 1200.0);
            var ctx = new ScenarioContext(initialInputIntensityFactor: 2.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 600; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.InputCost, Is.EqualTo(2400.0).Within(3.0));
        }

        [Test]
        public void InputCost_falls_with_full_maec_coverage()
        {
            var rule = new InputCostDynamicsRule();
            var model = new EcosystemModel(initialInputCost: 1200.0);
            var ctx = new ScenarioContext(initialMaecCoveragePercent: 100.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 600; i++) rule.Apply(model, ctx, rng);

            // target = 1200 × 1 × (1 - 0.3) × 1 = 840.
            Assert.That(model.InputCost, Is.EqualTo(840.0).Within(2.0));
        }

        [Test]
        public void InputCost_climbs_under_heat_and_drought()
        {
            var rule = new InputCostDynamicsRule();
            var model = new EcosystemModel(initialInputCost: 1200.0);
            var ctx = new ScenarioContext(
                initialTemperatureAnomalyC: 5.0,
                initialPrecipitationAnomalyPercent: -60.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 600; i++) rule.Apply(model, ctx, rng);

            // heat 0.20 + drought 0.20 = 0.40 → 1.40 × 1200 = 1680.
            Assert.That(model.InputCost, Is.EqualTo(1680.0).Within(3.0));
        }
    }

    public sealed class MaintenanceCostDynamicsRuleTests
    {
        private static SeededRandom RngFor(string subStream) =>
            new SeededRandom(42UL).DeriveSubStream(subStream);

        [Test]
        public void MaintenanceCost_is_linear_in_hedgerow_density()
        {
            // Rate = 1.0 €/m/yr (cf. Réseau Haies 2024 référentiel, share
            // out-of-pocket of total 3.69 €/ml).
            var rule = new MaintenanceCostDynamicsRule();
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            var modelLow = new EcosystemModel(initialHedgerowDensity: 60.0);
            rule.Apply(modelLow, ctx, rng);
            Assert.That(modelLow.MaintenanceCost, Is.EqualTo(60.0).Within(1e-9));

            var modelHigh = new EcosystemModel(initialHedgerowDensity: 150.0);
            rule.Apply(modelHigh, ctx, rng);
            Assert.That(modelHigh.MaintenanceCost, Is.EqualTo(150.0).Within(1e-9));
        }

        [Test]
        public void MaintenanceCost_is_zero_when_no_hedgerows()
        {
            var rule = new MaintenanceCostDynamicsRule();
            var model = new EcosystemModel(initialHedgerowDensity: 0.0);
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            rule.Apply(model, ctx, rng);
            Assert.AreEqual(0.0, model.MaintenanceCost, 1e-9);
        }

        [Test]
        public void MaintenanceCost_is_recomputed_each_tick_no_inertia()
        {
            var rule = new MaintenanceCostDynamicsRule();
            var model = new EcosystemModel(initialHedgerowDensity: 100.0);
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            rule.Apply(model, ctx, rng);
            double afterFirstTick = model.MaintenanceCost;

            model.SetHedgerowDensity(200.0);
            rule.Apply(model, ctx, rng);
            double afterSecondTick = model.MaintenanceCost;

            Assert.AreEqual(100.0, afterFirstTick, 1e-9);
            Assert.AreEqual(200.0, afterSecondTick, 1e-9);
        }
    }
}
