using Bocage.SimulationCore.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Test d'intégration de bout en bout (Couche 01, refonte) : le
    /// <see cref="SimulationEngine"/> assemblé, sur la climatologie réelle de
    /// Tourouvre-au-Perche. Test phare B2 : sous −50 % de pluie + réchauffement
    /// sur 10 ans, rendement, biodiversité ET carbone déclinent ensemble (la fin
    /// du « 10 ans et tout va bien sauf l'éco »). Plus déterminisme et bornes.
    /// </summary>
    public sealed class CascadeRefonteTests
    {
        private const int TenYears = 365 * 10;

        // Climatologie réelle de Tourouvre-au-Perche 2007-2024
        // (sortie de tools/extract_weather_normals.py).
        private static Climatology TourouvreClimatology()
        {
            double[] tmean = { 3.97, 4.91, 7.23, 9.76, 13.05, 16.69, 18.61, 18.25, 15.53, 11.81, 7.66, 4.87 };
            double[] tstd = { 3.74, 3.72, 3.04, 3.19, 3.11, 3.14, 2.96, 3.01, 3.09, 3.19, 3.12, 3.69 };
            double[] diurn = { 5.25, 6.8, 8.52, 10.79, 10.82, 11.26, 12.38, 11.82, 10.92, 8.18, 6.13, 5.42 };
            double[] precip = { 72.6, 54.8, 58.9, 49.6, 64.7, 62.9, 51.6, 51.1, 54.1, 73.0, 74.4, 83.9 };
            double[] pwet = { 0.417, 0.385, 0.368, 0.286, 0.313, 0.316, 0.25, 0.269, 0.29, 0.372, 0.4, 0.441 };
            double[] p11 = { 0.616, 0.611, 0.648, 0.562, 0.494, 0.509, 0.366, 0.407, 0.531, 0.585, 0.581, 0.614 };
            double[] p01 = { 0.28, 0.243, 0.202, 0.178, 0.227, 0.228, 0.211, 0.22, 0.193, 0.245, 0.279, 0.301 };
            double[] mu = { 1.344, 1.273, 1.306, 1.366, 1.436, 1.439, 1.364, 1.345, 1.36, 1.403, 1.402, 1.43 };
            double[] sig = { 0.834, 0.793, 0.775, 0.829, 0.902, 0.902, 0.982, 0.881, 0.884, 0.858, 0.864, 0.838 };
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(tmean[i], tstd[i], diurn[i], precip[i], pwet[i], p11[i], p01[i], mu[i], sig[i]);
            return new Climatology(months, 0.75, 2.157);
        }

        private static SimulationEngine MakeEngine(ulong seed, double precipitationFactor, double temperatureAnomaly)
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext
            {
                PrecipitationFactor = precipitationFactor,
                TemperatureAnomalyC = temperatureAnomaly
            };
            var weather = new WeatherGenerator(TourouvreClimatology(),
                new Bocage.SimulationCore.SeededRandom(seed).DeriveSubStream(WeatherGenerator.SubStreamId));
            return new SimulationEngine(model, scenario, weather);
        }

        [Test]
        public void B2_drought_and_warming_decline_yield_biodiversity_and_carbon()
        {
            var neutral = MakeEngine(seed: 100UL, precipitationFactor: 1.0, temperatureAnomaly: 0.0);
            var stressed = MakeEngine(seed: 100UL, precipitationFactor: 0.5, temperatureAnomaly: 3.0);
            neutral.Run(TenYears);
            stressed.Run(TenYears);

            Assert.Less(stressed.Model.CropYieldTPerHa, neutral.Model.CropYieldTPerHa,
                "le rendement doit décliner sous sécheresse + réchauffement");
            Assert.Less(stressed.Model.Biodiversity, neutral.Model.Biodiversity,
                "la biodiversité doit décliner");
            Assert.Less(stressed.Model.SoilCarbonTotalTPerHa, neutral.Model.SoilCarbonTotalTPerHa,
                "le carbone du sol doit décliner");
            Assert.Less(stressed.Model.CapitalEurosPerHa, neutral.Model.CapitalEurosPerHa,
                "le capital doit décliner");
        }

        [Test]
        public void Neutral_run_stays_in_realistic_bounds()
        {
            var engine = MakeEngine(seed: 100UL, precipitationFactor: 1.0, temperatureAnomaly: 0.0);
            engine.Run(TenYears);
            var m = engine.Model;
            Assert.That(m.CropYieldTPerHa, Is.InRange(3.0, 6.5), "rendement neutre plausible");
            Assert.That(m.Biodiversity, Is.InRange(0.35, 0.9), "biodiversité neutre plausible");
            Assert.That(m.SoilCarbonTotalTPerHa, Is.InRange(42.0, 58.0), "carbone neutre ~ référence BDAT");
            Assert.That(m.SoilWaterMm, Is.InRange(0.0, 135.0), "θ borné par RU_max");
            Assert.That(m.WaterTableDepthM, Is.InRange(0.0, 3.5), "nappe bornée près de l'équilibre profond");
        }

        [Test]
        public void Determinism_same_seed_same_state()
        {
            var a = MakeEngine(seed: 7UL, precipitationFactor: 1.0, temperatureAnomaly: 0.0);
            var b = MakeEngine(seed: 7UL, precipitationFactor: 1.0, temperatureAnomaly: 0.0);
            a.Run(730);
            b.Run(730);
            Assert.AreEqual(a.Model.CropYieldTPerHa, b.Model.CropYieldTPerHa, 1e-9);
            Assert.AreEqual(a.Model.SoilCarbonTotalTPerHa, b.Model.SoilCarbonTotalTPerHa, 1e-9);
            Assert.AreEqual(a.Model.Biodiversity, b.Model.Biodiversity, 1e-9);
            Assert.AreEqual(a.Model.MineralNitrogenKgPerHa, b.Model.MineralNitrogenKgPerHa, 1e-9);
        }

        [Test]
        public void Building_soil_with_cover_crops_raises_carbon_over_a_decade()
        {
            var baseline = MakeEngine(seed: 50UL, precipitationFactor: 1.0, temperatureAnomaly: 0.0);
            var withCover = MakeEngine(seed: 50UL, precipitationFactor: 1.0, temperatureAnomaly: 0.0);
            withCover.Scenario.CoverCropsCoveragePercent = 100.0;
            baseline.Run(TenYears);
            withCover.Run(TenYears);
            Assert.Greater(withCover.Model.SoilCarbonTotalTPerHa, baseline.Model.SoilCarbonTotalTPerHa,
                "les couverts (apports↑) doivent bâtir du carbone sur une décennie");
        }
    }
}
