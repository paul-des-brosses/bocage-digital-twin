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
    ///   <item>Intensity factor effect: CONCAVE (quadratic-plateau /
    ///         Mitscherlich N-response). A -20% input cut (I=0.8) costs
    ///         ~2.8% yield (Lechenet et al. 2017, Nature Plants 3:17008 :
    ///         -42% pesticides without yield loss on 59% of FR farms);
    ///         a -50% cut to the organic-extensive floor (I=0.5) costs
    ///         ~17.5% (organic meta-analyses: Ponisio 2015 -19%; de Ponti
    ///         2012 -20%, wheat -27%; Seufert 2012 -25%). Above I=1.0 the
    ///         response plateaus (+5% at I=2.0): over-fertilising barely
    ///         adds yield.</item>
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

        // Intensity -> yield: CONCAVE (quadratic-plateau / Mitscherlich N-response).
        // Below reference (1.0) the penalty grows with the SQUARE of the cut
        // depth: a -20% cut (I=0.8) loses ~2.8% yield, a -50% cut (I=0.5) ~17.5%.
        // Above reference a small linear gain that plateaus (over-fertilising
        // barely helps). Sources in the class docstring.
        private const double IntensityCutCurvature = 0.70;
        private const double IntensityOverGainPerUnit = 0.05;

        // Heat-stress term, additive on top of the scenario anomaly heat
        // penalty (chantier E2 / ADR #52). Captures the acute effect of
        // canicule episodes on yield via the daily WeatherStation reading,
        // which the scenario anomaly term (annual structural shift) can't
        // represent on its own. 0.3 %/day with a 30-day window caps the
        // penalty at 9 % — modest enough to keep the existing calibration
        // windows valid, large enough to be visible under the worst-case
        // scenario where July/August reach 25-29 °C peaks.
        private const double HeatStressPenaltyPerDay = 0.003;
        private const double HeatStressMaxPenalty = 0.09;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double hedgerowEffect = ComputeHedgerowEffect(model.HedgerowDensity);
            double waterEffect = ComputeWaterEffect(model.WaterTableDepth);
            double climateEffect = ComputeClimateEffect(
                scenario.TemperatureAnomalyC.Current,
                scenario.PrecipitationAnomalyPercent.Current);
            double intensityEffect = ComputeIntensityEffect(scenario.InputIntensityFactor.Current);
            double heatStressEffect = ComputeHeatStressEffect(model.RecentHeatDayCount);

            double target = BaselineTonnesPerHectare
                            * hedgerowEffect
                            * waterEffect
                            * climateEffect
                            * intensityEffect
                            * heatStressEffect;
            if (target < 0.0) target = 0.0;

            double current = model.CropYield;
            double next = current + TransitionRatePerDay * (target - current);
            model.SetCropYield(next);
        }

        /// <summary>
        /// Acute heat-stress multiplier driven by the rolling count of days
        /// above <see cref="EcosystemModel.HeatDayThresholdCelsius"/>
        /// (25 °C) over the last
        /// <see cref="EcosystemModel.HeatDayWindowDays"/> (30) days.
        /// Linear penalty capped at 9 %.
        /// </summary>
        public static double ComputeHeatStressEffect(int recentHeatDayCount)
        {
            if (recentHeatDayCount <= 0) return 1.0;
            double penalty = HeatStressPenaltyPerDay * recentHeatDayCount;
            if (penalty > HeatStressMaxPenalty) penalty = HeatStressMaxPenalty;
            return 1.0 - penalty;
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

        /// <summary>
        /// Concave (quadratic-plateau / Mitscherlich) yield response to the
        /// input-intensity factor. Below reference intensity (1.0) the penalty
        /// grows with the SQUARE of the cut depth, so the first reductions cost
        /// little yield (the flat top of the N-response curve) and deeper cuts
        /// cost progressively more; above reference the response plateaus.
        /// Calibrated to -2.8% at I=0.8 and -17.5% at I=0.5. Pure, tested.
        /// </summary>
        public static double ComputeIntensityEffect(double intensityFactor)
        {
            double effect;
            if (intensityFactor <= 1.0)
            {
                double cut = 1.0 - intensityFactor;            // 0 at reference, 0.5 at floor
                effect = 1.0 - IntensityCutCurvature * cut * cut;
            }
            else
            {
                effect = 1.0 + IntensityOverGainPerUnit * (intensityFactor - 1.0);
            }
            if (effect < 0.5) effect = 0.5;
            if (effect > 1.1) effect = 1.1;
            return effect;
        }
    }
}
