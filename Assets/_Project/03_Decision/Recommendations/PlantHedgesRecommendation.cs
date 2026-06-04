using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests planting fresh hedge segments to strengthen the bocage
    /// network. Triggered manually by the « Replanter haies » button, and
    /// (since chantier E9) algorithmically by
    /// <see cref="Bocage.Decision.RecommendationEngine"/> as the habitat
    /// response to a fauna anomaly once the input lever is exhausted. ADR #55 pattern :
    /// Title court, rationale d'action concrète, ligne « Effet
    /// modélisé : ... » chiffrée. The coût d'entretien Y €/ha/an
    /// induit par la densité plantée est repris automatiquement par
    /// <c>MaintenanceCostDynamicsRule</c> (linéaire en HedgerowDensity),
    /// donc pas dupliqué dans le rationale. Le coût upfront — le vrai
    /// signal manquant — est porté par <see cref="InvestmentCostEurosPerHectare"/>
    /// depuis le chantier E5 / ADR #50.
    /// </summary>
    public sealed class PlantHedgesRecommendation : IRecommendation
    {
        public const double HedgeRestoreMetersPerHectare = 30.0;

        /// <summary>
        /// Median planting price per linear metre, € per metre. Source
        /// CALIBRATION.md §Capital — Réseau Haies de France et MAEC
        /// référentiel coûts plantation 3-10 €/m, médiane retenue 5 €/m.
        /// Indépendant de la magnitude : <c>InvestmentCost (€/ha) =
        /// magnitude (m/ha) × EurosPerMeterPlanted (€/m)</c>.
        /// </summary>
        public const double EurosPerMeterPlanted = 5.0;
        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }
        public double InvestmentCostEurosPerHectare { get; }

        public PlantHedgesRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "plant-hedges#" + issuedOnDay,
                title: "Planter des linéaires de haies",
                rationale: FormatRationale(HedgeRestoreMetersPerHectare),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending,
                investmentCost: ComputeInvestmentCost(HedgeRestoreMetersPerHectare))
        {
        }

        private PlantHedgesRecommendation(string id, string title, string rationale,
            int issuedOnDay, string triggeredByEventId, DecisionVerdict defaultVerdict,
            double investmentCost)
        {
            Id = id;
            Title = title;
            Rationale = rationale;
            IssuedOnDay = issuedOnDay;
            TriggeredByEventId = triggeredByEventId;
            DefaultVerdict = defaultVerdict;
            InvestmentCostEurosPerHectare = investmentCost < 0.0 ? 0.0 : investmentCost;
        }

        /// <summary>
        /// Manual-pathway factory (ADR #47). The user has already clicked
        /// the « Replanter haies » button with a chosen <paramref name="magnitude"/>,
        /// so the rec ships as <see cref="DecisionVerdict.AutoAccepted"/>
        /// and the <see cref="Bocage.Decision.AutoActionPipeline"/>
        /// applies it on the next pass. <paramref name="sequence"/>
        /// disambiguates multiple clicks on the same simulated day
        /// (kept in <see cref="Bocage.Presentation.Simulation.SimulationRunner"/>).
        /// The investment cost is baked from the clicked magnitude so
        /// it matches the journal's <c>AppliedMagnitude</c>.
        /// </summary>
        public static PlantHedgesRecommendation Manual(int day, int sequence, double magnitude)
        {
            return new PlantHedgesRecommendation(
                id: "manual-plant-hedges#" + day + "-" + sequence,
                title: "Planter des linéaires de haies",
                rationale: FormatRationale(magnitude),
                issuedOnDay: day,
                triggeredByEventId: null,
                defaultVerdict: DecisionVerdict.AutoAccepted,
                investmentCost: ComputeInvestmentCost(magnitude));
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

        /// <summary>
        /// Pure helper: upfront capital cost (€/ha) for a given
        /// planted density (m/ha). Used by <see cref="Manual"/> at
        /// construction time, by the popup binding to refresh the
        /// « Coût upfront estimé » label live when the slider moves,
        /// and by <see cref="Bocage.Decision.DecisionJournal.TotalInvestmentEurosPerHectare"/>
        /// to cumulate after the action has been applied with the
        /// final magnitude.
        /// </summary>
        public static double ComputeInvestmentCost(double magnitude)
        {
            if (magnitude < 0.0) return 0.0;
            return magnitude * EurosPerMeterPlanted;
        }
    }
}
