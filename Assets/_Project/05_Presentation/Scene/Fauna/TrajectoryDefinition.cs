using UnityEngine;

namespace Bocage.Presentation.Scene.Fauna
{
    /// <summary>
    /// One traversal trajectory for a fauna sprite: two off-screen
    /// endpoints, a single duration in seconds, and a small vertical
    /// sinusoidal bob to break the visual monotony of pure linear
    /// motion. The direction at runtime (L→R or R→L) is picked
    /// stochastically by <see cref="FaunaPoolBinding"/>, deterministic
    /// under the master seed.
    /// <para>
    /// One trajectory hosts at most one active sprite at a time —
    /// max simultaneous instances of a species = number of trajectories
    /// declared on its <see cref="FaunaSpeciesDefinition"/>.
    /// </para>
    /// </summary>
    [System.Serializable]
    public struct TrajectoryDefinition
    {
        [Tooltip("Off-screen world position where the sprite enters/exits on the left side. Z is forced to 0 at spawn.")]
        public Vector2 leftPoint;

        [Tooltip("Off-screen world position where the sprite enters/exits on the right side. Z is forced to 0 at spawn.")]
        public Vector2 rightPoint;

        [Tooltip("Time in seconds to traverse from one side to the other. Same for both directions (sprite is mirrored).")]
        public float durationSec;

        [Tooltip("Amplitude of the vertical sinusoidal bob during traversal, in Unity world units. Set to 0 for pure linear motion.")]
        public float verticalBobAmplitude;

        [Tooltip("Frequency of the vertical sinusoidal bob, in Hz. Independent of the wing-flap frame rate.")]
        public float verticalBobFrequencyHz;
    }
}
