using System.Linq;
using Bocage.SimulationCore;
using Bocage.Sensors;
using Bocage.Decision;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Cycle de vie des recommandations (Couche 03) : apparition sur
    /// événement, dédup par type, disparition quand satisfaite, Valider =
    /// ApplyDecision, cooldown anti-spam après Ignorer, gate d'auto-popup, et
    /// suppression de l'auto-popup quand la reco est différée.
    /// </summary>
    public sealed class RecommendationLifecycleTests
    {
        private static Climatology UniformClimatology()
        {
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(11.0, 3.2, 9.0, 60.0, 0.33, 0.55, 0.22, 1.35, 0.85);
            return new Climatology(months, 0.75, 2.1);
        }

        // Carence azotée persistante → l'événement NitrogenDeficiency fire, une reco apparaît.
        private static SimulationSession DeficientSession()
        {
            var model = new EcosystemModel(initialMineralNitrogenKgPerHa: 15.0);
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 10.0 };
            return new SimulationSession(model, scenario, UniformClimatology(), 100UL);
        }

        private static Recommendation NitrogenReco(SimulationSession s)
            => s.PendingRecommendations.FirstOrDefault(r => r.TriggeredBy == EventKind.NitrogenDeficiency);

        [Test]
        public void Deficiency_event_produces_a_pending_recommendation()
        {
            var s = DeficientSession();
            s.Run(15);
            Recommendation reco = NitrogenReco(s);
            Assert.IsNotNull(reco, "une carence azotée doit faire apparaître une reco en attente");
            Assert.AreEqual(DecisionLever.NitrogenDose, reco.Lever);
            Assert.Greater(reco.RecommendedLevel, 10.0, "elle doit recommander d'augmenter l'azote");
        }

        [Test]
        public void One_recommendation_per_kind_no_duplicates()
        {
            var s = DeficientSession();
            s.Run(120); // bien au-delà d'un re-fire détecteur (30 j)
            int n = s.PendingRecommendations.Count(r => r.TriggeredBy == EventKind.NitrogenDeficiency);
            Assert.AreEqual(1, n, "une seule reco par type, même si l'événement re-fire");
        }

        [Test]
        public void Accept_applies_the_lever_and_clears_the_reco()
        {
            var s = DeficientSession();
            s.Run(15);
            Recommendation reco = NitrogenReco(s);
            Assert.IsNotNull(reco);
            double level = reco.RecommendedLevel;
            s.AcceptRecommendation(reco);
            Assert.AreEqual(level, s.Scenario.NitrogenDoseKgPerHaPerYear, 1e-9, "Valider pose le levier au niveau recommandé");
            Assert.IsNull(NitrogenReco(s), "la reco validée disparaît de la liste");
        }

        [Test]
        public void Dismiss_clears_and_cools_down()
        {
            var s = DeficientSession();
            s.Run(15);
            Recommendation reco = NitrogenReco(s);
            Assert.IsNotNull(reco);
            s.DismissRecommendation(reco);
            Assert.IsNull(NitrogenReco(s), "Ignorer retire la reco");
            s.Run(40); // l'événement re-fire vers 30 j, mais le cooldown reco (60 j) bloque
            Assert.IsNull(NitrogenReco(s), "pas de nouvelle reco du même type pendant le cooldown");
        }

        [Test]
        public void Satisfying_the_lever_manually_drops_the_reco()
        {
            var s = DeficientSession();
            s.Run(15);
            Recommendation reco = NitrogenReco(s);
            Assert.IsNotNull(reco);
            // L'utilisateur monte le slider lui-même au niveau recommandé.
            s.ApplyDecision(DecisionLever.NitrogenDose, reco.RecommendedLevel);
            s.Run(1);
            Assert.IsNull(NitrogenReco(s), "déplacer le levier au niveau recommandé fait disparaître la reco (satisfaite)");
        }

        [Test]
        public void ShouldAutoPopup_gates_on_class_and_biodiversity()
        {
            Recommendation winWin = MakeReco(RecommendationClass.WinWin);
            Recommendation econ = MakeReco(RecommendationClass.EconomicTradeoff);
            Recommendation ecol = MakeReco(RecommendationClass.EcologicalTradeoff);

            Assert.IsTrue(RecommendationSurfacing.ShouldAutoPopup(winWin, 0.70), "un win-win interrompt toujours");
            Assert.IsFalse(RecommendationSurfacing.ShouldAutoPopup(econ, 0.70), "un compromis économique patiente");
            Assert.IsFalse(RecommendationSurfacing.ShouldAutoPopup(ecol, 0.70), "un compromis écologique patiente si la biodiv va bien");
            Assert.IsTrue(RecommendationSurfacing.ShouldAutoPopup(ecol, 0.30), "… mais interrompt si la biodiv est sous le seuil critique");
        }

        [Test]
        public void Deferred_recommendation_is_not_auto_popped()
        {
            var s = DeficientSession();
            s.Run(15);
            Recommendation reco = NitrogenReco(s);
            Assert.IsNotNull(reco);
            s.DeferRecommendation(reco);
            Assert.IsTrue(s.IsDeferred(reco));
            Assert.AreNotSame(reco, s.NextAutoPopupRecommendation(), "une reco différée n'est jamais auto-ouverte");
        }

        private static Recommendation MakeReco(RecommendationClass cls)
            => new Recommendation(DecisionLever.NitrogenDose, 100.0, 60.0, default, cls, EventKind.LowProfitability, 1.0);
    }
}
