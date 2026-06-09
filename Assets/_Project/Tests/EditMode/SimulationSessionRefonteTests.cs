using Bocage.SimulationCore.Refonte;
using Bocage.Sensors.Refonte;
using Bocage.Decision.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests de la session d'orchestration (Couche 03, refonte) : le tick avance
    /// réel + fantôme, sans décision les deux restent identiques (apport techno
    /// nul), une décision bénéfique crée un apport positif, le stress génère des
    /// événements, et une carence produit une recommandation.
    /// </summary>
    public sealed class SimulationSessionRefonteTests
    {
        private static Climatology UniformClimatology()
        {
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(11.0, 3.2, 9.0, 60.0, 0.33, 0.55, 0.22, 1.35, 0.85);
            return new Climatology(months, 0.75, 2.1);
        }

        private static SimulationSession MakeSession(ScenarioContext scenario, EcosystemModel model = null, ulong seed = 100UL)
            => new SimulationSession(model ?? new EcosystemModel(), scenario, UniformClimatology(), seed);

        [Test]
        public void Tick_advances_real_and_shadow_together()
        {
            var session = MakeSession(new ScenarioContext());
            session.Run(50);
            Assert.AreEqual(50, session.RealModel.CurrentDay);
            Assert.AreEqual(50, session.ShadowModel.CurrentDay);
        }

        [Test]
        public void No_decision_keeps_tech_value_zero()
        {
            var session = MakeSession(new ScenarioContext());
            session.Run(365);
            Assert.AreEqual(0.0, session.TechValueNetEurosPerHa, 1e-6,
                "sans décision, réel et fantôme sont identiques → apport techno nul");
        }

        [Test]
        public void Beneficial_decision_creates_positive_tech_value()
        {
            // Carence SÉVÈRE (dose quasi nulle, gelée pour le fantôme) ; le réel corrige.
            var model = new EcosystemModel(initialMineralNitrogenKgPerHa: 15.0);
            var session = MakeSession(new ScenarioContext { NitrogenDoseKgPerHaPerYear = 10.0 }, model);
            session.Run(60);                                            // avant la saison de croissance
            session.ApplyDecision(DecisionLever.NitrogenDose, 150.0);   // corrige une carence sévère
            session.Run(1095);                                          // 3 ans de divergence
            Assert.Greater(session.TechValueNetEurosPerHa, 0.0,
                "corriger une carence sévère doit créer de la valeur vs le fantôme gelé");
        }

        [Test]
        public void Severe_drought_raises_alerts()
        {
            var session = MakeSession(new ScenarioContext { PrecipitationFactor = 0.2, TemperatureAnomalyC = 3.0 });
            session.Run(365);
            Assert.Greater(session.Events.Count, 0, "une sécheresse sévère doit déclencher des alertes");
        }

        [Test]
        public void Recommend_returns_a_recommendation_for_deficiency()
        {
            var model = new EcosystemModel(initialMineralNitrogenKgPerHa: 20.0);
            var session = MakeSession(new ScenarioContext { NitrogenDoseKgPerHaPerYear = 20.0 }, model);
            session.Run(120);
            Recommendation reco = session.Recommend(EventKind.NitrogenDeficiency);
            Assert.IsNotNull(reco);
            Assert.Greater(reco.RecommendedLevel, 20.0);
        }

        [Test]
        public void Determinism_same_seed_same_state()
        {
            var a = MakeSession(new ScenarioContext(), seed: 7UL);
            var b = MakeSession(new ScenarioContext(), seed: 7UL);
            a.Run(400);
            b.Run(400);
            Assert.AreEqual(a.RealModel.CropYieldTPerHa, b.RealModel.CropYieldTPerHa, 1e-9);
            Assert.AreEqual(a.TechValueNetEurosPerHa, b.TechValueNetEurosPerHa, 1e-9);
        }
    }
}
