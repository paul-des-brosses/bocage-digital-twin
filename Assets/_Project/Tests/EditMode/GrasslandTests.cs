using Bocage.SimulationCore;
using Bocage.Decision;
using Bocage.Indicators;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du levier « part de prairie » g (S0a) : rétro-compatibilité stricte à
    /// g=0, puis les couplages — coût d'opportunité (le downside), résilience à la
    /// sécheresse, co-bénéfices carbone + biodiversité, et un optimum qui MONTE
    /// quand le climat se dégrade (« optimiser, pas moraliser » : aucun coin imposé).
    /// </summary>
    public sealed class GrasslandTests
    {
        private static DailyWeather Weather(double tMean) => new DailyWeather(tMean - 4.0, tMean + 4.0, tMean, 0.0);

        private static Climatology UniformClimatology()
        {
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(11.0, 3.2, 9.0, 60.0, 0.33, 0.55, 0.22, 1.35, 0.85);
            return new Climatology(months, 0.75, 2.1);
        }

        private static RecommendationEngine MakeEngine()
            => new RecommendationEngine(new ModelOutcomeProjector(UniformClimatology()));

        // ---- Rétro-compatibilité : à g=0, rien ne bouge ----

        [Test]
        public void Margin_at_g0_matches_reference()
        {
            var model = new EcosystemModel(); // Y=5.5, densité=90, C=50
            var scenario = new ScenarioContext
            {
                NitrogenDoseKgPerHaPerYear = 120.0,
                PesticideIntensity = 1.0,
                TillageIntensity = 1.0,
                GrasslandFraction = 0.0
            };
            Assert.AreEqual(356.0, EconomyRule.AnnualMarginEurosPerHa(model, scenario), 1e-6,
                "à g=0 la marge de référence Perche (~356 €/ha) est inchangée");
        }

        [Test]
        public void Carbon_inputs_at_g0_match_reference()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext { CoverCropsCoveragePercent = 0.0, GrasslandFraction = 0.0 };
            Assert.AreEqual(2.5, CarbonDynamicsRule.CarbonInputsTPerHaPerYear(model, scenario), 1e-9,
                "à g=0 les apports carbone de référence (i≈2.5) sont inchangés");
        }

        [Test]
        public void Biodiversity_overloads_default_to_zero_grassland()
        {
            Assert.AreEqual(BiodiversityRule.HabitatFactor(90.0), BiodiversityRule.HabitatFactor(90.0, 0.0), 1e-12,
                "l'overload habitat à g=0 == la version historique");
            Assert.AreEqual(BiodiversityRule.InputsFactor(60.0, 1.0), BiodiversityRule.InputsFactor(60.0, 1.0, 0.0), 1e-12,
                "l'overload intrants à g=0 == la version historique");
        }

        // ---- Co-bénéfices : g monte → carbone + biodiv montent ----

        [Test]
        public void More_grassland_raises_carbon_equilibrium()
        {
            var model = new EcosystemModel(initialCropYieldTPerHa: 5.5, initialSoilWaterMm: 90.0);
            model.SetWeather(Weather(10.0));
            double eq0 = HeroIndicators.CarbonEquilibriumTPerHa(model, new ScenarioContext { GrasslandFraction = 0.0 });
            double eqHalf = HeroIndicators.CarbonEquilibriumTPerHa(model, new ScenarioContext { GrasslandFraction = 0.5 });
            Assert.Greater(eqHalf, eq0, "plus de prairie permanente → plus d'apports → équilibre carbone plus haut");
        }

        [Test]
        public void More_grassland_raises_biodiversity_target()
        {
            var model = new EcosystemModel(initialHedgerowDensityMPerHa: 90.0, initialSoilWaterMm: 90.0, initialMineralNitrogenKgPerHa: 60.0);
            double t0 = BiodiversityRule.Target(model, new ScenarioContext { PesticideIntensity = 1.0, GrasslandFraction = 0.0 });
            double tHalf = BiodiversityRule.Target(model, new ScenarioContext { PesticideIntensity = 1.0, GrasslandFraction = 0.5 });
            Assert.Greater(tHalf, t0, "plus de prairie → plus d'habitat + moins de pression intrants → cible biodiv plus haute");
        }

        // ---- Le trade-off : downside en bonne année, résilience en sécheresse ----

        [Test]
        public void Grassland_costs_margin_in_a_good_year()
        {
            var model = new EcosystemModel(initialCropYieldTPerHa: 5.5, initialSoilWaterMm: 90.0);
            var sBase = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PesticideIntensity = 1.0, TillageIntensity = 1.0, GrasslandFraction = 0.0 };
            var sGrass = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PesticideIntensity = 1.0, TillageIntensity = 1.0, GrasslandFraction = 0.5 };
            Assert.Less(EconomyRule.AnnualMarginEurosPerHa(model, sGrass), EconomyRule.AnnualMarginEurosPerHa(model, sBase),
                "en bonne année, convertir en prairie coûte de la marge (coût d'opportunité = le downside)");
        }

        [Test]
        public void Grassland_cushions_margin_under_drought()
        {
            // Sécheresse : eau du sol basse (Ks bas) et rendement effondré.
            var model = new EcosystemModel(initialCropYieldTPerHa: 2.0, initialSoilWaterMm: 10.0);
            var sBase = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PesticideIntensity = 1.0, TillageIntensity = 1.0, GrasslandFraction = 0.0 };
            var sGrass = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PesticideIntensity = 1.0, TillageIntensity = 1.0, GrasslandFraction = 0.5 };
            Assert.Greater(EconomyRule.AnnualMarginEurosPerHa(model, sGrass), EconomyRule.AnnualMarginEurosPerHa(model, sBase),
                "sous sécheresse, la prairie résiliente amortit la marge (moins négative)");
        }

        // ---- L'optimum émergent monte avec le stress climatique ----

        [Test]
        public void Optimal_grassland_rises_with_climate_stress()
        {
            var engine = MakeEngine();
            var model = new EcosystemModel();
            var benign = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PrecipitationFactor = 1.0, TemperatureAnomalyC = 0.0 };
            var stressed = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PrecipitationFactor = 0.5, TemperatureAnomalyC = 3.0 };
            (double gBenign, _, _) = engine.FindOptimalLevel(model, benign, 11UL, DecisionLever.Grassland);
            (double gStressed, _, _) = engine.FindOptimalLevel(model, stressed, 11UL, DecisionLever.Grassland);
            Assert.Greater(gStressed, gBenign,
                "la part de prairie optimale monte quand le climat se dégrade — optimum émergent, pas moralisé");
        }
    }
}
