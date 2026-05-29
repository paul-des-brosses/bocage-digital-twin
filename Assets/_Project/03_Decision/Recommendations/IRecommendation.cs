namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// A single actionable suggestion produced by the
    /// <see cref="RecommendationEngine"/> in response to an event from
    /// the Couche 2 sensor layer. Pure data: outcomes (with uncertainty
    /// at 2 horizons) are computed by <see cref="Outcomes.OutcomeProjector"/>
    /// against the current model state, and the verdict is recorded in
    /// the <see cref="DecisionJournal"/> after user arbitration (or
    /// applied straight away if <see cref="DefaultVerdict"/> is
    /// <see cref="DecisionVerdict.AutoAccepted"/>).
    /// </summary>
    public interface IRecommendation
    {
        /// <summary>Stable identifier (e.g. "plant-hedges#42") used for journalling and de-dup.</summary>
        string Id { get; }

        /// <summary>Short label for the decision panel.</summary>
        string Title { get; }

        /// <summary>One-line justification displayed beneath the title.</summary>
        string Rationale { get; }

        /// <summary>Simulated day on which the recommendation was issued.</summary>
        int IssuedOnDay { get; }

        /// <summary>
        /// Identifier of the <c>IEvent</c> that triggered this
        /// recommendation. Used to dedupe (one rec per event) and to
        /// surface the causal chain in the decision panel.
        /// </summary>
        string TriggeredByEventId { get; }

        /// <summary>
        /// Initial verdict before any user input. Most recs are
        /// <see cref="DecisionVerdict.Pending"/>, but trivial actions
        /// (e.g. observation-only) could ship as <see cref="DecisionVerdict.AutoAccepted"/>.
        /// </summary>
        DecisionVerdict DefaultVerdict { get; }

        /// <summary>
        /// Upfront capital cost of the action in € per hectare, baked
        /// at construction time from the embedded magnitude. Used by
        /// <see cref="DecisionJournal.TotalInvestmentEurosPerHectare"/>
        /// and the popup binding (chantier E5 / ADR #50). Manual recs
        /// (ADR #47) lock the click-time magnitude so the value is
        /// exact and matches the journal's <c>AppliedMagnitude</c>.
        /// Auto recs lock the default magnitude — if the user later
        /// moves the popup slider, the popup re-computes the displayed
        /// cost from the slider value and the journal cumul uses the
        /// applied magnitude (cf. <see cref="DecisionJournal.TotalInvestmentEurosPerHectare"/>).
        /// PlantHedges manual: <c>magnitude × 5 €/m</c> (médiane Réseau
        /// Haies 3-10 €/m). Irrigation and ReduceInputs: 0 — coût
        /// récurrent intégré dans <c>InputCost</c> / <c>WaterTableDepth</c>.
        /// </summary>
        double InvestmentCostEurosPerHectare { get; }
    }
}
