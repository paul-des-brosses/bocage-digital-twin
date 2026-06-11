using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the soil organic carbon Hero KPI
    /// (chantier E3 / ADR #48). Same pattern as
    /// <see cref="RC_HedgerowDensity"/> and <see cref="RC_WaterTableDepth"/>:
    /// single writer (RefonteSimulationRunner), many readers (UI labels, future
    /// onglet Climat &amp; Ressources binding).
    /// <para>
    /// Two channels:
    /// <list type="bullet">
    ///   <item><see cref="TonnesCarbonPerHectare"/> — raw value for UI labels (tC/ha).</item>
    ///   <item><see cref="Normalized01"/> — unit-range value (0 = degraded ≤ 30 tC/ha, 1 = living soil ≥ 100 tC/ha).</item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_SoilCarbonStock",
        fileName = "RC_SoilCarbonStock")]
    public sealed class RC_SoilCarbonStock : ScriptableObject
    {
        [SerializeField] private float tonnesCarbonPerHectare;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float TonnesCarbonPerHectare => tonnesCarbonPerHectare;
        public float Normalized01 => normalized01;

        public void Set(float newTonnesCarbonPerHectare, float newNormalized01)
        {
            if (tonnesCarbonPerHectare == newTonnesCarbonPerHectare && normalized01 == newNormalized01) return;
            tonnesCarbonPerHectare = newTonnesCarbonPerHectare;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(tonnesCarbonPerHectare);
        }

        public void ResetToZero()
        {
            tonnesCarbonPerHectare = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
