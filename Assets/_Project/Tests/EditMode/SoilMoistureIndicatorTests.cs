using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Sub-étape 9α — moisture proxy used to drive the meadow shader.
    /// The current formula is a strict function of WaterTableDepth via
    /// <see cref="WaterTableIndicator.Normalize"/>; these tests pin
    /// that relationship so a future refinement (precipitation
    /// smoothing) cannot silently break callers.
    /// </summary>
    public sealed class SoilMoistureIndicatorTests
    {
        [Test]
        public void Compute_at_shallow_water_table_returns_one()
        {
            var model = new EcosystemModel(initialWaterTableDepth: WaterTableIndicator.MinDepthMeters);
            Assert.AreEqual(1.0, SoilMoistureIndicator.Compute(model), 1e-9);
        }

        [Test]
        public void Compute_at_deep_water_table_returns_zero()
        {
            var model = new EcosystemModel(initialWaterTableDepth: WaterTableIndicator.MaxDepthMeters);
            Assert.AreEqual(0.0, SoilMoistureIndicator.Compute(model), 1e-9);
        }

        [Test]
        public void Compute_is_monotone_decreasing_in_water_table_depth()
        {
            var shallow = new EcosystemModel(initialWaterTableDepth: 1.0);
            var medium  = new EcosystemModel(initialWaterTableDepth: 3.0);
            var deep    = new EcosystemModel(initialWaterTableDepth: 5.0);
            double a = SoilMoistureIndicator.Compute(shallow);
            double b = SoilMoistureIndicator.Compute(medium);
            double c = SoilMoistureIndicator.Compute(deep);
            Assert.Greater(a, b);
            Assert.Greater(b, c);
        }

        [Test]
        public void Normalize_is_identity_inside_unit_range()
        {
            Assert.AreEqual(0.0, SoilMoistureIndicator.Normalize(0.0), 1e-9);
            Assert.AreEqual(0.5, SoilMoistureIndicator.Normalize(0.5), 1e-9);
            Assert.AreEqual(1.0, SoilMoistureIndicator.Normalize(1.0), 1e-9);
        }

        [Test]
        public void Normalize_clamps_out_of_range()
        {
            Assert.AreEqual(0.0, SoilMoistureIndicator.Normalize(-0.3), 1e-9);
            Assert.AreEqual(1.0, SoilMoistureIndicator.Normalize(1.7), 1e-9);
        }
    }
}
