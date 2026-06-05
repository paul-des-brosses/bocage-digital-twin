using System;
using Bocage.Decision;
using Bocage.Decision.Recommendations;
using Bocage.Indicators.Hero;
using Bocage.Sensors;
using Bocage.Sensors.Events;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests for the Couche 3 RecommendationEngine + DecisionJournal. Focus on
    /// dispatch correctness (one event → one rec of the expected type), the
    /// model-derived selection (the engine projects feasible levers and keeps
    /// the one that best serves the farmer), the economic gate that REPLACES the
    /// old hardcoded profit-optimum (raising inputs only fires when the
    /// projection shows a real gain), and the dedup guarantee. The projector
    /// itself is covered by <see cref="ModelOutcomeProjectorTests"/>.
    /// </summary>
    public sealed class RecommendationEngineTests
    {
        private static readonly SeasonalWeatherData Weather = SeasonalWeatherDataDefaults.MortagneAuPerche();
        private const ulong Seed = 4242UL;
        private static readonly Func<EcosystemModel, ScenarioContext, double> Profit = IntegratedProfitabilityIndicator.Compute;
        private static readonly Func<EcosystemModel, ScenarioContext, double> Biodiv = BiodiversityCompositeIndicator.Compute;

        // Runs the engine's single-event mapping with the real KPI evaluators,
        // exactly as the Couche 05 runner wires them.
        private static IRecommendation Produce(IEvent ev, ScenarioContext scenario, EcosystemModel model)
            => RecommendationEngine.TryProduceFor(ev, scenario, model, Seed, Weather, Profit, Biodiv);

        // ---------------- Dispatch (event → response domain) ----------------

        [Test]
        public void DroughtEvent_produces_IrrigationAdviceRecommendation()
        {
            var ev = new DroughtProlongedEvent(detectedOnDay: 50, waterTableDepthMeters: 6.0, consecutiveDryDays: 30);
            Assert.IsInstanceOf<IrrigationAdviceRecommendation>(Produce(ev, new ScenarioContext(), new EcosystemModel()));
        }

        [Test]
        public void FaunaEvent_above_floor_intensity_produces_ReduceInputsRecommendation()
        {
            // From conventional intensity (1.0, above the ~optimum), cutting inputs
            // both saves cost AND recovers fauna — a win the projection rates above
            // capital-costing planting, so the engine picks ReduceInputs.
            var ev = new FaunaAcousticAnomalyEvent(detectedOnDay: 200, faunaPopulation: 0.3);
            var rec = Produce(ev, new ScenarioContext(initialInputIntensityFactor: 1.0), new EcosystemModel());
            Assert.IsInstanceOf<ReduceInputsRecommendation>(rec);
        }

        [Test]
        public void FaunaEvent_at_floor_intensity_redirects_to_habitat()
        {
            // §17 redirect: inputs are at the organic-extensive floor (no headroom),
            // no active hedge removal → the only feasible lever is planting.
            var ev = new FaunaAcousticAnomalyEvent(detectedOnDay: 200, faunaPopulation: 0.3);
            var atFloor = new ScenarioContext(
                initialInputIntensityFactor: ReduceInputsRecommendation.MinInputIntensityFactor);
            Assert.IsInstanceOf<PlantHedgesRecommendation>(Produce(ev, atFloor, new EcosystemModel()));
        }

        [Test]
        public void FaunaEvent_with_active_removal_at_floor_slows_removal_first()
        {
            // Inputs floored AND the farmer is grubbing hedges → the only zero-cost
            // habitat move (stop grubbing) beats capital-costing planting.
            var ev = new FaunaAcousticAnomalyEvent(detectedOnDay: 200, faunaPopulation: 0.3);
            var scenario = new ScenarioContext(
                initialInputIntensityFactor: ReduceInputsRecommendation.MinInputIntensityFactor,
                initialHedgeRemovalRate: 8.0);
            Assert.IsInstanceOf<ReduceHedgeRemovalRecommendation>(Produce(ev, scenario, new EcosystemModel()));
        }

        [Test]
        public void FaunaEvent_all_habitat_levers_exhausted_produces_no_recommendation()
        {
            // Inputs floored, no removal, hedges saturated → the low fauna has
            // another cause; the engine stays silent (no impossible action, §17).
            var ev = new FaunaAcousticAnomalyEvent(detectedOnDay: 200, faunaPopulation: 0.3);
            var scenario = new ScenarioContext(
                initialInputIntensityFactor: ReduceInputsRecommendation.MinInputIntensityFactor);
            var saturated = new EcosystemModel(initialHedgerowDensity: 200.0);
            Assert.IsNull(Produce(ev, scenario, saturated));
        }

        [Test]
        public void SoilCarbonLowEvent_with_room_produces_a_carbon_lever()
        {
            // The engine projects cover crops vs residue restitution and keeps the
            // better-rated carbon lever. Either is a valid carbon response; both
            // address the event. (Which one wins is model-derived, asserted by the
            // dedicated projector tests, not pinned to a fixed priority here.)
            var ev = new SoilCarbonLowEvent(detectedOnDay: 100, soilCarbon: 40.0);
            var rec = Produce(ev, new ScenarioContext(), new EcosystemModel());
            Assert.IsTrue(rec is SowCoverCropsRecommendation || rec is RestoreResidueRecommendation,
                "Soil-carbon-low should produce a carbon-building lever, got " + (rec?.GetType().Name ?? "null"));
        }

        [Test]
        public void SoilCarbonLowEvent_cover_full_falls_back_to_residue()
        {
            // Cover crops saturated → the only feasible carbon lever is residue.
            var ev = new SoilCarbonLowEvent(detectedOnDay: 100, soilCarbon: 40.0);
            var scenario = new ScenarioContext(initialCoverCropsCoveragePercent: 100.0);
            Assert.IsInstanceOf<RestoreResidueRecommendation>(Produce(ev, scenario, new EcosystemModel()));
        }

        // ---------------- Economic counter-recs + derived optimum ----------------

        [Test]
        public void LowProfitability_well_below_optimum_with_healthy_fauna_raises_inputs()
        {
            // Intensity at the organic floor (0.5) is below the profit optimum, so
            // the projection shows raising inputs pays → RaiseInputs fires.
            var ev = new LowProfitabilityEvent(detectedOnDay: 300, profitEurosPerHectare: 20.0, biodiversity: 0.6);
            var scenario = new ScenarioContext(initialInputIntensityFactor: 0.5);
            Assert.IsInstanceOf<RaiseInputsRecommendation>(Produce(ev, scenario, new EcosystemModel()));
        }

        [Test]
        public void LowProfitability_does_not_trade_away_critical_biodiversity()
        {
            // Biodiversity below the critical threshold → the engine refuses the
            // economy-for-ecology trade and stays silent (guard before projection).
            var ev = new LowProfitabilityEvent(detectedOnDay: 300, profitEurosPerHectare: 20.0, biodiversity: 0.2);
            var scenario = new ScenarioContext(initialInputIntensityFactor: 0.5);
            Assert.IsNull(Produce(ev, scenario, new EcosystemModel()));
        }

        [Test]
        public void LowProfitability_overdense_unsubsidised_hedges_are_thinned()
        {
            // Intensity already high (1.5, past the optimum) so raising inputs would
            // LOSE profit and is gated out; the over-dense, weakly-subsidised hedges
            // are the lever that recovers margin → IncreaseHedgeRemoval.
            var ev = new LowProfitabilityEvent(detectedOnDay: 300, profitEurosPerHectare: 20.0, biodiversity: 0.6);
            var scenario = new ScenarioContext(
                initialInputIntensityFactor: 1.5,
                initialPseSubsidyRate: 0.0);
            var overdense = new EcosystemModel(initialHedgerowDensity: 160.0);
            Assert.IsInstanceOf<IncreaseHedgeRemovalRecommendation>(Produce(ev, scenario, overdense));
        }

        [Test]
        public void LowProfitability_past_optimum_with_normal_hedges_stays_silent()
        {
            // Intensity past the optimum (1.5) so raising loses profit (gated out),
            // and hedges are normal so there is nothing to thin → the engine fires
            // no economic counter-rec. This is the derived-optimum behaviour that
            // replaces the old hardcoded 0.8 threshold.
            var ev = new LowProfitabilityEvent(detectedOnDay: 300, profitEurosPerHectare: 20.0, biodiversity: 0.6);
            var scenario = new ScenarioContext(initialInputIntensityFactor: 1.5);
            Assert.IsNull(Produce(ev, scenario, new EcosystemModel()));
        }

        // ---------------- Engine vs journal ----------------

        [Test]
        public void ProduceRecommendations_one_per_event_when_journal_empty()
        {
            var engine = new RecommendationEngine();
            var log = new EventLog();
            var journal = new DecisionJournal();
            log.Append(new FaunaAcousticAnomalyEvent(10, 0.3));
            log.Append(new DroughtProlongedEvent(20, 6.0, 30));

            var recs = engine.ProduceRecommendations(log, journal, new ScenarioContext(), new EcosystemModel(), Seed, Weather, Profit, Biodiv);
            Assert.AreEqual(2, recs.Count);
        }

        [Test]
        public void ProduceRecommendations_skips_events_already_covered_by_journal()
        {
            var engine = new RecommendationEngine();
            var log = new EventLog();
            var journal = new DecisionJournal();
            var ev = new FaunaAcousticAnomalyEvent(10, 0.3);
            log.Append(ev);

            var firstPass = engine.ProduceRecommendations(log, journal, new ScenarioContext(), new EcosystemModel(), Seed, Weather, Profit, Biodiv);
            Assert.AreEqual(1, firstPass.Count);
            journal.Append(firstPass[0], currentDay: 10);

            var secondPass = engine.ProduceRecommendations(log, journal, new ScenarioContext(), new EcosystemModel(), Seed, Weather, Profit, Biodiv);
            Assert.AreEqual(0, secondPass.Count,
                "Re-running the engine should not re-issue covered recs.");
        }

        [Test]
        public void ProduceRecommendations_marks_declined_event_considered()
        {
            // A low-profitability event past the optimum with normal hedges yields
            // no reco. It must be marked CONSIDERED so the model-derived engine does
            // not re-run the forward projection for it on every later tick.
            var engine = new RecommendationEngine();
            var log = new EventLog();
            var journal = new DecisionJournal();
            var ev = new LowProfitabilityEvent(detectedOnDay: 300, profitEurosPerHectare: 20.0, biodiversity: 0.6);
            log.Append(ev);

            var pass = engine.ProduceRecommendations(
                log, journal, new ScenarioContext(initialInputIntensityFactor: 1.5), new EcosystemModel(),
                Seed, Weather, Profit, Biodiv);
            Assert.AreEqual(0, pass.Count, "Past the optimum with normal hedges, no economic counter-rec.");
            Assert.IsTrue(journal.IsEventCovered(RecommendationEngine.MakeEventInstanceId(ev)),
                "A declined event must be marked considered so it is not re-projected each tick.");
        }

        // ---------------- DecisionJournal ----------------

        [Test]
        public void Journal_Append_is_idempotent_per_triggering_event()
        {
            var journal = new DecisionJournal();
            var ev = new DroughtProlongedEvent(detectedOnDay: 10, waterTableDepthMeters: 4.0, consecutiveDryDays: 30);
            string evInstance = RecommendationEngine.MakeEventInstanceId(ev);
            var rec1 = new IrrigationAdviceRecommendation(issuedOnDay: 10, triggeredByEventId: evInstance);
            var rec2 = new IrrigationAdviceRecommendation(issuedOnDay: 12, triggeredByEventId: evInstance);

            Assert.IsTrue(journal.Append(rec1, currentDay: 10));
            Assert.IsFalse(journal.Append(rec2, currentDay: 12),
                "Appending a second rec for the same event id must be a no-op.");
            Assert.AreEqual(1, journal.Entries.Count);
        }

        [Test]
        public void Journal_SetVerdict_moves_entry_from_pending_to_accepted()
        {
            var journal = new DecisionJournal();
            var rec = new PlantHedgesRecommendation(10, "evt#10");
            journal.Append(rec, currentDay: 10);
            Assert.AreEqual(1, journal.PendingEntries.Count);

            bool ok = journal.SetVerdict(rec.Id, DecisionVerdict.Accepted, currentDay: 11);
            Assert.IsTrue(ok);
            Assert.AreEqual(0, journal.PendingEntries.Count);
            Assert.AreEqual(1, journal.ResolvedEntries.Count);
            Assert.AreEqual(DecisionVerdict.Accepted, journal.ResolvedEntries[0].Verdict);
        }

        [Test]
        public void Journal_SetVerdict_returns_false_for_unknown_id()
        {
            var journal = new DecisionJournal();
            Assert.IsFalse(journal.SetVerdict("does-not-exist", DecisionVerdict.Accepted, currentDay: 1));
        }
    }
}
