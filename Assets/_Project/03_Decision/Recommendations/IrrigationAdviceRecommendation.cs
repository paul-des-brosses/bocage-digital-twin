using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests deploying targeted irrigation / soil-cover practices in
    /// response to a prolonged drought event. ADR #55 pattern uniforme :
    /// Title court, rationale d'action concrète, ligne « Effet
    /// modélisé : ... », et pour la voie auto une ligne supplémentaire
    /// « Déclenché par : ... ». Two wordings coexist on this class :
    /// auto recommendation issued by <c>RecommendationEngine</c> on a
    /// drought event, and manual button click (ADR #47 pathway).
    /// </summary>
    public sealed class IrrigationAdviceRecommendation : IRecommendation
    {
        public const double WaterReliefDepthMeters = 1.5;
        public const double WaterTableFloorMeters = 0.5;
        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }
        /// <summary>
        /// ADR #50: irrigation is a recurring expense whose cost is
        /// folded into <c>InputCost</c>; no upfront capital. Always 0.
        /// </summary>
        public double InvestmentCostEurosPerHectare => 0.0;

        public IrrigationAdviceRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "irrigation-advice#" + issuedOnDay,
                title: "Irrigation ciblée + couvert anti-évaporation",
                rationale: FormatAutoRationale(WaterReliefDepthMeters),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending)
        {
        }

        private IrrigationAdviceRecommendation(string id, string title, string rationale,
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
        /// Manual-pathway factory (ADR #47). Used when the user clicks
        /// « Irrigation ponctuelle » with a chosen depth magnitude.
        /// </summary>
        public static IrrigationAdviceRecommendation Manual(int day, int sequence, double magnitude)
        {
            return new IrrigationAdviceRecommendation(
                id: "manual-irrigation#" + day + "-" + sequence,
                title: "Irrigation ponctuelle",
                rationale: FormatManualRationale(magnitude),
                issuedOnDay: day,
                triggeredByEventId: null,
                defaultVerdict: DecisionVerdict.AutoAccepted);
        }

        /// <summary>
        /// ADR #55 auto wording : action concrète + Effet modélisé +
        /// ligne Déclenché par (anchor capteur).
        /// </summary>
        public static string FormatAutoRationale(double magnitude)
        {
            return "Apport d'eau ciblé + couverts anti-évaporation sur 30 jours. "
                 + "Effet modélisé : remontée temporaire de la nappe phréatique de "
                 + magnitude.ToString("F2", FrFr) + " m (plancher " + WaterTableFloorMeters.ToString("F1", FrFr) + " m). "
                 + "Déclenché par : Sécheresse prolongée détectée par le piézomètre.";
        }

        /// <summary>
        /// ADR #55 manual wording : action concrète + Effet modélisé,
        /// sans ligne Déclenché par (l'utilisateur est l'initiateur).
        /// </summary>
        public static string FormatManualRationale(double magnitude)
        {
            return "Apport d'eau ciblé sur 30 jours. "
                 + "Effet modélisé : remontée temporaire de la nappe phréatique de "
                 + magnitude.ToString("F2", FrFr) + " m (plancher " + WaterTableFloorMeters.ToString("F1", FrFr) + " m).";
        }
    }
}
