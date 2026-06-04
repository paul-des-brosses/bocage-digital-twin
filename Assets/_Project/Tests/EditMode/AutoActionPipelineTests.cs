using Bocage.Decision;
using Bocage.Decision.Recommendations;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests for the Couche 5 (pure-C# part) <see cref="AutoActionPipeline"/>.
    /// Verify that each accepted recommendation mutates the real
    /// <see cref="EcosystemModel"/> in the documented direction, that
    /// the pipeline is idempotent (a single rec is applied exactly
    /// once), and that pending / rejected recs are left alone.
    /// </summary>
    public sealed class AutoActionPipelineTests
    {
        // ---------------- ApplyOne mechanics ----------------

        [Test]
        public void ApplyOne_PlantHedges_bumps_hedgerow_density()
        {
            var model = new EcosystemModel(initialHedgerowDensity: 50.0);
            var scenario = new ScenarioContext();
            var rec = new PlantHedgesRecommendation(10, "evt#10");
            AutoActionPipeline.ApplyOne(rec, model, scenario, PlantHedgesRecommendation.HedgeRestoreMetersPerHectare);
            Assert.AreEqual(50.0 + PlantHedgesRecommendation.HedgeRestoreMetersPerHectare,
                model.HedgerowDensity, 1e-9);
        }

        [Test]
        public void ApplyOne_Irrigation_reduces_water_table_depth()
        {
            var model = new EcosystemModel(initialWaterTableDepth: 6.0);
            var scenario = new ScenarioContext();
            var rec = new IrrigationAdviceRecommendation(10, "evt#10");
            AutoActionPipeline.ApplyOne(rec, model, scenario, IrrigationAdviceRecommendation.WaterReliefDepthMeters);
            Assert.AreEqual(6.0 - IrrigationAdviceRecommendation.WaterReliefDepthMeters,
                model.WaterTableDepth, 1e-9);
        }

        [Test]
        public void ApplyOne_Irrigation_floors_water_table_at_half_meter()
        {
            // Already very shallow — irrigation shouldn't push below 0.5 m.
            var model = new EcosystemModel(initialWaterTableDepth: 0.8);
            var scenario = new ScenarioContext();
            var rec = new IrrigationAdviceRecommendation(10, "evt#10");
            AutoActionPipeline.ApplyOne(rec, model, scenario, IrrigationAdviceRecommendation.WaterReliefDepthMeters);
            Assert.That(model.WaterTableDepth, Is.GreaterThanOrEqualTo(0.5));
        }

        [Test]
        public void ApplyOne_ReduceInputs_lowers_the_real_input_intensity()
        {
            // The recommendation is a sustained practice change: it lowers the
            // real run's input-intensity decision by the magnitude (the slider
            // then transitions there). 1.0 - 0.2 = 0.8.
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialInputIntensityFactor: 1.0);
            var rec = new ReduceInputsRecommendation(10, "evt#10");
            AutoActionPipeline.ApplyOne(rec, model, scenario, ReduceInputsRecommendation.IntensityCutPerStep);
            Assert.AreEqual(0.8, scenario.InputIntensityFactor.Target, 1e-9);
        }

        [Test]
        public void ApplyOne_ReduceInputs_floors_intensity_at_the_extensive_end()
        {
            // 0.6 - 0.2 = 0.4 would cross below the 0.5 organic-extensive floor.
            var model = new EcosystemModel();
            var scenario = new ScenarioContext(initialInputIntensityFactor: 0.6);
            var rec = new ReduceInputsRecommendation(10, "evt#10");
            AutoActionPipeline.ApplyOne(rec, model, scenario, ReduceInputsRecommendation.IntensityCutPerStep);
            Assert.AreEqual(0.5, scenario.InputIntensityFactor.Target, 1e-9);
        }

        // ---------------- Apply pipeline + journal ----------------

        [Test]
        public void Apply_skips_pending_entries()
        {
            var journal = new DecisionJournal();
            var rec = new PlantHedgesRecommendation(10, "evt#10");
            journal.Append(rec, currentDay: 10);
            // Verdict still Pending → must not be applied.
            var model = new EcosystemModel(initialHedgerowDensity: 50.0);
            int applied = AutoActionPipeline.Apply(journal, model, new ScenarioContext(), currentDay: 10);
            Assert.AreEqual(0, applied);
            Assert.AreEqual(50.0, model.HedgerowDensity, 1e-9);
        }

        [Test]
        public void Apply_skips_rejected_entries()
        {
            var journal = new DecisionJournal();
            var rec = new PlantHedgesRecommendation(10, "evt#10");
            journal.Append(rec, currentDay: 10);
            journal.SetVerdict(rec.Id, DecisionVerdict.Rejected, currentDay: 11);
            var model = new EcosystemModel(initialHedgerowDensity: 50.0);
            int applied = AutoActionPipeline.Apply(journal, model, new ScenarioContext(), currentDay: 12);
            Assert.AreEqual(0, applied);
            Assert.AreEqual(50.0, model.HedgerowDensity, 1e-9);
        }

        [Test]
        public void Apply_applies_accepted_entries_exactly_once()
        {
            var journal = new DecisionJournal();
            var rec = new PlantHedgesRecommendation(10, "evt#10");
            journal.Append(rec, currentDay: 10);
            journal.SetVerdict(rec.Id, DecisionVerdict.Accepted, currentDay: 11,
                appliedMagnitude: PlantHedgesRecommendation.HedgeRestoreMetersPerHectare);
            var model = new EcosystemModel(initialHedgerowDensity: 50.0);

            // First pass: applies, hedge bump.
            int firstPass = AutoActionPipeline.Apply(journal, model, new ScenarioContext(), currentDay: 12);
            Assert.AreEqual(1, firstPass);
            Assert.AreEqual(80.0, model.HedgerowDensity, 1e-9);

            // Second pass: already applied, no further change.
            int secondPass = AutoActionPipeline.Apply(journal, model, new ScenarioContext(), currentDay: 13);
            Assert.AreEqual(0, secondPass);
            Assert.AreEqual(80.0, model.HedgerowDensity, 1e-9);
        }

        [Test]
        public void Apply_applies_AutoAccepted_entries_too()
        {
            var journal = new DecisionJournal();
            var rec = new IrrigationAdviceRecommendation(10, "evt#10");
            journal.Append(rec, currentDay: 10);
            journal.SetVerdict(rec.Id, DecisionVerdict.AutoAccepted, currentDay: 10,
                appliedMagnitude: IrrigationAdviceRecommendation.WaterReliefDepthMeters);
            var model = new EcosystemModel(initialWaterTableDepth: 6.0);
            int applied = AutoActionPipeline.Apply(journal, model, new ScenarioContext(), currentDay: 10);
            Assert.AreEqual(1, applied);
            Assert.Less(model.WaterTableDepth, 6.0);
        }

        // ---------------- Journal MarkApplied / IsApplied ----------------

        [Test]
        public void MarkApplied_returns_false_on_second_call()
        {
            var journal = new DecisionJournal();
            Assert.IsTrue(journal.MarkApplied("rec#1", 10));
            Assert.IsFalse(journal.MarkApplied("rec#1", 11));
            Assert.IsTrue(journal.IsApplied("rec#1"));
            Assert.AreEqual(1, journal.AppliedCount);
        }
    }
}
