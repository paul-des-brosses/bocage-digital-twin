namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Suggests deploying targeted irrigation / soil-cover practices in
    /// response to a prolonged drought event. In the simplified bocage
    /// model the action's mechanical effect is a one-off injection on
    /// the water table (mulching + soil cover reduce evaporation;
    /// targeted irrigation supplies the missing rainfall).
    /// <para>
    /// Source: Chambre d'agriculture Normandie drought protocol, RMT
    /// Sols et Territoires recommendations on cover-cropping under
    /// water stress.
    /// </para>
    /// <para>
    /// Mechanical effect when accepted (sub-étape 8c.3 AutoAction):
    /// reduces <c>WaterTableDepth</c> by
    /// <see cref="WaterReliefDepthMeters"/> over a 30-day window.
    /// </para>
    /// </summary>
    public sealed class IrrigationAdviceRecommendation : IRecommendation
    {
        public const double WaterReliefDepthMeters = 1.5;

        public string Id { get; }
        public string Title => "Irrigation ciblée + couvert anti-évaporation";
        public string Rationale => "Sécheresse prolongée — apport eau et couverts pour relâcher la pression hydrique sur 30 jours.";
        public int IssuedOnDay { get; }
        public string TriggeredByEventId { get; }
        public DecisionVerdict DefaultVerdict => DecisionVerdict.Pending;

        public IrrigationAdviceRecommendation(int issuedOnDay, string triggeredByEventId)
        {
            Id = "irrigation-advice#" + issuedOnDay;
            IssuedOnDay = issuedOnDay;
            TriggeredByEventId = triggeredByEventId;
        }
    }
}
