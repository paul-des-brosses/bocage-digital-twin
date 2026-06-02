using UnityEngine;

namespace Bocage.Presentation.Scene.Sensors
{
    /// <summary>
    /// Raises <see cref="SensorClickedEventBus.SensorClicked"/> when the
    /// user clicks the sensor sprite this component is attached to. Uses
    /// Unity's legacy <c>OnMouseDown</c> callback, which fires when the
    /// cursor presses a GameObject carrying a <see cref="Collider2D"/> —
    /// no <c>Physics2DRaycaster</c> needed (same path
    /// <see cref="SensorHoverEmitter"/> uses for hover, proven for months).
    /// <para>
    /// Requires a <see cref="SensorMetadataTag"/> sibling (for the
    /// <see cref="SensorType"/>) and a <see cref="Collider2D"/> (for the
    /// click hit-test). <see cref="SensorVisualPlacer"/> attaches both at
    /// spawn time, alongside the existing hover infrastructure.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(SensorMetadataTag))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class SensorClickHandler : MonoBehaviour
    {
        private SensorMetadataTag _tag;

        private void Awake()
        {
            _tag = GetComponent<SensorMetadataTag>();
        }

        // Legacy GUI messaging — fires automatically when the mouse
        // presses the collider this is attached to. Same call path as
        // OnMouseEnter/OnMouseExit used by SensorHoverEmitter.
        private void OnMouseDown()
        {
            if (_tag != null) SensorClickedEventBus.RaiseClicked(_tag.Type);
        }
    }
}
