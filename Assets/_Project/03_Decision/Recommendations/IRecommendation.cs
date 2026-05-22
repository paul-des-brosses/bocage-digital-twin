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
    }
}
