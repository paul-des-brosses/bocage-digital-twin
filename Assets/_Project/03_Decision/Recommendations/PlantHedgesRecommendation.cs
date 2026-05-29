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
        public DecisionVerdict DefaultVerdict => DecisionVerdict.Pending;

        public PlantHedgesRecommendation(int issuedOnDay, string triggeredByEventId)
        {
            Id = "plant-hedges#" + issuedOnDay;
            IssuedOnDay = issuedOnDay;
            TriggeredByEventId = triggeredByEventId;
        }
    }
}
