namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests planting fresh hedge segments (or accelerated replanting
    /// of resistant species) in response to a hedge chalara detection.
    /// Source for the prescription: PNR du Perche replanting protocol
    /// against ash dieback (Hymenoscyphus fraxineus), substituting
    /// resistant species (charme, érable champêtre).
    /// <para>
    /// Mechanical effect when accepted (sub-étape 8c.3 AutoAction):
    /// a one-off boost of <c>HedgerowDensity</c> by
    /// <see cref="HedgeRestoreMetersPerHectare"/> over a 30-day
    /// implementation window.
    /// </para>
    /// </summary>
    public sealed class PlantHedgesRecommendation : IRecommendation
    {
        public const double HedgeRestoreMetersPerHectare = 30.0;

        public string Id { get; }
        public string Title => "Replanter des haies (essences résistantes)";
        public string Rationale => "Chalara détecté — boost ponctuel de la trame bocagère sur 30 jours via plantation de charme/érable champêtre.";
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
