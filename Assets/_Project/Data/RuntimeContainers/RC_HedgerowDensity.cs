using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable ScriptableObject mirroring the current value of the
    /// hedgerow density Hero KPI. This is the bridge between the
    /// pure-C# indicator (Couche 4) and the Unity-side consumers
    /// (UI labels, shaders) in Couche 5: a single writer (the
    /// publisher in <c>SimulationRunner</c>) pushes new values via
    /// <see cref="Set"/>, every subscriber listens via
    /// <see cref="OnChanged"/>.
    /// <para>
    /// The asset lives in <c>Assets/_Project/Data/RuntimeContainers/</c>
    /// per ARCHITECTURE.md §7. Naming convention <c>RC_*</c>
    /// ("Runtime Container") is reserved for this observable pattern.
    /// </para>
    /// <para>
    /// Two channels are exposed:
    /// <list type="bullet">
    ///   <item><see cref="MetersPerHectare"/> — raw value for UI labels and tooltips.</item>
    ///   <item><see cref="Normalized01"/> — unit-range value for shaders and gauges.</item>
    /// </list>
    /// Both are written together by <see cref="Set"/> to guarantee they
    /// never drift apart.
    /// </para>
    /// <para>
    /// Per CLAUDE.md §6, no allocation is performed in <see cref="Set"/>;
    /// it can be called every tick without GC pressure.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_HedgerowDensity",
        fileName = "RC_HedgerowDensity")]
    public sealed class RC_HedgerowDensity : ScriptableObject
    {
        [SerializeField] private float metersPerHectare;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        /// <summary>Raised after the value changed. Pushes the new raw
        /// value (m/ha) so subscribers don't have to read it back.</summary>
        public event Action<float> OnChanged;

        public float MetersPerHectare => metersPerHectare;
        public float Normalized01 => normalized01;

        /// <summary>
        /// Updates both representations and notifies subscribers. No-op
        /// (and no event) when the raw value is unchanged at single
        /// precision, to avoid spamming bindings every tick when the
        /// model is idle.
        /// </summary>
        public void Set(float newMetersPerHectare, float newNormalized01)
        {
            if (metersPerHectare == newMetersPerHectare && normalized01 == newNormalized01)
            {
                return;
            }
            metersPerHectare = newMetersPerHectare;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(metersPerHectare);
        }

        /// <summary>
        /// Reset to a known state. Called from editor utilities and
        /// from the runner at bootstrap so listeners that subscribed
        /// before the first tick receive an initial value.
        /// </summary>
        public void ResetToZero()
        {
            metersPerHectare = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
