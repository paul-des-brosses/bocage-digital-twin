using UnityEngine;

namespace Bocage.Presentation.Scene.Sensors
{
    /// <summary>
    /// Listens to <see cref="SensorHoverEventBus"/> and visually
    /// highlights the sensor GameObject this component is attached to
    /// when the event's <c>sensorId</c> matches the local
    /// <see cref="SensorMetadataTag.SensorId"/>. Highlight implementation
    /// is a transform scale bump (1.0 → <see cref="highlightScaleFactor"/>);
    /// it preserves any flipX convention from the original scale by
    /// scaling on the absolute factor and re-applying the sign.
    /// <para>
    /// Implementation note: we store the baseline scale at Awake (after
    /// <see cref="SensorVisualPlacer"/> has applied the placement scale
    /// at -9000 execution order, but before any hover event can fire),
    /// and restore it on exit. No tween: the scale change is instant
    /// to avoid drifting state if the user hovers in/out fast.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(SensorMetadataTag))]
    public sealed class SensorHoverHighlight : MonoBehaviour
    {
        [SerializeField, Tooltip("Multiplier applied to the baseline scale while the sensor is hovered. 1.0 = no visual change.")]
        private float highlightScaleFactor = 1.15f;

        private SensorMetadataTag _tag;
        private Vector3 _baselineScale;
        private bool _subscribed;

        private void Awake()
        {
            _tag = GetComponent<SensorMetadataTag>();
            _baselineScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (_subscribed) return;
            SensorHoverEventBus.SensorHoverEnter += HandleEnter;
            SensorHoverEventBus.SensorHoverExit += HandleExit;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            SensorHoverEventBus.SensorHoverEnter -= HandleEnter;
            SensorHoverEventBus.SensorHoverExit -= HandleExit;
            _subscribed = false;
        }

        private void HandleEnter(string sensorId)
        {
            if (_tag == null) return;
            if (sensorId != _tag.SensorId) return;
            transform.localScale = _baselineScale * highlightScaleFactor;
        }

        private void HandleExit(string sensorId)
        {
            if (_tag == null) return;
            if (sensorId != _tag.SensorId) return;
            transform.localScale = _baselineScale;
        }
    }
}
