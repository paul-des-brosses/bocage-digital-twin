using System;
using System.Collections.Generic;

namespace Bocage.Sensors
{
    /// <summary>
    /// Fixed-capacity circular buffer implementing <see cref="ISensorHistory{T}"/>.
    /// One reusable sliding-window container shared by every sensor reader
    /// (ADR #53 / #54) so the ring-buffer arithmetic lives in a single, tested
    /// place instead of being re-implemented per reader.
    /// <para>
    /// The backing array is allocated once at construction; <see cref="Record"/>
    /// overwrites the oldest slot in O(1) once the window is full. No
    /// allocation occurs after construction (CLAUDE.md §6 — no per-frame
    /// allocation on the hot path).
    /// </para>
    /// </summary>
    /// <typeparam name="T">The recorded sample type.</typeparam>
    public sealed class RollingSensorHistory<T> : ISensorHistory<T>
    {
        private readonly T[] _buffer;
        private int _head;  // index of the next slot to write
        private int _count; // number of valid samples (≤ buffer length)

        /// <summary>Creates a window retaining at most <paramref name="capacity"/> samples.</summary>
        public RollingSensorHistory(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be strictly positive.");
            _buffer = new T[capacity];
        }

        /// <inheritdoc />
        public int Capacity => _buffer.Length;

        /// <inheritdoc />
        public int HistoryCount => _count;

        /// <summary>Appends a sample, evicting the oldest once the window is full.</summary>
        public void Record(in T sample)
        {
            _buffer[_head] = sample;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
        }

        /// <inheritdoc />
        public int CopyHistoryTo(IList<T> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            int oldest = _count < _buffer.Length ? 0 : _head;
            for (int i = 0; i < _count; i++)
            {
                destination.Add(_buffer[(oldest + i) % _buffer.Length]);
            }
            return _count;
        }

        /// <inheritdoc />
        public bool TryGetLatest(out T value)
        {
            if (_count == 0)
            {
                value = default;
                return false;
            }
            int latest = (_head - 1 + _buffer.Length) % _buffer.Length;
            value = _buffer[latest];
            return true;
        }

        /// <summary>Drops all samples; capacity is unchanged. Called on the runner's Rebuild.</summary>
        public void Clear()
        {
            _head = 0;
            _count = 0;
        }
    }
}
