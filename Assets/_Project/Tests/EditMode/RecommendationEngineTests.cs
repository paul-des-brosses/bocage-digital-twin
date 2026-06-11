using Bocage.SimulationCore;
using Bocage.Sensors;
using Bocage.Decision;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du moteur de recommandation (Couche 03) : candidats par
    /// événement, classification, et surtout la <b>recherche de dose optimale</b>
    /// (le fix du P1) qui recommande le bon niveau d'azote — à la hausse quand on
    /// est carencé, à la baisse quand on est en excès.
    /// </summary>
    public sealed class RecommendationEngineTests
    {
        private static Climatology UniformClimatology()
        {
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(11.0, 3.2, 9.0, 60.0, 0.33, 0.55, 0.22, 1.35, 0.85);
            return new Climatology(months, 0.75, 2.1);
        }

        private static RecommendationEngine MakeEngine()
            => new RecommendationEngine(new ModelOutcomeProjector(UniformClimatology()));

        [Test]
        public void Each_event_has_candidate_levers()
        {
            Assert.IsNotEmpty(RecommendationEngine.CandidatesFor(EventKind.HydricStress));
            Assert.IsNotEmpty(RecommendationEngine.CandidatesFor(EventKind.FaunaAnomaly));
            Assert.IsNotEmpty(RecommendationEngine.CandidatesFor(EventKind.NitrogenDeficiency));
            Assert.IsNotEmpty(RecommendationEngine.CandidatesFor(EventKind.LowProfitability));
        }

        [Test]
        public void Classification_follows_the_signs()
        {
            LeverOutcome winwin = new LeverOutcome(new OutcomeDistribution(10, 20, 30), new OutcomeDistribution(0, 0.02, 0.04), default);
            LeverOutcome ecoTradeoff = new LeverOutcome(new OutcomeDistribution(10, 20, 30), new OutcomeDistribution(-0.04, -0.02, 0), default);
            LeverOutcome ecolTradeoff = new LeverOutcome(new OutcomeDistribution(-30, -20, -10), new OutcomeDistribution(0, 0.02, 0.04), default);
            LeverOutcome loseLose = new LeverOutcome(new OutcomeDistribution(-30, -20, -10), new OutcomeDistribution(-0.04, -0.02, 0), default);

            Assert.AreEqual(RecommendationClass.WinWin, RecommendationSurfacing.Classify(winwin));
            Assert.AreEqual(RecommendationClass.EconomicTradeoff, RecommendationSurfacing.Classify(ecoTradeoff));
            Assert.AreEqual(RecommendationClass.EcologicalTradeoff, RecommendationSurfacing.Classify(ecolTradeoff));
            Assert.AreEqual(RecommendationClass.LoseLose, RecommendationSurfacing.Classify(loseLose));
        }

        [Test]
        public void Optimal_nitrogen_is_higher_when_deficient()
        {
            var engine = MakeEngine();
            var model = new EcosystemModel(initialMineralNitrogenKgPerHa: 20.0); // carencé
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 30.0 }; // dose faible
            (double level, _, _) = engine.FindOptimalLevel(model, scenario, 1UL, DecisionLever.NitrogenDose);
            Assert.Greater(level, 30.0, "la dose optimale doit dépasser une dose faible quand le sol est carencé");
        }

        [Test]
        public void Deficiency_event_recommends_raising_nitrogen()
        {
            var engine = MakeEngine();
            var model = new EcosystemModel(initialMineralNitrogenKgPerHa: 20.0);
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 20.0 };
            Recommendation reco = engine.TryProduce(EventKind.NitrogenDeficiency, model, scenario, 2UL);
            Assert.IsNotNull(reco, "une carence doit produire une recommandation");
            Assert.AreEqual(DecisionLever.NitrogenDose, reco.Lever);
            Assert.Greater(reco.RecommendedLevel, 20.0, "elle doit recommander d'augmenter l'azote");
        }

        [Test]
        public void Excess_event_recommends_lowering_nitrogen()
        {
            var engine = MakeEngine();
            var model = new EcosystemModel(initialMineralNitrogenKgPerHa: 150.0); // excès
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 200.0 }; // dose forte
            Recommendation reco = engine.TryProduce(EventKind.NitrogenExcess, model, scenario, 3UL);
            Assert.IsNotNull(reco, "un excès doit produire une recommandation");
            Assert.Less(reco.RecommendedLevel, 200.0, "elle doit recommander de baisser l'azote");
        }
    }
}
