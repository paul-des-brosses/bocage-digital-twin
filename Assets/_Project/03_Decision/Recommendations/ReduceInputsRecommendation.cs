namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests reducing fertiliser/pesticide intensity in response to
    /// an acoustic fauna anomaly (passive recorders flagging a low
    /// signature). The pressure-release recommendation aligns with
    /// Hallmann et al. 2017 / MNHN 2024 and Vigie-Nature evidence
    /// that pesticide reduction is the fastest lever to recover
    /// farmland insect abundance.
    /// <para>
    /// Mechanical effect when accepted (sub-étape 8c.3 AutoAction):
    /// reduces the scenario's <c>InputIntensityFactor</c> by
    /// <see cref="IntensityCutPerStep"/> via a 30-day
    /// <c>TransitioningParameter.SetTarget</c>. Cap at
    /// <see cref="IntensityFloor"/> so the action is bounded.
    /// </para>
    /// </summary>
    public sealed class ReduceInputsRecommendation : IRecommendation
    {
        public const double IntensityCutPerStep = 0.2;
        public const double IntensityFloor = 0.5;

        public string Id { get; }
        public string Title => "Baisser l'intensité d'intrants";
        public string Rationale => "Anomalie acoustique faune — baisser intrants de 0,2 unité pour relâcher la pression chimique sur 30 jours.";
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict => DecisionVerdict.Pending;

        public ReduceInputsRecommendation(int issuedOnDay, string triggeredByEventId)
        {
            Id = "reduce-inputs#" + issuedOnDay;
            IssuedOnDay = issuedOnDay;
            TriggeredByEventId = triggeredByEventId;
        }
    }
}
