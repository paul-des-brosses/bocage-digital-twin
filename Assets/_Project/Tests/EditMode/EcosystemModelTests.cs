using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class EcosystemModelTests
    {
        [Test]
        public void DefaultsLandInPercheRealisticRanges()
        {
            var model = new EcosystemModel();

            Assert.AreEqual(0, model.CurrentDay);
            Assert.GreaterOrEqual(model.HedgerowDensity, 60.0,
                "Default hedgerow density should sit in Perche range (60-130 m/ha).");
            Assert.LessOrEqual(model.HedgerowDensity, 130.0);
            Assert.GreaterOrEqual(model.WaterTableDepth, 0.0);
        }

        [Test]
        public void AdvanceDayIncrementsCurrentDay()
        {
            var model = new EcosystemModel();
            for (int i = 0; i < 365; i++) model.AdvanceDay();
            Assert.AreEqual(365, model.CurrentDay);
        }

        [Test]
        public void NegativeWaterTableDepthIsClamped()
        {
            var model = new EcosystemModel();
            model.SetWaterTableDepth(-3.5);
            Assert.AreEqual(0.0, model.WaterTableDepth);
        }

        [Test]
        public void NegativeHedgerowDensityIsClamped()
        {
            var model = new EcosystemModel();
            model.SetHedgerowDensity(-10.0);
            Assert.AreEqual(0.0, model.HedgerowDensity);
        }

        [Test]
        public void NegativePrecipitationIsClampedAtConstruction()
        {
            var w = new Weather(15.0, -2.0);
            Assert.AreEqual(0.0, w.PrecipitationMillimeters);
        }

        [Test]
        public void SettersDoNotMutateUnrelatedFields()
        {
            var model = new EcosystemModel(
                initialWeather: new Weather(12.0, 1.5),
                initialWaterTableDepth: 2.0,
                initialHedgerowDensity: 90.0);

            model.SetHedgerowDensity(100.0);

            Assert.AreEqual(12.0, model.CurrentWeather.TemperatureCelsius);
            Assert.AreEqual(1.5, model.CurrentWeather.PrecipitationMillimeters);
            Assert.AreEqual(2.0, model.WaterTableDepth);
            Assert.AreEqual(100.0, model.HedgerowDensity);
        }
    }
}
