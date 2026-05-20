using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class WaterTableIndicatorTests
    {
        [Test]
        public void Compute_returns_model_water_table_depth_unchanged()
        {
            var model = new EcosystemModel(initialWaterTableDepth: 2.4);
            Assert.AreEqual(2.4, WaterTableIndicator.Compute(model), 1e-9);
        }

        [Test]
        public void Normalize_at_shallow_bound_returns_one()
        {
            // Shallow = healthy = 1.0 by convention (see indicator XMLdoc).
            double v = WaterTableIndicator.Normalize(WaterTableIndicator.MinDepthMeters);
            Assert.AreEqual(1.0, v, 1e-9);
        }

        [Test]
        public void Normalize_at_deep_bound_returns_zero()
        {
            double v = WaterTableIndicator.Normalize(WaterTableIndicator.MaxDepthMeters);
            Assert.AreEqual(0.0, v, 1e-9);
        }

        [Test]
        public void Normalize_clamps_shallower_than_min()
        {
            double v = WaterTableIndicator.Normalize(WaterTableIndicator.MinDepthMeters - 1.0);
            Assert.AreEqual(1.0, v, 1e-9);
        }

        [Test]
        public void Normalize_clamps_deeper_than_max()
        {
            double v = WaterTableIndicator.Normalize(WaterTableIndicator.MaxDepthMeters + 5.0);
            Assert.AreEqual(0.0, v, 1e-9);
        }

        [Test]
        public void Normalize_is_monotone_decreasing_inside_range()
        {
            double a = WaterTableIndicator.Normalize(1.0); // shallow
            double b = WaterTableIndicator.Normalize(3.0);
            double c = WaterTableIndicator.Normalize(5.0); // deep
            Assert.Greater(a, b);
            Assert.Greater(b, c);
        }
    }
}
