using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily update of the running crop yield estimate. The target yield
    /// is a function of the current state (hedgerow density, water table
    /// depth) and scenario inputs (climate stress, agricultural
    /// pressure); the actual yield drifts toward that target with a
    /// ~100-day time constant (EMA k = 0.01), reflecting the agronomic
    /// inertia between conditions and the harvest expectation.
    /// <para>
    /// <b>Calibration</b>: baseline 5.0 t/ha matches the Agreste 2022
    /// average for a mixed cereal/oilseed farm in the Perche. Multipliers
    /// stay in plausible orders of magnitude; this is a model honest
    /// about being a model, not a peer-reviewed agronomic equation.
    /// </para>
    /// </summary>
    public sealed class CropYieldDynamicsRule : IRule
    {
        public string SubStreamId => "crop-yield";

        private const double BaselineTonnesPerHectare = 5.0;
        private const double TransitionRatePerDay = 0.01; // EMA, ~100-day time constant

        // Hedgerow windbreak effect: max +15% at ~80 m/ha, slight competition
        // penalty below ~20 m/ha (no windbreak protection) and above ~200 m/ha
        // (shade and root competition with crops).
        private const double IdealHedgerowDensity = 80.0;
        private const double HedgerowEffectStrength = 0.15;
        private const double HedgerowDensityTolerance = 60.0;

        // Water table effect: optimum at 2 m, fall-off symmetric.
        private const double IdealWaterDepthMeters = 2.0;
        private const double WaterDepthSensitivity = 0.10;

        // Scenario effects.
        private const double ClimateStressPenalty = 0.30;
        private const double AgriculturalPressureBoost = 0.10;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double hedgerowEffect = ComputeHedgerowEffect(model.HedgerowDensity);
            double waterEffect = ComputeWaterEffect(model.WaterTableDepth);
            double climateEffect = 1.0 - ClimateStressPenalty * Clamp01(scenario.ClimateStress.Current);
            double pressureEffect = 1.0 + AgriculturalPressureBoost * Clamp01(scenario.AgriculturalPressure.Current);

            double target = BaselineTonnesPerHectare * hedgerowEffect * waterEffect * climateEffect * pressureEffect;
            if (target < 0.0) target = 0.0;

            double current = model.CropYield;
            double next = current + TransitionRatePerDay * (target - current);
            model.SetCropYield(next);
        }

        private static double ComputeHedgerowEffect(double densityMetersPerHectare)
        {
            double delta = densityMetersPerHectare - IdealHedgerowDensity;
            double normalisedDelta = delta / HedgerowDensityTolerance;
            // Gaussian-like bell: 1 + 0.15 at the peak, gradually falls below 1
            // outside the tolerance band.
            double bell = System.Math.Exp(-normalisedDelta * normalisedDelta);
            return 1.0 + HedgerowEffectStrength * (2.0 * bell - 1.0);
        }

        private static double ComputeWaterEffect(double depthMeters)
        {
            double delta = depthMeters - IdealWaterDepthMeters;
            double penalty = WaterDepthSensitivity * delta * delta;
            double effect = 1.0 - penalty;
            if (effect < 0.0) effect = 0.0;
            return effect;
        }

        private static double Clamp01(double v)
        {
            if (v < 0.0) return 0.0;
            if (v > 1.0) return 1.0;
            return v;
        }
    }
}
