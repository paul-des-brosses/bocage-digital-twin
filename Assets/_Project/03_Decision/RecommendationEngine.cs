using System.Collections.Generic;
using Bocage.Decision.Recommendations;
using Bocage.Sensors;
using Bocage.Sensors.Events;
using Bocage.SimulationCore.Scenario;

namespace Bocage.Decision
{
    /// <summary>
    /// Couche 3 engine that translates each unaddressed event from the
    /// <see cref="EventLog"/> into at most one <see cref="IRecommendation"/>.
    /// Stateless — the dedup logic queries the <see cref="DecisionJournal"/>
    /// for the set of events already covered by a recommendation, so
    /// running the engine repeatedly never produces duplicates and is
    /// safe to call every tick.
    /// <para>
    /// The mapping is one-to-one (drought → irrigation, fauna anomaly →
    /// reduce inputs), but a mapping may DECLINE to produce when the action
    /// would be incoherent in the current state. The fauna-anomaly →
    /// reduce-inputs rule is suppressed when the input intensity is already at
    /// its organic-extensive floor: the lever is exhausted, a productive farm
    /// cannot cut inputs further, and the low fauna therefore has another cause
    /// (habitat, water). Showing an impossible action would violate CLAUDE.md
    /// §17 (no recommendation without a coherent, honestly actionable rationale).
    /// </para>
    /// </summary>
    public sealed class RecommendationEngine
    {
        // Negligible headroom above the input-intensity floor: at or below this,
        // a "reduce inputs" advice could not produce a meaningful cut, so it is
        // suppressed rather than shown as an action that cannot move.
        private const double IntensityFloorTolerance = 0.01;

        /// <summary>
        /// Walks the <paramref name="eventLog"/> and returns the
        /// recommendations that should be issued now, given the
        /// <paramref name="journal"/> of past decisions and the current
        /// <paramref name="scenario"/> state (consulted for coherence guards).
        /// Recommendations already issued (whether Accepted, Rejected,
        /// AutoAccepted or still Pending in the journal) are NOT reissued —
        /// they're considered "addressed".
        /// </summary>
        public IReadOnlyList<IRecommendation> ProduceRecommendations(
            EventLog eventLog, DecisionJournal journal, ScenarioContext scenario)
        {
            var result = new List<IRecommendation>();
            if (eventLog == null) return result;

            for (int i = 0; i < eventLog.Events.Count; i++)
            {
                var ev = eventLog.Events[i];
                string eventInstanceId = MakeEventInstanceId(ev);
                if (journal != null && journal.IsEventCovered(eventInstanceId)) continue;

                var rec = TryProduceFor(ev, scenario);
                if (rec != null) result.Add(rec);
            }
            return result;
        }

        /// <summary>
        /// Single-event mapping, consulting the current <paramref name="scenario"/>
        /// for coherence guards. Returns null when the event maps to no
        /// recommendation, or when the mapped action would be incoherent in the
        /// current state (e.g. lowering inputs already at the floor). A null
        /// scenario disables the guards (the mapping produces as before).
        /// Exposed for tests that want to verify the dispatch without going
        /// through the log.
        /// </summary>
        public static IRecommendation TryProduceFor(IEvent ev, ScenarioContext scenario)
        {
            if (ev == null) return null;
            string instanceId = MakeEventInstanceId(ev);
            switch (ev)
            {
                case DroughtProlongedEvent _:
                    return new IrrigationAdviceRecommendation(ev.DetectedOnDay, instanceId);
                case FaunaAcousticAnomalyEvent _:
                    // Coherence guard (§17): only advise lowering inputs if there
                    // is real headroom above the organic-extensive floor. At the
                    // floor the lever is exhausted — the low fauna has another
                    // cause (habitat, water), so we do not nag with an action
                    // that cannot move the model.
                    if (scenario != null &&
                        scenario.InputIntensityFactor.Current
                            <= ReduceInputsRecommendation.MinInputIntensityFactor + IntensityFloorTolerance)
                    {
                        return null;
                    }
                    return new ReduceInputsRecommendation(ev.DetectedOnDay, instanceId);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Composes a stable per-occurrence id for the event by mixing
        /// its <c>Id</c> (event type) and its <c>DetectedOnDay</c>
        /// (occurrence within the run). Used as the dedup key in the
        /// journal so the same drought detection doesn't yield two
        /// recommendations across re-runs of the engine.
        /// </summary>
        public static string MakeEventInstanceId(IEvent ev)
        {
            return ev.Id + "#" + ev.DetectedOnDay;
        }
    }
}
