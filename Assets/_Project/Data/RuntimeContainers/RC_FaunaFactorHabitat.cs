using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the « habitat » factor of the fauna
    /// dynamics (chantier E5 / ADR #51). Derived from
    /// <c>HedgerowDensity</c> via
    /// <c>FaunaDynamicsRule.ComputeHabitatFactor</c> and normalised
    /// by <c>BiodiversityCompositeIndicator.NormalizeHabitat</c>.
    /// Same single-writer pattern as <see cref="RC_SoilCarbonStock"/>;
    /// read by the onglet Biodiv binding (chantier E6).
    /// <para>
    /// Two channels: <see cref="RawFactor"/> for the « 1.05× » label
    /// in the onglet and <see cref="Normalized01"/> for the unit-range
    /// gauge.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_FaunaFactorHabitat",
        fileName = "RC_FaunaFactorHabitat")]
    public sealed class RC_FaunaFactorHabitat : ScriptableObject
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
