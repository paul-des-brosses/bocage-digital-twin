using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the « Apport de la techno » Hero KPI: the
    /// NET cumulative € / ha value of the farmer's decisions — the
    /// operational profit advantage of the real run over the shadow run,
    /// integrated from day 0 (cf <c>CumulativeTechValueIndicator</c>),
    /// MINUS the cumulative upfront capital invested in actions (hedge
    /// plantations). Sensor costs are not counted. Reads 0 until decisions
    /// diverge the two runs; can go negative when the capital outlay
    /// outruns the banked gains (a poorly-managed run).
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_TechDelta",
        fileName = "RC_TechDelta")]
    public sealed class RC_TechDelta : ScriptableObject
    {
        [SerializeField, FormerlySerializedAs("deltaPercent"), FormerlySerializedAs("cumulativeEurosPerHa")]
        private float netEurosPerHa;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float NetEurosPerHa => netEurosPerHa;
        public float Normalized01 => normalized01;

        public void Set(float newNetEurosPerHa, float newNormalized01)
        {
            if (netEurosPerHa == newNetEurosPerHa && normalized01 == newNormalized01) return;
            netEurosPerHa = newNetEurosPerHa;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(netEurosPerHa);
        }

        public void ResetToZero()
        {
            netEurosPerHa = 0f;
            normalized01 = 0.25f; // 0 €/ha sits at 0.25 on the [-500, +1500] gauge
            OnChanged?.Invoke(0f);
        }
    }
}
