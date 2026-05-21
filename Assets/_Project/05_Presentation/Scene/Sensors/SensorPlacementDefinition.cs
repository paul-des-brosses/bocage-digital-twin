using System.Collections.Generic;
using UnityEngine;

namespace Bocage.Presentation.Scene.Sensors
{
    /// <summary>
    /// Data-driven definition of the sensor visual placements in the
    /// scene. One asset describes the network of sensors deployed in
    /// the bocage: type, position, sprite, model variable observed,
    /// online status. Read by <see cref="SensorVisualPlacer"/> at boot
    /// to instantiate one SpriteRenderer per sensor under a configurable
    /// spawn root (typically <c>_Scene_Visual/Sensors</c>, sorting layer
    /// <c>Sensors</c> per DECISIONS.md #38).
    /// <para>
    /// Why a separate SO from <c>SceneCompositionDefinition</c>: the
    /// static landscape and the sensor network have different lifecycles
    /// (sensors will gain interactive behaviour at 6c.3 with hover sync,
    /// and they carry metadata that landscape sprites don't need:
    /// online status, observed variable, deferred-until tag). Separating
    /// keeps each SO focused and the inspector readable.
    /// </para>
    /// <para>
    /// Honest design (CLAUDE.md §9 sensor primacy): the
    /// <see cref="SensorPlacement.onlineStatus"/> field is the explicit
    /// contract. A sensor marked <c>Online</c> claims to feed a real
    /// variable of <c>EcosystemModel</c>; a sensor marked
    /// <c>Deferred</c> is a deployed visual placeholder waiting for its
    /// underlying state variable to land at the étape referenced in
    /// <see cref="SensorPlacement.deferredUntilStep"/>.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Scene/Sensor Placement Definition",
        fileName = "SensorPlacement_Default")]
    public sealed class SensorPlacementDefinition : ScriptableObject
    {
        [SerializeField] private SensorPlacement[] sensors = new SensorPlacement[0];

        public IReadOnlyList<SensorPlacement> Sensors => sensors;
    }

    /// <summary>
    /// Distinct categories of sensors we plan to deploy in the bocage.
    /// Each value maps to one sprite asset (5 visual identities total).
    /// Enum kept narrow on purpose: extending it is a deliberate act
    /// that should also extend the model with the variable that the new
    /// sensor measures.
    /// </summary>
    public enum SensorType
    {
        Piezometer,      // observes WaterTableDepth
        WeatherStation,  // observes CurrentWeather
        EddyTower,       // observes CO2/CH4 flux — not yet in model
        AcousticSensor,  // observes FaunaPopulation — Étape 8
        CameraTrap       // observes FaunaPopulation — Étape 8
    }

    /// <summary>
    /// Whether a sensor is currently feeding a real model variable
    /// (Online) or merely deployed visually while waiting for its
    /// underlying state variable to land (Deferred).
    /// </summary>
    public enum SensorOnlineStatus
    {
        Online,
        Deferred
    }

    /// <summary>
    /// One sensor placement. Authored in the inspector; immutable at
    /// runtime. Mirrors the shape of <c>ScenicElement</c> for the
    /// transform/render fields, plus sensor-specific metadata
    /// (type, online status, observed variable, deferred-until tag).
    /// </summary>
    [System.Serializable]
    public struct SensorPlacement
    {
        [Tooltip("Stable identifier. Used as GameObject name and in log lines. Convention: 'sensor_<type>_<index>'.")]
        public string id;

        [Tooltip("Human-readable display name used by tooltips and the minimap (e.g. 'Piezomètre amont').")]
        public string displayName;

        [Tooltip("Sensor category. Drives the default sprite (overridden if 'sprite' field is set) and the semantics of 'observedModelVariable'.")]
        public SensorType type;

        [Tooltip("Online status. 'Online' = feeds a real EcosystemModel variable. 'Deferred' = sprite shown but model variable missing until 'deferredUntilStep' arrives.")]
        public SensorOnlineStatus onlineStatus;

        [Tooltip("Name of the EcosystemModel field this sensor observes (e.g. 'WaterTableDepth'). Free text — used for tooltips. Empty when status is Deferred.")]
        public string observedModelVariable;

        [Tooltip("If status is Deferred, free text indicating which roadmap étape will bring the underlying variable online (e.g. 'Étape 8').")]
        public string deferredUntilStep;

        [Tooltip("Sprite asset to render. If null the placement is skipped.")]
        public Sprite sprite;

        [Tooltip("World-space position in scene units. Z is forced to 0.")]
        public Vector2 worldPosition;

        [Tooltip("Non-uniform scale (X horizontal stretch, Y vertical stretch). 0 or negative clamps to 1.")]
        public Vector2 scale;

        [Tooltip("Sorting layer name. Default expected: 'Sensors' (DECISIONS.md #38).")]
        public string sortingLayerName;

        [Tooltip("Order within the sorting layer (back to front).")]
        public int sortingOrderInLayer;

        [Tooltip("Mirror the sprite horizontally (useful for symmetric variants).")]
        public bool flipX;
    }
}
