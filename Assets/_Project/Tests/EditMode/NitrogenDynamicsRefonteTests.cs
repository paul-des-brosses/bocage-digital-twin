using System;
using Bocage.SimulationCore.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du bilan azoté (Couche 01, refonte) : état d'équilibre plausible et
    /// borné, lessivage porté par le drainage, minéralisation qui flambe au chaud,
    /// effet de la dose, et fermeture du bilan (ΔN = entrées − sorties, B9).
    /// </summary>
    public sealed class NitrogenDynamicsRefonteTests
    {
        private static DailyWeather Weather(double tMean)
            => new DailyWeather(tMean - 4.0, tMean + 4.0, tMean, 0.0);

        // Une année : cycle de T°, drainage hivernal, sol humide ; applique le bilan azoté.
        private static void RunYear(EcosystemModel model, ScenarioContext scenario, NitrogenDynamicsRule rule)
        {
            for (int doy = 1; doy <= 365; doy++)
            {
                double tMean = 11.0 + 8.0 * Math.Sin(2.0 * Math.PI * doy / 365.0);
                double drainage = (doy < 60 || doy > 300) ? 3.0 : 0.0; // drainage d'hiver
                model.SetWeather(Weather(tMean));
                model.SetSoilWaterMm(90.0);
                model.SetLastDrainageMm(drainage);
                rule.Apply(model, scenario, doy);
            }
        }

        [Test]
        public void Nitrogen_reaches_a_bounded_steady_state()
        {
            var model = new EcosystemModel(initialCarbonOldTPerHa: 46.43, initialMineralNitrogenKgPerHa: 40.0);
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0 };
            var rule = new NitrogenDynamicsRule();

            for (int year = 0; year < 4; year++) RunYear(model, scenario, rule);
            double afterYear4 = model.MineralNitrogenKgPerHa;
            RunYear(model, scenario, rule);
            double afterYear5 = model.MineralNitrogenKgPerHa;

            Assert.That(afterYear5, Is.InRange(10.0, 150.0), "N doit rester dans une plage agronomique plausible");
            Assert.AreEqual(afterYear4, afterYear5, 5.0, "N doit être stable d'une année sur l'autre");
        }

        [Test]
        public void Leaching_increases_with_drainage()
        {
            var model = new EcosystemModel(initialMineralNitrogenKgPerHa: 50.0);
            model.SetWeather(Weather(8.0));
            model.SetSoilWaterMm(90.0);
            var scenario = new ScenarioContext();

            model.SetLastDrainageMm(1.0);
            double low = NitrogenDynamicsRule.ComputeFlux(model, scenario, 330).LeachingKgPerHa;
            model.SetLastDrainageMm(6.0);
            double high = NitrogenDynamicsRule.ComputeFlux(model, scenario, 330).LeachingKgPerHa;

            Assert.Greater(high, low, "plus de drainage → plus de lessivage");
            Assert.Greater(low, 0.0);
        }

        [Test]
        public void Mineralisation_rises_with_temperature()
        {
            // Hors fenêtres de fertilisation/demande → les entrées = N_min + dépôt.
            var model = new EcosystemModel(initialCarbonOldTPerHa: 50.0);
            model.SetSoilWaterMm(90.0); // f_θ = 1
            var scenario = new ScenarioContext();

            model.SetWeather(Weather(5.0));
            double cool = NitrogenDynamicsRule.ComputeFlux(model, scenario, 320).InputsKgPerHa;
            model.SetWeather(Weather(15.0));
            double warm = NitrogenDynamicsRule.ComputeFlux(model, scenario, 320).InputsKgPerHa;

            Assert.Greater(warm, cool, "le réchauffement doit flamber la minéralisation (N_min)");
        }

        [Test]
        public void Higher_dose_raises_the_nitrogen_pool()
        {
            var low = new EcosystemModel(initialMineralNitrogenKgPerHa: 40.0);
            var high = new EcosystemModel(initialMineralNitrogenKgPerHa: 40.0);
            var rule = new NitrogenDynamicsRule();

            RunYear(low, new ScenarioContext { NitrogenDoseKgPerHaPerYear = 40.0 }, rule);
            RunYear(high, new ScenarioContext { NitrogenDoseKgPerHaPerYear = 220.0 }, rule);

            Assert.Greater(high.MineralNitrogenKgPerHa, low.MineralNitrogenKgPerHa,
                "une dose plus forte doit laisser un pool d'azote plus élevé");
        }

        [Test]
        public void Balance_closes_without_clamping()
        {
            var model = new EcosystemModel(initialCarbonOldTPerHa: 46.43, initialMineralNitrogenKgPerHa: 50.0);
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0 };
            var rule = new NitrogenDynamicsRule();

            double nInit = model.MineralNitrogenKgPerHa;
            double sumNet = 0.0, minN = double.MaxValue;
            for (int doy = 1; doy <= 365; doy++)
            {
                double tMean = 11.0 + 8.0 * Math.Sin(2.0 * Math.PI * doy / 365.0);
                double drainage = (doy < 60 || doy > 300) ? 3.0 : 0.0;
                model.SetWeather(Weather(tMean));
                model.SetSoilWaterMm(90.0);
                model.SetLastDrainageMm(drainage);
                sumNet += NitrogenDynamicsRule.ComputeFlux(model, scenario, doy).NetKgPerHa;
                rule.Apply(model, scenario, doy);
                if (model.MineralNitrogenKgPerHa < minN) minN = model.MineralNitrogenKgPerHa;
            }
            Assert.Greater(minN, 0.0, "N doit rester positif (système auto-stabilisant, pas d'écrêtage)");
            Assert.AreEqual(sumNet, model.MineralNitrogenKgPerHa - nInit, 1e-6, "le bilan azoté doit boucler (B9)");
        }
    }
}
