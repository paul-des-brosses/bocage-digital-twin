namespace Bocage.Sensors.Events
{
    /// <summary>
    /// Marker for any event emitted by <see cref="EventDetector"/> on
    /// behalf of the sensor layer (Couche 2). Concrete events expose
    /// their detection day, severity and a human-readable summary; the
    /// recommendation engine in Couche 3 dispatches on the runtime type
    /// (e.g. <c>DroughtProlongedEvent</c>) to produce a fitting action.
    /// <para>
    /// Per CLAUDE.md §9 (primauté du capteur), every event is derived
    /// from a measurable state of <see cref="Bocage.SimulationCore.Model.EcosystemModel"/> —
    /// no calendar trigger, no scripted appearance.
    /// </para>
    /// </summary>
    public interface IEvent
    {
        /// <summary>Stable identifier for telemetry and journalling.</summary>
        string Id { get; }

        /// <summary>The simulated day on which the event was detected.</summary>
        int DetectedOnDay { get; }

        /// <summary>How urgent the event is in the UI hierarchy.</summary>
        EventSeverity Severity { get; }

        /// <summary>One-line human-readable description.</summary>
        string Summary { get; }
    }
}
