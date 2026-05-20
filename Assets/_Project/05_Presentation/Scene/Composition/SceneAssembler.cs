using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Scene.Composition
{
    /// <summary>
    /// Instantiates the static scene composition from a
    /// <see cref="SceneCompositionDefinition"/> at Awake. One
    /// SpriteRenderer GameObject is created per element under
    /// <see cref="spawnRoot"/> (or this transform if not set).
    /// <para>
    /// Idempotent: clears previously-spawned children before rebuilding,
    /// so the component can be re-run safely (e.g. via a future editor
    /// rebuild button without leaving stale objects in the scene).
    /// </para>
    /// <para>
    /// Allocations occur only at boot; per CLAUDE.md §6 no per-frame
    /// allocation is performed here.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class SceneAssembler : MonoBehaviour
    {
        [SerializeField] private SceneCompositionDefinition composition;

        [SerializeField, Tooltip("Parent transform that receives spawned sprites. Falls back to this transform if null.")]
        private Transform spawnRoot;

        private void Awake()
        {
            if (composition == null)
            {
                SimLogger.DebugLog("[SceneAssembler] no composition assigned, skipping");
                return;
            }

            var parent = spawnRoot != null ? spawnRoot : transform;
            ClearChildren(parent);
            int spawned = BuildFrom(composition, parent);
            SimLogger.DebugLog("[SceneAssembler] composed " + spawned + " elements from " + composition.name);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only rebuild used by the custom inspector's "Rebuild Now"
        /// button. Lets the artist iterate on the composition asset without
        /// entering Play Mode.
        /// </summary>
        public void RebuildInEditor()
        {
            if (composition == null)
            {
                SimLogger.DebugLog("[SceneAssembler] no composition assigned, skipping editor rebuild");
                return;
            }

            var parent = spawnRoot != null ? spawnRoot : transform;
            ClearChildren(parent);
            int spawned = BuildFrom(composition, parent);
            SimLogger.DebugLog("[SceneAssembler] (editor) rebuilt " + spawned + " elements from " + composition.name);
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

        private static int BuildFrom(SceneCompositionDefinition def, Transform parent)
        {
            int count = 0;
            var elements = def.Elements;
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element.sprite == null)
                {
                    SimLogger.DebugLog("[SceneAssembler] element " + i + " (" + element.id + ") has no sprite, skipping");
                    continue;
                }

                string goName = string.IsNullOrEmpty(element.id) ? ("Element_" + i) : element.id;
                var go = new GameObject(goName);
                go.transform.SetParent(parent, worldPositionStays: false);
                go.transform.localPosition = new Vector3(element.worldPosition.x, element.worldPosition.y, 0f);

                float scaleX = element.scale.x <= 0f ? 1f : element.scale.x;
                float scaleY = element.scale.y <= 0f ? 1f : element.scale.y;
                go.transform.localScale = new Vector3(element.flipX ? -scaleX : scaleX, scaleY, 1f);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = element.sprite;
                if (element.material != null)
                {
                    // sharedMaterial avoids instantiating a per-renderer copy;
                    // per-element shader values are pushed at runtime via
                    // MaterialPropertyBlock by the relevant binding (cf
                    // HedgerowShaderBinding for the hedges).
                    renderer.sharedMaterial = element.material;
                }
                if (!string.IsNullOrEmpty(element.sortingLayerName))
                {
                    renderer.sortingLayerName = element.sortingLayerName;
                }
                renderer.sortingOrder = element.sortingOrderInLayer;
                count++;
            }
            return count;
        }
    }
}
