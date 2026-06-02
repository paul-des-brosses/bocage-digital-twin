using System.Collections.Generic;

namespace Bocage.Sensors
{
    /// <summary>
    /// Read-only view over a sensor's rolling measurement history — the
    /// sliding window that chantier E6 consumes uniformly across the
    /// inspection panel (ADR #53) and the Niveau B tabs (ADR #54),
    /// regardless of the sample type a given sensor records (a
    /// <c>Weather</c> struct, a scalar flux in kgCO2/ha/day, …).
    /// <para>
    /// The window is bounded: once <see cref="HistoryCount"/> reaches
    /// <see cref="Capacity"/>, recording a new sample evicts the oldest.
    /// Implementations pre-allocate their backing storage so reading and
    /// recording never allocate on the hot path (CLAUDE.md §6).
    /// </para>
    /// </summary>
    /// <typeparam name="T">The recorded sample type.</typeparam>
    public interface ISensorHistory<T>
    {
        /// <summary>Number of samples currently retained (0..<see cref="Capacity"/>).</summary>
        int HistoryCount { get; }

        /// <summary>Maximum number of samples retained before the oldest is evicted.</summary>
        int Capacity { get; }

        /// <summary>
        /// Copies the retained samples into <paramref name="destination"/> in
        /// chronological order (oldest first), clearing it first. Returns the
        /// number of samples written.
        /// </summary>
        int CopyHistoryTo(IList<T> destination);

        /// <summary>
        /// Gets the most recently recorded sample without copying the window.
        /// Returns <c>false</c> (and <paramref name="value"/> = default) when
        /// no sample has been recorded yet.
        /// </summary>
        bool TryGetLatest(out T value);
    }
}
