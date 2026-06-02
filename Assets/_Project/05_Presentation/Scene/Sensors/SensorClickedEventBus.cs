using System;

namespace Bocage.Presentation.Scene.Sensors
{
    /// <summary>
    /// Static event bus for sensor click events (chantier E6 / ADR #53).
    /// Decouples the scene side (sprite click via <see cref="SensorClickHandler"/>
    /// on a Collider2D) from the UI side (the inspection panel binding
    /// that opens the matching modal). The event payload is the
    /// <see cref="SensorType"/> so the binding can switch on the enum to
    /// pick the right layout — no string parsing.
    /// <para>
    /// Per CLAUDE.md §6, an EventBus is appropriate for punctual events
    /// like a click. Mirrors <see cref="SensorHoverEventBus"/> in spirit
    /// and lifecycle: subscribers MUST unsubscribe on disable/destroy
    /// since the event is static and would otherwise keep dead listeners
    /// alive.
    /// </para>
    /// </summary>
    public static class SensorClickedEventBus
    {
        /// <summary>Raised when the user clicks a sensor sprite in the scene.</summary>
        public static event Action<SensorType> SensorClicked;

        /// <summary>Invokes the event with the sensor type. Safe to call when no subscriber is wired (no-op).</summary>
        public static void RaiseClicked(SensorType type)
        {
            SensorClicked?.Invoke(type);
        }
    }
}
