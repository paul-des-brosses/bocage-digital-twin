using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the water table depth Hero KPI (Étape 6).
    /// Same pattern as <see cref="RC_HedgerowDensity"/>: single writer
    /// (SimulationRunner), many readers (UI labels, future gauges).
    /// <para>
    /// Two channels:
    /// <list type="bullet">
    ///   <item><see cref="DepthMeters"/> — raw value for UI labels (m below surface, positive).</item>
    ///   <item><see cref="Normalized01"/> — inverted unit-range value (1 = shallow/healthy, 0 = deep/stressed).</item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_WaterTableDepth",
        fileName = "RC_WaterTableDepth")]
    public sealed class RC_WaterTableDepth : ScriptableObject
    {
        [SerializeField] private float depthMeters;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float DepthMeters => depthMeters;
        public float Normalized01 => normalized01;

        public void Set(float newDepthMeters, float newNormalized01)
        {
            if (depthMeters == newDepthMeters && normalized01 == newNormalized01) return;
            depthMeters = newDepthMeters;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(depthMeters);
        }

        public void ResetToZero()
        {
            depthMeters = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
