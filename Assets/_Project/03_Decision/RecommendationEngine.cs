using System.Collections.Generic;
using Bocage.Decision.Recommendations;
using Bocage.Sensors;
using Bocage.Sensors.Events;

namespace Bocage.Decision
{
    /// <summary>
    /// Couche 3 engine that translates each unaddressed event from the
    /// <see cref="EventLog"/> into exactly one <see cref="IRecommendation"/>.
    /// Stateless — the dedup logic queries the <see cref="DecisionJournal"/>
    /// for the set of events already covered by a recommendation, so
    /// running the engine repeatedly never produces duplicates and is
    /// safe to call every tick.
    /// <para>
    /// The mapping is currently one-to-one (chalara → plant hedges,
    /// drought → irrigation, fauna anomaly → reduce inputs). Future
    /// versions could produce multiple recs per event, or rank them by
    /// urgency. Out of scope for sub-étape 8c.2.
    /// </para>
    /// </summary>
    public sealed class RecommendationEngine
    {
        /// <summary>
        /// Walks the <paramref name="eventLog"/> and returns the
        /// recommendations that should be issued now, given the
        /// <paramref name="journal"/> of past decisions. Recommendations
        /// already issued (whether Accepted, Rejected, AutoAccepted or
        /// still Pending in the journal) are NOT reissued — they're
        /// considered "addressed".
        /// </summary>
        public IReadOnlyList<IRecommendation> ProduceRecommendations(EventLog eventLog, DecisionJournal journal)
        {
            var result = new List<IRecommendation>();
            if (eventLog == null) return result;

            for (int i = 0; i < eventLog.Events.Count; i++)
            {
                var ev = eventLog.Events[i];
                string eventInstanceId = MakeEventInstanceId(ev);
                if (journal != null && journal.IsEventCovered(eventInstanceId)) continue;

                var rec = TryProduceFor(ev);
                if (rec != null) result.Add(rec);
            }
            return result;
        }

        /// <summary>
        /// Single-event mapping. Exposed for tests that want to verify
        /// the dispatch without going through the log.
        /// </summary>
        public static IRecommendation TryProduceFor(IEvent ev)
        {
            if (ev == null) return null;
            string instanceId = MakeEventInstanceId(ev);
            switch (ev)
            {
                case HedgeChalaraEvent _:           return new PlantHedgesRecommendation(ev.DetectedOnDay, instanceId);
                case DroughtProlongedEvent _:       return new IrrigationAdviceRecommendation(ev.DetectedOnDay, instanceId);
                case FaunaAcousticAnomalyEvent _:   return new ReduceInputsRecommendation(ev.DetectedOnDay, instanceId);
                default:                            return null;
            }
        }

        /// <summary>
        /// Composes a stable per-occurrence id for the event by mixing
        /// its <c>Id</c> (event type) and its <c>DetectedOnDay</c>
        /// (occurrence within the run). Used as the dedup key in the
        /// journal so the same chalara detection doesn't yield two
        /// recommendations across re-runs of the engine.
        /// </summary>
        public static string MakeEventInstanceId(IEvent ev)
        {
            return ev.Id + "#" + ev.DetectedOnDay;
        }
    }
}
