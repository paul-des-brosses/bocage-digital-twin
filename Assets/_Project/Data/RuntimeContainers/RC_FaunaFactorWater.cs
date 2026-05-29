using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the « eau » factor of the fauna
    /// dynamics (chantier E5 / ADR #51). Derived from
    /// <c>WaterTableDepth</c> via
    /// <c>FaunaDynamicsRule.ComputeWaterFactor</c> and normalised by
    /// <c>BiodiversityCompositeIndicator.NormalizeWater</c>. Mirrors
    /// <see cref="RC_FaunaFactorHabitat"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_FaunaFactorWater",
        fileName = "RC_FaunaFactorWater")]
    public sealed class RC_FaunaFactorWater : ScriptableObject
    {
        [SerializeField] private float rawFactor;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float RawFactor => rawFactor;
        public float Normalized01 => normalized01;

        public void Set(float newRawFactor, float newNormalized01)
        {
            if (rawFactor == newRawFactor && normalized01 == newNormalized01) return;
            rawFactor = newRawFactor;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(rawFactor);
        }

        public void ResetToZero()
        {
            rawFactor = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
