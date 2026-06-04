using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Ecological recommendation: leave more crop residues on the field (vs.
    /// exporting them as straw) to rebuild soil carbon. Raises
    /// <c>ScenarioContext.ResidueRestitutionPercent</c>, which drives the residue
    /// input term of <c>SoilCarbonDynamicsRule</c>. Triggered, like cover crops,
    /// by the eddy-flux tower measuring a low / declining soil carbon stock; it is
    /// the lighter-touch alternative when cover-crop coverage is already high.
    /// <para>
    /// Source: Solagro Afterres2050 (residue restitution ~0.8 tC/ha/yr at 100%);
    /// INRAE / CIRAD (organic-matter return feeds soil macrofauna — the model's
    /// "sol vivant" fauna bonus above 80 tC/ha). Practice change, no upfront
    /// capital (foregone straw sale only).
    /// </para>
    /// </summary>
    public sealed class RestoreResidueRecommendation : IRecommendation
    {
        /// <summary>Default restitution increase (percentage points) proposed.</summary>
        public const double RestitutionRaisePerStep = 25.0;
        public const double MaxRestitutionPercent = 100.0;

        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }

        public double InvestmentCostEurosPerHectare => 0.0;

        public RestoreResidueRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "restore-residue#" + issuedOnDay,
                title: "Restituer les résidus de culture",
                rationale: FormatAutoRationale(RestitutionRaisePerStep),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending)
        {
        }

        private RestoreResidueRecommendation(string id, string title, string rationale,
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
            return "Restitue davantage de résidus de culture au sol (+"
                 + magnitude.ToString("0", FrFr) + " %). Effet : carbone et "
                 + "fertilité du sol en hausse, nourrit la macrofaune. "
                 + "Déclenché par : carbone du sol mesuré bas (tour à flux).";
        }
    }
}
