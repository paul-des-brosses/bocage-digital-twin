using System;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Updates the water table depth from today's weather plus a slow
    /// recharge term that pulls the table toward its long-term mean.
    /// <list type="bullet">
    ///   <item><b>Rain</b> raises the water level (depth decreases).</item>
    ///   <item><b>Evapotranspiration</b> lowers the water level (depth
    ///         increases).</item>
    ///   <item><b>Recharge</b> represents underground inflow + aquifer
    ///         geometry: when the table is below its long-term mean,
    ///         underground recharge slowly brings it back up; when above,
    ///         excess drains away. Without this term the table would
    ///         run away under sustained climate stress (no physical
    ///         equilibrium), which is non-realistic for a real Perche
    ///         aquifer constrained by geology and regional water budget.</item>
    /// </list>
    /// <para>
    /// The recharge equilibrium <c>RechargeTargetDepth</c> = 2.0 m matches
    /// the historical Perche bocage mean. Combined with sustained climate
    /// stress, the equilibrium depth shifts: at RCP4.5 horizon 2050
    /// (+2 °C / −20 % precip) the new equilibrium ≈ 4 m, plausible.
    /// At neutral, the table stays bounded around 2 m with seasonal noise.
    /// </para>
    /// </summary>
    public sealed class WaterTableDynamicsRule : IRule
    {
        public string SubStreamId => "water-table";

        private const double InfiltrationFactor = 0.0001;
        private const double EvaporationBase = 0.003;
        public const double RechargeTargetDepth = 2.0;
        // Calibrated 2026-05-21 against CalibrationScenarioValidationTests:
        // 0.0005/day was too slow → neutral equilibrium at ~4 m (waterEffect
        // collapse), failing scenarios 1 (neutral) and 3 (virtuous). At
        // 0.002/day the recharge timescale is ~500 days, aligning with a
        // realistic Perche bocage aquifer response, and neutral equilibrium
        // sits at ~2.5 m (waterEffect ≈ 0.97).
        public const double RechargeRatePerDay = 0.002;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double rainTerm = -model.CurrentWeather.PrecipitationMillimeters * InfiltrationFactor;
            double tempNormalized = Math.Max(0.0, model.CurrentWeather.TemperatureCelsius / 30.0);
            double evapTerm = tempNormalized * EvaporationBase;

            // Recharge pulls depth toward the long-term mean. Sign convention:
            // delta_depth = (target - currentDepth) × rate
            // If currentDepth > target (table too deep), the term is negative
            // (depth decreases, water rises). If currentDepth < target, the
            // term is positive (depth increases, drains away).
            double rechargeTerm = (RechargeTargetDepth - model.WaterTableDepth) * RechargeRatePerDay;

            double change = rainTerm + evapTerm + rechargeTerm;

            model.SetWaterTableDepth(model.WaterTableDepth + change);
        }
    }
}
