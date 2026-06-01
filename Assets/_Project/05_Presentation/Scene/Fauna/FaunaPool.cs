using System.Collections.Generic;
using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Scene.Fauna
{
    /// <summary>
    /// Pre-instantiates one disabled sprite GameObject per trajectory of
    /// every species declared in a <see cref="FaunaPlacementDefinition"/>.
    /// One trajectory = one slot = max one bird simultaneously on that
    /// path (so a 2-trajectory species can show 2 birds at once, a
    /// 1-trajectory species shows max 1).
    /// <para>
    /// CLAUDE.md §6 forbids runtime Instantiate/Destroy. All sprites are
    /// created once at Awake under <see cref="spawnRoot"/> with
    /// <c>SetActive(false)</c>, each carrying a configured
    /// <see cref="FaunaTraversalMotion"/>; the spawn driver
    /// (<see cref="FaunaPoolBinding"/>) toggles activity probabilistically.
    /// </para>
    /// <para>
    /// Execution order -9000 matches <c>SensorVisualPlacer</c> / scene
    /// assembly: children exist before any default-order binding runs.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class FaunaPool : MonoBehaviour
    {
        [SerializeField, Tooltip("Root SO listing the visible species and their trajectories.")]
        private FaunaPlacementDefinition placement;

        [SerializeField, Tooltip("Parent transform that receives the pooled sprites. Falls back to this transform if null. Typically '_Scene_Visual/Fauna'.")]
        private Transform spawnRoot;

        private readonly List<PooledSprite> _pooled = new List<PooledSprite>();

        /// <summary>
        /// Read-only view of all pooled sprites, in stable order across
        /// the species/trajectory grid. Iterated by
        /// <see cref="FaunaPoolBinding"/> to drive activation.
        /// </summary>
        public IReadOnlyList<PooledSprite> PooledSprites => _pooled;

        private void Awake()
        {
            Rebuild();
        }

        /// <summary>
        /// Pre-instantiate one disabled GameObject per (species, trajectory)
        /// pair. Called once at <see cref="Awake"/> in normal runtime;
        /// also callable directly so EditMode tests can trigger
        /// construction without relying on the Awake lifecycle (which is
        /// not auto-fired in EditMode test frames) and so an editor button
        /// could re-run the build after authoring changes to the SO.
        /// Idempotent: re-running clears the previous children first.
        /// </summary>
        public void Rebuild()
        {
            if (placement == null)
            {
                SimLogger.DebugLog("[FaunaPool] no placement assigned, skipping");
                return;
            }

            var parent = spawnRoot != null ? spawnRoot : transform;
            ClearChildren(parent);
            _pooled.Clear();
            BuildPool(placement, parent);
        }

        private void BuildPool(FaunaPlacementDefinition def, Transform parent)
        {
            var species = def.Species;
            for (int s = 0; s < species.Count; s++)
            {
                var sp = species[s];
                if (sp == null) continue;

                int n = sp.TrajectoryCount;
                for (int i = 0; i < n; i++)
                {
                    var go = new GameObject(sp.Id + "_" + i);
                    go.transform.SetParent(parent, worldPositionStays: false);

                    var renderer = go.AddComponent<SpriteRenderer>();
                    if (sp.FrameCount > 0)
                    {
                        renderer.sprite = sp.Frames[0];
                    }
                    if (!string.IsNullOrEmpty(sp.SortingLayerName))
                    {
                        renderer.sortingLayerName = sp.SortingLayerName;
                    }
                    renderer.sortingOrder = sp.SortingOrderInLayer;

                    var motion = go.AddComponent<FaunaTraversalMotion>();
                    motion.Configure(sp.Frames, sp.FramesPerSecond, sp.Trajectories[i], sp.DefaultFacesRight);

                    go.SetActive(false);

                    _pooled.Add(new PooledSprite(go, motion, sp, i));
                }
            }

            SimLogger.DebugLog("[FaunaPool] pre-instantiated " + _pooled.Count + " sprites across " + species.Count + " species");
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Stable handle to one pooled sprite. Carries everything the spawn
    /// driver needs to decide activation + drive the traversal, without
    /// re-querying the SO each frame.
    /// </summary>
    public sealed class PooledSprite
    {
        public GameObject GameObject { get; }
        public FaunaTraversalMotion Motion { get; }
        public FaunaSpeciesDefinition Species { get; }
        public int TrajectoryIndex { get; }

        public PooledSprite(
            GameObject go,
            FaunaTraversalMotion motion,
            FaunaSpeciesDefinition species,
            int trajectoryIndex)
        {
            GameObject = go;
            Motion = motion;
            Species = species;
            TrajectoryIndex = trajectoryIndex;
        }
    }
}
