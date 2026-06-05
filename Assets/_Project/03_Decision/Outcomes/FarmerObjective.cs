namespace Bocage.Decision.Outcomes
{
    /// <summary>
    /// The internal objective the decision engine maximises when it ranks
    /// candidate levers (chantier modèle vivant / A1). It scores a projected
    /// outcome <c>U = w_eco · profit̂ + w_bio · ΔbiodivΔ</c> from the
    /// priorities of a REAL farmer, not from an artificial neutrality:
    /// <list type="bullet">
    ///   <item><b>Economic viability dominates</b> — a farm has to be
    ///         profitable to survive, so the profit delta carries the bulk of
    ///         the weight.</item>
    ///   <item><b>Biodiversity carries little DIRECT weight</b>, but it enters
    ///         strongly THROUGH the economy: the forward-projected profit
    ///         already embeds the windbreak yield bonus of hedges, the
    ///         fertility return of living soil, the PSE/MAEC subsidy on
    ///         maintained hedges and the resilience of yield to climate. So an
    ///         ecological move that pays shows up as a positive profit delta on
    ///         its own.</item>
    /// </list>
    /// That is what makes the thesis honest AND rigorous: with farmer weights
    /// (economy first), does ecology still get recommended? The answer EMERGES
    /// from the coupled model (the instrumentation reveals where ecology pays),
    /// it is not imposed by the weights.
    /// <para>
    /// Weights are internal (no new slider — CLAUDE.md §17 « on n'élargit pas »)
    /// and documented. Hierarchy sourced on the farm-decision literature, where
    /// income / economic survival is the primary objective and environmental
    /// goals are secondary and largely mediated by their economic payoff
    /// (Edwards-Jones 2006, « Modelling farmer decision-making »; Reimer et al.
    /// 2012 on conservation adoption driven by economic and agronomic fit).
    /// </para>
    /// <para>Pure Couche 03 — no Unity, no Couche 04 dependency. Testable in EditMode.</para>
    /// </summary>
    public static class FarmerObjective
    {
        /// <summary>Weight on the (normalised) profit delta. Dominant.</summary>
        public const double EconomicWeight = 0.80;

        /// <summary>Weight on the biodiversity delta. Weak DIRECT term — ecology
        /// mostly enters through the economic term above.</summary>
        public const double BiodiversityWeight = 0.20;

        /// <summary>
        /// Profit normalisation scale, in €/ha/yr. A profit swing of this size
        /// is treated as « one unit » of economic desirability, putting it on
        /// the same [-1, +1] footing as the biodiversity index so the two terms
        /// can be summed. 150 €/ha/yr is a meaningful margin swing for a Perche
        /// grandes-cultures system (≈ half the neutral net margin), so a lever
        /// that moves profit by that much weighs a full economic unit.
        /// </summary>
        public const double ProfitScaleEurosPerHectare = 150.0;

        /// <summary>
        /// The farmer-priority desirability of a projected outcome: dominant
        /// economic term (normalised) plus a small direct biodiversity term.
        /// Higher is better. Used to rank candidate levers; the winner is the
        /// lever that best serves the farmer once ecology's economic payoff is
        /// counted.
        /// </summary>
        public static double DeltaUtility(OutcomeDistribution longTerm)
        {
            double normalisedProfit = longTerm.ProfitDeltaExpected / ProfitScaleEurosPerHectare;
            return EconomicWeight * normalisedProfit
                 + BiodiversityWeight * longTerm.BiodiversityDeltaExpected;
        }
    }
}
