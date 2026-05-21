using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily update of the running crop yield estimate. Target yield is
    /// a function of model state (hedgerow density, water table depth)
    /// and scenario inputs (temperature anomaly, precipitation anomaly,
    /// input intensity factor). Actual yield drifts toward the target
    /// with a ~100-day time constant (EMA k = 0.01) reflecting the
    /// agronomic inertia between conditions and the harvest expectation.
    /// <para>
    /// <b>Calibration sources (revision 2026-05-21)</b>
    /// <list type="bullet">
    ///   <item>Baseline yield 5.5 t/ha : moyenne pondérée blé tendre +
    ///         colza pour Eure-et-Loir / Orne (Agreste 2015-2024). Blé
    ///         ~6.5 t/ha, colza ~3.2 t/ha, mix typique 70/30.</item>
    ///   <item>Hedge effect bell: at the ideal density of 80 m/ha the
    ///         windbreak benefit is already embedded in the Agreste
    ///         baseline (most Perche farms operate at 60-110 m/ha,
    ///         INRAE reference). The bell therefore peaks at 1.0 (no
    ///         additional bonus) and penalises deviations: −15% at
    ///         extreme low or extreme high density. Avoids the
    ///         double-counting bug of the previous iteration where
    ///         the baseline was averaged over hedge-equipped farms
    ///         AND amplified by the bell.</item>
    ///   <item>Heat penalty 6%/°C : IPCC AR6 chap. 5 reports 5-7%
    ///         cereal yield loss per °C above optimum for temperate
    ///         climates.</item>
    ///   <item>Drought penalty 0.5%/% precip deficit : European
    ///         cereals sensitivity, plage 0.3-0.7% selon études INRAE.</item>
    ///   <item>Intensity factor effect ±10% : agronomic literature
    ///         on intensive vs extensive practices, modest effect on
    ///         yield (most of the impact is on inputs, not yield).</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class CropYieldDynamicsRule : IRule
    {
        public string SubStreamId => "crop-yield";

        public const double BaselineTonnesPerHectare = 5.5;
        private const double TransitionRatePerDay = 0.01;

        // Hedgerow windbreak effect: penalty for being away from the ideal
        // density. Peak = 1.0 (no bonus, already in baseline), extreme = 0.85.
        // Ideal aligned to the Perche departmental average (90 m/ha) so the
        // default initial state (also 90 m/ha) sits exactly at the bell peak
        // and CropYield doesn't drift away from baseline at boot.
        private const double IdealHedgerowDensity = 90.0;
        private const double HedgerowMaxPenalty = 0.15;
        private const double HedgerowDensityTolerance = 60.0;

        // Water table optimum.
        private const double IdealWaterDepthMeters = 2.0;
        private const double WaterDepthSensitivity = 0.10;

        // Climate stress penalties.
        private const double HeatPenaltyPerDegree = 0.06;          // 30% / 5°C, IPCC AR6
        private const double DroughtPenaltyPerPercent = 0.005;     // 30% / 60% deficit

        // Intensification boost: ±10% around factor 1.0.
        private const double IntensityBoostPerUnit = 0.10;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double hedgerowEffect = ComputeHedgerowEffect(model.HedgerowDensity);
            double waterEffect = ComputeWaterEffect(model.WaterTableDepth);
            double climateEffect = ComputeClimateEffect(
                scenario.TemperatureAnomalyC.Current,
                scenario.PrecipitationAnomalyPercent.Current);
            double intensityEffect = ComputeIntensityEffect(scenario.InputIntensityFactor.Current);

            double target = BaselineTonnesPerHectare * hedgerowEffect * waterEffect * climateEffect * intensityEffect;
            if (target < 0.0) target = 0.0;

            double current = model.CropYield;
            double next = current + TransitionRatePerDay * (target - current);
            model.SetCropYield(next);
        }

        /// <summary>
        /// Bell-shaped penalty: 1.0 at ideal density (baseline already
        /// includes the windbreak benefit), drops to 1 − HedgerowMaxPenalty
        /// at extreme deviations. Symmetric around the ideal.
        /// </summary>
        public static double ComputeHedgerowEffect(double densityMetersPerHectare)
        {
            double delta = densityMetersPerHectare - IdealHedgerowDensity;
            double normalisedDelta = delta / HedgerowDensityTolerance;
            double bell = System.Math.Exp(-normalisedDelta * normalisedDelta);
            return 1.0 - HedgerowMaxPenalty * (1.0 - bell);
        }

        private static double ComputeWaterEffect(double depthMeters)
        {
            double delta = depthMeters - IdealWaterDepthMeters;
            double penalty = WaterDepthSensitivity * delta * delta;
            double effect = 1.0 - penalty;
            if (effect < 0.0) effect = 0.0;
            return effect;
        }

        private static double ComputeClimateEffect(double tempAnomalyC, double precipAnomalyPct)
        {
            double heatPenalty = tempAnomalyC > 0.0 ? tempAnomalyC * HeatPenaltyPerDegree : 0.0;
            if (heatPenalty > 0.30) heatPenalty = 0.30;

            double droughtPenalty = precipAnomalyPct < 0.0 ? -precipAnomalyPct * DroughtPenaltyPerPercent : 0.0;
            if (droughtPenalty > 0.30) droughtPenalty = 0.30;

            return (1.0 - heatPenalty) * (1.0 - droughtPenalty);
        }

        private static double ComputeIntensityEffect(double intensityFactor)
        {
            double delta = intensityFactor - 1.0;
            double effect = 1.0 + IntensityBoostPerUnit * delta;
            if (effect < 0.5) effect = 0.5;
            if (effect > 1.5) effect = 1.5;
            return effect;
        }
    }
}
