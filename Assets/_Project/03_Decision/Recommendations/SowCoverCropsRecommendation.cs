using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Ecological recommendation: sow cover crops between cash crops to rebuild
    /// soil carbon. Raises <c>ScenarioContext.CoverCropsCoveragePercent</c>, which
    /// drives the cover-crop input term of <c>SoilCarbonDynamicsRule</c>.
    /// Triggered by the eddy-flux tower measuring a low / declining soil carbon
    /// stock — the sensor that, until E9, drove no decision at all.
    /// <para>
    /// Source: INRAE 4 pour 1000 (cover crops store ~0.31 tC/ha/yr in 90% of
    /// cases, France's single most cost-effective carbon lever); Arvalis / Terres
    /// Inovia (30-100 kg N/ha restored to the following crop, ~50% less nitrate
    /// leaching). Practice change, no upfront capital.
    /// </para>
    /// </summary>
    public sealed class SowCoverCropsRecommendation : IRecommendation
    {
        /// <summary>Default coverage increase (percentage points) proposed.</summary>
        public const double CoverageRaisePerStep = 25.0;
        public const double MaxCoveragePercent = 100.0;

        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }

        public double InvestmentCostEurosPerHectare => 0.0;

        public SowCoverCropsRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "cover-crops#" + issuedOnDay,
                title: "Implanter des couverts d'interculture",
                rationale: FormatAutoRationale(CoverageRaisePerStep),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending)
        {
        }

        private SowCoverCropsRecommendation(string id, string title, string rationale,
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
            return "Implante des couverts d'interculture (+" + magnitude.ToString("0", FrFr)
                 + " % de couverture). Effet : stockage de carbone du sol "
                 + "(~0,3 tC/ha/an), azote restitué à la culture suivante, "
                 + "moins de lessivage. Déclenché par : carbone du sol mesuré bas "
                 + "(tour à flux).";
        }
    }
}
