using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="FaunaDynamicsRule"/> and the
    /// <see cref="EcosystemModel.FaunaPopulation"/> state variable it
    /// updates. Tests target both the static factor helpers (which are
    /// pure functions and trivially verifiable) and the dynamic
    /// convergence of the rule under a constant scenario (proves the
    /// EMA reaches the expected equilibrium without overshooting).
    /// </summary>
    public sealed class FaunaDynamicsRuleTests
    {
        private const int TicksToReachEquilibrium = 3650; // 10 years, ample for ~1y TC

        // ---------------- Static factor helpers ----------------

        [Test]
        public void HedgeFactor_at_zero_density_is_0_5()
        {
            Assert.AreEqual(0.5, FaunaDynamicsRule.ComputeHedgeFactor(0.0), 1e-9);
        }

        [Test]
        public void HedgeFactor_at_baseline_density_is_1_0()
        {
            // 90 m/ha = Perche reference, factor exactly 1.0 so neutral
            // scenario produces target = 1.0 and FaunaPopulation stays put.
            Assert.AreEqual(1.0, FaunaDynamicsRule.ComputeHedgeFactor(90.0), 1e-9);
        }

        [Test]
        public void HedgeFactor_caps_at_1_4()
        {
            // Linear slope would give 1.5 at 180 m/ha; the cap prevents that.
            Assert.AreEqual(FaunaDynamicsRule.HedgeFactorCap,
                FaunaDynamicsRule.ComputeHedgeFactor(300.0), 1e-9);
        }

        [Test]
        public void WaterFactor_flat_above_critical_depth()
        {
            // Up to 3 m the water table is still reachable for wetland fauna.
            Assert.AreEqual(1.0, FaunaDynamicsRule.ComputeWaterFactor(0.0), 1e-9);
            Assert.AreEqual(1.0, FaunaDynamicsRule.ComputeWaterFactor(2.0), 1e-9);
            Assert.AreEqual(1.0, FaunaDynamicsRule.ComputeWaterFactor(3.0), 1e-9);
        }

        [Test]
        public void WaterFactor_declines_linearly_below_critical_depth()
        {
            // 8 m → excess = 5 m → factor = 1.0 - 5 × 0.08 = 0.6
            Assert.AreEqual(0.6, FaunaDynamicsRule.ComputeWaterFactor(8.0), 1e-6);
        }

        [Test]
        public void WaterFactor_floors_at_0_5()
        {
            // At extreme drought (15 m) the floor of 0.5 must hold.
            Assert.AreEqual(0.5, FaunaDynamicsRule.ComputeWaterFactor(15.0), 1e-9);
        }

        [Test]
        public void InputFactor_neutral_at_intensity_one()
        {
            Assert.AreEqual(1.0, FaunaDynamicsRule.ComputeInputFactor(1.0), 1e-9);
        }

        [Test]
        public void InputFactor_penalises_intensification()
        {
            // intensity 2.0 → factor = 1.0 - 1.0 × 0.5 = 0.5
            Assert.AreEqual(0.5, FaunaDynamicsRule.ComputeInputFactor(2.0), 1e-9);
        }

        [Test]
        public void InputFactor_rewards_extensification()
        {
            // intensity 0.5 → factor = 1.0 + 0.5 × 0.2 = 1.1
            Assert.AreEqual(1.1, FaunaDynamicsRule.ComputeInputFactor(0.5), 1e-9);
        }

        [Test]
        public void InputFactor_floors_at_0_4_under_extreme_intensification()
        {
            Assert.AreEqual(0.4, FaunaDynamicsRule.ComputeInputFactor(5.0), 1e-9);
        }

        // ---------------- Integrated convergence ----------------

        [Test]
        public void Neutral_scenario_keeps_fauna_at_baseline()
        {
            // All factors = 1.0 → target = 1.0. Initial state is also 1.0.
            // The EMA is a no-op; population should stay at 1.0.
            var engine = DefaultSimulation.Build(1UL);
            for (int i = 0; i < TicksToReachEquilibrium; i++) engine.Tick();
            Assert.That(engine.Model.FaunaPopulation, Is.EqualTo(1.0).Within(0.05),
                "Neutral scenario should keep fauna ≈ 1.0. Got " + engine.Model.FaunaPopulation);
        }

        [Test]
        public void Intensive_farming_collapses_fauna_below_baseline()
        {
            // intensity 2.0 → inputFactor 0.5. Hedges and water at baseline.
            // Target = 1.0 × 1.0 × 1.0 × 0.5 = 0.5. After 10 years, fauna
            // should sit close to 0.5.
            var scenario = new ScenarioContext(initialInputIntensityFactor: 2.0);
            var engine = DefaultSimulation.Build(1UL, scenario: scenario);
            for (int i = 0; i < TicksToReachEquilibrium; i++) engine.Tick();
            Assert.That(engine.Model.FaunaPopulation, Is.EqualTo(0.5).Within(0.10),
                "Intensive (×2) scenario should drag fauna to ~0.5. Got " + engine.Model.FaunaPopulation);
        }

        [Test]
        public void Virtuous_bocage_lifts_fauna_above_baseline()
        {
            // Bio extensive (intensity 0.5) → inputFactor 1.1.
            // Hedges and water at baseline → other factors = 1.0.
            // Target = 1.1. Fauna should be ~1.1 after 10 years.
            var scenario = new ScenarioContext(initialInputIntensityFactor: 0.5);
            var engine = DefaultSimulation.Build(1UL, scenario: scenario);
            for (int i = 0; i < TicksToReachEquilibrium; i++) engine.Tick();
            Assert.That(engine.Model.FaunaPopulation, Is.GreaterThan(1.0),
                "Bio scenario should lift fauna above 1.0. Got " + engine.Model.FaunaPopulation);
            Assert.That(engine.Model.FaunaPopulation, Is.LessThan(1.3),
                "Without hedge increase, fauna shouldn't exceed 1.3. Got " + engine.Model.FaunaPopulation);
        }

        [Test]
        public void Determinism_same_seed_same_trajectory()
        {
            // Same seed + same scenario + same initial state → identical
            // FaunaPopulation after N ticks. Guards against accidental
            // non-determinism in the dynamics (e.g. introducing rng usage).
            var s1 = new ScenarioContext(initialTemperatureAnomalyC: 1.5);
            var s2 = new ScenarioContext(initialTemperatureAnomalyC: 1.5);
            var e1 = DefaultSimulation.Build(42UL, scenario: s1);
            var e2 = DefaultSimulation.Build(42UL, scenario: s2);
            for (int i = 0; i < 365; i++) { e1.Tick(); e2.Tick(); }
            Assert.AreEqual(e1.Model.FaunaPopulation, e2.Model.FaunaPopulation, 1e-12);
        }

        [Test]
        public void Bounds_fauna_stays_non_negative_under_worst_case()
        {
            // +5 °C, −60 % precip, intensity ×2, hedge removal 10 m/ha/yr.
            // After 10 years the bocage has collapsed; fauna should be very
            // low but never negative (ClampNonNegative in the setter).
            var scenario = new ScenarioContext(
                initialTemperatureAnomalyC: 5.0,
                initialPrecipitationAnomalyPercent: -60.0,
                initialHedgeRemovalRate: 10.0,
                initialInputIntensityFactor: 2.0);
            var engine = DefaultSimulation.Build(1UL, scenario: scenario);
            for (int i = 0; i < TicksToReachEquilibrium; i++) engine.Tick();
            Assert.That(engine.Model.FaunaPopulation, Is.GreaterThanOrEqualTo(0.0),
                "FaunaPopulation must never go negative. Got " + engine.Model.FaunaPopulation);
        }
    }
}
