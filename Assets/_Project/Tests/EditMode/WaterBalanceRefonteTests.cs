using System;
using Bocage.SimulationCore.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du bilan hydrique de la refonte (Couche 01) : capacité couplée au
    /// carbone, ETP de Hargreaves croissante avec la température, bornes de θ,
    /// vidange sous sécheresse, conservation de la masse (B9), et le test phare
    /// « le réchauffement assèche le sol à pluie égale ».
    /// </summary>
    public sealed class WaterBalanceRefonteTests
    {
        private static DailyWeather Weather(double tMean, double precip, double diurnal = 8.0)
            => new DailyWeather(tMean - diurnal / 2.0, tMean + diurnal / 2.0, tMean, precip);

        [Test]
        public void Capacity_increases_with_soil_carbon()
        {
            Assert.AreEqual(WaterBalanceRule.RuBaseMm,
                WaterBalanceRule.SoilWaterCapacityMm(50.0), 1e-9);
            Assert.Greater(WaterBalanceRule.SoilWaterCapacityMm(60.0),
                WaterBalanceRule.SoilWaterCapacityMm(50.0));
            Assert.Less(WaterBalanceRule.SoilWaterCapacityMm(40.0),
                WaterBalanceRule.SoilWaterCapacityMm(50.0));
        }

        [Test]
        public void Et0_rises_with_temperature_and_in_summer()
        {
            // Même jour, plus chaud → plus d'ETP.
            double cool = Hargreaves.ReferenceEt0(196, 48.5, 10, 18, 14);
            double warm = Hargreaves.ReferenceEt0(196, 48.5, 15, 27, 21);
            Assert.Greater(warm, cool);

            // Été nettement supérieur à l'hiver.
            double summer = Hargreaves.ReferenceEt0(196, 48.5, 12, 24, 18);
            double winter = Hargreaves.ReferenceEt0(15, 48.5, 0, 6, 3);
            Assert.Greater(summer, winter);
            Assert.Greater(summer, 3.0, "ETP estivale ~4-5 mm/j attendue");
            Assert.Less(winter, 1.5, "ETP hivernale < 1,5 mm/j attendue");
        }

        [Test]
        public void Soil_water_stays_within_bounds_over_a_year()
        {
            var model = new EcosystemModel();   // C=50 → RU_max=130
            double ruMax = WaterBalanceRule.SoilWaterCapacityMm(model.SoilCarbonTotalTPerHa);
            var rule = new WaterBalanceRule();
            var noise = new Random(1);          // météo de test (hors modèle) — déterministe via seed
            for (int day = 1; day <= 365; day++)
            {
                double precip = noise.NextDouble() < 0.35 ? noise.NextDouble() * 20.0 : 0.0;
                double tMean = 11.0 + 8.0 * Math.Sin(2.0 * Math.PI * day / 365.0);
                model.SetWeather(Weather(tMean, precip));
                rule.Apply(model, day);
                Assert.GreaterOrEqual(model.SoilWaterMm, 0.0);
                Assert.LessOrEqual(model.SoilWaterMm, ruMax + 1e-9);
            }
        }

        [Test]
        public void Sustained_drought_drains_the_reserve()
        {
            var model = new EcosystemModel(initialSoilWaterMm: 100.0);
            var rule = new WaterBalanceRule();
            for (int day = 1; day <= 120; day++)
            {
                model.SetWeather(Weather(22.0, 0.0)); // chaud et sec
                rule.Apply(model, 196);
            }
            Assert.Less(model.SoilWaterMm, 20.0,
                "une sécheresse soutenue doit vider la réserve en eau du sol");
        }

        [Test]
        public void Warming_dries_the_soil_in_a_dry_spell()
        {
            double ruMax = WaterBalanceRule.SoilWaterCapacityMm(50.0);
            var cool = new EcosystemModel(initialSoilWaterMm: ruMax);
            var warm = new EcosystemModel(initialSoilWaterMm: ruMax);
            var rule = new WaterBalanceRule();
            for (int day = 0; day < 25; day++)
            {
                cool.SetWeather(Weather(18.0, 0.0));
                warm.SetWeather(Weather(23.0, 0.0)); // +5 °C, pluie nulle dans les deux cas
                rule.Apply(cool, 196);
                rule.Apply(warm, 196);
            }
            Assert.Less(warm.SoilWaterMm, cool.SoilWaterMm, "+5 °C doit assécher plus vite à pluie égale");
            Assert.Greater(cool.SoilWaterMm, 0.0, "le run frais ne doit pas être totalement vidé sur 25 j");
        }

        [Test]
        public void Water_balance_conserves_mass()
        {
            var model = new EcosystemModel(initialSoilWaterMm: 80.0);
            var rule = new WaterBalanceRule();
            var noise = new Random(42);
            double thetaInit = model.SoilWaterMm;
            double sumPrecip = 0.0, sumEt = 0.0, sumDrain = 0.0;
            for (int day = 1; day <= 300; day++)
            {
                double precip = noise.NextDouble() < 0.40 ? noise.NextDouble() * 25.0 : 0.0;
                double tMean = 11.0 + 8.0 * Math.Sin(2.0 * Math.PI * day / 365.0);
                model.SetWeather(Weather(tMean, precip));
                rule.Apply(model, day);
                sumPrecip += precip;
                sumEt += model.LastEvapotranspirationMm;
                sumDrain += model.LastDrainageMm;
            }
            double balance = sumPrecip - sumEt - sumDrain - (model.SoilWaterMm - thetaInit);
            Assert.AreEqual(0.0, balance, 1e-6, "le bilan hydrique doit boucler (entrées = sorties + Δθ)");
        }
    }
}
