using System.Globalization;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests lowering the farm's input intensity (fertiliser / pesticide)
    /// in response to an acoustic fauna anomaly. ADR #55 pattern: short title,
    /// concrete action rationale, « Déclenché par : ... » trailer pointing to
    /// the sensor that fired the event.
    /// <para>
    /// Unlike a one-off action, this is a sustained PRACTICE change: accepting
    /// it lowers the real run's input-intensity decision (the slider) by the
    /// chosen magnitude, and the effect — lower input cost, recovering fauna,
    /// adjusted yield — then flows through the biophysical rules over the
    /// following weeks. The shadow run keeps its frozen baseline intensity, so
    /// the resulting gap is exactly what the tech-value KPI measures. Applied
    /// in <see cref="Bocage.Decision.AutoActionPipeline.ApplyOne"/>.
    /// </para>
    /// <para>
    /// Source: Hallmann et al. 2017 / MNHN 2024 / Vigie-Nature — pesticide
    /// reduction is the fastest lever to recover farmland insect abundance.
    /// </para>
    /// </summary>
    public sealed class ReduceInputsRecommendation : IRecommendation
    {
        /// <summary>Default intensity reduction proposed by the recommendation.</summary>
        public const double IntensityCutPerStep = 0.2;

        /// <summary>
        /// Organic-extensive floor of the input-intensity factor: the lowest
        /// value the slider, the auto-action clamp and the recommendation
        /// coherence guard all respect (0.5 = bio extensif, -50% inputs vs
        /// conventional). Single source of truth for that floor — kept in sync
        /// with the « input-intensity-slider » low-value in Dashboard.uxml
        /// (UXML cannot reference a C# const). Below it the yield/cost/fauna
        /// response curves are no longer calibrated, and a productive farm
        /// still uses some inputs, so 0 is not a meaningful setpoint.
        /// </summary>
        public const double MinInputIntensityFactor = 0.5;
        private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

        public string Id { get; }
        public string Title { get; }
        public string Rationale { get; }
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict { get; }

        /// <summary>
        /// A practice change carries no upfront capital — the savings flow
        /// through <c>InputCost</c>. Always 0.
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
        /// ADR #55 auto wording, with « Déclenché par : ... » trailer pointing
        /// to the acoustic sensor that fired the event.
        /// </summary>
        public static string FormatAutoRationale(double magnitude)
        {
            return "Baisse l'intensité d'intrants de " + magnitude.ToString("0.0", FrFr)
                 + " (vers des pratiques plus extensives). "
                 + "Effet : coût des intrants en baisse et faune qui se rétablit sur les "
                 + "semaines suivantes, via le modèle. "
                 + "Déclenché par : Anomalie acoustique faune détectée par le capteur acoustique.";
        }
    }
}
