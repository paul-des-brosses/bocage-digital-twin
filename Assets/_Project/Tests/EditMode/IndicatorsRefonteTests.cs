using Bocage.SimulationCore.Refonte;
using Bocage.Indicators.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests des Hero KPI (Couche 04, refonte) : valeurs métier + normalisations
    /// bornées, biodiversité laggée vs pression, la trajectoire carbone (équilibre
    /// vers lequel le sol tend), la réserve en eau %RU, et l'apport de la techno.
    /// </summary>
    public sealed class IndicatorsRefonteTests
    {
        private static DailyWeather Weather(double tMean) => new DailyWeather(tMean - 4.0, tMean + 4.0, tMean, 0.0);

        [Test]
        public void Margin_value_and_normalization()
        {
            var model = new EcosystemModel(initialCropYieldTPerHa: 5.5, initialHedgerowDensityMPerHa: 90.0);
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PesticideIntensity = 1.0, TillageIntensity = 1.0 };
            double margin = HeroIndicators.MarginEurosPerHa(model, scenario);
            Assert.That(margin, Is.InRange(250.0, 450.0));
            Assert.That(HeroIndicators.MarginNormalized(margin), Is.InRange(0.0, 1.0));
        }

        [Test]
        public void Yield_value_and_normalization()
        {
            var model = new EcosystemModel(initialCropYieldTPerHa: 5.0);
            Assert.AreEqual(5.0, HeroIndicators.YieldTPerHa(model), 1e-9);
            Assert.That(HeroIndicators.YieldNormalized(5.0), Is.InRange(0.0, 1.0));
            Assert.AreEqual(0.0, HeroIndicators.YieldNormalized(-1.0), 1e-9);
            Assert.AreEqual(1.0, HeroIndicators.YieldNormalized(99.0), 1e-9);
        }

        [Test]
        public void Biodiversity_exposes_lagged_state_and_instantaneous_pressure()
        {
            var model = new EcosystemModel(initialBiodiversity: 0.30, initialHedgerowDensityMPerHa: 90.0,
                initialSoilWaterMm: 90.0, initialMineralNitrogenKgPerHa: 60.0);
            var scenario = new ScenarioContext { PesticideIntensity = 1.0 };
            Assert.AreEqual(0.30, HeroIndicators.Biodiversity(model), 1e-9, "l'état laggé est l'état du modèle");
            double pressure = HeroIndicators.BiodiversityPressure(model, scenario);
            Assert.Greater(pressure, model.Biodiversity, "la pression (cible) doit dépasser l'état laggé bas");
        }

        [Test]
        public void Carbon_trajectory_points_down_under_drought_stress()
        {
            var scenario = new ScenarioContext();

            var neutral = new EcosystemModel(initialCropYieldTPerHa: 5.5, initialSoilWaterMm: 90.0);
            neutral.SetWeather(Weather(10.0));
            double neutralEq = HeroIndicators.CarbonEquilibriumTPerHa(neutral, scenario);

            var stressed = new EcosystemModel(initialCropYieldTPerHa: 2.0, initialSoilWaterMm: 90.0);
            stressed.SetWeather(Weather(15.0)); // plus chaud → r_e ↑, rendement bas → apports ↓
            double stressedEq = HeroIndicators.CarbonEquilibriumTPerHa(stressed, scenario);

            Assert.Less(stressedEq, neutralEq, "sous stress, l'équilibre carbone (la trajectoire) descend");
            Assert.Less(stressedEq, 50.0, "le sol file alors en-dessous de la référence (en perte)");
            Assert.That(neutralEq, Is.InRange(45.0, 55.0), "au neutre, la trajectoire ~ référence BDAT");
        }

        [Test]
        public void Water_reserve_percent_reflects_theta_over_capacity()
        {
            var model = new EcosystemModel(); // C=50 → RU_max=150
            model.SetSoilWaterMm(75.0);
            Assert.AreEqual(50.0, HeroIndicators.WaterReservePercent(model), 0.5);
            Assert.AreEqual(0.5, HeroIndicators.WaterReserveNormalized(model), 0.01);
        }

        [Test]
        public void Tech_value_is_real_minus_shadow_minus_investment()
        {
            Assert.AreEqual(250.0, HeroIndicators.TechValueNetEurosPerHa(1000.0, 700.0, 50.0), 1e-9);
        }
    }
}
