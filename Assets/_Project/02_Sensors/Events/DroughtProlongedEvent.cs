namespace Bocage.Sensors.Events
{
    /// <summary>
    /// Signals that the water table has been below the wetland-habitat
    /// critical depth for an extended stretch, i.e. a prolonged drought
    /// affecting fauna, hedges and crop yield in the medium term.
    /// Detected when <c>WaterTableDepth</c> stays above
    /// <see cref="EventDetector.DroughtDepthThresholdMeters"/> for at
    /// least <see cref="EventDetector.DroughtConsecutiveDaysThreshold"/>
    /// consecutive simulated days.
    /// <para>
    /// Sources: OFB / RMT Zones humides flag 5 m + 30 days as the
    /// threshold for amphibian mortality and lasting agricultural
    /// stress in Perche-like bocage.
    /// </para>
    /// </summary>
    public sealed class DroughtProlongedEvent : IEvent
    {
        public string Id => "drought-prolonged";
        public int DetectedOnDay { get; }
        public EventSeverity Severity => EventSeverity.Critical;
        public string Summary => "Sécheresse prolongée — nappe profonde > 30 jours";

        public double WaterTableDepthAtDetection { get; }
        public int ConsecutiveDryDays { get; }

        public DroughtProlongedEvent(int detectedOnDay, double waterTableDepthMeters, int consecutiveDryDays)
        {
            DetectedOnDay = detectedOnDay;
            WaterTableDepthAtDetection = waterTableDepthMeters;
            ConsecutiveDryDays = consecutiveDryDays;
        }
    }
}
