using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests reducing fertiliser/pesticide intensity in response to
    /// an acoustic fauna anomaly (auto) or a user click on « Baisser
    /// intrants » (manual). ADR #55 pattern uniforme : Title court,
    /// rationale d'action concrète, ligne « Effet modélisé : ... »
    /// chiffrée. Two wordings coexist on this class : voie auto avec
    /// ligne « Déclenché par : ... », voie manuelle sans.
    /// <para>
    /// Sources : Hallmann et al. 2017 / MNHN 2024 / Vigie-Nature —
    /// pesticide reduction is the fastest lever to recover farmland
    /// insect abundance. The mechanical scaling per intensity unit
    /// (+0.05 fauna index, −200 €/ha/an input cost) is calibrated in
    /// <see cref="Bocage.Decision.AutoActionPipeline.ApplyOne"/>.
    /// </para>
    /// </summary>
    public sealed class ReduceInputsRecommendation : IRecommendation
    {
        public const double IntensityCutPerStep = 0.2;
        public const double IntensityFloor = 0.5;
        private const double FaunaBoostPerCutUnit = 0.05;
        private const double InputCostReductionPerCutUnit = 200.0;
        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }
        /// <summary>
        /// ADR #50: reducing input intensity is a recurring policy
        /// change whose savings flow through <c>InputCost</c>; no
        /// upfront capital. Always 0.
        /// </summary>
        public double InvestmentCostEurosPerHectare => 0.0;

        public ReduceInputsRecommendation(int issuedOnDay, string triggeredByEventId)
            : this(
                id: "reduce-inputs#" + issuedOnDay,
                title: "Baisser l'intensité d'intrants",
                rationale: FormatAutoRationale(IntensityCutPerStep),
                issuedOnDay: issuedOnDay,
                triggeredByEventId: triggeredByEventId,
                defaultVerdict: DecisionVerdict.Pending)
        {
        }

        private ReduceInputsRecommendation(string id, string title, string rationale,
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
        /// « Baisser intrants » with a chosen intensity-cut magnitude.
        /// </summary>
        public static ReduceInputsRecommendation Manual(int day, int sequence, double magnitude)
        {
            return new ReduceInputsRecommendation(
                id: "manual-reduce-inputs#" + day + "-" + sequence,
                title: "Baisser l'intensité d'intrants",
                rationale: FormatManualRationale(magnitude),
                issuedOnDay: day,
                triggeredByEventId: null,
                defaultVerdict: DecisionVerdict.AutoAccepted);
        }

        /// <summary>
        /// ADR #55 auto wording, with « Déclenché par : ... » trailer
        /// pointing to the acoustic sensor that fired the event.
        /// </summary>
        public static string FormatAutoRationale(double magnitude)
        {
            return "Réduction des intrants chimiques sur 30 jours. "
                 + "Effet modélisé : " + FormatEffectClause(magnitude) + ". "
                 + "Déclenché par : Anomalie acoustique faune détectée par le capteur acoustique.";
        }

        /// <summary>
        /// ADR #55 manual wording, sans « Déclenché par : ... ».
        /// </summary>
        public static string FormatManualRationale(double magnitude)
        {
            return "Réduction des intrants chimiques sur 30 jours. "
                 + "Effet modélisé : " + FormatEffectClause(magnitude) + ".";
        }

        private static string FormatEffectClause(double magnitude)
        {
            double ratio = magnitude / IntensityCutPerStep;
            if (ratio < 0) ratio = 0;
            double faunaBoost = FaunaBoostPerCutUnit * ratio;
            double inputCostDrop = InputCostReductionPerCutUnit * ratio;
            return "+" + faunaBoost.ToString("F2", FrFr) + " de population faune, −"
                 + inputCostDrop.ToString("F0", FrFr) + " €/ha de coût d'intrants";
        }
    }
}
