using System.Collections.Generic;
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
    /// choosing the lever that best moves the bocage toward balance FROM THE
    /// CURRENT STATE. Stateless — the dedup logic queries the
    /// <see cref="DecisionJournal"/> for the set of events already covered, so
    /// running the engine repeatedly never produces duplicates and is safe to
    /// call every tick.
    /// <para>
    /// The dispatch is state-aware (chantier E9):
    /// <list type="bullet">
    ///   <item>Drought → irrigation.</item>
    ///   <item>Fauna anomaly → the habitat/input lever WITH HEADROOM, in order of
    ///         speed and cost: lower inputs (fastest fauna lever, Hallmann 2017)
    ///         if above the organic-extensive floor; else stop active hedge
    ///         removal; else plant hedges; else stay silent (the low fauna has
    ///         another cause — CLAUDE.md §17, no impossible action).</item>
    ///   <item>Soil carbon low (eddy tower) → rebuild it: cover crops if there is
    ///         room (best documented, INRAE 4 pour 1000), else residue restitution.</item>
    /// </list>
    /// The economic counter-recommendations (raise inputs, thin hedges) are
    /// state-triggered on abnormally low profitability rather than on a sensor
    /// event, and are added in a later increment.
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

        // Biodiversity composite below this is "ecologically critical" — the
        // engine will not offer to trade it away for profit.
        private const double BiodiversityCriticalThreshold = 0.30;
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
        /// state (consulted for the state-aware dispatch). Already-issued events
        /// are not reissued.
        /// </summary>
        public IReadOnlyList<IRecommendation> ProduceRecommendations(
            EventLog eventLog, DecisionJournal journal, ScenarioContext scenario, EcosystemModel model)
        {
            var result = new List<IRecommendation>();
            if (eventLog == null) return result;

            for (int i = 0; i < eventLog.Events.Count; i++)
            {
                var ev = eventLog.Events[i];
                string eventInstanceId = MakeEventInstanceId(ev);
                if (journal != null && journal.IsEventCovered(eventInstanceId)) continue;

                var rec = TryProduceFor(ev, scenario, model);
                if (rec != null) result.Add(rec);
            }
            return result;
        }

        /// <summary>
        /// Single-event mapping, consulting the current <paramref name="scenario"/>
        /// and <paramref name="model"/> to pick the lever with headroom. Returns
        /// null when the event maps to no recommendation or when every relevant
        /// lever is exhausted. Exposed for tests.
        /// </summary>
        public static IRecommendation TryProduceFor(IEvent ev, ScenarioContext scenario, EcosystemModel model)
        {
            if (ev == null) return null;
            string instanceId = MakeEventInstanceId(ev);
            switch (ev)
            {
                case DroughtProlongedEvent _:
                    return new IrrigationAdviceRecommendation(ev.DetectedOnDay, instanceId);
                case FaunaAcousticAnomalyEvent _:
                    return ChooseFaunaResponse(ev.DetectedOnDay, instanceId, scenario, model);
                case SoilCarbonLowEvent _:
                    return ChooseSoilCarbonResponse(ev.DetectedOnDay, instanceId, scenario);
                case LowProfitabilityEvent lpe:
                    return ChooseEconomicResponse(ev.DetectedOnDay, instanceId, scenario, model, lpe.BiodiversityAtDetection);
                default:
                    return null;
            }
        }

        // Fauna anomaly: the habitat/input lever with real headroom, fastest first.
        private static IRecommendation ChooseFaunaResponse(
            int day, string evtId, ScenarioContext scenario, EcosystemModel model)
        {
            // No scenario to inspect: fall back to the canonical input lever.
            if (scenario == null) return new ReduceInputsRecommendation(day, evtId);

            // 1. Lower inputs — the fastest lever on farmland fauna (Hallmann
            //    2017) — while there is room above the organic-extensive floor.
            if (scenario.InputIntensityFactor.Current
                > ReduceInputsRecommendation.MinInputIntensityFactor + IntensityFloorTolerance)
            {
                return new ReduceInputsRecommendation(day, evtId);
            }

            // 2. Inputs floored: stop active hedge grubbing first (no capital).
            if (scenario.HedgeRemovalRate.Current > HedgeRemovalActiveTolerance)
            {
                return new ReduceHedgeRemovalRecommendation(day, evtId);
            }

            // 3. Else build habitat by planting, unless hedges have saturated.
            if (model != null && model.HedgerowDensity < HedgeHabitatSaturationMeters)
            {
                return new PlantHedgesRecommendation(day, evtId);
            }

            // 4. Every habitat/input lever is exhausted — the low fauna has
            //    another cause (water, climate). No impossible action (§17).
            return null;
        }

        // Soil carbon low: rebuild it, cover crops first then residue restitution.
        private static IRecommendation ChooseSoilCarbonResponse(int day, string evtId, ScenarioContext scenario)
        {
            if (scenario == null) return new SowCoverCropsRecommendation(day, evtId);
            if (scenario.CoverCropsCoveragePercent.Current < CoveragePercentFullTolerance)
                return new SowCoverCropsRecommendation(day, evtId);
            if (scenario.ResidueRestitutionPercent.Current < CoveragePercentFullTolerance)
                return new RestoreResidueRecommendation(day, evtId);
            return null;
        }

        // Low profitability: the economic counterweight. Steer toward the profit
        // optimum from whichever side, but never trade away already-critical fauna
        // for margin — when biodiversity is critical only the ecological recos
        // (which interrupt) should fire.
        private static IRecommendation ChooseEconomicResponse(
            int day, string evtId, ScenarioContext scenario, EcosystemModel model, double biodiversity)
        {
            if (scenario == null || model == null) return null;
            if (biodiversity < BiodiversityCriticalThreshold) return null;

            // 1. Over-extensified below the profit optimum -> nudge inputs up.
            if (scenario.InputIntensityFactor.Current
                < RaiseInputsRecommendation.ProfitOptimalIntensityFactor - IntensityFloorTolerance)
            {
                return new RaiseInputsRecommendation(day, evtId);
            }

            // 2. Over-dense, weakly-subsidised hedges -> thin them (narrow corner).
            if (model.HedgerowDensity > HedgeOverdenseThresholdMeters
                && scenario.PseSubsidyRate.Current < PseLowThresholdEurosPerMeter)
            {
                return new IncreaseHedgeRemovalRecommendation(day, evtId);
            }

            return null;
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
