using System.Collections.Generic;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Fixed-capacity, newest-first store of recent log lines backing the
    /// activity console. Pure C# (no UnityEngine) so the capping logic is
    /// covered by EditMode tests; the MonoBehaviour <see cref="ConsoleBinding"/>
    /// owns one instance and renders <see cref="Lines"/> into the dashboard.
    /// </summary>
    public sealed class ConsoleLogBuffer
    {
        private readonly List<string> _lines = new List<string>();

        /// <summary>Maximum retained lines; the oldest drop off beyond this.</summary>
        public int Capacity { get; }

        public ConsoleLogBuffer(int capacity)
        {
            // A console with no room makes no sense; clamp to at least one.
            Capacity = capacity < 1 ? 1 : capacity;
        }

        /// <summary>Lines from newest (index 0) to oldest. Never null.</summary>
        public IReadOnlyList<string> Lines => _lines;

        /// <summary>
        /// Pushes a line as the new newest entry, dropping the oldest once
        /// <see cref="Capacity"/> is exceeded. A null line becomes "".
        /// </summary>
        public void Add(string line)
        {
            _lines.Insert(0, line ?? string.Empty);
            if (_lines.Count > Capacity)
            {
                _lines.RemoveAt(_lines.Count - 1);
            }
        }
    }
}
