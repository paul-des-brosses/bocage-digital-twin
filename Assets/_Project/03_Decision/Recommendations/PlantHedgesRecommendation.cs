namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests planting fresh hedge segments to strengthen the bocage
    /// network. Triggered exclusively by the "Replanter des haies"
    /// manual button in the dashboard's Espace agriculteur (no
    /// algorithmic emission from <see cref="Bocage.Decision.RecommendationEngine"/>).
    /// Source for the prescription: PNR du Perche replanting protocol
    /// (charme, érable champêtre, noisetier).
    /// <para>
    /// Mechanical effect when applied: a one-off boost of
    /// <c>HedgerowDensity</c> by the user-chosen magnitude (default
    /// <see cref="HedgeRestoreMetersPerHectare"/> m/ha).
    /// </para>
    /// </summary>
    public sealed class PlantHedgesRecommendation : IRecommendation
    {
        public const double HedgeRestoreMetersPerHectare = 30.0;

        public string Id { get; }
        public string Title => "Replanter des haies (essences résistantes)";
        public string Rationale => "Renforcer la trame bocagère via plantation d'essences locales (charme, érable champêtre, noisetier) pour gagner en résilience long terme.";
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }

        public PlantHedgesRecommendation(int issuedOnDay, string triggeredByEventId)
            : this("plant-hedges#" + issuedOnDay, issuedOnDay, triggeredByEventId, DecisionVerdict.Pending)
        {
        }

        private PlantHedgesRecommendation(string id, int issuedOnDay, string triggeredByEventId, DecisionVerdict defaultVerdict)
        {
            Id = id;
            IssuedOnDay = issuedOnDay;
            TriggeredByEventId = triggeredByEventId;
            DefaultVerdict = defaultVerdict;
        }

        /// <summary>
        /// Manual-pathway factory (ADR #47). The user has already clicked
        /// the « Replanter haies » button with a chosen magnitude, so
        /// the rec ships as <see cref="DecisionVerdict.AutoAccepted"/>
        /// and the <see cref="Bocage.Decision.AutoActionPipeline"/>
        /// applies it on the next pass. <paramref name="sequence"/>
        /// disambiguates multiple clicks on the same simulated day
        /// (kept in <see cref="Bocage.Presentation.Simulation.SimulationRunner"/>).
        /// </summary>
        public static PlantHedgesRecommendation Manual(int day, int sequence)
        {
            return new PlantHedgesRecommendation(
                id: "manual-plant-hedges#" + day + "-" + sequence,
                issuedOnDay: day,
                triggeredByEventId: null,
                defaultVerdict: DecisionVerdict.AutoAccepted);
        }
    }
}
