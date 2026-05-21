using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Fifth Hero KPI: relative difference of integrated profitability
    /// between the real run (with future tech decisions applied) and the
    /// shadow run (same seed and scenario, but tech decisions not
    /// applied). Expressed as a percentage of the shadow value.
    /// <para>
    /// At sub-étape 8b the shadow run is identical to the real run
    /// because no decisions exist yet — the delta will therefore display
    /// 0 % by construction. The KPI becomes meaningful at sub-étape 8c
    /// when <c>RecommendationEngine</c> and <c>AutoActions</c> land:
    /// auto-actions are applied to the real engine but skipped in the
    /// shadow, and the resulting profit divergence is exactly what this
    /// indicator captures.
    /// </para>
    /// <para>
    /// Honesty note (CLAUDE.md §9): displaying 0 % is the correct
    /// behaviour at 8b, NOT a placeholder. The "tech makes no difference"
    /// reading is literally true while no tech actions exist. Once
    /// decisions land at 8c, the delta will spread naturally.
    /// </para>
    /// </summary>
    public static class TechDeltaIndicator
    {
        // Bounds for the normalised gauge. Beyond ±100 % we saturate.
        // Chosen to match the natural reading "tech can double or halve
        // farm profitability in extreme scenarios".
        public const double MinDeltaPercent = -100.0;
        public const double MaxDeltaPercent = 100.0;

        // Profit floor used in the denominator to avoid division by
        // near-zero shadow profit (which would explode the percentage).
        // When shadow profit is small, the delta is computed against a
        // floor of 1 €/ha/yr so the absolute difference still drives
        // the indicator without amplification artefacts.
        private const double DenominatorFloor = 1.0;

        /// <summary>
        /// Returns the relative profit advantage of the real run over the
        /// shadow run, in percent. Positive = tech helps (real beats
        /// shadow); negative = tech hurts (real loses to shadow);
        /// 0 = identical (or shadow profit too close to zero for a
        /// meaningful ratio, in which case the absolute delta is
        /// expressed as a percent of <see cref="DenominatorFloor"/>).
        /// </summary>
        public static double Compute(EcosystemModel realModel, EcosystemModel shadowModel, ScenarioContext scenario)
        {
            double realProfit = IntegratedProfitabilityIndicator.Compute(realModel, scenario);
            double shadowProfit = IntegratedProfitabilityIndicator.Compute(shadowModel, scenario);

            double absShadow = shadowProfit < 0.0 ? -shadowProfit : shadowProfit;
            double denominator = absShadow > DenominatorFloor ? absShadow : DenominatorFloor;

            return 100.0 * (realProfit - shadowProfit) / denominator;
        }

        /// <summary>
        /// Maps the delta percent to <c>[0, 1]</c> with 0.5 at delta = 0,
        /// 0 at <see cref="MinDeltaPercent"/>, 1 at <see cref="MaxDeltaPercent"/>.
        /// Used by gauges that want a "centred" visualisation of the
        /// real-vs-shadow advantage.
        /// </summary>
        public static double Normalize(double deltaPercent)
        {
            double range = MaxDeltaPercent - MinDeltaPercent;
            double t = (deltaPercent - MinDeltaPercent) / range;
            if (t < 0.0) return 0.0;
            if (t > 1.0) return 1.0;
            return t;
        }
    }
}
