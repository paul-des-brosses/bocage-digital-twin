using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the « horizon de rentabilité » metric
    /// (chantier E5 / ADR #50, refondu E8). Single-writer pattern: written
    /// by <c>SimulationRunner.PublishIndicators</c> from the Couche 04
    /// indicators, read by the Économie tab binding.
    /// <para>
    /// Two channels:
    /// <list type="bullet">
    ///   <item><see cref="IsReached"/> — true once the NET tech value
    ///         reached break-even at least once while an investment
    ///         existed. When false, UI bindings display « Non atteint »
    ///         (or « Sans objet » if no investment has been made yet),
    ///         no matter what <see cref="HorizonYears"/> says.</item>
    ///   <item><see cref="HorizonYears"/> — simulated years from day 0 to
    ///         break-even, latched on first crossing. Undefined while
    ///         <see cref="IsReached"/> is false; readers must gate on the
    ///         flag.</item>
    /// </list>
    /// The running NET itself is surfaced by the Hero KPI
    /// (<c>RC_TechDelta</c>), so it is not duplicated here.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_InvestmentHorizon",
        fileName = "RC_InvestmentHorizon")]
    public sealed class RC_InvestmentHorizon : ScriptableObject
    {
        [SerializeField] private bool isReached;
        [SerializeField] private float horizonYears;

        public event Action<bool> OnChanged;

        public bool IsReached => isReached;
        public float HorizonYears => horizonYears;

        public void Set(bool newIsReached, float newHorizonYears)
        {
            if (isReached == newIsReached && horizonYears == newHorizonYears) return;
            isReached = newIsReached;
            horizonYears = newHorizonYears;
            OnChanged?.Invoke(isReached);
        }

        public void ResetToZero()
        {
            isReached = false;
            horizonYears = 0f;
            OnChanged?.Invoke(false);
        }
    }
}
