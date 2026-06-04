using Bocage.Decision;
using Bocage.Decision.Outcomes;
using Bocage.Decision.Recommendations;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests for the E9 balanced recommendation set: the five new lever recos
    /// (raise inputs, cover crops, residue restitution, reduce / increase hedge
    /// removal). Covers construction, the AutoActionPipeline lever effect with its
    /// caps/floors, and the OutcomeProjector sign of each (the win-win vs
    /// trade-off surfacing classification depends on these signs).
    /// </summary>
    public sealed class BalancedRecommendationsTests
    {
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
        public void RaiseInputs_raises_intensity_capped_at_profit_optimum()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialInputIntensityFactor: 0.7);
            AutoActionPipeline.ApplyOne(new RaiseInputsRecommendation(1, "e"), model, scenario, 0.5);
            // 0.7 + 0.5 = 1.2, capped at the profit optimum 0.8.
            Assert.AreEqual(RaiseInputsRecommendation.ProfitOptimalIntensityFactor,
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

        // ---------------- OutcomeProjector signs (drive win-win vs trade-off) ----------------

        [Test]
        public void Economic_recos_project_profit_up_biodiversity_down()
        {
            foreach (var rec in new IRecommendation[]
            {
                new RaiseInputsRecommendation(1, "e"),
                new IncreaseHedgeRemovalRecommendation(1, "e"),
            })
            {
                var longTerm = OutcomeProjector.Project(rec)[1];
                Assert.Greater(longTerm.ProfitDeltaExpected, 0.0, rec.Id + " profit");
                Assert.Less(longTerm.BiodiversityDeltaExpected, 0.0, rec.Id + " biodiv");
            }
        }

        [Test]
        public void Ecological_recos_project_biodiversity_up_long_term()
        {
            foreach (var rec in new IRecommendation[]
            {
                new SowCoverCropsRecommendation(1, "e"),
                new RestoreResidueRecommendation(1, "e"),
                new ReduceHedgeRemovalRecommendation(1, "e"),
            })
            {
                var longTerm = OutcomeProjector.Project(rec)[1];
                Assert.Greater(longTerm.BiodiversityDeltaExpected, 0.0, rec.Id);
            }
        }

        [Test]
        public void New_recos_respect_worst_le_expected_le_best()
        {
            foreach (var rec in AllNew())
            {
                foreach (var o in OutcomeProjector.Project(rec))
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
