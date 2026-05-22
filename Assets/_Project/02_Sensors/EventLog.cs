using System.Collections.Generic;
using Bocage.Sensors.Events;

namespace Bocage.Sensors
{
    /// <summary>
    /// Append-only chronological history of every <see cref="IEvent"/>
    /// emitted by <see cref="EventDetector"/> during a run. Listeners
    /// (Couche 3 recommendation engine, Couche 5 decision panel) read
    /// the log; only the detector writes to it.
    /// <para>
    /// Pure C# data structure, no Unity dependency, no allocation per
    /// query (the read-only view is returned once and points at the
    /// underlying list).
    /// </para>
    /// </summary>
    public sealed class EventLog
    {
        private readonly List<IEvent> _events = new List<IEvent>();

        /// <summary>Read-only view of the events in chronological order.</summary>
        public IReadOnlyList<IEvent> Events => _events;

        /// <summary>Total number of events recorded so far.</summary>
        public int Count => _events.Count;

        /// <summary>
        /// Appends a new event to the log. Returns the index assigned
        /// to it (current count after insertion - 1) so callers can
        /// reference it later from telemetry or recommendation chains.
        /// </summary>
        public int Append(IEvent e)
        {
            _events.Add(e);
            return _events.Count - 1;
        }

        /// <summary>
        /// Returns the latest event of the requested concrete type, or
        /// null if none has been recorded. Used by the detector itself
        /// to enforce per-type cooldowns without scanning the whole log
        /// (the iteration is bounded by recent history; for typical
        /// runs the log has few dozen entries).
        /// </summary>
        public T LatestOfType<T>() where T : class, IEvent
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                if (_events[i] is T match) return match;
            }
            return null;
        }
    }
}
