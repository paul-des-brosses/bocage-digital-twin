using System;

namespace Bocage.SimulationCore
{
    /// <summary>
    /// A scalar parameter that interpolates linearly toward a new target over
    /// a configurable number of simulated days. Driven by Tick(), called once
    /// per simulated day. CLAUDE.md §15 requires that scenario parameter
    /// changes never apply abruptly — they spread over 7-14 simulated days.
    /// </summary>
    public sealed class TransitioningParameter<T>
    {
        private readonly Func<T, T, double, T> _lerp;
        private T _start;
        private T _target;
        private int _durationInDays;
        private int _elapsedInDays;

        public T Current { get; private set; }
        public T Target => _target;
        public bool IsTransitioning => _elapsedInDays < _durationInDays;

        public TransitioningParameter(T initial, Func<T, T, double, T> lerp)
        {
            if (lerp == null) throw new ArgumentNullException(nameof(lerp));
            _lerp = lerp;
            Current = initial;
            _start = initial;
            _target = initial;
            _durationInDays = 0;
            _elapsedInDays = 0;
        }

        public void SetTarget(T newTarget, int durationInDays)
        {
            if (durationInDays < 1)
                throw new ArgumentOutOfRangeException(nameof(durationInDays), "Must be >= 1.");
            _start = Current;
            _target = newTarget;
            _durationInDays = durationInDays;
            _elapsedInDays = 0;
        }

        public void Tick()
        {
            if (!IsTransitioning) return;

            _elapsedInDays++;
            if (_elapsedInDays >= _durationInDays)
            {
                Current = _target;
                return;
            }
            double t = (double)_elapsedInDays / _durationInDays;
            Current = _lerp(_start, _target, t);
        }
    }

    public static class TransitioningParameter
    {
        public static TransitioningParameter<double> ForDouble(double initial)
            => new TransitioningParameter<double>(initial, (a, b, t) => a + (b - a) * t);

        public static TransitioningParameter<float> ForFloat(float initial)
            => new TransitioningParameter<float>(initial, (a, b, t) => a + (b - a) * (float)t);
    }
}
