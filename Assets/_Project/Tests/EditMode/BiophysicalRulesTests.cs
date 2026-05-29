using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class WeatherUpdateRuleTests
    {
        // Mortagne-au-Perche annual mean from SeasonalWeatherDataDefaults
        // (10.77 °C). The σ = 2 noise per day plus the seasonal cycle average
        // out over a few simulated years, so the empirical mean over n ticks
        // should converge to this value (± a small noise floor).
        private const double ExpectedAnnualMeanCelsius = 10.77;
        private const double ExpectedAnnualMeanPrecipitationMm = 720.4 / 365.0; // 1.974 mm/day

        [Test]
        public void AnnualMeanTemperatureMatchesSeasonalAverageAtNeutralClimate()
        {
            var rule = new WeatherUpdateRule(SeasonalWeatherDataDefaults.MortagneAuPerche());
            var model = new EcosystemModel();
            var ctx = new ScenarioContext();
            var rng = new SeededRandom(1UL).DeriveSubStream(rule.SubStreamId);

            double sum = 0.0;
            const int n = 3650; // 10 years smooths both seasonality and σ=2 noise
            for (int i = 0; i < n; i++)
            {
                rule.Apply(model, ctx, rng);
                sum += model.CurrentWeather.TemperatureCelsius;
                model.AdvanceDay();
            }
            double mean = sum / n;
            Assert.That(mean, Is.EqualTo(ExpectedAnnualMeanCelsius).Within(0.5),
                "Mean daily temperature over 10 years should converge to the Mortagne-au-Perche "
                + "annual mean (~10.77 °C). Got " + mean);
        }

        [Test]
        public void PositiveTemperatureAnomalyShiftsAnnualMeanUpward()
        {
            var rule = new WeatherUpdateRule(SeasonalWeatherDataDefaults.MortagneAuPerche());
            var model = new EcosystemModel();
            var ctx = new ScenarioContext(initialTemperatureAnomalyC: 5.0);
            var rng = new SeededRandom(2UL).DeriveSubStream(rule.SubStreamId);

            double sum = 0.0;
            const int n = 3650;
            for (int i = 0; i < n; i++)
            {
                rule.Apply(model, ctx, rng);
                sum += model.CurrentWeather.TemperatureCelsius;
                model.AdvanceDay();
            }
            double mean = sum / n;
            Assert.That(mean, Is.EqualTo(ExpectedAnnualMeanCelsius + 5.0).Within(0.5),
                "A +5 °C anomaly should additively shift the annual mean by exactly 5 °C. Got " + mean);
        }

        [Test]
        public void NegativePrecipitationAnomalyReducesAnnualMean()
        {
            var rule = new WeatherUpdateRule(SeasonalWeatherDataDefaults.MortagneAuPerche());
            var model = new EcosystemModel();
            var ctx = new ScenarioContext(initialPrecipitationAnomalyPercent: -50.0);
            var rng = new SeededRandom(3UL).DeriveSubStream(rule.SubStreamId);

            double sum = 0.0;
            const int n = 3650;
            for (int i = 0; i < n; i++)
            {
                rule.Apply(model, ctx, rng);
                sum += model.CurrentWeather.PrecipitationMillimeters;
                model.AdvanceDay();
            }
            double mean = sum / n;
            double expected = ExpectedAnnualMeanPrecipitationMm * 0.5;
            Assert.That(mean, Is.EqualTo(expected).Within(0.25),
                "-50% anomaly should halve the daily precipitation expectation "
                + "(≈ 0.99 mm/day vs baseline ≈ 1.97 mm/day). Got " + mean);
        }

        [Test]
        public void DeterministicForSameSeed()
        {
            var data = SeasonalWeatherDataDefaults.MortagneAuPerche();
            var ctx = new ScenarioContext();

            var rule1 = new WeatherUpdateRule(data);
            var run1 = new EcosystemModel();
            var rng1 = new SeededRandom(42UL).DeriveSubStream(rule1.SubStreamId);

            var rule2 = new WeatherUpdateRule(data);
            var run2 = new EcosystemModel();
            var rng2 = new SeededRandom(42UL).DeriveSubStream(rule2.SubStreamId);

            for (int i = 0; i < 200; i++)
            {
                rule1.Apply(run1, ctx, rng1);
                rule2.Apply(run2, ctx, rng2);
                Assert.AreEqual(run1.CurrentWeather.TemperatureCelsius,
                                run2.CurrentWeather.TemperatureCelsius);
                Assert.AreEqual(run1.CurrentWeather.PrecipitationMillimeters,
                                run2.CurrentWeather.PrecipitationMillimeters);
                run1.AdvanceDay();
                run2.AdvanceDay();
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
        public void NoRemovalNoLoss()
        {
            var rule = new AgriculturalPressureImpactRule();
            var model = new EcosystemModel(initialHedgerowDensity: 100.0);
            var ctx = new ScenarioContext(initialHedgeRemovalRate: 0.0);
            var rng = new SeededRandom(0UL);

            for (int i = 0; i < 365; i++) rule.Apply(model, ctx, rng);

            Assert.AreEqual(100.0, model.HedgerowDensity, 1e-9);
        }

        [Test]
        public void FiveMperHaPerYearRemovalCloses100To95()
        {
            // The rate is expressed directly in m/ha/yr, no more arbitrary
            // [0,1] mapping. 5 m/ha/yr × 365 days = 5 m/ha lost over a year.
            var rule = new AgriculturalPressureImpactRule();
            var model = new EcosystemModel(initialHedgerowDensity: 100.0);
            var ctx = new ScenarioContext(initialHedgeRemovalRate: 5.0);
            var rng = new SeededRandom(0UL);

            for (int i = 0; i < 365; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.HedgerowDensity, Is.EqualTo(95.0).Within(0.01));
        }
    }
}
