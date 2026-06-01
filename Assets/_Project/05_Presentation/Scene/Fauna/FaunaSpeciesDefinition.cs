using System.Collections.Generic;
using UnityEngine;

namespace Bocage.Presentation.Scene.Fauna
{
    /// <summary>
    /// Determines how a fauna species is realised in the scene by
    /// <see cref="FaunaPool"/> + <see cref="FaunaPoolBinding"/>.
    /// </summary>
    public enum FaunaMotionMode
    {
        /// <summary>One sprite per trajectory, probabilistic Poisson
        /// activation, linear lerp between off-screen endpoints with
        /// sin Y bob. Used by swallow / owl / buzzard (transient
        /// passages across the scene).</summary>
        Traversal = 0,

        /// <summary>One sprite at a fixed <c>staticPosition</c>,
        /// GameObject always active, visibility driven by alpha
        /// fade-in / fade-out based on biodiv vs
        /// <c>appearanceThreshold</c>. Used by heron (sentinel
        /// indicator of good ecological state — present when biodiv
        /// is high, fades out otherwise).</summary>
        StaticAppearance = 1,
    }

    /// <summary>
    /// Data-driven definition of one visible fauna species. One asset per
    /// species, aggregated by <see cref="FaunaPlacementDefinition"/>,
    /// instantiated by <see cref="FaunaPool"/> at Awake, driven
    /// probabilistically by <see cref="FaunaPoolBinding"/>.
    /// <para>
    /// The species owns one or more <see cref="TrajectoryDefinition"/>
    /// (1 for solitary species like buse/chouette, 2 for swallow which
    /// allows up to 2 birds simultaneously). The pool pre-instantiates
    /// exactly one sprite per trajectory; the binding probabilistically
    /// activates each per-trajectory sprite based on the biodiv signal.
    /// </para>
    /// <para>
    /// Honest design (CLAUDE.md §9 sensor primacy): the appearance of
    /// each bird is gated on <c>RC_BiodiversityComposite</c> via
    /// <see cref="appearanceThreshold"/> + <see cref="spawnRateAtMaxBiodiv"/>.
    /// No calendar-driven spawn, no scenic logic — birds appear when
    /// the model says biodiv is high enough.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Scene/Fauna Species Definition",
        fileName = "FaunaSpecies_Default")]
    public sealed class FaunaSpeciesDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable identifier. Used as GameObject prefix and in log lines (e.g. 'swallow', 'owl', 'buzzard').")]
        private string id;

        [SerializeField, Tooltip("Sprites of the wing-flap cycle, in display order. Typically the N sub-sprites of a sliced sprite sheet (e.g. swallow_0, swallow_1, swallow_2). For static species, a single-element array is fine.")]
        private Sprite[] frames = new Sprite[0];

        [SerializeField, Tooltip("Wing-flap rate in frames per second. 8 fps for swallow, 6 fps for owl, 2 fps for buzzard (planar). Constant during the whole traversal.")]
        private float framesPerSecond = 6f;

        [SerializeField, Range(0f, 1f), Tooltip("Biodiv composite threshold below which the effective spawn rate is zero. Above the threshold, λ scales linearly from 0 to spawnRateAtMaxBiodiv.")]
        private float appearanceThreshold = 0.3f;

        [SerializeField, Tooltip("Maximum Poisson spawn rate per trajectory (spawns/sec) when biodiv = 1. Effective rate at runtime = spawnRateAtMaxBiodiv × max(0, (biodiv - threshold) / (1 - threshold)).")]
        private float spawnRateAtMaxBiodiv = 0.1f;

        [SerializeField, Tooltip("Set TRUE if the sprite source faces RIGHT at rest (most common — swallow). Set FALSE if it faces LEFT (e.g. buzzard top-down view drawn facing left). The motion component XORs this with the runtime direction so the bird always visually faces where it's going. Irrelevant for StaticAppearance mode.")]
        private bool defaultFacesRight = true;

        [SerializeField, Tooltip("How this species is realised. Traversal = transient passages (Poisson spawn). StaticAppearance = fixed-position sentinel that fades in/out on biodiv threshold.")]
        private FaunaMotionMode motionMode = FaunaMotionMode.Traversal;

        [SerializeField, Tooltip("World-space position where the sprite sits when in StaticAppearance mode. Ignored for Traversal.")]
        private Vector2 staticPosition;

        [SerializeField, Tooltip("Fade-in / fade-out duration in seconds for StaticAppearance mode. Ignored for Traversal.")]
        private float fadeDurationSec = 1.5f;

        [SerializeField, Tooltip("Uniform world-space scale applied to the spawned sprite GameObject (transform.localScale = Vector3.one * worldScale). 1.0 = native PPU size. Use to make a species visually bigger / smaller without re-importing the sprite.")]
        private float worldScale = 1f;

        [SerializeField, Tooltip("Sorting layer name for the spawned sprites (e.g. 'Foreground').")]
        private string sortingLayerName = "Default";

        [SerializeField, Tooltip("Order within the sorting layer (back to front). Higher = drawn on top.")]
        private int sortingOrderInLayer = 0;

        [SerializeField, Tooltip("Trajectories this species can fly. The pool pre-instantiates one sprite per trajectory; max simultaneous instances = trajectory count.")]
        private TrajectoryDefinition[] trajectories = new TrajectoryDefinition[0];

        public string Id => id;
        public IReadOnlyList<Sprite> Frames => frames;
        public float FramesPerSecond => framesPerSecond;
        public float AppearanceThreshold => appearanceThreshold;
        public float SpawnRateAtMaxBiodiv => spawnRateAtMaxBiodiv;
        public bool DefaultFacesRight => defaultFacesRight;
        public FaunaMotionMode MotionMode => motionMode;
        public Vector2 StaticPosition => staticPosition;
        public float FadeDurationSec => fadeDurationSec;
        public float WorldScale => worldScale > 0f ? worldScale : 1f;
        public string SortingLayerName => sortingLayerName;
        public int SortingOrderInLayer => sortingOrderInLayer;
        public IReadOnlyList<TrajectoryDefinition> Trajectories => trajectories;

        public int TrajectoryCount => trajectories?.Length ?? 0;
        public int FrameCount => frames?.Length ?? 0;
    }
}
