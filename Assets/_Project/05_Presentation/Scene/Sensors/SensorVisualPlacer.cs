using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Scene.Sensors
{
    /// <summary>
    /// Instantiates the sensor visual placements from a
    /// <see cref="SensorPlacementDefinition"/> at Awake. One
    /// SpriteRenderer GameObject is created per placement under
    /// <see cref="spawnRoot"/> (or this transform if not set).
    /// Idempotent: clears previously-spawned children before rebuilding,
    /// so the component is safe to re-run (editor button at 6c.2 / 6c.3).
    /// <para>
    /// Mirrors <c>SceneAssembler</c> for the architectural pattern:
    /// data-driven spawn at Awake, no runtime mutation of children,
    /// no per-frame allocation. The sensor metadata (online status,
    /// observed variable, deferred-until tag) is preserved on the
    /// spawned GameObject via a <see cref="SensorMetadataTag"/>
    /// component so that the hover/minimap bindings (6c.3) can read it
    /// without re-resolving the SO.
    /// </para>
    /// <para>
    /// Execution order -9000, identical to SceneAssembler. Sensors and
    /// landscape spawn in parallel; subsequent bindings run at the
    /// default order and find both children populated.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class SensorVisualPlacer : MonoBehaviour
    {
        [SerializeField] private SensorPlacementDefinition placement;

        [SerializeField, Tooltip("Parent transform that receives spawned sensor sprites. Falls back to this transform if null. Typically '_Scene_Visual/Sensors'.")]
        private Transform spawnRoot;

        private void Awake()
        {
            if (placement == null)
            {
                SimLogger.DebugLog("[SensorVisualPlacer] no placement assigned, skipping");
                return;
            }

            var parent = spawnRoot != null ? spawnRoot : transform;
            ClearChildren(parent);
            int spawned = BuildFrom(placement, parent);
            SimLogger.DebugLog("[SensorVisualPlacer] placed " + spawned + " sensors from " + placement.name);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only rebuild used by the custom inspector's "Rebuild
        /// Now" button. Lets the artist iterate on the placement asset
        /// without entering Play Mode.
        /// </summary>
        public void RebuildInEditor()
        {
            if (placement == null)
            {
                SimLogger.DebugLog("[SensorVisualPlacer] no placement assigned, skipping editor rebuild");
                return;
            }

            var parent = spawnRoot != null ? spawnRoot : transform;
            ClearChildren(parent);
            int spawned = BuildFrom(placement, parent);
            SimLogger.DebugLog("[SensorVisualPlacer] (editor) rebuilt " + spawned + " sensors from " + placement.name);
        }
#endif

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static int BuildFrom(SensorPlacementDefinition def, Transform parent)
        {
            int count = 0;
            var sensors = def.Sensors;
            for (int i = 0; i < sensors.Count; i++)
            {
                var s = sensors[i];
                if (s.sprite == null)
                {
                    SimLogger.DebugLog("[SensorVisualPlacer] sensor " + i + " (" + s.id + ") has no sprite, skipping");
                    continue;
                }

                string goName = string.IsNullOrEmpty(s.id) ? ("Sensor_" + i) : s.id;
                var go = new GameObject(goName);
                go.transform.SetParent(parent, worldPositionStays: false);
                go.transform.localPosition = new Vector3(s.worldPosition.x, s.worldPosition.y, 0f);

                float scaleX = s.scale.x <= 0f ? 1f : s.scale.x;
                float scaleY = s.scale.y <= 0f ? 1f : s.scale.y;
                go.transform.localScale = new Vector3(s.flipX ? -scaleX : scaleX, scaleY, 1f);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = s.sprite;
                if (!string.IsNullOrEmpty(s.sortingLayerName))
                {
                    renderer.sortingLayerName = s.sortingLayerName;
                }
                renderer.sortingOrder = s.sortingOrderInLayer;

                // Attach metadata so the hover/minimap bindings can read it
                // without re-resolving the SO at runtime.
                var tag = go.AddComponent<SensorMetadataTag>();
                tag.Initialize(s);

                // Hover infrastructure (sub-étape 6c.3): a Collider2D sized
                // to the sprite gives Unity's legacy OnMouseEnter/Exit a
                // hit target, an emitter raises events on the static bus,
                // and a highlight component listens and scales the local
                // transform.
                var collider = go.AddComponent<BoxCollider2D>();
                if (renderer.sprite != null)
                {
                    collider.size = renderer.sprite.bounds.size;
                }
                go.AddComponent<SensorHoverEmitter>();
                go.AddComponent<SensorHoverHighlight>();

                count++;
            }
            return count;
        }
    }
}
