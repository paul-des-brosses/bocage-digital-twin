using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the « Apport de la techno » Hero KPI: the
    /// cumulative € / ha advantage of the real run over the shadow run on
    /// integrated profitability, integrated from day 0 (cf
    /// <c>CumulativeTechValueIndicator</c>). Reads 0 until tech decisions
    /// diverge the two runs; never collapses (a transient action plateaus,
    /// a sustained strategy keeps growing).
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_TechDelta",
        fileName = "RC_TechDelta")]
    public sealed class RC_TechDelta : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("deltaPercent")] private float cumulativeEurosPerHa;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float CumulativeEurosPerHa => cumulativeEurosPerHa;
        public float Normalized01 => normalized01;

        public void Set(float newCumulativeEurosPerHa, float newNormalized01)
        {
            if (cumulativeEurosPerHa == newCumulativeEurosPerHa && normalized01 == newNormalized01) return;
            cumulativeEurosPerHa = newCumulativeEurosPerHa;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(cumulativeEurosPerHa);
        }

        public void ResetToZero()
        {
            cumulativeEurosPerHa = 0f;
            normalized01 = 0.25f; // 0 €/ha sits at 0.25 on the [-500, +1500] gauge
            OnChanged?.Invoke(0f);
        }
    }
}
