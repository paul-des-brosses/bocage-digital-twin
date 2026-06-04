namespace Bocage.Sensors.Events
{
    /// <summary>
    /// Signals that integrated farm profitability has fallen below the alert
    /// threshold (<see cref="Bocage.Sensors.EventDetector.ProfitLowThresholdEurosPerHectare"/>) —
    /// the farm is in real financial tension. Carries the biodiversity index
    /// measured at the same moment so the decision engine can refuse to trade
    /// away already-critical ecology for margin.
    /// <para>
    /// Unlike the other events this one is not a biophysical sensor reading but a
    /// threshold on the integrated-profitability indicator (Couche 04), passed
    /// into the detector by the runner. It exists to drive the ECONOMIC
    /// counter-recommendations (raise inputs toward the profit optimum, thin
    /// over-dense unsubsidised hedges) that keep the digital twin from being a
    /// one-sided ecological advisor.
    /// </para>
    /// </summary>
    public sealed class LowProfitabilityEvent : IEvent
    {
        public string Id => "low-profitability";
        public int DetectedOnDay { get; }
        public EventSeverity Severity => EventSeverity.Warning;
        public string Summary => "Rentabilité anormalement basse — la ferme est sous tension";

        public double ProfitAtDetectionEurosPerHectare { get; }
        public double BiodiversityAtDetection { get; }

        public LowProfitabilityEvent(int detectedOnDay, double profitEurosPerHectare, double biodiversity)
        {
            DetectedOnDay = detectedOnDay;
            ProfitAtDetectionEurosPerHectare = profitEurosPerHectare;
            BiodiversityAtDetection = biodiversity;
        }
    }
}
