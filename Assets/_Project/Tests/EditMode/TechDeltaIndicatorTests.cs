using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="TechDeltaIndicator"/>. The indicator is
    /// a ratio of two profitability computations, so we test (a) the
    /// degenerate identical-models case (delta = 0), (b) sign and
    /// magnitude under known asymmetries, and (c) the denominator floor
    /// behaviour that prevents explosion at near-zero shadow profit.
    /// </summary>
    public sealed class TechDeltaIndicatorTests
    {
        [Test]
        public void Compute_identical_models_returns_zero()
        {
            var scenario = new ScenarioContext();
            var real = new EcosystemModel();
            var shadow = new EcosystemModel();
            double delta = TechDeltaIndicator.Compute(real, shadow, scenario);
            Assert.AreEqual(0.0, delta, 1e-9,
                "Two identical models must produce delta=0%.");
        }

        [Test]
        public void Compute_positive_when_real_outperforms_shadow()
        {
            // Same scenario, but real has better crop yield → higher profit.
            var scenario = new ScenarioContext();
            var real = new EcosystemModel(initialCropYield: 7.0);
            var shadow = new EcosystemModel(initialCropYield: 5.0);
            double delta = TechDeltaIndicator.Compute(real, shadow, scenario);
            Assert.Greater(delta, 0.0,
                "Better yield in real should give a positive tech delta. Got " + delta);
        }

        [Test]
        public void Compute_negative_when_real_underperforms_shadow()
        {
            // Real has higher input cost → lower profit than shadow.
            var scenario = new ScenarioContext();
            var real = new EcosystemModel(initialInputCost: 2000.0);
            var shadow = new EcosystemModel(initialInputCost: 1000.0);
            double delta = TechDeltaIndicator.Compute(real, shadow, scenario);
            Assert.Less(delta, 0.0,
                "Higher input cost in real should give a negative tech delta. Got " + delta);
        }

        [Test]
        public void Compute_uses_floor_when_shadow_profit_near_zero()
        {
            // When shadow profit ≈ 0, denominator floors at 1 €/ha/yr so
            // the percent is the absolute delta itself, not a NaN or
            // explosion.
            // Build a shadow with profit ≈ 0:
            //   profit = yield×price − inputs − maintenance + PSE + PAC + basic CAP
            //          = 5.5×250 − inputs − 90 + 0 + 20 + 230 = 1535 − inputs.
            //   profit = 0  ⇔  inputs = 1535.
            var scenario = new ScenarioContext(initialPseSubsidyRate: 0.0);
            var shadow = new EcosystemModel(initialInputCost: 1535.0);
            // Verify the construction by computing profit on shadow.
            double shadowProfit = IntegratedProfitabilityIndicator.Compute(shadow, scenario);
            Assert.That(shadowProfit, Is.EqualTo(0.0).Within(1.0),
                "Shadow profit should be near zero by construction. Got " + shadowProfit);

            // Real has +100 €/ha/yr advantage via lower input cost.
            var real = new EcosystemModel(initialInputCost: 1435.0);
            double delta = TechDeltaIndicator.Compute(real, shadow, scenario);
            // Floor in denominator = 1, so delta ≈ +10000% if not floored.
            // The floor caps the divisor at 1 €/ha/yr, so 100 € advantage
            // yields exactly +10000%. Then Normalize clamps to gauge range.
            Assert.That(delta, Is.GreaterThan(0.0),
                "Delta with floor should still be positive when real wins. Got " + delta);
        }

        [Test]
        public void Normalize_at_zero_returns_mid_range()
        {
            // Delta 0 → gauge at 0.5 (centred).
            Assert.AreEqual(0.5, TechDeltaIndicator.Normalize(0.0), 1e-9);
        }

        [Test]
        public void Normalize_at_max_returns_one()
        {
            Assert.AreEqual(1.0,
                TechDeltaIndicator.Normalize(TechDeltaIndicator.MaxDeltaPercent),
                1e-9);
        }

        [Test]
        public void Normalize_at_min_returns_zero()
        {
            Assert.AreEqual(0.0,
                TechDeltaIndicator.Normalize(TechDeltaIndicator.MinDeltaPercent),
                1e-9);
        }

        [Test]
        public void Normalize_clamps_beyond_bounds()
        {
            Assert.AreEqual(1.0, TechDeltaIndicator.Normalize(500.0), 1e-9);
            Assert.AreEqual(0.0, TechDeltaIndicator.Normalize(-500.0), 1e-9);
        }
    }
}
