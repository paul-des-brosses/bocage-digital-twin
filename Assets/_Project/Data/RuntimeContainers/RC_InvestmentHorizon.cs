using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the « horizon de rentabilité » metric
    /// (chantier E5 / ADR #50). Single-writer pattern: written by
    /// <c>SimulationRunner.PublishIndicators</c> from
    /// <see cref="Bocage.Indicators.Hero.InvestmentHorizonIndicator"/>,
    /// read by the popup and Économie tab bindings (chantier E6).
    /// <para>
    /// Three channels:
    /// <list type="bullet">
    ///   <item><see cref="IsReached"/> — true once the cumul matched
    ///         total investment at least once. When false, UI bindings
    ///         must display « Non encore atteint » instead of the
    ///         numeric value, no matter what
    ///         <see cref="HorizonYears"/> says.</item>
    ///   <item><see cref="HorizonYears"/> — simulated years to break
    ///         even, latched on first crossing. Undefined while
    ///         <see cref="IsReached"/> is false; readers must gate
    ///         on the flag.</item>
    ///   <item><see cref="CumulativeProfitDeltaEurosPerHa"/> — running
    ///         integral of (real − shadow) / 365 since first investment,
    ///         in € / ha. Useful for the popup « écart accumulé » line
    ///         even before the horizon is reached.</item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_InvestmentHorizon",
        fileName = "RC_InvestmentHorizon")]
    public sealed class RC_InvestmentHorizon : ScriptableObject
    {
        [SerializeField] private bool isReached;
        [SerializeField] private float horizonYears;
        [SerializeField] private float cumulativeProfitDeltaEurosPerHa;

        public event Action<bool> OnChanged;

        public bool IsReached => isReached;
        public float HorizonYears => horizonYears;
        public float CumulativeProfitDeltaEurosPerHa => cumulativeProfitDeltaEurosPerHa;

        public void Set(bool newIsReached, float newHorizonYears, float newCumulativeDelta)
        {
            if (isReached == newIsReached
                && horizonYears == newHorizonYears
                && cumulativeProfitDeltaEurosPerHa == newCumulativeDelta) return;
            isReached = newIsReached;
            horizonYears = newHorizonYears;
            cumulativeProfitDeltaEurosPerHa = newCumulativeDelta;
            OnChanged?.Invoke(isReached);
        }

        public void ResetToZero()
        {
            isReached = false;
            horizonYears = 0f;
            cumulativeProfitDeltaEurosPerHa = 0f;
            OnChanged?.Invoke(false);
        }
    }
}
