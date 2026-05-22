namespace Bocage.Sensors.Events
{
    /// <summary>
    /// Signals that the hedge network has degraded past a threshold
    /// compatible with the symptoms of <i>Hymenoscyphus fraxineus</i>
    /// (chalara dieback of ash trees, which dominates Perche bocage
    /// hedges). Detected when <c>HedgerowDensity</c> drops below
    /// <see cref="EventDetector.HedgeAlertThresholdMetersPerHectare"/>.
    /// <para>
    /// The trigger is calibrated against INRAE chalara monitoring:
    /// ~30 % loss of ash cover (i.e. 90 → ≈60 m/ha on a baseline
    /// hedge-rich bocage) is the level at which farmers and the PNR
    /// du Perche typically receive treatment recommendations.
    /// </para>
    /// </summary>
    public sealed class HedgeChalaraEvent : IEvent
    {
        public string Id => "hedge-chalara";
        public int DetectedOnDay { get; }
        public EventSeverity Severity => EventSeverity.Warning;
        public string Summary => "Dépérissement haie compatible chalara fraxinea";

        public double HedgerowDensityAtDetection { get; }

        public HedgeChalaraEvent(int detectedOnDay, double hedgerowDensityMetersPerHectare)
        {
            DetectedOnDay = detectedOnDay;
            HedgerowDensityAtDetection = hedgerowDensityMetersPerHectare;
        }
    }
}
