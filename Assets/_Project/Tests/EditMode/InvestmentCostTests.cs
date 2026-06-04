using Bocage.Decision;
using Bocage.Decision.Recommendations;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the upfront capital cost machinery introduced in
    /// chantier E5 / ADR #50 : the
    /// <see cref="IRecommendation.InvestmentCostEurosPerHectare"/>
    /// field, the
    /// <see cref="PlantHedgesRecommendation.ComputeInvestmentCost"/>
    /// helper and the cumulative
    /// <see cref="DecisionJournal.TotalInvestmentEurosPerHectare"/>
    /// projection.
    /// </summary>
    public sealed class InvestmentCostTests
    {
        // ---------------- Per-rec cost ----------------

        [Test]
        public void PlantHedges_manual_cost_is_magnitude_times_rate()
        {
            // 30 m/ha × 5 €/m = 150 €/ha. Matches CALIBRATION.md §Capital
            // « médiane Réseau Haies retenue 5 €/m ».
            var rec = PlantHedgesRecommendation.Manual(day: 10, sequence: 1, magnitude: 30.0);
            Assert.AreEqual(150.0, rec.InvestmentCostEurosPerHectare, 1e-9);
        }

        [Test]
        public void PlantHedges_manual_cost_scales_linearly_with_magnitude()
        {
            // 100 m/ha (upper plage) × 5 €/m = 500 €/ha. Vs 10 m/ha × 5 = 50 €/ha.
            var heavy = PlantHedgesRecommendation.Manual(day: 1, sequence: 1, magnitude: 100.0);
            var light = PlantHedgesRecommendation.Manual(day: 1, sequence: 2, magnitude: 10.0);
            Assert.AreEqual(500.0, heavy.InvestmentCostEurosPerHectare, 1e-9);
            Assert.AreEqual(50.0, light.InvestmentCostEurosPerHectare, 1e-9);
        }

        [Test]
        public void PlantHedges_zero_magnitude_yields_zero_cost()
        {
            var rec = PlantHedgesRecommendation.Manual(day: 1, sequence: 1, magnitude: 0.0);
            Assert.AreEqual(0.0, rec.InvestmentCostEurosPerHectare, 1e-9);
        }

        [Test]
        public void PlantHedges_negative_magnitude_clamps_to_zero()
        {
            // Defensive: the SimulationRunner clamps to 0 too, but the
            // recommendation factory should be robust on its own.
            var rec = PlantHedgesRecommendation.Manual(day: 1, sequence: 1, magnitude: -5.0);
            Assert.AreEqual(0.0, rec.InvestmentCostEurosPerHectare, 1e-9);
        }

        [Test]
        public void Irrigation_advice_has_zero_investment_cost()
        {
            // ADR #50: recurring expense, no upfront capital.
            var manual = IrrigationAdviceRecommendation.Manual(day: 1, sequence: 1, magnitude: 1.5);
            var auto = new IrrigationAdviceRecommendation(issuedOnDay: 1, triggeredByEventId: "evt");
            Assert.AreEqual(0.0, manual.InvestmentCostEurosPerHectare, 1e-9);
            Assert.AreEqual(0.0, auto.InvestmentCostEurosPerHectare, 1e-9);
        }

        [Test]
        public void Reduce_inputs_has_zero_investment_cost()
        {
            var auto = new ReduceInputsRecommendation(issuedOnDay: 1, triggeredByEventId: "evt");
            Assert.AreEqual(0.0, auto.InvestmentCostEurosPerHectare, 1e-9);
        }

        // ---------------- Cumul via journal ----------------

        [Test]
        public void Journal_total_investment_starts_at_zero()
        {
            var journal = new DecisionJournal();
            Assert.AreEqual(0.0, journal.TotalInvestmentEurosPerHectare, 1e-9);
        }

        [Test]
        public void Journal_cumulates_plant_hedges_across_actions()
        {
            // Two manual plantations: 30 m/ha + 20 m/ha → 150 + 100 = 250 €/ha.
            var journal = new DecisionJournal();
            var first = PlantHedgesRecommendation.Manual(day: 5, sequence: 1, magnitude: 30.0);
            var second = PlantHedgesRecommendation.Manual(day: 80, sequence: 1, magnitude: 20.0);
            journal.Append(first, currentDay: 5, initialMagnitude: 30.0);
            journal.Append(second, currentDay: 80, initialMagnitude: 20.0);
            Assert.AreEqual(250.0, journal.TotalInvestmentEurosPerHectare, 1e-9);
        }

        [Test]
        public void Journal_ignores_irrigation_and_reduce_inputs_in_cumul()
        {
            // Only PlantHedges contributes. Adding Irrigation + ReduceInputs
            // entries should leave the total unchanged.
            var journal = new DecisionJournal();
            journal.Append(
                PlantHedgesRecommendation.Manual(day: 1, sequence: 1, magnitude: 30.0),
                currentDay: 1, initialMagnitude: 30.0);
            journal.Append(
                IrrigationAdviceRecommendation.Manual(day: 2, sequence: 1, magnitude: 1.5),
                currentDay: 2, initialMagnitude: 1.5);
            journal.Append(
                new ReduceInputsRecommendation(issuedOnDay: 3, triggeredByEventId: "evt"),
                currentDay: 3, initialMagnitude: 0.2);
            Assert.AreEqual(150.0, journal.TotalInvestmentEurosPerHectare, 1e-9);
        }

        [Test]
        public void Journal_excludes_rejected_and_pending_entries_from_cumul()
        {
            // Only Accepted / AutoAccepted entries count. A Pending or
            // Rejected PlantHedges should not show up in the total.
            var journal = new DecisionJournal();
            // Auto-pathway: lands Pending. No magnitude applied yet.
            var pending = new PlantHedgesRecommendation(issuedOnDay: 1, triggeredByEventId: "evt-1");
            journal.Append(pending, currentDay: 1);
            // User rejects.
            journal.SetVerdict(pending.Id, DecisionVerdict.Rejected, currentDay: 2, appliedMagnitude: 0.0);
            // Cumul stays at 0.
            Assert.AreEqual(0.0, journal.TotalInvestmentEurosPerHectare, 1e-9);

            // Now a manual action lands and is auto-accepted.
            var manual = PlantHedgesRecommendation.Manual(day: 3, sequence: 1, magnitude: 30.0);
            journal.Append(manual, currentDay: 3, initialMagnitude: 30.0);
            Assert.AreEqual(150.0, journal.TotalInvestmentEurosPerHectare, 1e-9);
        }
    }
}
