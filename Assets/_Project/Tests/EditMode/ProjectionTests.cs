using Bocage.SimulationCore;
using Bocage.Decision;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests de la machinerie de projection (Couche 03) : copies d'état
    /// indépendantes, projection d'un levier nul = zéro, déterminisme, un levier
    /// clairement bénéfique améliore la marge, et la fonction-objectif récompense
    /// la marge et pénalise le risque.
    /// </summary>
    public sealed class ProjectionTests
    {
        private static Climatology UniformClimatology()
        {
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(11.0, 3.2, 9.0, 60.0, 0.33, 0.55, 0.22, 1.35, 0.85);
            return new Climatology(months, 0.75, 2.1);
        }

        // ---------------- Copies ----------------

        [Test]
        public void Model_copy_is_independent()
        {
            var original = new EcosystemModel();
            original.SetSoilWaterMm(100.0);
            var copy = new EcosystemModel(original);
            copy.SetSoilWaterMm(50.0);
            Assert.AreEqual(100.0, original.SoilWaterMm, 1e-9, "l'original ne doit pas bouger");
            Assert.AreEqual(50.0, copy.SoilWaterMm, 1e-9);
        }

        [Test]
        public void Scenario_copy_is_independent()
        {
            var original = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0 };
            var copy = new ScenarioContext(original);
            copy.NitrogenDoseKgPerHaPerYear = 40.0;
            Assert.AreEqual(120.0, original.NitrogenDoseKgPerHaPerYear, 1e-9);
            Assert.AreEqual(40.0, copy.NitrogenDoseKgPerHaPerYear, 1e-9);
        }

        // ---------------- Projection ----------------

        [Test]
        public void No_op_lever_projects_zero_delta()
        {
            var projector = new ModelOutcomeProjector(UniformClimatology());
            LeverOutcome outcome = projector.Project(new EcosystemModel(), new ScenarioContext(), 1UL, _ => { });
            Assert.AreEqual(0.0, outcome.DeltaMarginEurosPerHa.Expected, 1e-6, "ne rien changer → Δ nul");
            Assert.AreEqual(0.0, outcome.DeltaBiodiversity.Expected, 1e-9);
            Assert.AreEqual(0.0, outcome.DeltaCarbonTPerHa.Expected, 1e-9);
        }

        [Test]
        public void Projection_is_deterministic()
        {
            var projector = new ModelOutcomeProjector(UniformClimatology());
            var model = new EcosystemModel();
            var scenario = new ScenarioContext();
            LeverOutcome a = projector.Project(model, scenario, 5UL, s => s.NitrogenDoseKgPerHaPerYear = 100.0);
            LeverOutcome b = projector.Project(model, scenario, 5UL, s => s.NitrogenDoseKgPerHaPerYear = 100.0);
            Assert.AreEqual(a.DeltaMarginEurosPerHa.Expected, b.DeltaMarginEurosPerHa.Expected, 1e-9);
            Assert.AreEqual(a.DeltaCarbonTPerHa.Expected, b.DeltaCarbonTPerHa.Expected, 1e-9);
        }

        [Test]
        public void Raising_nitrogen_from_deficit_helps_margin()
        {
            var projector = new ModelOutcomeProjector(UniformClimatology());
            var model = new EcosystemModel(initialMineralNitrogenKgPerHa: 20.0); // sol carencé
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 30.0 }; // dose faible
            LeverOutcome outcome = projector.Project(model, scenario, 9UL, s => s.NitrogenDoseKgPerHaPerYear = 130.0);
            Assert.Greater(outcome.DeltaMarginEurosPerHa.Expected, 0.0,
                "ajouter de l'azote quand la culture est carencée doit améliorer la marge");
        }

        // ---------------- Objectif ----------------

        [Test]
        public void Objective_rewards_margin_and_penalises_risk()
        {
            var lowRisk = new LeverOutcome(new OutcomeDistribution(80.0, 100.0, 120.0), default, default);
            var highRisk = new LeverOutcome(new OutcomeDistribution(0.0, 100.0, 200.0), default, default);
            var higherMargin = new LeverOutcome(new OutcomeDistribution(180.0, 200.0, 220.0), default, default);

            Assert.Greater(FarmerObjective.Utility(lowRisk), FarmerObjective.Utility(highRisk),
                "à marge espérée égale, moins de risque baissier est préféré");
            Assert.Greater(FarmerObjective.Utility(higherMargin), FarmerObjective.Utility(lowRisk),
                "plus de marge espérée est préféré");
        }
    }
}
