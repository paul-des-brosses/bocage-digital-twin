using Bocage.Decision.Outcomes;
using Bocage.Decision.Recommendations;

namespace Bocage.Decision
{
    /// <summary>
    /// Classifies a recommendation by the SIGN of its projected long-term outcome
    /// (<see cref="OutcomeProjector"/>) and decides whether it should interrupt
    /// the player with a popup or sit passively in the decision list (chantier E9
    /// surfacing).
    /// <para>
    /// The rule: the twin only INTERRUPTS for (1) wins with no loser and (2)
    /// ecological emergencies — a profit-costing ecological fix escalates to a
    /// popup when biodiversity is critical. Every comfort trade-off, and every
    /// economy-for-ecology move, stays in the passive list with a « compromis »
    /// marker, so the system never pushes a value-laden choice.
    /// </para>
    /// <para>Pure Couche 03 — no presentation dependency, testable in EditMode.</para>
    /// </summary>
    public static class RecommendationSurfacing
    {
        public enum Kind
        {
            WinWin,
            EconomicTradeoff,
            EcologicalTradeoff,
            LoseLose,
        }

        /// <summary>
        /// Classifies by the long-horizon projected deltas: win-win when neither
        /// profit nor biodiversity worsens; economic trade-off when profit gains
        /// at biodiversity's expense; ecological trade-off when biodiversity gains
        /// at profit's expense; lose-lose if both worsen.
        /// </summary>
        public static Kind Classify(IRecommendation rec)
        {
            if (rec == null) return Kind.WinWin;
            var outcomes = OutcomeProjector.Project(rec);
            var longTerm = outcomes[outcomes.Length - 1];
            bool profitOk = longTerm.ProfitDeltaExpected >= 0.0;
            bool biodivOk = longTerm.BiodiversityDeltaExpected >= 0.0;
            if (profitOk && biodivOk) return Kind.WinWin;
            if (profitOk) return Kind.EconomicTradeoff;
            if (biodivOk) return Kind.EcologicalTradeoff;
            return Kind.LoseLose;
        }

        /// <summary>True for anything that is not a clean win-win (gets the marker).</summary>
        public static bool IsTradeoff(IRecommendation rec) => Classify(rec) != Kind.WinWin;

        /// <summary>
        /// Whether the recommendation should auto-open as an interrupting popup.
        /// Win-win always interrupts. An ecological trade-off escalates to a popup
        /// only when <paramref name="biodiversity"/> is below the critical
        /// threshold (the durable-damage tipping point). Economic trade-offs and
        /// lose-lose stay passive (decision list only).
        /// </summary>
        public static bool ShouldAutoPopup(IRecommendation rec, double biodiversity)
        {
            switch (Classify(rec))
            {
                case Kind.WinWin:
                    return true;
                case Kind.EcologicalTradeoff:
                    return biodiversity < RecommendationEngine.BiodiversityCriticalThreshold;
                default:
                    return false;
            }
        }
    }
}
