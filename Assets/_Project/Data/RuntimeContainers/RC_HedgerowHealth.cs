using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the hedgerow health proxy (sub-étape 9β).
    /// Consumed by <c>HedgerowShaderBinding</c> to drive a desaturation
    /// / browning pass on the hedge sprites when active chalara or
    /// drought events are biting. Single writer
    /// (<c>SimulationRunner</c>), many readers.
    /// <para>
    /// Health is a derived presentation channel, not a model state
    /// variable — see <c>HedgerowHealthIndicator</c> for the rationale.
    /// The raw and normalized channels both carry the same [0,1] value
    /// (the indicator is unit-range by construction).
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_HedgerowHealth",
        fileName = "RC_HedgerowHealth")]
    public sealed class RC_HedgerowHealth : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float health01;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float Health01 => health01;
        public float Normalized01 => normalized01;

        public void Set(float newHealth01, float newNormalized01)
        {
            if (health01 == newHealth01 && normalized01 == newNormalized01) return;
            health01 = newHealth01;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(health01);
        }

        public void ResetToZero()
        {
            health01 = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
