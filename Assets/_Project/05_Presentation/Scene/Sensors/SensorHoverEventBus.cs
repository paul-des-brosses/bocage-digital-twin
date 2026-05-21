using System;

namespace Bocage.Presentation.Scene.Sensors
{
    /// <summary>
    /// Static event bus for sensor hover events. Decouples the scene
    /// side (sprite hover via Collider2D + OnMouseEnter) from the UI
    /// side (list row hover via PointerEnter/PointerExit). Either side
    /// raises an event keyed by <c>sensorId</c>; the matching listener
    /// on the other side toggles its visual highlight.
    /// <para>
    /// Per CLAUDE.md §6, an EventBus is appropriate for punctual events
    /// (a hover, a button press) — not for persistent state. Here we
    /// raise on enter/exit only; the highlight state is local to each
    /// subscriber and recomputed from event reception.
    /// </para>
    /// <para>
    /// Subscribers MUST unsubscribe on disable/destroy: events are
    /// static and would otherwise keep dead listeners alive. The
    /// included subscribers <c>SensorHoverHighlight</c> and
    /// <c>SensorListBinding</c> handle this in OnDestroy / OnDisable.
    /// </para>
    /// </summary>
    public static class SensorHoverEventBus
    {
        /// <summary>Raised when the pointer enters the visual representation of a sensor (scene sprite or list row).</summary>
        public static event Action<string> SensorHoverEnter;

        /// <summary>Raised when the pointer leaves the visual representation of a sensor.</summary>
        public static event Action<string> SensorHoverExit;

        public static void RaiseEnter(string sensorId)
        {
            if (string.IsNullOrEmpty(sensorId)) return;
            SensorHoverEnter?.Invoke(sensorId);
        }

        public static void RaiseExit(string sensorId)
        {
            if (string.IsNullOrEmpty(sensorId)) return;
            SensorHoverExit?.Invoke(sensorId);
        }
    }
}
