using Bocage.SimulationCore;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests de l'économie (Couche 01) : marge de référence plausible,
    /// effondrement sous sécheresse, et la monétisation des services
    /// écosystémiques (PSE haies, MAEC bas-phyto, crédit carbone) qui rend
    /// l'écologie rentable en euros traçables.
    /// </summary>
    public sealed class EconomyTests
    {
        private static EcosystemModel ReferenceModel()
            => new EcosystemModel(initialCropYieldTPerHa: 5.5,
                initialHedgerowDensityMPerHa: 90.0,
                initialCarbonYoungTPerHa: 3.0, initialCarbonOldTPerHa: 47.0);

        private static ScenarioContext ReferenceScenario()
            => new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PesticideIntensity = 1.0, TillageIntensity = 1.0 };

        [Test]
        public void Reference_margin_is_plausible()
        {
            double margin = EconomyRule.AnnualMarginEurosPerHa(ReferenceModel(), ReferenceScenario());
            Assert.That(margin, Is.InRange(250.0, 450.0), "marge de référence ~ marges réelles Perche");
        }

        [Test]
        public void Drought_collapses_the_margin()
        {
            var model = ReferenceModel();
            model.SetCropYieldTPerHa(3.0); // mauvaise année (sécheresse)
            double margin = EconomyRule.AnnualMarginEurosPerHa(model, ReferenceScenario());
            Assert.Less(margin, 0.0, "une forte chute de rendement doit rendre l'année déficitaire");
        }

        [Test]
        public void Lower_nitrogen_dose_costs_less()
        {
            var model = ReferenceModel();
            double high = EconomyRule.AnnualMarginEurosPerHa(model, new ScenarioContext { NitrogenDoseKgPerHaPerYear = 200.0 });
            double low = EconomyRule.AnnualMarginEurosPerHa(model, new ScenarioContext { NitrogenDoseKgPerHaPerYear = 40.0 });
            Assert.Greater(low, high, "à rendement égal, moins d'azote coûte moins cher");
        }

        [Test]
        public void Low_pesticide_unlocks_maec_payment()
        {
            var model = ReferenceModel();
            double conventional = EconomyRule.AnnualMarginEurosPerHa(model, new ScenarioContext { PesticideIntensity = 1.0 });
            double lowInput = EconomyRule.AnnualMarginEurosPerHa(model, new ScenarioContext { PesticideIntensity = 0.5 });
            Assert.Greater(lowInput, conventional, "baisser l'IFT débloque la MAEC + économise du phyto");
        }

        [Test]
        public void Full_grassland_qualifies_for_maec_even_with_stale_pesticide()
        {
            // 100 % prairie : aucune culture à traiter → IFT effectif 0 → MAEC due, même si le
            // slider phyto est resté à 1 (le fix : la MAEC se gate sur l'usage effectif, pas sur le slider).
            MarginBreakdown bd = EconomyRule.Breakdown(ReferenceModel(),
                new ScenarioContext { GrasslandFraction = 1.0, PesticideIntensity = 1.0 });
            Assert.AreEqual(EconomyRule.MaecPaymentEurosPerHa, bd.MaecEurosPerHa, 1e-9,
                "une ferme 100 % prairie ne pulvérise rien → MAEC due malgré le slider phyto");
            Assert.AreEqual(0.0, bd.PesticideCostEurosPerHa, 1e-9, "pas de culture → pas de coût phyto");
        }

        [Test]
        public void Pure_crop_at_reference_pesticide_still_gets_no_maec()
        {
            // Régression : sans prairie, IFT effectif = intensité → au-dessus du seuil, pas de MAEC.
            MarginBreakdown bd = EconomyRule.Breakdown(ReferenceModel(),
                new ScenarioContext { GrasslandFraction = 0.0, PesticideIntensity = 1.0 });
            Assert.AreEqual(0.0, bd.MaecEurosPerHa, 1e-9, "ferme tout-culture à l'IFT de référence : pas de MAEC");
        }

        [Test]
        public void Carbon_above_baseline_is_paid()
        {
            var rich = new EcosystemModel(initialCarbonYoungTPerHa: 3.0, initialCarbonOldTPerHa: 67.0); // 70 tC/ha
            var baseline = new EcosystemModel(initialCarbonYoungTPerHa: 3.0, initialCarbonOldTPerHa: 47.0); // 50 tC/ha
            double withCredit = EconomyRule.AnnualMarginEurosPerHa(rich, ReferenceScenario());
            double withoutCredit = EconomyRule.AnnualMarginEurosPerHa(baseline, ReferenceScenario());
            Assert.Greater(withCredit, withoutCredit, "le carbone au-dessus de la baseline doit être payé (crédit)");
        }

        [Test]
        public void Capital_accumulates_the_annual_margin()
        {
            var model = ReferenceModel();
            var scenario = ReferenceScenario();
            double expected = EconomyRule.AnnualMarginEurosPerHa(model, scenario);
            var rule = new EconomyRule();
            for (int day = 0; day < 365; day++) rule.Apply(model, scenario);
            Assert.AreEqual(expected, model.CapitalEurosPerHa, 0.5, "le capital cumule la marge annuelle");
            Assert.AreEqual(expected, model.LastAnnualMarginEurosPerHa, 1e-9);
        }
    }
}
