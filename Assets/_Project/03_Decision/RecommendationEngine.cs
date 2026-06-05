using System;
using System.Collections.Generic;
using Bocage.Decision.Outcomes;
using Bocage.Decision.Recommendations;
using Bocage.Sensors;
using Bocage.Sensors.Events;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.Decision
{
    /// <summary>
    /// Couche 3 engine that translates each unaddressed event from the
    /// <see cref="EventLog"/> into at most one <see cref="IRecommendation"/>,
    /// choosing the lever that best serves the farmer FROM THE CURRENT STATE.
    /// Stateless — the dedup logic queries the <see cref="DecisionJournal"/>
    /// for the set of events already covered, so running the engine repeatedly
    /// never produces duplicates and is safe to call every tick.
    /// <para>
    /// <b>Model-derived selection (chantier modèle vivant / A1).</b> For each
    /// event the engine builds the FEASIBLE levers (the headroom guards keep
    /// CLAUDE.md §17: no impossible action), projects each one forward on a
    /// copy of the real state (<see cref="ModelOutcomeProjector"/>), and keeps
    /// the lever that best improves the farmer objective
    /// (<see cref="FarmerObjective"/>, economy-dominant). The choice is no
    /// longer a fixed priority list: it emerges from the real ΔKPI the lever
    /// produces in THIS state.
    /// <list type="bullet">
    ///   <item>Drought → irrigation (the single water lever).</item>
    ///   <item>Fauna anomaly → the most economically-sensible feasible
    ///         habitat/input lever (lower inputs, stop grubbing, plant hedges).</item>
    ///   <item>Soil carbon low → cover crops or residue restitution, whichever
    ///         the projection rates higher.</item>
    ///   <item>Low profitability → raise inputs or thin over-dense hedges, but
    ///         only when the projection shows a real profit gain. This is what
    ///         REPLACES the old hardcoded profit-optimum: above the optimum,
    ///         raising inputs projects a loss and is gated out.</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class RecommendationEngine
    {
        // Negligible headroom above the input-intensity floor: at or below this,
        // "reduce inputs" cannot move the model, so the fauna response falls
        // through to a habitat lever instead.
        private const double IntensityFloorTolerance = 0.01;

        // A hedge-removal rate (m/ha/yr) above this counts as "actively grubbing",
        // so the cheapest habitat move is to slow it before planting anew.
        private const double HedgeRemovalActiveTolerance = 0.5;

        // Above this hedge density (m/ha) the fauna habitat factor has saturated
        // (FaunaDynamicsRule caps around 180), so planting more no longer helps —
        // the low fauna must have another cause.
        private const double HedgeHabitatSaturationMeters = 180.0;

        // A coverage lever (cover crops / residues, in %) counts as "not yet full"
        // below this, leaving meaningful room for the reco to raise it.
        private const double CoveragePercentFullTolerance = 99.0;

        // Biodiversity composite below this is "ecologically critical": the engine
        // will not trade it away for profit, and the surfacing escalates a costly
        // ecological fix to a popup. Shared with RecommendationSurfacing.
        public const double BiodiversityCriticalThreshold = 0.30;
        // Hedge density (m/ha) clearly above the agronomic optimum (~90): beyond
        // it, extra hedges start costing yield via the bell penalty.
        private const double HedgeOverdenseThresholdMeters = 120.0;
        // PSE rate (EUR/m/yr) below which hedges are weakly subsidised, so an
        // over-dense stand can cost more (maintenance + yield) than it returns.
        private const double PseLowThresholdEurosPerMeter = 1.0;

        /// <summary>
        /// Walks the <paramref name="eventLog"/> and returns the recommendations
        /// to issue now, given the <paramref name="journal"/> of past decisions
        /// and the current <paramref name="scenario"/> + <paramref name="model"/>
        /// state. The lever selection projects candidates forward, so the engine
        /// needs the run's <paramref name="masterSeed"/> and
        /// <paramref name="weather"/> plus the KPI evaluators
        /// (<paramref name="profitFn"/>, <paramref name="biodivFn"/>) the Couche 05
        /// runner wires to the Couche 04 indicators. Already-issued events are not
        /// reissued.
        /// </summary>
        public IReadOnlyList<IRecommendation> ProduceRecommendations(
            EventLog eventLog, DecisionJournal journal, ScenarioContext scenario, EcosystemModel model,
            ulong masterSeed, SeasonalWeatherData weather,
            Func<EcosystemModel, ScenarioContext, double> profitFn,
            Func<EcosystemModel, ScenarioContext, double> biodivFn)
        {
            var result = new List<IRecommendation>();
            if (eventLog == null) return result;

            for (int i = 0; i < eventLog.Events.Count; i++)
            {
                var ev = eventLog.Events[i];
                string eventInstanceId = MakeEventInstanceId(ev);
                if (journal != null && journal.IsEventCovered(eventInstanceId)) continue;

                var rec = TryProduceFor(ev, scenario, model, masterSeed, weather, profitFn, biodivFn);
                if (rec != null) result.Add(rec);
                // Declined (no feasible / worthwhile lever): record it as considered
                // so the forward projection is not re-run for this event every tick.
                else if (journal != null) journal.MarkEventConsidered(eventInstanceId);
            }
            return result;
        }

        /// <summary>
        /// Single-event mapping. Consults the current <paramref name="scenario"/>
        /// and <paramref name="model"/> for the feasible levers, then projects
        /// them forward (<paramref name="masterSeed"/> / <paramref name="weather"/>
        /// + the KPI evaluators) to pick the one that best serves the farmer.
        /// Returns null when the event maps to no recommendation or when every
        /// relevant lever is exhausted. Exposed for tests.
        /// </summary>
        public static IRecommendation TryProduceFor(
            IEvent ev, ScenarioContext scenario, EcosystemModel model,
            ulong masterSeed, SeasonalWeatherData weather,
            Func<EcosystemModel, ScenarioContext, double> profitFn,
            Func<EcosystemModel, ScenarioContext, double> biodivFn)
        {
            if (ev == null) return null;
            string instanceId = MakeEventInstanceId(ev);
            switch (ev)
            {
                case DroughtProlongedEvent _:
                    return new IrrigationAdviceRecommendation(ev.DetectedOnDay, instanceId);
                case FaunaAcousticAnomalyEvent _:
                    return ChooseFaunaResponse(ev.DetectedOnDay, instanceId, scenario, model, masterSeed, weather, profitFn, biodivFn);
                case SoilCarbonLowEvent _:
                    return ChooseSoilCarbonResponse(ev.DetectedOnDay, instanceId, scenario, model, masterSeed, weather, profitFn, biodivFn);
                case LowProfitabilityEvent lpe:
                    return ChooseEconomicResponse(ev.DetectedOnDay, instanceId, scenario, model, lpe.BiodiversityAtDetection, masterSeed, weather, profitFn, biodivFn);
                default:
                    return null;
            }
        }

        // Fauna anomaly: among the feasible habitat/input levers, the one the
        // farmer objective rates highest (cheapest ecological win first, but
        // derived from the real projection, not a fixed order).
        private static IRecommendation ChooseFaunaResponse(
            int day, string evtId, ScenarioContext scenario, EcosystemModel model,
            ulong masterSeed, SeasonalWeatherData weather,
            Func<EcosystemModel, ScenarioContext, double> profitFn,
            Func<EcosystemModel, ScenarioContext, double> biodivFn)
        {
            if (scenario == null) return new ReduceInputsRecommendation(day, evtId);

            var candidates = new List<IRecommendation>();
            // 1. Lower inputs — only while there is room above the organic floor.
            if (scenario.InputIntensityFactor.Current
                > ReduceInputsRecommendation.MinInputIntensityFactor + IntensityFloorTolerance)
                candidates.Add(new ReduceInputsRecommendation(day, evtId));
            // 2. Stop active hedge grubbing (no capital).
            if (scenario.HedgeRemovalRate.Current > HedgeRemovalActiveTolerance)
                candidates.Add(new ReduceHedgeRemovalRecommendation(day, evtId));
            // 3. Build habitat by planting, unless hedges have saturated.
            if (model != null && model.HedgerowDensity < HedgeHabitatSaturationMeters)
                candidates.Add(new PlantHedgesRecommendation(day, evtId));

            // Every habitat/input lever exhausted → the low fauna has another
            // cause (water, climate). No impossible action (§17).
            return BestByUtility(candidates, model, scenario, masterSeed, weather, profitFn, biodivFn).rec;
        }

        // Soil carbon low: rebuild it; cover crops or residue restitution,
        // whichever the projection rates higher for the farmer.
        private static IRecommendation ChooseSoilCarbonResponse(
            int day, string evtId, ScenarioContext scenario, EcosystemModel model,
            ulong masterSeed, SeasonalWeatherData weather,
            Func<EcosystemModel, ScenarioContext, double> profitFn,
            Func<EcosystemModel, ScenarioContext, double> biodivFn)
        {
            if (scenario == null) return new SowCoverCropsRecommendation(day, evtId);

            var candidates = new List<IRecommendation>();
            if (scenario.CoverCropsCoveragePercent.Current < CoveragePercentFullTolerance)
                candidates.Add(new SowCoverCropsRecommendation(day, evtId));
            if (scenario.ResidueRestitutionPercent.Current < CoveragePercentFullTolerance)
                candidates.Add(new RestoreResidueRecommendation(day, evtId));

            return BestByUtility(candidates, model, scenario, masterSeed, weather, profitFn, biodivFn).rec;
        }

        // Low profitability: the economic counterweight. Pick the feasible lever
        // the projection rates highest, but never trade away already-critical
        // fauna for margin, and only fire when it actually gains profit (a farmer
        // doesn't act to lose margin). The « only if profit gains » gate is what
        // replaces the old hardcoded profit-optimum 0.8: above the optimum,
        // raising inputs projects a loss and is gated out here.
        private static IRecommendation ChooseEconomicResponse(
            int day, string evtId, ScenarioContext scenario, EcosystemModel model, double biodiversity,
            ulong masterSeed, SeasonalWeatherData weather,
            Func<EcosystemModel, ScenarioContext, double> profitFn,
            Func<EcosystemModel, ScenarioContext, double> biodivFn)
        {
            if (scenario == null || model == null) return null;
            if (biodiversity < BiodiversityCriticalThreshold) return null;

            var candidates = new List<IRecommendation>();
            // Raise inputs while there is physical room below the intensive cap;
            // the projection decides whether it pays.
            if (scenario.InputIntensityFactor.Current
                < RaiseInputsRecommendation.MaxInputIntensityFactor - IntensityFloorTolerance)
                candidates.Add(new RaiseInputsRecommendation(day, evtId));
            // Over-dense, weakly-subsidised hedges → thin them (narrow corner).
            if (model.HedgerowDensity > HedgeOverdenseThresholdMeters
                && scenario.PseSubsidyRate.Current < PseLowThresholdEurosPerMeter)
                candidates.Add(new IncreaseHedgeRemovalRecommendation(day, evtId));

            var best = BestByUtility(candidates, model, scenario, masterSeed, weather, profitFn, biodivFn);
            if (best.rec == null) return null;
            // Gate on a real projected profit gain (skip only when the projection
            // is available; the heuristic fallback fires the first feasible).
            if (profitFn != null && biodivFn != null && best.longTerm.ProfitDeltaExpected <= 0.0) return null;
            return best.rec;
        }

        // Ranks feasible candidates by the farmer objective (ΔU from the forward
        // projection) and returns the best with its long-term outcome. Falls back
        // to the first feasible candidate when the KPI delegates are absent
        // (defensive — the real runner always supplies them).
        private static (IRecommendation rec, OutcomeDistribution longTerm) BestByUtility(
            List<IRecommendation> candidates,
            EcosystemModel model, ScenarioContext scenario, ulong masterSeed, SeasonalWeatherData weather,
            Func<EcosystemModel, ScenarioContext, double> profitFn,
            Func<EcosystemModel, ScenarioContext, double> biodivFn)
        {
            if (candidates == null || candidates.Count == 0) return (null, default(OutcomeDistribution));
            if (profitFn == null || biodivFn == null || model == null || scenario == null)
                return (candidates[0], default(OutcomeDistribution)); // heuristic fallback: first feasible

            IRecommendation best = null;
            OutcomeDistribution bestLong = default(OutcomeDistribution);
            double bestUtility = double.NegativeInfinity;
            for (int i = 0; i < candidates.Count; i++)
            {
                var outcomes = ModelOutcomeProjector.Project(
                    candidates[i], model, scenario, masterSeed, weather, profitFn, biodivFn);
                var longTerm = outcomes[outcomes.Length - 1];
                double utility = FarmerObjective.DeltaUtility(longTerm);
                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    best = candidates[i];
                    bestLong = longTerm;
                }
            }
            return (best, bestLong);
        }

        /// <summary>
        /// Composes a stable per-occurrence id for the event by mixing its
        /// <c>Id</c> (event type) and its <c>DetectedOnDay</c>. Used as the dedup
        /// key in the journal.
        /// </summary>
        public static string MakeEventInstanceId(IEvent ev)
        {
            return ev.Id + "#" + ev.DetectedOnDay;
        }
    }
}
