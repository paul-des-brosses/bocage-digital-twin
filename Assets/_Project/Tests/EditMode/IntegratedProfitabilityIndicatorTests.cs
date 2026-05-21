using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class IntegratedProfitabilityIndicatorTests
    {
        [Test]
        public void Compute_at_baseline_with_pse_05()
        {
            // Defaults: CropYield 5.5, InputCost 1200, MaintenanceCost 90, hedge 90.
            // Scenario PseSubsidyRate 0.50. PacHedgeBonus 20 (forfait).
            // Basic CAP payment 230 (DPB + greening + écorégime).
            // profit = 5.5*250 - 1200 - 90 + 90*0.5 + 20 + 230
            //        = 1375 - 1200 - 90 + 45 + 20 + 230 = 380
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.50);
            double profit = IntegratedProfitabilityIndicator.Compute(model, scenario);
            Assert.AreEqual(380.0, profit, 1e-6);
        }

        [Test]
        public void Compute_at_baseline_without_pse()
        {
            // Without PSE but with PAC bonus and basic CAP:
            // profit = 1375 - 1200 - 90 + 0 + 20 + 230 = 335
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.0);
            double profit = IntegratedProfitabilityIndicator.Compute(model, scenario);
            Assert.AreEqual(335.0, profit, 1e-6);
        }

        [Test]
        public void Compute_basic_cap_payment_always_credited_even_without_hedges()
        {
            // Even with no hedges and zero PSE, the basic CAP payment
            // (230 €/ha) is always credited — it's a flat support
            // independent of farm structure. The PAC bonus haie is
            // however zero (no hedges to credit).
            var model = new EcosystemModel(
                initialHedgerowDensity: 0.0,
                initialMaintenanceCost: 0.0);
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.0);
            double profit = IntegratedProfitabilityIndicator.Compute(model, scenario);
            // profit = 1375 - 1200 - 0 + 0 + 0 (no PAC haie bonus) + 230 = 405
            Assert.AreEqual(405.0, profit, 1e-6);
        }

        [Test]
        public void Compute_increases_with_yield_all_else_equal()
        {
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.50);
            var low = new EcosystemModel(initialCropYield: 3.0);
            var high = new EcosystemModel(initialCropYield: 7.0);
            Assert.Less(
                IntegratedProfitabilityIndicator.Compute(low, scenario),
                IntegratedProfitabilityIndicator.Compute(high, scenario));
        }

        [Test]
        public void Compute_decreases_with_input_cost_all_else_equal()
        {
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.50);
            var cheap = new EcosystemModel(initialInputCost: 600.0);
            var costly = new EcosystemModel(initialInputCost: 2000.0);
            Assert.Greater(
                IntegratedProfitabilityIndicator.Compute(cheap, scenario),
                IntegratedProfitabilityIndicator.Compute(costly, scenario));
        }

        [Test]
        public void Compute_can_be_negative_under_extreme_costs()
        {
            // Yield 0.5 × 250 = 125. Inputs 2000, maintenance 50. No hedge so no PSE, no PAC bonus.
            // profit = 125 - 2000 - 50 + 0 + 0 = -1925.
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.0);
            var model = new EcosystemModel(
                initialCropYield: 0.5,
                initialInputCost: 2000.0,
                initialMaintenanceCost: 50.0,
                initialHedgerowDensity: 0.0);
            double profit = IntegratedProfitabilityIndicator.Compute(model, scenario);
            Assert.Less(profit, 0.0);
        }

        [Test]
        public void Compute_grows_with_hedge_density_via_PSE()
        {
            // With MaintenanceCost held constant (default 90 in both models),
            // diff in profit between sparse and dense hedges = pure PSE term.
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.50);
            var sparse = new EcosystemModel(initialHedgerowDensity: 30.0);
            var dense = new EcosystemModel(initialHedgerowDensity: 130.0);
            double pSparse = IntegratedProfitabilityIndicator.Compute(sparse, scenario);
            double pDense = IntegratedProfitabilityIndicator.Compute(dense, scenario);
            // Both models have hedge > 0 → same PAC bonus +20 cancels in diff.
            Assert.AreEqual((130.0 - 30.0) * 0.50, pDense - pSparse, 1e-6);
        }

        [Test]
        public void Compute_zero_hedge_drops_pac_bonus()
        {
            // No hedges → no PAC bonus. Sanity: bonus is only credited if
            // some hedges are present.
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.0);
            var noHedge = new EcosystemModel(initialHedgerowDensity: 0.0);
            var withHedge = new EcosystemModel(initialHedgerowDensity: 50.0);
            double pNoHedge = IntegratedProfitabilityIndicator.Compute(noHedge, scenario);
            double pWithHedge = IntegratedProfitabilityIndicator.Compute(withHedge, scenario);
            // diff = pacBonus (20) + 0 (no PSE) = 20
            Assert.AreEqual(20.0, pWithHedge - pNoHedge, 1e-6);
        }

        [Test]
        public void Normalize_at_min_returns_zero()
        {
            Assert.AreEqual(0.0,
                IntegratedProfitabilityIndicator.Normalize(IntegratedProfitabilityIndicator.MinEurosPerHectare),
                1e-9);
        }

        [Test]
        public void Normalize_at_max_returns_one()
        {
            Assert.AreEqual(1.0,
                IntegratedProfitabilityIndicator.Normalize(IntegratedProfitabilityIndicator.MaxEurosPerHectare),
                1e-9);
        }

        [Test]
        public void Normalize_clamps_negative_and_extreme()
        {
            Assert.AreEqual(0.0, IntegratedProfitabilityIndicator.Normalize(-2000.0), 1e-9);
            Assert.AreEqual(1.0, IntegratedProfitabilityIndicator.Normalize(5000.0), 1e-9);
        }
    }
}
