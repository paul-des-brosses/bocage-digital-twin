using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests planting fresh hedge segments to strengthen the bocage
    /// network. Triggered exclusively by the « Replanter haies » manual
    /// button (no algorithmic emission since E0). ADR #55 pattern :
    /// Title court, rationale d'action concrète, ligne « Effet
    /// modélisé : ... » chiffrée. The coût d'entretien Y €/ha/an
    /// mentionné dans la spec ADR #55 est omis tant qu'il n'est pas
    /// modélisé — garde-fou contre les chimères non modélisées.
    /// </summary>
    public sealed class PlantHedgesRecommendation : IRecommendation
    {
        public const double HedgeRestoreMetersPerHectare = 30.0;
        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }

        public PlantHedgesRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "plant-hedges#" + issuedOnDay,
                title: "Planter des linéaires de haies",
                rationale: FormatRationale(HedgeRestoreMetersPerHectare),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending)
        {
        }

        private PlantHedgesRecommendation(string id, string title, string rationale,
            int issuedOnDay, string triggeredByEventId, DecisionVerdict defaultVerdict)
        {
            Id = id;
            Title = title;
            Rationale = rationale;
            IssuedOnDay = issuedOnDay;
            TriggeredByEventId = triggeredByEventId;
            DefaultVerdict = defaultVerdict;
        }

        /// <summary>
        /// Manual-pathway factory (ADR #47). The user has already clicked
        /// the « Replanter haies » button with a chosen <paramref name="magnitude"/>,
        /// so the rec ships as <see cref="DecisionVerdict.AutoAccepted"/>
        /// and the <see cref="Bocage.Decision.AutoActionPipeline"/>
        /// applies it on the next pass. <paramref name="sequence"/>
        /// disambiguates multiple clicks on the same simulated day
        /// (kept in <see cref="Bocage.Presentation.Simulation.SimulationRunner"/>).
        /// </summary>
        public static PlantHedgesRecommendation Manual(int day, int sequence, double magnitude)
        {
            return new PlantHedgesRecommendation(
                id: "manual-plant-hedges#" + day + "-" + sequence,
                title: "Planter des linéaires de haies",
                rationale: FormatRationale(magnitude),
                issuedOnDay: day,
                triggeredByEventId: null,
                defaultVerdict: DecisionVerdict.AutoAccepted);
        }

        /// <summary>
        /// Builds the ADR #55 « action concrète + Effet modélisé »
        /// rationale for a given <paramref name="magnitude"/> in m/ha.
        /// </summary>
        public static string FormatRationale(double magnitude)
        {
            return "Plantation d'essences locales (charme, érable champêtre, noisetier) sur bordures de parcelles. "
                 + "Effet modélisé : +" + magnitude.ToString("F1", FrFr) + " m/ha de densité de haies.";
        }
    }
}
