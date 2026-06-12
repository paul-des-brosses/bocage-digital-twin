using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the crop-yield Hero KPI (chantier Hero-realign,
    /// ADR R5 superseding #39). Same pattern as <see cref="RC_SoilCarbonStock"/>:
    /// single writer (<c>SimulationRunner</c>), one reader (the Hero label).
    /// Surfaces the model yield, which until now only appeared in the Économie
    /// onglet (read straight off the runner), as a first-class Hero KPI.
    /// <para>
    /// Two channels:
    /// <list type="bullet">
    ///   <item><see cref="TonnesPerHectare"/> — raw value for the UI label (t/ha).</item>
    ///   <item><see cref="Normalized01"/> — unit-range value for the gauge tint
    ///   (1 ≈ 1,2 × potentiel atteignable, cf <c>HeroIndicators.YieldNormalizeMax</c>).</item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_CropYield",
        fileName = "RC_CropYield")]
    public sealed class RC_CropYield : ScriptableObject
    {
        [SerializeField] private float tonnesPerHectare;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float TonnesPerHectare => tonnesPerHectare;
        public float Normalized01 => normalized01;

        public void Set(float newTonnesPerHectare, float newNormalized01)
        {
            if (tonnesPerHectare == newTonnesPerHectare && normalized01 == newNormalized01) return;
            tonnesPerHectare = newTonnesPerHectare;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(tonnesPerHectare);
        }

        public void ResetToZero()
        {
            tonnesPerHectare = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
