namespace Bocage.Sensors.Events
{
    /// <summary>
    /// Signals that the composite fauna abundance has dropped below the
    /// half-baseline threshold, which corresponds in real-world
    /// monitoring to a clear acoustic anomaly on bird / orthoptera
    /// passive recorders. Detected when <c>FaunaPopulation</c> falls
    /// under <see cref="EventDetector.FaunaAcousticAnomalyThreshold"/>.
    /// <para>
    /// Sources: Vigie-Nature acoustic protocols flag a "low signature"
    /// at ~−50 % of the reference biodiversity baseline; Hallmann
    /// et al. 2017 (Krefeld, −75 % insect biomass) and MNHN 2024
    /// (−70-80 % European agro-industrial farmland) confirm the
    /// order of magnitude.
    /// </para>
    /// </summary>
    public sealed class FaunaAcousticAnomalyEvent : IEvent
    {
        public string Id => "fauna-acoustic-anomaly";
        public int DetectedOnDay { get; }
        public EventSeverity Severity => EventSeverity.Warning;
        public string Summary => "Anomalie acoustique faune — abondance < 0,5 × référence";

        public double FaunaPopulationAtDetection { get; }

        public FaunaAcousticAnomalyEvent(int detectedOnDay, double faunaPopulation)
        {
            DetectedOnDay = detectedOnDay;
            FaunaPopulationAtDetection = faunaPopulation;
        }
    }
}
