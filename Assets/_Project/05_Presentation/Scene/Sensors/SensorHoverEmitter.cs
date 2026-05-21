using UnityEngine;

namespace Bocage.Presentation.Scene.Sensors
{
    /// <summary>
    /// Raises <see cref="SensorHoverEventBus.SensorHoverEnter"/> and
    /// <see cref="SensorHoverEventBus.SensorHoverExit"/> when the mouse
    /// enters or leaves the GameObject this component is attached to.
    /// Relies on Unity's legacy <c>OnMouseEnter</c>/<c>OnMouseExit</c>
    /// callbacks, which fire automatically when the GameObject has a
    /// <see cref="Collider2D"/> and the cursor crosses it.
    /// <para>
    /// Requires a <see cref="SensorMetadataTag"/> sibling component for
    /// the sensor id and a <see cref="Collider2D"/> (any subclass) so
    /// the mouse messages are dispatched. <see cref="SensorVisualPlacer"/>
    /// attaches both at spawn time.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(SensorMetadataTag))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SensorHoverEmitter : MonoBehaviour
    {
        private SensorMetadataTag _tag;

        private void Awake()
        {
            _tag = GetComponent<SensorMetadataTag>();
        }

        // Unity calls OnMouseEnter on a GameObject when its collider is
        // first hit by the cursor (legacy GUI messaging — works in 2D
        // with Collider2D, no raycaster required).
        private void OnMouseEnter()
        {
            if (_tag != null) SensorHoverEventBus.RaiseEnter(_tag.SensorId);
        }

        private void OnMouseExit()
        {
            if (_tag != null) SensorHoverEventBus.RaiseExit(_tag.SensorId);
        }
    }
}
