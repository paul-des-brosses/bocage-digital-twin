using UnityEngine;

namespace Bocage.Presentation.Scene.Sensors
{
    /// <summary>
    /// Read-only tag component attached to each spawned sensor GameObject
    /// by <see cref="SensorVisualPlacer"/>. Mirrors the
    /// <see cref="SensorPlacement"/> data so downstream bindings
    /// (minimap dots, hover tooltip — 6c.2/6c.3) can read the metadata
    /// without having to query the source SO.
    /// <para>
    /// The fields are populated once at spawn via
    /// <see cref="Initialize"/> and never mutated after.
    /// </para>
    /// </summary>
    public sealed class SensorMetadataTag : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string sensorId;
        [SerializeField, HideInInspector] private string displayName;
        [SerializeField, HideInInspector] private SensorType type;
        [SerializeField, HideInInspector] private SensorOnlineStatus onlineStatus;
        [SerializeField, HideInInspector] private string observedModelVariable;
        [SerializeField, HideInInspector] private string deferredUntilStep;

        public string SensorId => sensorId;
        public string DisplayName => displayName;
        public SensorType Type => type;
        public SensorOnlineStatus OnlineStatus => onlineStatus;
        public string ObservedModelVariable => observedModelVariable;
        public string DeferredUntilStep => deferredUntilStep;

        internal void Initialize(SensorPlacement source)
        {
            sensorId = source.id;
            displayName = source.displayName;
            type = source.type;
            onlineStatus = source.onlineStatus;
            observedModelVariable = source.observedModelVariable;
            deferredUntilStep = source.deferredUntilStep;
        }
    }
}
