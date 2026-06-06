namespace Bocage.Sensors.Events
{
    /// <summary>
    /// Signals that the composite fauna abundance has dropped below the
    /// alert threshold (<see cref="EventDetector.FaunaAcousticAnomalyThreshold"/>
    /// = 0.7 × baseline, i.e. −30 %), which corresponds in real-world monitoring
    /// to a clear acoustic anomaly on bird / orthoptera passive recorders.
    /// Detected when the SENSOR-measured <c>FaunaPopulation</c> falls under that
    /// threshold.
    /// <para>
    /// The −30 % alert is an EARLY warning: Vigie-Nature flags farmland-bird
    /// decline from the −30 % range, while the deeper losses in the literature —
    /// Hallmann et al. 2017 (Krefeld, −75 % insect biomass) and MNHN 2024
    /// (−70-80 % European agro-industrial farmland) — show how far it runs in
    /// intensified zones.
    /// </para>
    /// </summary>
    public sealed class FaunaAcousticAnomalyEvent : IEvent
    {
        public string Id => "fauna-acoustic-anomaly";
        public int DetectedOnDay { get; }
        public EventSeverity Severity => EventSeverity.Warning;
        public string Summary => "Anomalie acoustique faune — abondance < 0,7 × référence";

        public double FaunaPopulationAtDetection { get; }

        public FaunaAcousticAnomalyEvent(int detectedOnDay, double faunaPopulation)
        {
            DetectedOnDay = detectedOnDay;
            FaunaPopulationAtDetection = faunaPopulation;
        }
    }
}
