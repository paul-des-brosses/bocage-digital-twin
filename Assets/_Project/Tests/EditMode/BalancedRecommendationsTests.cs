using System;
using Bocage.Decision;
using Bocage.Decision.Outcomes;
using Bocage.Decision.Recommendations;
using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests for the E9 balanced recommendation set (raise inputs, cover crops,
    /// residue restitution, reduce / increase hedge removal): construction, the
    /// AutoActionPipeline lever effect with its caps/floors, and the
    /// model-derived projected SIGN of the directional levers (which drives the
    /// win-win vs trade-off surfacing). The projector internals are covered by
    /// <see cref="ModelOutcomeProjectorTests"/>.
    /// </summary>
    public sealed class BalancedRecommendationsTests
    {
        private static readonly SeasonalWeatherData Weather = SeasonalWeatherDataDefaults.MortagneAuPerche();
        private const ulong Seed = 4242UL;

        private static OutcomeDistribution ProjectLong(IRecommendation rec, ScenarioContext scenario, EcosystemModel model)
            => ModelOutcomeProjector.Project(
                rec, model, scenario, Seed, Weather,
                IntegratedProfitabilityIndicator.Compute,
                BiodiversityCompositeIndicator.Compute)[1];

        private static IRecommendation[] AllNew() => new IRecommendation[]
        {
            new RaiseInputsRecommendation(10, "evt"),
            new SowCoverCropsRecommendation(10, "evt"),
            new RestoreResidueRecommendation(10, "evt"),
            new ReduceHedgeRemovalRecommendation(10, "evt"),
            new IncreaseHedgeRemovalRecommendation(10, "evt"),
        };

        // ---------------- Construction ----------------

        [Test]
        public void New_recos_construct_pending_with_no_investment()
        {
            foreach (var rec in AllNew())
            {
                Assert.AreEqual(DecisionVerdict.Pending, rec.DefaultVerdict, rec.Id);
                Assert.AreEqual(0.0, rec.InvestmentCostEurosPerHectare, 1e-9, rec.Id);
                Assert.IsFalse(string.IsNullOrEmpty(rec.Title), rec.Id);
                Assert.IsFalse(string.IsNullOrEmpty(rec.Rationale), rec.Id);
            }
        }

        // ---------------- AutoActionPipeline lever effect ----------------

        [Test]
        public void RaiseInputs_raises_intensity_capped_at_intensive_max()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialInputIntensityFactor: 1.8);
            AutoActionPipeline.ApplyOne(new RaiseInputsRecommendation(1, "e"), model, scenario, 0.5);
            // 1.8 + 0.5 = 2.3, capped at the physical intensive max (2.0). The
            // profit optimum is no longer the cap — it is enforced upstream by the
            // engine's projection gate.
            Assert.AreEqual(RaiseInputsRecommendation.MaxInputIntensityFactor,
                scenario.InputIntensityFactor.Target, 1e-9);
        }

        [Test]
        public void CoverCrops_raises_coverage_capped_at_100()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialCoverCropsCoveragePercent: 90.0);
            AutoActionPipeline.ApplyOne(new SowCoverCropsRecommendation(1, "e"), model, scenario, 25.0);
            Assert.AreEqual(100.0, scenario.CoverCropsCoveragePercent.Target, 1e-9);
        }

        [Test]
        public void RestoreResidue_raises_restitution()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialResidueRestitutionPercent: 40.0);
            AutoActionPipeline.ApplyOne(new RestoreResidueRecommendation(1, "e"), model, scenario, 25.0);
            Assert.AreEqual(65.0, scenario.ResidueRestitutionPercent.Target, 1e-9);
        }

        [Test]
        public void ReduceHedgeRemoval_lowers_rate_floored_at_zero()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialHedgeRemovalRate: 3.0);
            AutoActionPipeline.ApplyOne(new ReduceHedgeRemovalRecommendation(1, "e"), model, scenario, 5.0);
            // 3 - 5 = -2, floored at 0.
            Assert.AreEqual(0.0, scenario.HedgeRemovalRate.Target, 1e-9);
        }

        [Test]
        public void IncreaseHedgeRemoval_raises_rate()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialHedgeRemovalRate: 2.0);
            AutoActionPipeline.ApplyOne(new IncreaseHedgeRemovalRecommendation(1, "e"), model, scenario, 5.0);
            Assert.AreEqual(7.0, scenario.HedgeRemovalRate.Target, 1e-9);
        }

        // ---------- Model-derived projected signs (drive win-win vs trade-off) ----------

        [Test]
        public void Economic_levers_project_profit_up_biodiversity_down()
        {
            // RaiseInputs from below the optimum: profit up, biodiversity down.
            var raise = ProjectLong(new RaiseInputsRecommendation(1, "e"),
                new ScenarioContext(initialInputIntensityFactor: 0.5), new EcosystemModel());
            Assert.Greater(raise.ProfitDeltaExpected, 0.0, "RaiseInputs profit");
            Assert.Less(raise.BiodiversityDeltaExpected, 0.0, "RaiseInputs biodiv");

            // Thinning over-dense, weakly-subsidised hedges: profit up (less
            // maintenance + yield penalty), biodiversity down (fewer hedges).
            var thin = ProjectLong(new IncreaseHedgeRemovalRecommendation(1, "e"),
                new ScenarioContext(initialPseSubsidyRate: 0.0), new EcosystemModel(initialHedgerowDensity: 160.0));
            Assert.Greater(thin.ProfitDeltaExpected, 0.0, "IncreaseHedgeRemoval profit");
            Assert.Less(thin.BiodiversityDeltaExpected, 0.0, "IncreaseHedgeRemoval biodiv");
        }

        [Test]
        public void Fauna_levers_project_biodiversity_up()
        {
            // Reducing inputs from conventional intensity recovers fauna.
            var reduceInputs = ProjectLong(new ReduceInputsRecommendation(1, "e"),
                new ScenarioContext(initialInputIntensityFactor: 1.0), new EcosystemModel());
            Assert.Greater(reduceInputs.BiodiversityDeltaExpected, 0.0, "ReduceInputs biodiv");

            // Slowing active hedge grubbing keeps more habitat than the baseline.
            var slowRemoval = ProjectLong(new ReduceHedgeRemovalRecommendation(1, "e"),
                new ScenarioContext(initialHedgeRemovalRate: 8.0), new EcosystemModel());
            Assert.Greater(slowRemoval.BiodiversityDeltaExpected, 0.0, "ReduceHedgeRemoval biodiv");
        }

        [Test]
        public void New_recos_respect_worst_le_expected_le_best()
        {
            var scenario = new ScenarioContext();
            foreach (var rec in AllNew())
            {
                foreach (var o in ModelOutcomeProjector.Project(
                    rec, new EcosystemModel(), scenario, Seed, Weather,
                    IntegratedProfitabilityIndicator.Compute, BiodiversityCompositeIndicator.Compute))
                {
                    Assert.LessOrEqual(o.ProfitDeltaWorstCase, o.ProfitDeltaExpected, rec.Id);
                    Assert.LessOrEqual(o.ProfitDeltaExpected, o.ProfitDeltaBestCase, rec.Id);
                    Assert.LessOrEqual(o.BiodiversityDeltaWorstCase, o.BiodiversityDeltaExpected, rec.Id);
                    Assert.LessOrEqual(o.BiodiversityDeltaExpected, o.BiodiversityDeltaBestCase, rec.Id);
                }
            }
        }
    }
}
