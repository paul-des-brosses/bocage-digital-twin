using System;
using UnityEngine;

namespace Bocage.Data.RuntimeContainers
{
    /// <summary>
    /// Observable container for the cumulative upfront capital
    /// invested by the user across all manual « Replanter haies »
    /// actions (chantier E5 / ADR #50). Same single-writer pattern as
    /// <see cref="RC_IntegratedProfitability"/>: written by
    /// <c>RefonteSimulationRunner.PublishIndicators</c> from
    /// <see cref="Bocage.Decision.DecisionJournal.TotalInvestmentEurosPerHectare"/>,
    /// read by the Économie tab binding (chantier E6) and the popup
    /// for context display.
    /// <para>
    /// Two channels:
    /// <list type="bullet">
    ///   <item><see cref="EurosPerHectare"/> — raw cumulative cost (€/ha).</item>
    ///   <item><see cref="Normalized01"/> — unit-range value for any gauge
    ///         (0 = nothing invested, 1 = <see cref="MaxEurosPerHectare"/>
    ///         worth of plantations, a plausible upper bound for a
    ///         decadal MVP run).</item>
    /// </list>
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Data/RC_TotalInvestment",
        fileName = "RC_TotalInvestment")]
    public sealed class RC_TotalInvestment : ScriptableObject
    {
        public const float MaxEurosPerHectare = 1000.0f;

        [SerializeField] private float eurosPerHectare;
        [SerializeField, Range(0f, 1f)] private float normalized01;

        public event Action<float> OnChanged;

        public float EurosPerHectare => eurosPerHectare;
        public float Normalized01 => normalized01;

        public void Set(float newEurosPerHectare, float newNormalized01)
        {
            if (eurosPerHectare == newEurosPerHectare && normalized01 == newNormalized01) return;
            eurosPerHectare = newEurosPerHectare;
            normalized01 = newNormalized01;
            OnChanged?.Invoke(eurosPerHectare);
        }

        public void ResetToZero()
        {
            eurosPerHectare = 0f;
            normalized01 = 0f;
            OnChanged?.Invoke(0f);
        }
    }
}
