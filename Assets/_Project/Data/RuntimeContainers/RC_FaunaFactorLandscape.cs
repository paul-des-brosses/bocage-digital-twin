using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the « paysage » factor of the biodiversity
    /// composite (piste B2). Derived from the land-use mosaic (grassland share)
    /// and the hedge network via <c>BiodiversityRule.LandscapeFactor</c>.
    /// Single-writer pattern (the <c>SimulationRunner</c>); read by the onglet
    /// Biodiv binding, in mirror of the habitat / water / inputs factors.
    /// <para>
    /// Two channels: <see cref="RawFactor"/> and the unit-range
    /// <see cref="Normalized01"/> (here identical — the factor is [0,1] by
    /// construction).
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_FaunaFactorLandscape",
        fileName = "RC_FaunaFactorLandscape")]
    public sealed class RC_FaunaFactorLandscape : ScriptableObject
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
