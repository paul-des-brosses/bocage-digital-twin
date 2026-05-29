using Bocage.Decision;
using Bocage.Decision.Recommendations;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Pins the journal's append + supersession contract added at
    /// sub-étape 10a. Two rules under test:
    /// <list type="number">
    ///   <item>Event-instance dedup (existing): same TriggeredByEventId → no double-append.</item>
    ///   <item>Type-level supersession (new): a new Pending of an
    ///         existing type marks the older Pending Superseded so
    ///         <see cref="DecisionJournal.PendingEntries"/> never holds
    ///         more than one of a given type.</item>
    /// </list>
    /// Already-resolved entries (Accepted/Rejected/AutoAccepted) are
    /// invariant under supersession — these tests pin that too.
    /// </summary>
    public sealed class DecisionJournalTests
    {
        [Test]
        public void Append_dedups_by_triggering_event_id()
        {
            var journal = new DecisionJournal();
            var rec1 = new PlantHedgesRecommendation(issuedOnDay: 28, triggeredByEventId: "manual#28");
            // Same event id, different rec instance → should be rejected.
            var rec2 = new PlantHedgesRecommendation(issuedOnDay: 29, triggeredByEventId: "manual#28");

            Assert.IsTrue(journal.Append(rec1, currentDay: 28));
            Assert.IsFalse(journal.Append(rec2, currentDay: 29));
            Assert.AreEqual(1, journal.Entries.Count);
        }

        [Test]
        public void Append_supersedes_previous_pending_of_same_type()
        {
            var journal = new DecisionJournal();
            var first = new PlantHedgesRecommendation(issuedOnDay: 28, triggeredByEventId: "manual#28");
            var second = new PlantHedgesRecommendation(issuedOnDay: 58, triggeredByEventId: "manual#58");

            journal.Append(first, currentDay: 28);
            journal.Append(second, currentDay: 58);

            // Two entries total — both are recorded, but the first
            // has been auto-Superseded.
            Assert.AreEqual(2, journal.Entries.Count);
            Assert.AreEqual(DecisionVerdict.Superseded, journal.Entries[0].Verdict);
            Assert.AreEqual(DecisionVerdict.Pending, journal.Entries[1].Verdict);
            // History list shows the latest only.
            Assert.AreEqual(1, journal.PendingEntries.Count);
            Assert.AreSame(second, journal.PendingEntries[0].Recommendation);
        }

        [Test]
        public void Append_does_not_supersede_accepted_of_same_type()
        {
            // The user accepted the first PlantHedges. A later event
            // produces a new PlantHedges — the Accepted one MUST NOT
            // be touched (it carries the magnitude applied to the
            // model), and the new one comes in as a fresh Pending.
            var journal = new DecisionJournal();
            var accepted = new PlantHedgesRecommendation(issuedOnDay: 28, triggeredByEventId: "manual#28");
            journal.Append(accepted, currentDay: 28);
            journal.SetVerdict(accepted.Id, DecisionVerdict.Accepted, currentDay: 28, appliedMagnitude: 30.0);

            var fresh = new PlantHedgesRecommendation(issuedOnDay: 58, triggeredByEventId: "manual#58");
            journal.Append(fresh, currentDay: 58);

            Assert.AreEqual(DecisionVerdict.Accepted, journal.Entries[0].Verdict);
            Assert.AreEqual(30.0, journal.Entries[0].AppliedMagnitude, 1e-9);
            Assert.AreEqual(DecisionVerdict.Pending, journal.Entries[1].Verdict);
            Assert.AreEqual(1, journal.PendingEntries.Count);
        }

        [Test]
        public void Append_does_not_supersede_rejected_of_same_type()
        {
            // The user Ignorer'd the first; the Rejected entry should
            // stay Rejected in the audit trail, and the new Pending
            // is added cleanly.
            var journal = new DecisionJournal();
            var rejected = new PlantHedgesRecommendation(issuedOnDay: 28, triggeredByEventId: "manual#28");
            journal.Append(rejected, currentDay: 28);
            journal.SetVerdict(rejected.Id, DecisionVerdict.Rejected, currentDay: 28);

            var fresh = new PlantHedgesRecommendation(issuedOnDay: 58, triggeredByEventId: "manual#58");
            journal.Append(fresh, currentDay: 58);

            Assert.AreEqual(DecisionVerdict.Rejected, journal.Entries[0].Verdict);
            Assert.AreEqual(DecisionVerdict.Pending, journal.Entries[1].Verdict);
            Assert.AreEqual(1, journal.PendingEntries.Count);
        }

        [Test]
        public void Supersession_only_targets_same_type_not_different_types()
        {
            // PlantHedges arrives, then Irrigation arrives. The two
            // are different types — both should stay Pending side by
            // side.
            var journal = new DecisionJournal();
            var hedges = new PlantHedgesRecommendation(issuedOnDay: 28, triggeredByEventId: "manual#28");
            var irrig = new IrrigationAdviceRecommendation(issuedOnDay: 35, triggeredByEventId: "drought-prolonged#35");

            journal.Append(hedges, currentDay: 28);
            journal.Append(irrig, currentDay: 35);

            Assert.AreEqual(2, journal.PendingEntries.Count);
        }

        [Test]
        public void Three_recos_same_type_supersede_in_chain()
        {
            // Simulates a user repeatedly ignoring manual plant-hedges
            // recos over a long run: only the LATEST one ever shows in
            // pending.
            var journal = new DecisionJournal();
            var r1 = new PlantHedgesRecommendation(issuedOnDay: 28, triggeredByEventId: "manual#28");
            var r2 = new PlantHedgesRecommendation(issuedOnDay: 58, triggeredByEventId: "manual#58");
            var r3 = new PlantHedgesRecommendation(issuedOnDay: 88, triggeredByEventId: "manual#88");

            journal.Append(r1, 28);
            journal.Append(r2, 58);
            journal.Append(r3, 88);

            Assert.AreEqual(3, journal.Entries.Count);
            Assert.AreEqual(DecisionVerdict.Superseded, journal.Entries[0].Verdict);
            Assert.AreEqual(DecisionVerdict.Superseded, journal.Entries[1].Verdict);
            Assert.AreEqual(DecisionVerdict.Pending, journal.Entries[2].Verdict);
            Assert.AreEqual(1, journal.PendingEntries.Count);
            Assert.AreSame(r3, journal.PendingEntries[0].Recommendation);
        }
    }
}
