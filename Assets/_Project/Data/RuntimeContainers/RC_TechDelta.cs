using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the tech delta Hero KPI (sub-étape 8b).
    /// Mirrors the percent advantage of the real run over the shadow
    /// run on integrated profitability (cf <c>TechDeltaIndicator</c>).
    /// <para>
    /// At sub-étape 8b the value is 0 by construction because the
    /// shadow run is identical to the real run; meaningful values
    /// emerge at sub-étape 8c when auto-actions differentiate the two.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_TechDelta",
        fileName = "RC_TechDelta")]
    public sealed class RC_TechDelta : ScriptableObject
    {
        [SerializeField] private float deltaPercent;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float DeltaPercent => deltaPercent;
        public float Normalized01 => normalized01;

        public void Set(float newDeltaPercent, float newNormalized01)
        {
            if (deltaPercent == newDeltaPercent && normalized01 == newNormalized01) return;
            deltaPercent = newDeltaPercent;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(deltaPercent);
        }

        public void ResetToZero()
        {
            deltaPercent = 0f;
            normalized01 = 0.5f; // 0% delta corresponds to mid-range on the [-100, +100] gauge
            OnChanged?.Invoke(0f);
        }
    }
}
