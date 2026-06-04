using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Economic (balance) recommendation: thin out over-dense, unsubsidised
    /// hedges. Raises <c>ScenarioContext.HedgeRemovalRate</c> (m/ha/yr). Coherent
    /// only in a narrow corner — when hedge density is well above the agronomic
    /// optimum (~90 m/ha, the yield-bell peak) AND the hedges are not paying
    /// (low PSE): there the marginal hedge costs more in lost yield + maintenance
    /// than it returns in PSE + PAC. The system surfaces it honestly (with its
    /// biodiversity cost shown), passively (decision list, never a popup), and
    /// only when profitability is abnormally low — it never pushes hedge
    /// destruction.
    /// <para>
    /// Source: the yield bell of <c>CropYieldDynamicsRule</c> (penalty above the
    /// ideal density), the maintenance cost of <c>MaintenanceCostDynamicsRule</c>,
    /// and the PSE/PAC structure of <c>IntegratedProfitabilityIndicator</c>.
    /// Counterpart of <see cref="ReduceHedgeRemovalRecommendation"/>.
    /// </para>
    /// </summary>
    public sealed class IncreaseHedgeRemovalRecommendation : IRecommendation
    {
        /// <summary>Default increase of the removal rate (m/ha/yr) proposed.</summary>
        public const double RemovalRaisePerStep = 5.0;

        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }

        public double InvestmentCostEurosPerHectare => 0.0;

        public IncreaseHedgeRemovalRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "increase-hedge-removal#" + issuedOnDay,
                title: "Éclaircir les haies en surdensité",
                rationale: FormatAutoRationale(RemovalRaisePerStep),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending)
        {
        }

        private IncreaseHedgeRemovalRecommendation(string id, string title, string rationale,
            int issuedOnDay, string triggeredByEventId, DecisionVerdict defaultVerdict)
        {
            Id = id;
            Title = title;
            Rationale = rationale;
            IssuedOnDay = issuedOnDay;
            TriggeredByEventId = triggeredByEventId;
            DefaultVerdict = defaultVerdict;
        }

        public static string FormatAutoRationale(double magnitude)
        {
            return "Éclaircit les haies en surdensité (+" + magnitude.ToString("0", FrFr)
                 + " m/ha/an d'arrachage). Au-dessus de l'optimum agronomique "
                 + "(~90 m/ha) et sans subvention, la haie marginale coûte en "
                 + "rendement et entretien plus qu'elle ne rapporte. "
                 + "Compromis : perte d'habitat. Déclenché par : densité de haies "
                 + "au-dessus de l'optimum et faiblement subventionnée.";
        }
    }
}
