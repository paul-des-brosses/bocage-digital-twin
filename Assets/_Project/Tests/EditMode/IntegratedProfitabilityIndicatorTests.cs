using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class IntegratedProfitabilityIndicatorTests
    {
        [Test]
        public void Compute_at_baseline_state_matches_hand_calculation()
        {
            // Defaults: yield 5.0 t/ha, inputs 400 €/ha/yr, maintenance 27 €/ha/yr,
            // hedge 90 m/ha. Revenue = 5 × 250 = 1250. PSE = 90 × 0.5 = 45.
            // Profit = 1250 - 400 - 27 + 45 = 868.
            var model = new EcosystemModel();
            double profit = IntegratedProfitabilityIndicator.Compute(model);
            Assert.AreEqual(868.0, profit, 1e-6);
        }

        [Test]
        public void Compute_increases_with_yield_all_else_equal()
        {
            var low = new EcosystemModel(initialCropYield: 3.0);
            var high = new EcosystemModel(initialCropYield: 7.0);
            Assert.Less(
                IntegratedProfitabilityIndicator.Compute(low),
                IntegratedProfitabilityIndicator.Compute(high));
        }

        [Test]
        public void Compute_decreases_with_input_cost_all_else_equal()
        {
            var cheap = new EcosystemModel(initialInputCost: 200.0);
            var costly = new EcosystemModel(initialInputCost: 800.0);
            Assert.Greater(
                IntegratedProfitabilityIndicator.Compute(cheap),
                IntegratedProfitabilityIndicator.Compute(costly));
        }

        [Test]
        public void Compute_can_be_negative_under_extreme_costs()
        {
            // Tank yield, blow up input cost → profit goes negative.
            var model = new EcosystemModel(
                initialCropYield: 0.5,
                initialInputCost: 1100.0,
                initialMaintenanceCost: 50.0,
                initialHedgerowDensity: 0.0);
            double profit = IntegratedProfitabilityIndicator.Compute(model);
            Assert.Less(profit, 0.0);
        }

        [Test]
        public void Compute_grows_with_hedge_density_via_PSE()
        {
            // All else equal, more hedges → more PSE income.
            // (Maintenance growing linearly with hedges would normally offset
            // partially, but here we hold MaintenanceCost constant to isolate
            // the PSE term; in a full simulation MaintenanceCostDynamicsRule
            // would re-link them — the indicator itself reads whatever the
            // model exposes.)
            var sparse = new EcosystemModel(initialHedgerowDensity: 30.0);
            var dense = new EcosystemModel(initialHedgerowDensity: 130.0);
            double pSparse = IntegratedProfitabilityIndicator.Compute(sparse);
            double pDense = IntegratedProfitabilityIndicator.Compute(dense);
            Assert.AreEqual((130.0 - 30.0) * IntegratedProfitabilityIndicator.HedgerowPseRate,
                            pDense - pSparse,
                            1e-6);
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
            Assert.AreEqual(0.0, IntegratedProfitabilityIndicator.Normalize(-500.0), 1e-9);
            Assert.AreEqual(1.0, IntegratedProfitabilityIndicator.Normalize(5000.0), 1e-9);
        }
    }
}
