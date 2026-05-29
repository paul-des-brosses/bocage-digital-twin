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
    /// updates. Covers (a) the three static factor helpers — habitat,
    /// water, intrants — which are pure functions and trivially
    /// verifiable, (b) the two E5 modulators (canicule, soil carbon),
    /// (c) the dynamic convergence under a constant scenario.
    /// </summary>
    public sealed class FaunaDynamicsRuleTests
    {
        private const int TicksToReachEquilibrium = 3650; // 10 years, ample for ~1y TC

        // ---------------- Habitat factor (chantier E5 rename, ADR #51) ----------------

        [Test]
        public void HabitatFactor_at_zero_density_is_0_5()
        {
            Assert.AreEqual(0.5, FaunaDynamicsRule.ComputeHabitatFactor(0.0), 1e-9);
        }

        [Test]
        public void HabitatFactor_at_baseline_density_is_1_0()
        {
            // 90 m/ha = Perche reference, factor exactly 1.0 so neutral
            // scenario produces target = 1.0 and FaunaPopulation stays put.
            Assert.AreEqual(1.0, FaunaDynamicsRule.ComputeHabitatFactor(90.0), 1e-9);
        }

        [Test]
        public void HabitatFactor_caps_at_1_4()
        {
            // Linear slope would give 1.5 at 180 m/ha; the cap prevents that.
            Assert.AreEqual(FaunaDynamicsRule.HabitatFactorCap,
                FaunaDynamicsRule.ComputeHabitatFactor(300.0), 1e-9);
        }

        // ---------------- Water factor ----------------

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

        // ---------------- Inputs factor (chantier E5 rename, ADR #51) ----------------

        [Test]
        public void InputsFactor_neutral_at_intensity_one()
        {
            Assert.AreEqual(1.0, FaunaDynamicsRule.ComputeInputsFactor(1.0), 1e-9);
        }

        [Test]
        public void InputsFactor_penalises_intensification()
        {
            // intensity 2.0 → factor = 1.0 - 1.0 × 0.5 = 0.5
            Assert.AreEqual(0.5, FaunaDynamicsRule.ComputeInputsFactor(2.0), 1e-9);
        }

        [Test]
        public void InputsFactor_rewards_extensification()
        {
            // intensity 0.5 → factor = 1.0 + 0.5 × 0.2 = 1.1
            Assert.AreEqual(1.1, FaunaDynamicsRule.ComputeInputsFactor(0.5), 1e-9);
        }

        [Test]
        public void InputsFactor_floors_at_0_4_under_extreme_intensification()
        {
            Assert.AreEqual(0.4, FaunaDynamicsRule.ComputeInputsFactor(5.0), 1e-9);
        }

        // ---------------- Canicule modulator (chantier E5 / ADR #51) ----------------

        [Test]
        public void CanicularPenalty_zero_when_no_canicular_days()
        {
            Assert.AreEqual(0.0, FaunaDynamicsRule.ComputeCanicularPenalty(0), 1e-9);
        }

        [Test]
        public void CanicularPenalty_linear_below_cap()
        {
            // 5 canicular days × 0.01 = 0.05 penalty.
            Assert.AreEqual(-0.05, FaunaDynamicsRule.ComputeCanicularPenalty(5), 1e-9);
        }

        [Test]
        public void CanicularPenalty_caps_at_minus_0_15()
        {
            // 30 canicular days would be 0.30 raw, capped at 0.15.
            Assert.AreEqual(-FaunaDynamicsRule.CanicularPenaltyCap,
                FaunaDynamicsRule.ComputeCanicularPenalty(30), 1e-9);
        }

        // ---------------- Soil-carbon modulator (chantier E5 / ADR #51) ----------------

        [Test]
        public void SoilCarbonBonus_zero_below_threshold()
        {
            // Default soil carbon stock (50 tC/ha) is below the « sol
            // vivant » threshold (80 tC/ha), so no bonus.
            Assert.AreEqual(0.0, FaunaDynamicsRule.ComputeSoilCarbonBonus(50.0), 1e-9);
            Assert.AreEqual(0.0, FaunaDynamicsRule.ComputeSoilCarbonBonus(80.0), 1e-9);
        }

        [Test]
        public void SoilCarbonBonus_active_above_threshold()
        {
            // Just above threshold → bonus active. Step function.
            Assert.AreEqual(FaunaDynamicsRule.SoilCarbonBonus,
                FaunaDynamicsRule.ComputeSoilCarbonBonus(80.1), 1e-9);
            Assert.AreEqual(FaunaDynamicsRule.SoilCarbonBonus,
                FaunaDynamicsRule.ComputeSoilCarbonBonus(120.0), 1e-9);
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
            // intensity 2.0 → inputs factor 0.5. Hedges and water at baseline.
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
            // Bio extensive (intensity 0.5) → inputs factor 1.1.
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

        // ---------------- E5 modulators integrated effect ----------------

        [Test]
        public void Canicule_pulls_fauna_target_below_neutral()
        {
            // Direct check of the rule: under neutral scenario + a model
            // state with 10 recent canicular days, the target shifts by
            // −0.10 — well above the EMA noise floor over 1 day.
            var model = new EcosystemModel();
            for (int i = 0; i < 10; i++) model.RecordDailyTemperatureForWindow(32.0);
            Assert.AreEqual(10, model.RecentCanicularDayCount);

            var scenario = new ScenarioContext();
            var rule = new FaunaDynamicsRule();
            double before = model.FaunaPopulation;
            rule.Apply(model, scenario, new SeededRandom(1UL));
            double after = model.FaunaPopulation;
            Assert.That(after, Is.LessThan(before),
                "Canicular days should pull fauna target below baseline. Before=" + before + " after=" + after);
        }

        [Test]
        public void Soil_carbon_above_threshold_lifts_fauna_target()
        {
            // Above the « sol vivant » threshold, the rule adds +0.02 to
            // the target. Under neutral scenario fauna sits at 1.0; the
            // tick should pull it up by k × 0.02 toward 1.02.
            var model = new EcosystemModel(initialSoilCarbonStock: 100.0);
            var scenario = new ScenarioContext();
            var rule = new FaunaDynamicsRule();
            double before = model.FaunaPopulation;
            rule.Apply(model, scenario, new SeededRandom(1UL));
            double after = model.FaunaPopulation;
            Assert.That(after, Is.GreaterThan(before),
                "Soil-carbon bonus should lift fauna target above baseline. Before=" + before + " after=" + after);
        }
    }
}
