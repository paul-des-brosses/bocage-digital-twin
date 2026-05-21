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
        public void Yield_converges_close_to_baseline_at_neutral_conditions()
        {
            // Hedgerow at ideal 80 m/ha, water at 2 m, no climate stress,
            // no pressure → multipliers ≈ 1 + 0.15 (bell at peak) × 1 × 1 × 1.
            var rule = new CropYieldDynamicsRule();
            var model = new EcosystemModel(
                initialHedgerowDensity: 80.0,
                initialWaterTableDepth: 2.0,
                initialCropYield: 5.0);
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 1000; i++) rule.Apply(model, ctx, rng);

            // Target ≈ 5.0 × 1.15 × 1 × 1 × 1 = 5.75
            Assert.That(model.CropYield, Is.EqualTo(5.75).Within(0.05));
        }

        [Test]
        public void Yield_collapses_under_heavy_climate_stress()
        {
            var rule = new CropYieldDynamicsRule();
            var model = new EcosystemModel(initialCropYield: 5.0);
            var ctx = new ScenarioContext(initialClimateStress: 1.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 1000; i++) rule.Apply(model, ctx, rng);

            // climate factor (1 - 0.3) = 0.7 → strong reduction.
            Assert.Less(model.CropYield, 5.0);
        }

        [Test]
        public void Yield_zero_when_hedgerow_and_water_both_terrible()
        {
            var rule = new CropYieldDynamicsRule();
            var model = new EcosystemModel(
                initialHedgerowDensity: 0.0,
                initialWaterTableDepth: 10.0,  // water effect clamps to 0
                initialCropYield: 5.0);
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 1500; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.CropYield, Is.LessThan(0.2));
        }

        [Test]
        public void Yield_is_deterministic_under_same_seed()
        {
            var rule = new CropYieldDynamicsRule();
            var ctx = new ScenarioContext(initialAgriculturalPressure: 0.5);

            var modelA = new EcosystemModel(initialCropYield: 5.0);
            var rngA = RngFor(rule.SubStreamId);
            for (int i = 0; i < 200; i++) rule.Apply(modelA, ctx, rngA);

            var modelB = new EcosystemModel(initialCropYield: 5.0);
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
            var rule = new InputCostDynamicsRule();
            var model = new EcosystemModel(initialInputCost: 400.0);
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 600; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.InputCost, Is.EqualTo(400.0).Within(1.0));
        }

        [Test]
        public void InputCost_climbs_with_agricultural_pressure()
        {
            var rule = new InputCostDynamicsRule();
            var model = new EcosystemModel(initialInputCost: 400.0);
            var ctx = new ScenarioContext(initialAgriculturalPressure: 1.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 600; i++) rule.Apply(model, ctx, rng);

            // Target = 400 × (1 + 0.5) × 1 × 1 = 600.
            Assert.That(model.InputCost, Is.EqualTo(600.0).Within(1.0));
        }

        [Test]
        public void InputCost_falls_with_regulatory_constraints()
        {
            var rule = new InputCostDynamicsRule();
            var model = new EcosystemModel(initialInputCost: 400.0);
            var ctx = new ScenarioContext(initialRegulatoryConstraints: 1.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 600; i++) rule.Apply(model, ctx, rng);

            // Target = 400 × 1 × (1 - 0.3) × 1 = 280.
            Assert.That(model.InputCost, Is.EqualTo(280.0).Within(1.0));
        }

        [Test]
        public void InputCost_combined_pressure_regs_climate_compounds()
        {
            var rule = new InputCostDynamicsRule();
            var model = new EcosystemModel(initialInputCost: 400.0);
            var ctx = new ScenarioContext(
                initialAgriculturalPressure: 1.0,
                initialRegulatoryConstraints: 0.5,
                initialClimateStress: 1.0);
            var rng = RngFor(rule.SubStreamId);

            for (int i = 0; i < 600; i++) rule.Apply(model, ctx, rng);

            // Target = 400 × (1 + 0.5) × (1 - 0.15) × (1 + 0.2)
            //        = 400 × 1.5 × 0.85 × 1.2 = 612.
            Assert.That(model.InputCost, Is.EqualTo(612.0).Within(1.5));
        }
    }

    public sealed class MaintenanceCostDynamicsRuleTests
    {
        private static SeededRandom RngFor(string subStream) =>
            new SeededRandom(42UL).DeriveSubStream(subStream);

        [Test]
        public void MaintenanceCost_is_linear_in_hedgerow_density()
        {
            var rule = new MaintenanceCostDynamicsRule();
            var ctx = new ScenarioContext();
            var rng = RngFor(rule.SubStreamId);

            var modelLow = new EcosystemModel(initialHedgerowDensity: 60.0);
            rule.Apply(modelLow, ctx, rng);
            Assert.That(modelLow.MaintenanceCost, Is.EqualTo(18.0).Within(1e-9));

            var modelHigh = new EcosystemModel(initialHedgerowDensity: 150.0);
            rule.Apply(modelHigh, ctx, rng);
            Assert.That(modelHigh.MaintenanceCost, Is.EqualTo(45.0).Within(1e-9));
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
            double afterFirstTick = model.MaintenanceCost; // 30 €/ha/yr

            model.SetHedgerowDensity(200.0);
            rule.Apply(model, ctx, rng);
            double afterSecondTick = model.MaintenanceCost; // 60 €/ha/yr immediately

            Assert.AreEqual(30.0, afterFirstTick, 1e-9);
            Assert.AreEqual(60.0, afterSecondTick, 1e-9);
        }
    }
}
