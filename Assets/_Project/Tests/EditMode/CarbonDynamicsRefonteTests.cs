using System;
using Bocage.SimulationCore.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du carbone ICBM (Couche 01, refonte). Le test phare : l'équilibre
    /// <c>C* ≈ 50 tC/ha</c> (doc 11 §2.1, BDAT). Plus : la chute de C* sous
    /// réchauffement (couplage carbone↔climat), l'effet des couverts, et la
    /// conservation ΔC = apports − respiration (B9).
    /// </summary>
    public sealed class CarbonDynamicsRefonteTests
    {
        // Apport de référence (Y=5,5 ; densité=90 ; sans couverts).
        private static double ReferenceInputs()
            => CarbonDynamicsRule.CarbonInputsTPerHaPerYear(new EcosystemModel(), new ScenarioContext());

        private static double YoungEquilibrium(double i) => i / CarbonDynamicsRule.DecayYoungPerYear;
        private static double OldEquilibrium(double i)
            => CarbonDynamicsRule.HumificationFraction * i / CarbonDynamicsRule.DecayOldPerYear;

        // Fait tourner le carbone à T° et θ constants pendant 'days' jours.
        private static void RunCarbon(EcosystemModel model, ScenarioContext scenario,
            double tMeanCelsius, double soilWaterMm, int days)
        {
            model.SetWeather(new DailyWeather(tMeanCelsius - 4.0, tMeanCelsius + 4.0, tMeanCelsius, 0.0));
            model.SetSoilWaterMm(soilWaterMm);
            var rule = new CarbonDynamicsRule();
            for (int d = 0; d < days; d++) rule.Apply(model, scenario);
        }

        [Test]
        public void Reference_inputs_are_about_2_5()
        {
            Assert.AreEqual(2.5, ReferenceInputs(), 1e-9,
                "l'apport de référence doit donner i ≈ 2,5 → C* ≈ 50");
        }

        [Test]
        public void Equilibrium_is_a_fixed_point_near_50()
        {
            double i = ReferenceInputs();
            double cStar = YoungEquilibrium(i) + OldEquilibrium(i);
            var model = new EcosystemModel(
                initialCarbonYoungTPerHa: YoungEquilibrium(i),
                initialCarbonOldTPerHa: OldEquilibrium(i));
            RunCarbon(model, new ScenarioContext(), CarbonDynamicsRule.TempReferenceCelsius, 90.0, 365);
            Assert.AreEqual(cStar, model.SoilCarbonTotalTPerHa, 0.02);
            Assert.AreEqual(50.0, cStar, 1.0, "l'équilibre doit tomber sur ~50 tC/ha (BDAT)");
        }

        [Test]
        public void Converges_to_equilibrium_from_below()
        {
            double i = ReferenceInputs();
            double cStar = YoungEquilibrium(i) + OldEquilibrium(i);
            var model = new EcosystemModel(initialCarbonYoungTPerHa: 2.0, initialCarbonOldTPerHa: 30.0);
            RunCarbon(model, new ScenarioContext(), CarbonDynamicsRule.TempReferenceCelsius, 90.0, 365 * 1000);
            Assert.AreEqual(cStar, model.SoilCarbonTotalTPerHa, 0.5,
                "doit converger vers C* (~50) depuis un sol appauvri");
        }

        [Test]
        public void Warming_lowers_the_carbon_equilibrium()
        {
            double i = ReferenceInputs();
            double cyStar = YoungEquilibrium(i), coStar = OldEquilibrium(i);
            double cStar = cyStar + coStar;

            var cool = new EcosystemModel(initialCarbonYoungTPerHa: cyStar, initialCarbonOldTPerHa: coStar);
            var warm = new EcosystemModel(initialCarbonYoungTPerHa: cyStar, initialCarbonOldTPerHa: coStar);
            RunCarbon(cool, new ScenarioContext(), 10.0, 90.0, 365 * 1000); // reste à l'équilibre
            RunCarbon(warm, new ScenarioContext(), 14.0, 90.0, 365 * 1000); // +4 °C → décline

            double reWarm = Math.Pow(CarbonDynamicsRule.Q10, (14.0 - CarbonDynamicsRule.TempReferenceCelsius) / 10.0);
            double warmStar = cStar / reWarm; // C* ∝ 1/r_e

            Assert.Less(warm.SoilCarbonTotalTPerHa, cool.SoilCarbonTotalTPerHa,
                "le réchauffement doit faire baisser le carbone");
            Assert.AreEqual(warmStar, warm.SoilCarbonTotalTPerHa, 1.0,
                "le nouvel équilibre chaud doit valoir C*/r_e (~37,6 à +4 °C)");
        }

        [Test]
        public void Cover_crops_raise_carbon()
        {
            double i = ReferenceInputs();
            double cyStar = YoungEquilibrium(i), coStar = OldEquilibrium(i);

            var reference = new EcosystemModel(initialCarbonYoungTPerHa: cyStar, initialCarbonOldTPerHa: coStar);
            var withCover = new EcosystemModel(initialCarbonYoungTPerHa: cyStar, initialCarbonOldTPerHa: coStar);
            var coverScenario = new ScenarioContext { CoverCropsCoveragePercent = 100.0 };

            RunCarbon(reference, new ScenarioContext(), 10.0, 90.0, 365 * 800);
            RunCarbon(withCover, coverScenario, 10.0, 90.0, 365 * 800);

            Assert.Greater(withCover.SoilCarbonTotalTPerHa, reference.SoilCarbonTotalTPerHa + 10.0,
                "les couverts (apports↑) doivent élever le carbone");
        }

        [Test]
        public void Carbon_balance_conserves_mass()
        {
            double i = ReferenceInputs();
            var model = new EcosystemModel(
                initialCarbonYoungTPerHa: YoungEquilibrium(i),
                initialCarbonOldTPerHa: OldEquilibrium(i));
            var scenario = new ScenarioContext();
            var rule = new CarbonDynamicsRule();

            double cInit = model.SoilCarbonTotalTPerHa;
            double sumInput = 0.0, sumResp = 0.0;
            for (int day = 1; day <= 400; day++)
            {
                double tMean = 12.0 + 4.0 * Math.Sin(2.0 * Math.PI * day / 365.0);
                double theta = 60.0 + 30.0 * Math.Sin(2.0 * Math.PI * day / 365.0);
                model.SetWeather(new DailyWeather(tMean - 4.0, tMean + 4.0, tMean, 0.0));
                model.SetSoilWaterMm(theta);
                rule.Apply(model, scenario);
                sumInput += model.LastCarbonInputTPerHa;
                sumResp += model.LastCarbonRespirationTPerHa;
            }
            double balance = sumInput - sumResp - (model.SoilCarbonTotalTPerHa - cInit);
            Assert.AreEqual(0.0, balance, 1e-6, "ΔC doit égaler apports − respiration (B9)");
        }
    }
}
