using System.Collections.Generic;
using Bocage.Decision.Recommendations;

namespace Bocage.Decision
{
    /// <summary>
    /// Append-only history of recommendations issued during a run, with
    /// their current verdict. Drives the decision panel UI (Couche 5)
    /// and the auto-actions consumer that applies accepted
    /// recommendations to the real engine. Pure C# data structure,
    /// no Unity dependency.
    /// <para>
    /// Two read-only projections are exposed:
    /// <list type="bullet">
    ///   <item><see cref="Entries"/> — every entry in chronological order.</item>
    ///   <item><see cref="PendingEntries"/> — only entries with
    ///         <see cref="DecisionVerdict.Pending"/> verdict (i.e. those
    ///         awaiting arbitration in the decision panel).</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class DecisionJournal
    {
        public readonly struct Entry
        {
            public IRecommendation Recommendation { get; }
            public DecisionVerdict Verdict { get; }
            public int VerdictSetOnDay { get; }

            public Entry(IRecommendation recommendation, DecisionVerdict verdict, int verdictSetOnDay)
            {
                Recommendation = recommendation;
                Verdict = verdict;
                VerdictSetOnDay = verdictSetOnDay;
            }
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly HashSet<string> _coveredEventIds = new HashSet<string>();
        private readonly Dictionary<string, int> _appliedOnDayByRecId = new Dictionary<string, int>();

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>
        /// Number of unique events for which the journal already holds
        /// a recommendation (any verdict state). Exposed for telemetry
        /// and the decision panel summary.
        /// </summary>
        public int CoveredEventCount => _coveredEventIds.Count;

        /// <summary>
        /// True if a recommendation has already been appended to the
        /// journal for the given event instance id
        /// (cf <see cref="RecommendationEngine.MakeEventInstanceId"/>).
        /// Read by the engine to skip already-addressed events. O(1).
        /// </summary>
        public bool IsEventCovered(string eventInstanceId)
        {
            return eventInstanceId != null && _coveredEventIds.Contains(eventInstanceId);
        }

        /// <summary>
        /// Appends a brand-new recommendation to the journal at its
        /// default verdict. Returns false if a recommendation already
        /// exists for the same triggering event (idempotent).
        /// </summary>
        public bool Append(IRecommendation rec, int currentDay)
        {
            if (rec == null) return false;
            if (_coveredEventIds.Contains(rec.TriggeredByEventId)) return false;
            _entries.Add(new Entry(rec, rec.DefaultVerdict, currentDay));
            _coveredEventIds.Add(rec.TriggeredByEventId);
            return true;
        }

        /// <summary>
        /// Resolves a pending entry with a final verdict. Returns true
        /// if an entry was updated, false if the id was not found or
        /// the entry was already resolved.
        /// </summary>
        public bool SetVerdict(string recommendationId, DecisionVerdict newVerdict, int currentDay)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Recommendation.Id == recommendationId)
                {
                    _entries[i] = new Entry(_entries[i].Recommendation, newVerdict, currentDay);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Snapshot of all entries still awaiting arbitration. Allocates
        /// a fresh list per call — callers in the hot UI path should
        /// cache.
        /// </summary>
        public List<Entry> PendingEntries
        {
            get
            {
                var pending = new List<Entry>();
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].Verdict == DecisionVerdict.Pending) pending.Add(_entries[i]);
                }
                return pending;
            }
        }

        /// <summary>
        /// Snapshot of all entries already resolved (Accepted / Rejected
        /// / AutoAccepted), so the Couche 5 auto-actions can iterate
        /// and apply the Accepted/AutoAccepted ones.
        /// </summary>
        public List<Entry> ResolvedEntries
        {
            get
            {
                var resolved = new List<Entry>();
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].Verdict != DecisionVerdict.Pending) resolved.Add(_entries[i]);
                }
                return resolved;
            }
        }

        /// <summary>
        /// Records that the mechanical effect of the given recommendation
        /// has been applied to the real model on day
        /// <paramref name="currentDay"/>. The
        /// <see cref="Bocage.Decision.AutoActionPipeline"/> calls this
        /// after a successful application to guarantee idempotence:
        /// the same accepted rec is never re-applied. Returns false if
        /// the rec was already marked applied (caller should skip).
        /// </summary>
        public bool MarkApplied(string recommendationId, int currentDay)
        {
            if (recommendationId == null) return false;
            if (_appliedOnDayByRecId.ContainsKey(recommendationId)) return false;
            _appliedOnDayByRecId[recommendationId] = currentDay;
            return true;
        }

        /// <summary>True if <see cref="MarkApplied"/> has been called for this rec id.</summary>
        public bool IsApplied(string recommendationId)
        {
            return recommendationId != null && _appliedOnDayByRecId.ContainsKey(recommendationId);
        }

        /// <summary>
        /// Number of recommendations whose mechanical effect has been
        /// applied to the real model. Exposed for diagnostics.
        /// </summary>
        public int AppliedCount => _appliedOnDayByRecId.Count;
    }
}
