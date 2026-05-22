using Bocage.Decision;
using Bocage.Decision.Outcomes;
using Bocage.Decision.Recommendations;
using Bocage.Sensors;
using Bocage.Sensors.Events;
using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests for the Couche 3 RecommendationEngine + DecisionJournal +
    /// OutcomeProjector. Focus on the dispatch correctness (one event
    /// → one rec of the expected type), the dedup guarantee (the
    /// engine doesn't reissue once journalled), and the outcome
    /// distributions' rough magnitude and sign.
    /// </summary>
    public sealed class RecommendationEngineTests
    {
        // ---------------- Dispatch ----------------

        [Test]
        public void ChalaraEvent_produces_PlantHedgesRecommendation()
        {
            var ev = new HedgeChalaraEvent(detectedOnDay: 100, hedgerowDensityMetersPerHectare: 55.0);
            var rec = RecommendationEngine.TryProduceFor(ev);
            Assert.IsInstanceOf<PlantHedgesRecommendation>(rec);
            Assert.AreEqual(100, rec.IssuedOnDay);
        }

        [Test]
        public void DroughtEvent_produces_IrrigationAdviceRecommendation()
        {
            var ev = new DroughtProlongedEvent(detectedOnDay: 50, waterTableDepthMeters: 6.0, consecutiveDryDays: 30);
            var rec = RecommendationEngine.TryProduceFor(ev);
            Assert.IsInstanceOf<IrrigationAdviceRecommendation>(rec);
        }

        [Test]
        public void FaunaEvent_produces_ReduceInputsRecommendation()
        {
            var ev = new FaunaAcousticAnomalyEvent(detectedOnDay: 200, faunaPopulation: 0.3);
            var rec = RecommendationEngine.TryProduceFor(ev);
            Assert.IsInstanceOf<ReduceInputsRecommendation>(rec);
        }

        // ---------------- Engine vs journal ----------------

        [Test]
        public void ProduceRecommendations_one_per_event_when_journal_empty()
        {
            var engine = new RecommendationEngine();
            var log = new EventLog();
            var journal = new DecisionJournal();
            log.Append(new HedgeChalaraEvent(10, 55));
            log.Append(new DroughtProlongedEvent(20, 6.0, 30));

            var recs = engine.ProduceRecommendations(log, journal);
            Assert.AreEqual(2, recs.Count);
        }

        [Test]
        public void ProduceRecommendations_skips_events_already_covered_by_journal()
        {
            var engine = new RecommendationEngine();
            var log = new EventLog();
            var journal = new DecisionJournal();
            var ev = new HedgeChalaraEvent(10, 55);
            log.Append(ev);

            // First pass: produces 1 rec, append it.
            var firstPass = engine.ProduceRecommendations(log, journal);
            Assert.AreEqual(1, firstPass.Count);
            journal.Append(firstPass[0], currentDay: 10);

            // Second pass: same log, journal now covers the event → 0 new.
            var secondPass = engine.ProduceRecommendations(log, journal);
            Assert.AreEqual(0, secondPass.Count,
                "Re-running the engine should not re-issue covered recs.");
        }

        // ---------------- DecisionJournal ----------------

        [Test]
        public void Journal_Append_is_idempotent_per_triggering_event()
        {
            var journal = new DecisionJournal();
            var ev = new HedgeChalaraEvent(10, 55);
            string evInstance = RecommendationEngine.MakeEventInstanceId(ev);
            var rec1 = new PlantHedgesRecommendation(issuedOnDay: 10, triggeredByEventId: evInstance);
            var rec2 = new PlantHedgesRecommendation(issuedOnDay: 12, triggeredByEventId: evInstance);

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

        // ---------------- OutcomeProjector ----------------

        [Test]
        public void OutcomeProjector_returns_two_horizons()
        {
            var rec = new PlantHedgesRecommendation(10, "evt#10");
            var outcomes = OutcomeProjector.Project(rec, new EcosystemModel());
            Assert.AreEqual(2, outcomes.Length);
            Assert.AreEqual(OutcomeProjector.ShortHorizonDays, outcomes[0].HorizonInDays);
            Assert.AreEqual(OutcomeProjector.LongHorizonDays, outcomes[1].HorizonInDays);
        }

        [Test]
        public void OutcomeProjector_plant_hedges_costs_short_term_gains_long_term()
        {
            var rec = new PlantHedgesRecommendation(10, "evt#10");
            var outcomes = OutcomeProjector.Project(rec, new EcosystemModel());
            // Short term expected profit delta should be negative
            // (implementation cost) and biodiversity ≈ 0.
            Assert.Less(outcomes[0].ProfitDeltaExpected, 0.0,
                "PlantHedges short-term expected profit should be negative (planting cost).");
            // Long term expected biodiversity gain should be positive.
            Assert.Greater(outcomes[1].BiodiversityDeltaExpected, 0.0,
                "PlantHedges long-term expected biodiversity should be positive.");
        }

        [Test]
        public void OutcomeProjector_worst_le_expected_le_best()
        {
            // Invariant: the 3-point bracket must respect order on each axis.
            // Guards against accidental swap when tuning future coefficients.
            var recs = new IRecommendation[]
            {
                new PlantHedgesRecommendation(10, "a"),
                new IrrigationAdviceRecommendation(10, "b"),
                new ReduceInputsRecommendation(10, "c"),
            };
            foreach (var rec in recs)
            {
                var outcomes = OutcomeProjector.Project(rec, new EcosystemModel());
                foreach (var o in outcomes)
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
