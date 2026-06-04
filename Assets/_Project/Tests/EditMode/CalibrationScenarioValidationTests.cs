using Bocage.Indicators.Hero;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Runs the four calibration scenarios documented in CALIBRATION.md
    /// through the actual SimulationEngine and asserts the resulting
    /// IntegratedProfitability is in the plausibility window claimed by
    /// the calibration document. This converts the "mental simulations"
    /// of the documentation into executable proof.
    /// <para>
    /// Each test:
    /// <list type="number">
    ///   <item>Builds a ScenarioContext with the scenario's initial values.</item>
    ///   <item>Runs the engine for enough days (3-10 years) so that
    ///         CropYield (EMA k=0.01, ~100-day TC), InputCost (k=0.017,
    ///         ~60-day TC), and WaterTableDepth (recharge rate 0.0005,
    ///         ~2000-day TC) all reach their steady state.</item>
    ///   <item>Asserts the resulting profit falls in a tolerance window
    ///         that accounts for daily stochastic noise on weather.</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class CalibrationScenarioValidationTests
    {
        private const int TicksToReachEquilibrium = 3650; // 10 years
        private const double ProfitTolerance = 60.0; // €/ha/yr, daily noise leak after smoothing

        private static double RunAndComputeProfit(ScenarioContext scenario, ulong seed = 1UL)
        {
            var engine = DefaultSimulation.Build(seed, scenario: scenario);
            for (int i = 0; i < TicksToReachEquilibrium; i++) engine.Tick();
            return IntegratedProfitabilityIndicator.Compute(engine.Model, engine.Scenario);
        }

        [Test]
        public void Scenario1_Reference_neutral_profit_around_335()
        {
            // Expected ~335 €/ha/yr (RICA Agreste 2024 baseline grandes cultures Perche).
            var scenario = new ScenarioContext();
            double profit = RunAndComputeProfit(scenario);
            Assert.That(profit, Is.EqualTo(335.0).Within(ProfitTolerance),
                "Neutral baseline should converge to ~335 €/ha/yr. Got " + profit);
        }

        [Test]
        public void Scenario2_RCP45_horizon_2050_profit_around_minus_300()
        {
            // +2°C, -20% precip. WaterTable equilibrium drifts to ~4 m
            // under sustained heat+drought, which compounds the yield loss.
            // Expected profit ≈ -300 €/ha/yr (climate stress is severe).
            var scenario = new ScenarioContext(
                initialTemperatureAnomalyC: 2.0,
                initialPrecipitationAnomalyPercent: -20.0);
            double profit = RunAndComputeProfit(scenario);
            // Wide window because water table dynamics compound non-linearly
            // with yield bell when water drifts beyond optimum.
            Assert.That(profit, Is.LessThan(0.0),
                "RCP4.5 scenario should make the farm unprofitable. Got " + profit);
            Assert.That(profit, Is.GreaterThan(-700.0),
                "Loss shouldn't exceed -700 €/ha/yr at +2°C. Got " + profit);
        }

        [Test]
        public void Scenario3_BocageBio_MAEC_PSE_solidly_profitable()
        {
            // Bio extensif (I=0.5) + MAEC 100% + PSE max + hedges intacts.
            // After the E9 recalibration the virtuous path is profitable
            // BECAUSE of the subsidies (MAEC input-cost cut + max PSE + CAP),
            // NOT because of free input savings: extensification now costs real
            // yield (-17.5%) and the ~70% fixed cost share stays put. Hand
            // estimate of the steady state ~660 €/ha/yr (yield 4.54 t/ha × 250
            // - input 714 - maint 90 + PSE 90 + PAC 20 + CAP 220). Window kept
            // wide pending a confirmed Unity run; tighten on the actual value.
            var scenario = new ScenarioContext(
                initialInputIntensityFactor: 0.5,
                initialMaecCoveragePercent: 100.0,
                initialPseSubsidyRate: 1.0);
            double profit = RunAndComputeProfit(scenario);
            Assert.That(profit, Is.GreaterThan(450.0),
                "Virtuous bocage farming should stay solidly profitable. Got " + profit);
            Assert.That(profit, Is.LessThan(900.0),
                "But no longer the inflated >900 of the pre-E9 free-input bug. Got " + profit);
        }

        [Test]
        public void Scenario4_WorstCase_profit_strongly_negative()
        {
            // +5°C, -60% precip, intensive inputs, no MAEC. Hedge removal at
            // 10 m/ha/yr wipes 90 m/ha over 9 years, so the bocage collapses.
            // After the E9 recalibration over-intensification is LESS ruinous
            // on the cost side (only the 30% variable share scales: input cost
            // ~2184 instead of the old ~3360), so the catastrophe now comes
            // mainly from the yield collapse. Threshold relaxed accordingly;
            // confirm the actual converged value on a Unity run and tighten.
            var scenario = new ScenarioContext(
                initialTemperatureAnomalyC: 5.0,
                initialPrecipitationAnomalyPercent: -60.0,
                initialHedgeRemovalRate: 10.0,
                initialInputIntensityFactor: 2.0);
            double profit = RunAndComputeProfit(scenario);
            Assert.That(profit, Is.LessThan(-800.0),
                "Worst-case scenario should still be catastrophic (<-800 €/ha/yr). Got " + profit);
        }

        [Test]
        public void Sensitivity_plus_one_degree_alone_profit_drops_around_130()
        {
            // The single-variable sensitivity test that motivated the
            // calibration audit: +1°C alone should drop profit by ~100-200 €/ha/yr.
            var neutral = new ScenarioContext();
            var plusOne = new ScenarioContext(initialTemperatureAnomalyC: 1.0);
            double neutralProfit = RunAndComputeProfit(neutral);
            double plusOneProfit = RunAndComputeProfit(plusOne);
            double delta = neutralProfit - plusOneProfit;
            Assert.That(delta, Is.GreaterThan(80.0),
                "+1°C should cost more than 80 €/ha/yr. Got delta=" + delta);
            Assert.That(delta, Is.LessThan(220.0),
                "+1°C shouldn't cost more than 220 €/ha/yr. Got delta=" + delta);
        }

        [Test]
        public void Water_table_stays_bounded_under_sustained_heat()
        {
            // Without the recharge term added at the 2026-05-21 fix the
            // water table would run away. This test guards against
            // regression.
            var scenario = new ScenarioContext(
                initialTemperatureAnomalyC: 3.0,
                initialPrecipitationAnomalyPercent: -40.0);
            var engine = DefaultSimulation.Build(1UL, scenario: scenario);
            for (int i = 0; i < TicksToReachEquilibrium; i++) engine.Tick();
            // Equilibrium under +3°C/-40% should stabilise around 4-6 m,
            // not run away to 10+ m.
            Assert.That(engine.Model.WaterTableDepth, Is.LessThan(8.0),
                "WaterTableDepth should not exceed 8 m even under sustained drought. Got " + engine.Model.WaterTableDepth);
            Assert.That(engine.Model.WaterTableDepth, Is.GreaterThan(2.0),
                "WaterTableDepth should be deeper than baseline 2 m under +3°C. Got " + engine.Model.WaterTableDepth);
        }
    }
}
