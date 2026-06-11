using Bocage.SimulationCore.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du rendement (Mitscherlich limité eau+azote) et de la pression
    /// d'adventices (Couche 01, refonte), dont l'arbitrage non-labour ↔ phyto ↔
    /// rendement : le semis direct sans désherbage chimique fait grimper le
    /// salissement et pénalise le rendement.
    /// </summary>
    public sealed class YieldWeedRefonteTests
    {
        private static DailyWeather Weather(double tMean) => new DailyWeather(tMean - 4.0, tMean + 4.0, tMean, 0.0);

        private static void RunWeed(EcosystemModel model, ScenarioContext scenario, int days)
        {
            var rule = new WeedPressureRule();
            for (int d = 0; d < days; d++) rule.Apply(model, scenario);
        }

        private static void RunYield(EcosystemModel model, int days)
        {
            var rule = new YieldRule();
            for (int d = 0; d < days; d++) rule.Apply(model, (d % 365) + 1);
        }

        // ---------------- Adventices ----------------

        [Test]
        public void No_till_without_pesticide_grows_weeds()
        {
            var model = new EcosystemModel(initialWeedPressure: 0.2);
            RunWeed(model, new ScenarioContext { TillageIntensity = 0.0, PesticideIntensity = 0.0 }, 600);
            Assert.Greater(model.WeedPressure, 0.8, "semis direct + zéro phyto → salissement élevé");
        }

        [Test]
        public void Tillage_and_pesticide_suppress_weeds()
        {
            var model = new EcosystemModel(initialWeedPressure: 0.8);
            RunWeed(model, new ScenarioContext { TillageIntensity = 1.0, PesticideIntensity = 1.0 }, 600);
            Assert.Less(model.WeedPressure, 0.1, "labour + phyto → adventices maîtrisées");
        }

        [Test]
        public void Reducing_tillage_raises_the_weed_target()
        {
            double noTill = WeedPressureRule.Target(new ScenarioContext { TillageIntensity = 0.0, PesticideIntensity = 1.0 });
            double plowed = WeedPressureRule.Target(new ScenarioContext { TillageIntensity = 1.0, PesticideIntensity = 1.0 });
            Assert.Greater(noTill, plowed);
        }

        // ---------------- Rendement ----------------

        [Test]
        public void Reference_yield_approaches_potential()
        {
            var model = new EcosystemModel(initialCropYieldTPerHa: 3.0,
                initialMineralNitrogenKgPerHa: 120.0, initialWeedPressure: 0.0);
            model.SetWeather(Weather(15.0));
            model.SetSoilWaterMm(90.0);
            RunYield(model, 800);
            Assert.That(model.CropYieldTPerHa, Is.InRange(7.2, 7.7),
                "au référentiel (eau ample, N suffisant, sans adventices) → ~potentiel 7,6 t/ha");
        }

        [Test]
        public void Drought_cuts_yield()
        {
            var wet = new EcosystemModel(initialMineralNitrogenKgPerHa: 120.0, initialWeedPressure: 0.0);
            var dry = new EcosystemModel(initialMineralNitrogenKgPerHa: 120.0, initialWeedPressure: 0.0);
            wet.SetSoilWaterMm(90.0);
            dry.SetSoilWaterMm(15.0);
            RunYield(wet, 800);
            RunYield(dry, 800);
            Assert.Less(dry.CropYieldTPerHa, wet.CropYieldTPerHa);
            Assert.Less(dry.CropYieldTPerHa, 4.0, "stress hydrique fort → rendement nettement réduit");
        }

        [Test]
        public void Nitrogen_shortage_cuts_yield()
        {
            var high = new EcosystemModel(initialMineralNitrogenKgPerHa: 120.0, initialWeedPressure: 0.0);
            var low = new EcosystemModel(initialMineralNitrogenKgPerHa: 15.0, initialWeedPressure: 0.0);
            high.SetSoilWaterMm(90.0);
            low.SetSoilWaterMm(90.0);
            RunYield(high, 800);
            RunYield(low, 800);
            Assert.Less(low.CropYieldTPerHa, high.CropYieldTPerHa);
        }

        [Test]
        public void Weeds_cut_yield()
        {
            var clean = new EcosystemModel(initialMineralNitrogenKgPerHa: 120.0, initialWeedPressure: 0.0);
            var weedy = new EcosystemModel(initialMineralNitrogenKgPerHa: 120.0, initialWeedPressure: 1.0);
            clean.SetSoilWaterMm(90.0);
            weedy.SetSoilWaterMm(90.0);
            RunYield(clean, 800);
            RunYield(weedy, 800);
            Assert.Less(weedy.CropYieldTPerHa, clean.CropYieldTPerHa);
        }

        // ---------------- Arbitrage non-labour ↔ phyto ↔ rendement ----------------

        [Test]
        public void No_till_without_pesticide_hurts_yield_via_weeds()
        {
            var controlled = new EcosystemModel(initialMineralNitrogenKgPerHa: 120.0, initialWeedPressure: 0.2);
            var infested = new EcosystemModel(initialMineralNitrogenKgPerHa: 120.0, initialWeedPressure: 0.2);
            controlled.SetSoilWaterMm(90.0);
            infested.SetSoilWaterMm(90.0);
            var sControlled = new ScenarioContext { TillageIntensity = 0.0, PesticideIntensity = 1.0 };
            var sInfested = new ScenarioContext { TillageIntensity = 0.0, PesticideIntensity = 0.0 };
            var weed = new WeedPressureRule();
            var yield = new YieldRule();
            for (int d = 0; d < 800; d++)
            {
                int doy = (d % 365) + 1;
                weed.Apply(controlled, sControlled); yield.Apply(controlled, doy);
                weed.Apply(infested, sInfested); yield.Apply(infested, doy);
            }
            Assert.Less(infested.CropYieldTPerHa, controlled.CropYieldTPerHa,
                "le non-labour sans désherbage chimique pénalise le rendement (salissement)");
        }
    }
}
