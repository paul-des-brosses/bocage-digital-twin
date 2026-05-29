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
            /// <summary>
            /// Magnitude of the action the user chose when accepting the
            /// recommendation. Meaningful only when
            /// <see cref="Verdict"/> is Accepted / AutoAccepted; ignored
            /// otherwise. Units depend on the recommendation type
            /// (m/ha for PlantHedges, m for Irrigation, intensity-unit
            /// for ReduceInputs).
            /// </summary>
            public double AppliedMagnitude { get; }

            public Entry(IRecommendation recommendation, DecisionVerdict verdict, int verdictSetOnDay, double appliedMagnitude = 0.0)
            {
                Recommendation = recommendation;
                Verdict = verdict;
                VerdictSetOnDay = verdictSetOnDay;
                AppliedMagnitude = appliedMagnitude;
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
        /// exists for the same triggering event (idempotent on event
        /// instance).
        /// <para>
        /// Per-type supersession (sub-étape 10a): if an existing entry
        /// of the same TYPE prefix is still in <see cref="DecisionVerdict.Pending"/>
        /// when the new one lands, the older entry is marked
        /// <see cref="DecisionVerdict.Superseded"/>. The journal thus
        /// holds at most one Pending per type at any instant — the
        /// latest — which keeps the history list from growing when
        /// the user has chosen to ignore a recurring detection.
        /// Already-resolved entries (Accepted / Rejected / AutoAccepted)
        /// are never touched here; they remain in the audit trail
        /// untouched. Manual actions (ADR #47) land as AutoAccepted,
        /// so they never trigger supersession of older Pending entries.
        /// </para>
        /// </summary>
        public bool Append(IRecommendation rec, int currentDay)
        {
            return Append(rec, currentDay, 0.0);
        }

        /// <summary>
        /// Overload that records an initial <paramref name="initialMagnitude"/>
        /// alongside the appended entry. Used by the manual-action
        /// pathway (ADR #47): the user has already chosen the slider
        /// value at click time, so the entry lands as
        /// <see cref="DecisionVerdict.AutoAccepted"/> with the magnitude
        /// baked in — no follow-up <see cref="SetVerdict"/> call needed.
        /// </summary>
        public bool Append(IRecommendation rec, int currentDay, double initialMagnitude)
        {
            if (rec == null) return false;
            // Dedup by triggering event id only when one is supplied.
            // Manual actions (ADR #47) carry TriggeredByEventId=null
            // and are explicitly cumulable — multiple appends share the
            // null key but each carries a unique rec.Id, so they coexist.
            if (!string.IsNullOrEmpty(rec.TriggeredByEventId)
                && _coveredEventIds.Contains(rec.TriggeredByEventId)) return false;

            // Type-level supersession of any older Pending entry. We
            // mark in place (struct entries) so the journal stays a
            // strictly-growing list — no removal, no reordering.
            string newTypePrefix = ExtractTypePrefix(rec.Id);
            if (newTypePrefix != null)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    var existing = _entries[i];
                    if (existing.Verdict != DecisionVerdict.Pending) continue;
                    if (ExtractTypePrefix(existing.Recommendation.Id) != newTypePrefix) continue;
                    _entries[i] = new Entry(
                        existing.Recommendation,
                        DecisionVerdict.Superseded,
                        currentDay,
                        existing.AppliedMagnitude);
                }
            }

            _entries.Add(new Entry(rec, rec.DefaultVerdict, currentDay, initialMagnitude));
            if (!string.IsNullOrEmpty(rec.TriggeredByEventId))
            {
                _coveredEventIds.Add(rec.TriggeredByEventId);
            }
            return true;
        }

        /// <summary>
        /// Recommendation ids follow the pattern <c>type#dayOrSalt</c>
        /// (cf. PlantHedgesRecommendation.Id and friends). Strip the
        /// suffix to compare by type. Returns null for null/empty input.
        /// </summary>
        private static string ExtractTypePrefix(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            int sep = id.IndexOf('#');
            return sep < 0 ? id : id.Substring(0, sep);
        }

        /// <summary>
        /// Resolves a pending entry with a final verdict. Returns true
        /// if an entry was updated, false if the id was not found or
        /// the entry was already resolved. The <paramref name="appliedMagnitude"/>
        /// is meaningful only when <paramref name="newVerdict"/> is
        /// Accepted / AutoAccepted (the user-chosen magnitude of the
        /// action); for Rejected, pass 0.
        /// </summary>
        public bool SetVerdict(string recommendationId, DecisionVerdict newVerdict, int currentDay, double appliedMagnitude = 0.0)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Recommendation.Id == recommendationId)
                {
                    _entries[i] = new Entry(_entries[i].Recommendation, newVerdict, currentDay, appliedMagnitude);
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
