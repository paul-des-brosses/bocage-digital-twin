using Bocage.SimulationCore;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests de la flore/densité de haie et de la biodiversité (Couche 01) :
    /// capacité d'accueil pilotée par eau/intrants + levier de
    /// gestion ; biodiversité composite couplée au climat (sécheresse, intrants,
    /// canicule) avec sa latence d'~1 an.
    /// </summary>
    public sealed class FloraBiodiversityTests
    {
        private const int EightYears = 365 * 8;

        private static void RunFlora(EcosystemModel model, ScenarioContext scenario, int days)
        {
            var rule = new HedgeFloraRule();
            for (int d = 0; d < days; d++) rule.Apply(model, scenario);
        }

        private static void RunBiodiversity(EcosystemModel model, ScenarioContext scenario, int days)
        {
            var rule = new BiodiversityRule();
            for (int d = 0; d < days; d++) rule.Apply(model, scenario);
        }

        // ---------------- Flore / densité ----------------

        [Test]
        public void Reference_density_holds_near_ninety()
        {
            var model = new EcosystemModel(initialSoilWaterMm: 90.0, initialMineralNitrogenKgPerHa: 60.0);
            RunFlora(model, new ScenarioContext(), EightYears);
            Assert.That(model.HedgerowDensityMPerHa, Is.InRange(88.0, 92.0));
        }

        [Test]
        public void Drought_lowers_flora_density()
        {
            var wet = new EcosystemModel(initialSoilWaterMm: 90.0, initialMineralNitrogenKgPerHa: 60.0);
            var dry = new EcosystemModel(initialSoilWaterMm: 30.0, initialMineralNitrogenKgPerHa: 60.0);
            RunFlora(wet, new ScenarioContext(), EightYears);
            RunFlora(dry, new ScenarioContext(), EightYears);
            Assert.Less(dry.HedgerowDensityMPerHa, wet.HedgerowDensityMPerHa);
        }

        [Test]
        public void Heavy_fertilisation_lowers_flora_density()
        {
            var moderate = new EcosystemModel(initialSoilWaterMm: 90.0, initialMineralNitrogenKgPerHa: 60.0);
            var intensive = new EcosystemModel(initialSoilWaterMm: 90.0, initialMineralNitrogenKgPerHa: 180.0);
            RunFlora(moderate, new ScenarioContext(), EightYears);
            RunFlora(intensive, new ScenarioContext(), EightYears);
            Assert.Less(intensive.HedgerowDensityMPerHa, moderate.HedgerowDensityMPerHa);
        }

        [Test]
        public void Planting_raises_and_removal_lowers_density()
        {
            var planted = new EcosystemModel(initialSoilWaterMm: 90.0, initialMineralNitrogenKgPerHa: 60.0);
            var removed = new EcosystemModel(initialSoilWaterMm: 90.0, initialMineralNitrogenKgPerHa: 60.0);
            RunFlora(planted, new ScenarioContext { HedgeManagementMetersPerHaPerYear = 10.0 }, EightYears);
            RunFlora(removed, new ScenarioContext { HedgeManagementMetersPerHaPerYear = -10.0 }, EightYears);
            Assert.Greater(planted.HedgerowDensityMPerHa, 95.0);
            Assert.Less(removed.HedgerowDensityMPerHa, 85.0);
        }

        [Test]
        public void Visual_vigor_spans_full_range_unlike_the_density_floor()
        {
            Assert.That(HedgeFloraRule.VisualVigor(90.0, 40.0), Is.EqualTo(1.0).Within(1e-9),
                "à la référence, la haie est en pleine vigueur visible");
            double severe = HedgeFloraRule.VisualVigor(10.0, 240.0);
            Assert.Less(severe, 0.2, "sous stress sévère eau+azote, la vigueur visible s'effondre vers 0");
            Assert.Greater(HedgeFloraRule.WaterHealth(10.0), 0.49,
                "… alors que la santé qui pilote la densité garde son plancher de résilience 0.5");
        }

        // ---------------- Biodiversité ----------------

        [Test]
        public void Reference_biodiversity_converges_to_target()
        {
            var model = new EcosystemModel(initialSoilWaterMm: 90.0, initialHedgerowDensityMPerHa: 90.0,
                initialMineralNitrogenKgPerHa: 60.0, initialBiodiversity: 0.3);
            var scenario = new ScenarioContext { PesticideIntensity = 1.0 };
            double target = BiodiversityRule.Target(model, scenario);
            RunBiodiversity(model, scenario, EightYears);
            Assert.AreEqual(target, model.Biodiversity, 0.01);
            Assert.That(model.Biodiversity, Is.InRange(0.65, 0.71)); // 4 facteurs : le paysage pénalise la monoculture g=0
        }

        [Test]
        public void Intensification_lowers_biodiversity()
        {
            var model = new EcosystemModel(initialSoilWaterMm: 90.0, initialHedgerowDensityMPerHa: 90.0,
                initialMineralNitrogenKgPerHa: 60.0);
            double moderate = BiodiversityRule.Target(model, new ScenarioContext { PesticideIntensity = 1.0 });
            model.SetMineralNitrogenKgPerHa(180.0);
            double intensive = BiodiversityRule.Target(model, new ScenarioContext { PesticideIntensity = 2.0 });
            Assert.Less(intensive, moderate, "N + IFT élevés → biodiversité plus basse (Hallmann)");
        }

        [Test]
        public void Reducing_inputs_raises_biodiversity()
        {
            var model = new EcosystemModel(initialSoilWaterMm: 90.0, initialHedgerowDensityMPerHa: 90.0,
                initialMineralNitrogenKgPerHa: 60.0);
            double reference = BiodiversityRule.Target(model, new ScenarioContext { PesticideIntensity = 1.0 });
            model.SetMineralNitrogenKgPerHa(0.0);
            double extensive = BiodiversityRule.Target(model, new ScenarioContext { PesticideIntensity = 0.0 });
            Assert.Greater(extensive, reference, "baisser N et l'IFT doit relever la biodiversité (le gain éco)");
        }

        [Test]
        public void Heatwaves_lower_biodiversity()
        {
            var model = new EcosystemModel(initialSoilWaterMm: 90.0, initialHedgerowDensityMPerHa: 90.0,
                initialMineralNitrogenKgPerHa: 60.0);
            var scenario = new ScenarioContext { PesticideIntensity = 1.0 };
            double calm = BiodiversityRule.Target(model, scenario);
            for (int i = 0; i < 20; i++) model.RecordDailyTemperatureForWindow(32.0); // 20 jours caniculaires
            double hot = BiodiversityRule.Target(model, scenario);
            Assert.Less(hot, calm, "les canicules doivent abaisser la biodiversité");
        }

        // ---------------- Cascade : sécheresse → flore → biodiversité ----------------

        [Test]
        public void Drought_lowers_biodiversity_through_water_and_habitat()
        {
            var wet = new EcosystemModel(initialSoilWaterMm: 90.0, initialHedgerowDensityMPerHa: 90.0,
                initialMineralNitrogenKgPerHa: 60.0, initialBiodiversity: 0.5);
            var dry = new EcosystemModel(initialSoilWaterMm: 30.0, initialHedgerowDensityMPerHa: 90.0,
                initialMineralNitrogenKgPerHa: 60.0, initialBiodiversity: 0.5);
            var scenario = new ScenarioContext { PesticideIntensity = 1.0 };
            var flora = new HedgeFloraRule();
            var biodiv = new BiodiversityRule();
            for (int d = 0; d < EightYears; d++)
            {
                flora.Apply(wet, scenario); biodiv.Apply(wet, scenario);
                flora.Apply(dry, scenario); biodiv.Apply(dry, scenario);
            }
            Assert.Less(dry.Biodiversity, wet.Biodiversity,
                "la sécheresse abaisse la biodiversité par l'eau ET l'habitat (densité↓)");
        }

        // ---------------- Diversité du paysage (B2) ----------------

        [Test]
        public void Landscape_factor_peaks_at_balanced_mosaic()
        {
            double mono = BiodiversityRule.LandscapeFactor(0.0, 90.0);
            double allGrass = BiodiversityRule.LandscapeFactor(1.0, 90.0);
            double balanced = BiodiversityRule.LandscapeFactor(0.5, 90.0);
            Assert.Greater(balanced, mono, "une mosaïque équilibrée est plus diverse qu'une monoculture de culture");
            Assert.Greater(balanced, allGrass, "… et qu'une monoculture de prairie");
        }

        [Test]
        public void Landscape_factor_rises_with_hedge_network()
        {
            double sparse = BiodiversityRule.LandscapeFactor(0.3, 30.0);
            double dense = BiodiversityRule.LandscapeFactor(0.3, 130.0);
            Assert.Greater(dense, sparse, "un maillage de haies plus dense augmente la diversité du paysage");
        }

        [Test]
        public void Biodiversity_weights_sum_to_one()
        {
            Assert.AreEqual(1.0,
                BiodiversityRule.HabitatWeight + BiodiversityRule.WaterWeight
                + BiodiversityRule.InputsWeight + BiodiversityRule.LandscapeWeight, 1e-9);
        }
    }
}
