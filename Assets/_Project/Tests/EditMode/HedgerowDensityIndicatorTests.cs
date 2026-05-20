using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class HedgerowDensityIndicatorTests
    {
        [Test]
        public void Compute_returns_model_value_unchanged()
        {
            var model = new EcosystemModel(initialHedgerowDensity: 87.5);
            double v = HedgerowDensityIndicator.Compute(model);
            Assert.AreEqual(87.5, v, 1e-9);
        }

        [Test]
        public void Normalize_at_min_returns_zero()
        {
            double v = HedgerowDensityIndicator.Normalize(HedgerowDensityIndicator.MinMetersPerHectare);
            Assert.AreEqual(0.0, v, 1e-9);
        }

        [Test]
        public void Normalize_at_max_returns_one()
        {
            double v = HedgerowDensityIndicator.Normalize(HedgerowDensityIndicator.MaxMetersPerHectare);
            Assert.AreEqual(1.0, v, 1e-9);
        }

        [Test]
        public void Normalize_clamps_below_min()
        {
            double v = HedgerowDensityIndicator.Normalize(HedgerowDensityIndicator.MinMetersPerHectare - 50.0);
            Assert.AreEqual(0.0, v, 1e-9);
        }

        [Test]
        public void Normalize_clamps_above_max()
        {
            double v = HedgerowDensityIndicator.Normalize(HedgerowDensityIndicator.MaxMetersPerHectare + 50.0);
            Assert.AreEqual(1.0, v, 1e-9);
        }

        [Test]
        public void Normalize_is_monotone_increasing_inside_range()
        {
            double a = HedgerowDensityIndicator.Normalize(60.0);
            double b = HedgerowDensityIndicator.Normalize(90.0);
            double c = HedgerowDensityIndicator.Normalize(120.0);
            Assert.Less(a, b);
            Assert.Less(b, c);
        }
    }
}
