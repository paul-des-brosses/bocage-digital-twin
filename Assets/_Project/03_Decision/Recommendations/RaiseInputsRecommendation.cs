using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Economic (balance) recommendation: raise input intensity back toward the
    /// profit optimum when the farm has over-extensified and margin suffers.
    /// <para>
    /// The recalibration creates an interior profit optimum (concave yield
    /// response): below it, the marginal yield gain of more inputs outweighs
    /// their marginal cost, so nudging intensity up recovers profit. The engine
    /// no longer hardcodes that optimum — it PROJECTS the raise forward and only
    /// recommends it when profit actually gains, so the optimum emerges from the
    /// model. This still TRADES some biodiversity for margin (more inputs press
    /// on fauna) — a value-laden arbitrage, so the dispatch surfaces it passively
    /// (decision list, never an interrupting popup) and only when profitability
    /// is abnormally low. The system does not push ecology-for-money unless the
    /// farm needs it.
    /// </para>
    /// <para>Source: Lechenet et al. 2017 (Nature Plants 3:17008) and the concave
    /// N-response (CALIBRATION.md). Counterpart of
    /// <see cref="ReduceInputsRecommendation"/> on the same lever.</para>
    /// </summary>
    public sealed class RaiseInputsRecommendation : IRecommendation
    {
        /// <summary>Default intensity increase proposed by the recommendation.</summary>
        public const double IntensityRaisePerStep = 0.2;

        /// <summary>
        /// Intensive cap of the input-intensity factor (the « intensive » end of
        /// the ScenarioContext range, mirror of
        /// <see cref="ReduceInputsRecommendation.MinInputIntensityFactor"/>). The
        /// auto-action clamp will not raise intensity past it. The profit optimum
        /// itself is NO LONGER hardcoded — it emerges from the forward projection
        /// in <see cref="Bocage.Decision.RecommendationEngine"/> (above the optimum,
        /// raising inputs projects a loss and is gated out), so this is only a
        /// physical bound, not the decision threshold.
        /// </summary>
        public const double MaxInputIntensityFactor = 2.0;

        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }

        /// <summary>Practice change, no upfront capital. Always 0.</summary>
        public double InvestmentCostEurosPerHectare => 0.0;

        public RaiseInputsRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "raise-inputs#" + issuedOnDay,
                title: "Remonter l'intensité d'intrants",
                rationale: FormatAutoRationale(IntensityRaisePerStep),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending)
        {
        }

        private RaiseInputsRecommendation(string id, string title, string rationale,
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
            return "Remonte l'intensité d'intrants de " + magnitude.ToString("0.0", FrFr)
                 + " vers l'optimum de marge. Sous l'optimum (rendement concave), "
                 + "le gain de rendement dépasse le coût des intrants. "
                 + "Compromis : pression accrue sur la faune. "
                 + "Déclenché par : rentabilité mesurée anormalement basse.";
        }
    }
}
