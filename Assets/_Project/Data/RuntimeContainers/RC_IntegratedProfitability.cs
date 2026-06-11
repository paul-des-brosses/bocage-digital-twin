using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the integrated profitability Hero KPI
    /// (sub-étape 7b). Mirrors the pattern of <see cref="RC_HedgerowDensity"/>
    /// and <see cref="RC_WaterTableDepth"/>: single writer
    /// (RefonteSimulationRunner), many readers (UI label binding, future
    /// gauges).
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_IntegratedProfitability",
        fileName = "RC_IntegratedProfitability")]
    public sealed class RC_IntegratedProfitability : ScriptableObject
    {
        [SerializeField] private float eurosPerHectare;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float EurosPerHectare => eurosPerHectare;
        public float Normalized01 => normalized01;

        public void Set(float newEurosPerHectare, float newNormalized01)
        {
            if (eurosPerHectare == newEurosPerHectare && normalized01 == newNormalized01) return;
            eurosPerHectare = newEurosPerHectare;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(eurosPerHectare);
        }

        public void ResetToZero()
        {
            eurosPerHectare = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
