namespace Bocage.Sensors.Events
{
    /// <summary>
    /// Signals that the soil carbon stock has fallen below the alert threshold,
    /// flagged by the eddy-flux tower monitoring the field's carbon balance.
    /// Detected when the stock drops under
    /// <see cref="Bocage.Sensors.EventDetector.SoilCarbonLowThresholdTonnesPerHectare"/>
    /// — the sensor signal that, until chantier E9, drove no decision at all.
    /// <para>
    /// Sources: INRAE 4 pour 1000 (cultivated soils lose carbon under low organic
    /// inputs); BDAT INRAE reference stocks. A low / declining stock is the cue to
    /// rebuild it with cover crops or residue restitution.
    /// </para>
    /// </summary>
    public sealed class SoilCarbonLowEvent : IEvent
    {
        public string Id => "soil-carbon-low";
        public int DetectedOnDay { get; }
        public EventSeverity Severity => EventSeverity.Warning;
        public string Summary => "Carbone du sol bas — stock sous le seuil d'alerte (tour à flux)";

        public double SoilCarbonAtDetection { get; }

        public SoilCarbonLowEvent(int detectedOnDay, double soilCarbon)
        {
            DetectedOnDay = detectedOnDay;
            SoilCarbonAtDetection = soilCarbon;
        }
    }
}
