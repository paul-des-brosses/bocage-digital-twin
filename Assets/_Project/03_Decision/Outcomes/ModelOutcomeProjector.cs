using System;
using Bocage.Decision.Recommendations;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.Decision.Outcomes
{
    /// <summary>
    /// Projects the impact of a recommendation by actually SIMULATING it
    /// forward on a copy of the current state, instead of reading fixed
    /// coefficients. For each horizon (short 30 d, long 365 d) it runs, under
    /// several weather realisations:
    /// <list type="bullet">
    ///   <item>a "do nothing" baseline (current practices kept), and</item>
    ///   <item>the same world with the lever applied,</item>
    /// </list>
    /// both from the same seed and seasonal weather, and takes the difference
    /// in the hero KPIs. The projection is therefore state-dependent (a lever
    /// that is already exhausted projects ~0) and honest. The spread across
    /// weather realisations gives a worst / expected / best band that reflects
    /// real climate uncertainty rather than an arbitrary multiplier.
    /// <para>
    /// The KPI evaluation (profit, biodiversity) is injected as delegates so
    /// this Couche 03 type stays free of any Couche 04 dependency. The caller
    /// (Couche 05) wires <c>IntegratedProfitabilityIndicator.Compute</c> and
    /// <c>BiodiversityCompositeIndicator.Compute</c>. Read-only on the real
    /// model: it only ever ticks throwaway copies.
    /// </para>
    /// </summary>
    public static class ModelOutcomeProjector
    {
        public const int ShortHorizonDays = 30;
        public const int LongHorizonDays = 365;
        private const int WeatherRealisations = 3;

        public static OutcomeDistribution[] Project(
            IRecommendation recommendation,
            EcosystemModel model,
            ScenarioContext scenario,
            ulong masterSeed,
            SeasonalWeatherData weather,
            Func<EcosystemModel, ScenarioContext, double> profitFn,
            Func<EcosystemModel, ScenarioContext, double> biodivFn)
        {
            return new[]
            {
                ProjectAtHorizon(recommendation, model, scenario, masterSeed, weather, profitFn, biodivFn, ShortHorizonDays),
                ProjectAtHorizon(recommendation, model, scenario, masterSeed, weather, profitFn, biodivFn, LongHorizonDays),
            };
        }

        private static OutcomeDistribution ProjectAtHorizon(
            IRecommendation rec, EcosystemModel model, ScenarioContext scenario,
            ulong masterSeed, SeasonalWeatherData weather,
            Func<EcosystemModel, ScenarioContext, double> profitFn,
            Func<EcosystemModel, ScenarioContext, double> biodivFn,
            int horizonDays)
        {
            if (rec == null || model == null || scenario == null || profitFn == null || biodivFn == null)
                return new OutcomeDistribution(horizonDays, 0, 0, 0, 0, 0, 0);

            double magnitude = DefaultMagnitudeFor(rec);
            var profitDeltas = new double[WeatherRealisations];
            var biodivDeltas = new double[WeatherRealisations];

            for (int r = 0; r < WeatherRealisations; r++)
            {
                // Distinct but deterministic weather realisations.
                ulong seed = masterSeed + (ulong)r * 1000003UL;

                // Baseline: same state, current practices, no lever.
                var baseModel = Snapshot(model);
                var baseScenario = Snapshot(scenario);
                var baseEngine = DefaultSimulation.Build(seed, baseModel, baseScenario, weather);

                // Lever: same state and seed, lever applied to the copy.
                var leverModel = Snapshot(model);
                var leverScenario = Snapshot(scenario);
                AutoActionPipeline.ApplyOne(rec, leverModel, leverScenario, magnitude);
                var leverEngine = DefaultSimulation.Build(seed, leverModel, leverScenario, weather);

                for (int d = 0; d < horizonDays; d++)
                {
                    baseEngine.Tick();
                    leverEngine.Tick();
                }

                profitDeltas[r] = profitFn(leverModel, leverScenario) - profitFn(baseModel, baseScenario);
                biodivDeltas[r] = biodivFn(leverModel, leverScenario) - biodivFn(baseModel, baseScenario);
            }

            // Sort so worst = min, expected = median, best = max (per dimension);
            // guarantees worst <= expected <= best by construction.
            Array.Sort(profitDeltas);
            Array.Sort(biodivDeltas);
            int mid = WeatherRealisations / 2;
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: profitDeltas[0],
                profitDeltaExpected: profitDeltas[mid],
                profitDeltaBestCase: profitDeltas[WeatherRealisations - 1],
                biodiversityDeltaWorstCase: biodivDeltas[0],
                biodiversityDeltaExpected: biodivDeltas[mid],
                biodiversityDeltaBestCase: biodivDeltas[WeatherRealisations - 1]);
        }

        // Independent value-copy of the model state (the rolling heat windows
        // restart empty — acceptable for a short forward projection).
        private static EcosystemModel Snapshot(EcosystemModel m)
        {
            return new EcosystemModel(
                initialDay: m.CurrentDay,
                initialWeather: m.CurrentWeather,
                initialWaterTableDepth: m.WaterTableDepth,
                initialHedgerowDensity: m.HedgerowDensity,
                initialCropYield: m.CropYield,
                initialInputCost: m.InputCost,
                initialMaintenanceCost: m.MaintenanceCost,
                initialFaunaPopulation: m.FaunaPopulation,
                initialSoilCarbonStock: m.SoilCarbonStock);
        }

        // Fully independent scenario copy at the current values (no shared
        // references, so ticking the projection never touches the real run).
        private static ScenarioContext Snapshot(ScenarioContext s)
        {
            return new ScenarioContext(
                initialTemperatureAnomalyC: s.TemperatureAnomalyC.Current,
                initialPrecipitationAnomalyPercent: s.PrecipitationAnomalyPercent.Current,
                initialHedgeRemovalRate: s.HedgeRemovalRate.Current,
                initialInputIntensityFactor: s.InputIntensityFactor.Current,
                initialMaecCoveragePercent: s.MaecCoveragePercent.Current,
                initialPseSubsidyRate: s.PseSubsidyRate.Current,
                initialCoverCropsCoveragePercent: s.CoverCropsCoveragePercent.Current,
                initialResidueRestitutionPercent: s.ResidueRestitutionPercent.Current,
                startingMonth: s.StartingMonth,
                horizonInDays: s.HorizonInDays);
        }

        // The default per-step application size, the same constants the
        // AutoActionPipeline applies, so the projection matches a one-click action.
        private static double DefaultMagnitudeFor(IRecommendation rec)
        {
            switch (rec)
            {
                case PlantHedgesRecommendation _: return PlantHedgesRecommendation.HedgeRestoreMetersPerHectare;
                case IrrigationAdviceRecommendation _: return IrrigationAdviceRecommendation.WaterReliefDepthMeters;
                case ReduceInputsRecommendation _: return ReduceInputsRecommendation.IntensityCutPerStep;
                case RaiseInputsRecommendation _: return RaiseInputsRecommendation.IntensityRaisePerStep;
                case SowCoverCropsRecommendation _: return SowCoverCropsRecommendation.CoverageRaisePerStep;
                case RestoreResidueRecommendation _: return RestoreResidueRecommendation.RestitutionRaisePerStep;
                case ReduceHedgeRemovalRecommendation _: return ReduceHedgeRemovalRecommendation.RemovalCutPerStep;
                case IncreaseHedgeRemovalRecommendation _: return IncreaseHedgeRemovalRecommendation.RemovalRaisePerStep;
                default: return 0.0;
            }
        }
    }
}
