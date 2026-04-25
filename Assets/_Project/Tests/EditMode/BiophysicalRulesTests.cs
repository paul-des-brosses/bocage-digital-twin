using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class WeatherUpdateRuleTests
    {
        [Test]
        public void MeanTemperatureCloseTo12AtNeutralClimate()
        {
            var rule = new WeatherUpdateRule();
            var model = new EcosystemModel();
            var ctx = new ScenarioContext();
            var rng = new SeededRandom(1UL).DeriveSubStream(rule.SubStreamId);

            double sum = 0.0;
            const int n = 2000;
            for (int i = 0; i < n; i++)
            {
                rule.Apply(model, ctx, rng);
                sum += model.CurrentWeather.TemperatureCelsius;
            }
            double mean = sum / n;
            Assert.That(mean, Is.EqualTo(12.0).Within(0.5),
                "Mean daily temperature should converge to ~12 °C at neutral climate stress.");
        }

        [Test]
        public void HighClimateStressShiftsTemperatureUpward()
        {
            var rule = new WeatherUpdateRule();
            var model = new EcosystemModel();
            var ctx = new ScenarioContext(initialClimateStress: 1.0);
            var rng = new SeededRandom(2UL).DeriveSubStream(rule.SubStreamId);

            double sum = 0.0;
            const int n = 2000;
            for (int i = 0; i < n; i++)
            {
                rule.Apply(model, ctx, rng);
                sum += model.CurrentWeather.TemperatureCelsius;
            }
            double mean = sum / n;
            Assert.That(mean, Is.EqualTo(17.0).Within(0.5),
                "Climate stress = 1 should add ~5 °C to the mean.");
        }

        [Test]
        public void DeterministicForSameSeed()
        {
            var rule = new WeatherUpdateRule();
            var ctx = new ScenarioContext();

            var run1 = new EcosystemModel();
            var rng1 = new SeededRandom(42UL).DeriveSubStream(rule.SubStreamId);
            var run2 = new EcosystemModel();
            var rng2 = new SeededRandom(42UL).DeriveSubStream(rule.SubStreamId);

            for (int i = 0; i < 50; i++)
            {
                rule.Apply(run1, ctx, rng1);
                rule.Apply(run2, ctx, rng2);
                Assert.AreEqual(run1.CurrentWeather.TemperatureCelsius,
                                run2.CurrentWeather.TemperatureCelsius);
                Assert.AreEqual(run1.CurrentWeather.PrecipitationMillimeters,
                                run2.CurrentWeather.PrecipitationMillimeters);
            }
        }
    }

    public sealed class WaterTableDynamicsRuleTests
    {
        [Test]
        public void HeavyRainLowersDepth()
        {
            var rule = new WaterTableDynamicsRule();
            var model = new EcosystemModel(initialWaterTableDepth: 2.0);
            model.SetWeather(new Weather(0.0, 50.0));
            var ctx = new ScenarioContext();
            var rng = new SeededRandom(0UL);

            rule.Apply(model, ctx, rng);

            Assert.Less(model.WaterTableDepth, 2.0);
        }

        [Test]
        public void HotDryDayRaisesDepth()
        {
            var rule = new WaterTableDynamicsRule();
            var model = new EcosystemModel(initialWaterTableDepth: 2.0);
            model.SetWeather(new Weather(30.0, 0.0));
            var ctx = new ScenarioContext();
            var rng = new SeededRandom(0UL);

            rule.Apply(model, ctx, rng);

            Assert.Greater(model.WaterTableDepth, 2.0);
        }
    }

    public sealed class HedgerowGrowthRuleTests
    {
        [Test]
        public void GrowthAccumulatesOverAYearAtIdealDepth()
        {
            var rule = new HedgerowGrowthRule();
            var model = new EcosystemModel(initialWaterTableDepth: 2.0, initialHedgerowDensity: 90.0);
            var ctx = new ScenarioContext();
            var rng = new SeededRandom(0UL);

            for (int i = 0; i < 365; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.HedgerowDensity, Is.EqualTo(90.5).Within(0.01),
                "At ideal water table, annual growth should be ≈ 0.5 m/ha.");
        }

        [Test]
        public void DeepDroughtStallsGrowth()
        {
            var rule = new HedgerowGrowthRule();
            var model = new EcosystemModel(initialWaterTableDepth: 10.0, initialHedgerowDensity: 90.0);
            var ctx = new ScenarioContext();
            var rng = new SeededRandom(0UL);

            for (int i = 0; i < 365; i++) rule.Apply(model, ctx, rng);

            Assert.AreEqual(90.0, model.HedgerowDensity, 1e-9,
                "Drought (depth = 10 m) should drive the growth multiplier to zero.");
        }
    }

    public sealed class AgriculturalPressureImpactRuleTests
    {
        [Test]
        public void NoPressureNoLoss()
        {
            var rule = new AgriculturalPressureImpactRule();
            var model = new EcosystemModel(initialHedgerowDensity: 100.0);
            var ctx = new ScenarioContext(initialAgriculturalPressure: 0.0);
            var rng = new SeededRandom(0UL);

            for (int i = 0; i < 365; i++) rule.Apply(model, ctx, rng);

            Assert.AreEqual(100.0, model.HedgerowDensity, 1e-9);
        }

        [Test]
        public void FullPressureRemovesAboutFiveMetersPerHectarePerYear()
        {
            var rule = new AgriculturalPressureImpactRule();
            var model = new EcosystemModel(initialHedgerowDensity: 100.0);
            var ctx = new ScenarioContext(initialAgriculturalPressure: 1.0);
            var rng = new SeededRandom(0UL);

            for (int i = 0; i < 365; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.HedgerowDensity, Is.EqualTo(95.0).Within(0.01));
        }
    }
}
