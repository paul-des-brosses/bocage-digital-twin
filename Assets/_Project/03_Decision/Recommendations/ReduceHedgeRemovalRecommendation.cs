using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Ecological recommendation: slow the rate at which the farmer is grubbing
    /// out hedges. Lowers <c>ScenarioContext.HedgeRemovalRate</c> (m/ha/yr), which
    /// drives the loss term of <c>AgriculturalPressureImpactRule</c>. Only
    /// coherent when removal is actually active (guarded by the dispatch), and it
    /// is the cheapest habitat lever — stopping destruction costs no upfront
    /// capital, unlike planting.
    /// <para>
    /// Source: INRAE / OFB — hedges are the strongest single biodiversity driver
    /// in temperate bocage (Constant et al. 1976 via Réseau Haies: ~doubling of
    /// breeding birds in dense bocage), and they carry PSE + PAC payments, so
    /// keeping them is usually profit-neutral-to-positive. Counterpart of
    /// <see cref="IncreaseHedgeRemovalRecommendation"/> on the same lever.
    /// </para>
    /// </summary>
    public sealed class ReduceHedgeRemovalRecommendation : IRecommendation
    {
        /// <summary>Default reduction of the removal rate (m/ha/yr) proposed.</summary>
        public const double RemovalCutPerStep = 5.0;

        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }

        public double InvestmentCostEurosPerHectare => 0.0;

        public ReduceHedgeRemovalRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "reduce-hedge-removal#" + issuedOnDay,
                title: "Réduire l'arrachage des haies",
                rationale: FormatAutoRationale(RemovalCutPerStep),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending)
        {
        }

        private ReduceHedgeRemovalRecommendation(string id, string title, string rationale,
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
            return "Réduit le rythme d'arrachage des haies de "
                 + magnitude.ToString("0", FrFr) + " m/ha/an. Effet : préserve "
                 + "l'habitat (corridor de biodiversité) et les paiements PSE/PAC. "
                 + "Déclenché par : anomalie faune avec arrachage encore actif.";
        }
    }
}
