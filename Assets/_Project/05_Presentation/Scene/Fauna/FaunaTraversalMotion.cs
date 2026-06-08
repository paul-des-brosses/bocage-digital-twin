using System.Collections.Generic;
using Bocage.Presentation.Simulation;
using UnityEngine;

namespace Bocage.Presentation.Scene.Fauna
{
    /// <summary>
    /// Per-active-sprite component that drives one traversal of a
    /// <see cref="TrajectoryDefinition"/>: linear X interpolation from
    /// one off-screen endpoint to the other, vertical sinusoidal bob,
    /// sprite flip according to direction, wing-flap frame swap at
    /// constant FPS. Decoupled from the spawn driver
    /// (<see cref="FaunaPoolBinding"/>) — receives everything it needs
    /// via <see cref="Configure"/> + <see cref="StartTraversal"/> and
    /// signals completion via <see cref="IsFinished"/> so the pool can
    /// deactivate the GameObject without re-querying the SO.
    /// <para>
    /// CLAUDE.md §6 hot-path discipline: no allocations in Update, no
    /// boxing, no string formatting. Frame index via integer modulo,
    /// position update via <c>Mathf.Lerp</c> + <c>Mathf.Sin</c> only.
    /// SpriteRenderer reference cached at Awake.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FaunaTraversalMotion : MonoBehaviour
    {
        public enum Direction
        {
            LeftToRight,
            RightToLeft,
        }

        private SpriteRenderer _renderer;

        // Configuration cache (set once at Configure, immutable per traversal).
        private IReadOnlyList<Sprite> _frames;
        private float _framesPerSecond;
        private Vector2 _leftPoint;
        private Vector2 _rightPoint;
        private float _durationSec = 1f;  // non-zero default to avoid div-by-zero before Configure
        private float _bobAmplitude;
        private float _bobFrequencyHz;
        private bool _defaultFacesRight = true;
        private bool _configured;

        // Per-traversal state.
        private Direction _direction;
        private float _elapsed;
        private float _phaseOffset;
        private bool _isActive;

        public bool IsActive => _isActive;
        public bool IsFinished => _isActive && _elapsed >= _durationSec;
        public float ElapsedSec => _elapsed;
        public float DurationSec => _durationSec;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Configure the static trajectory + frame data for this sprite.
        /// Typically called once by <see cref="FaunaPool"/> after
        /// pre-instantiating the GameObject at Awake. Does NOT start the
        /// traversal — call <see cref="StartTraversal"/> for that.
        /// </summary>
        public void Configure(
            IReadOnlyList<Sprite> frames,
            float framesPerSecond,
            TrajectoryDefinition trajectory,
            bool defaultFacesRight = true)
        {
            _frames = frames;
            _framesPerSecond = framesPerSecond;
            _leftPoint = trajectory.leftPoint;
            _rightPoint = trajectory.rightPoint;
            _durationSec = Mathf.Max(0.01f, trajectory.durationSec);
            _bobAmplitude = trajectory.verticalBobAmplitude;
            _bobFrequencyHz = trajectory.verticalBobFrequencyHz;
            _defaultFacesRight = defaultFacesRight;
            _configured = true;
        }

        /// <summary>
        /// Activate this sprite for one traversal in the given direction.
        /// <paramref name="sinPhaseOffset"/> staggers the vertical bob
        /// phase between instances so two simultaneous birds don't sway
        /// in sync — typically derived from a seeded sub-stream by
        /// <see cref="FaunaPoolBinding"/>.
        /// </summary>
        public void StartTraversal(Direction direction, float sinPhaseOffset)
        {
            if (!_configured) return;
            _direction = direction;
            _elapsed = 0f;
            _phaseOffset = sinPhaseOffset;
            _isActive = true;

            if (_frames != null && _frames.Count > 0)
            {
                _renderer.sprite = _frames[0];
            }
            // XOR : flip the sprite iff its default facing disagrees with
            // the runtime direction. Default faces RIGHT + going RIGHT → no
            // flip ; default faces LEFT + going RIGHT → flip ; etc.
            bool goingRight = _direction == Direction.LeftToRight;
            _renderer.flipX = _defaultFacesRight != goingRight;
            ApplyTransformAt(0f);
        }

        /// <summary>
        /// Deactivate the traversal — called by the pool after
        /// <see cref="IsFinished"/> reads true. Idempotent.
        /// </summary>
        public void Stop()
        {
            _isActive = false;
        }

        /// <summary>
        /// Pure compute: position at a given elapsed time, exposed so the
        /// EditMode tests can verify linear X + sin Y without spinning a
        /// Unity Update loop.
        /// </summary>
        public Vector2 SamplePositionAt(float elapsed, Direction direction)
        {
            float t = Mathf.Clamp01(elapsed / _durationSec);
            Vector2 start = direction == Direction.LeftToRight ? _leftPoint : _rightPoint;
            Vector2 end = direction == Direction.LeftToRight ? _rightPoint : _leftPoint;
            float x = Mathf.Lerp(start.x, end.x, t);
            float y = Mathf.Lerp(start.y, end.y, t)
                    + _bobAmplitude * Mathf.Sin((elapsed * _bobFrequencyHz * 2f * Mathf.PI) + _phaseOffset);
            return new Vector2(x, y);
        }

        private void Update()
        {
            // Freeze on pause: the traversal is real-time cosmetic, but it must
            // not drift while the simulated clock is stopped (else birds keep
            // flying on pause). Mirrors the runner's ticking state.
            if (!_isActive || !SimulationRunner.IsTicking) return;
            _elapsed += Time.deltaTime;
            ApplyTransformAt(_elapsed);
            ApplyFrameAt(_elapsed);
        }

        private void ApplyTransformAt(float elapsed)
        {
            var p = SamplePositionAt(elapsed, _direction);
            var pos = transform.position;
            pos.x = p.x;
            pos.y = p.y;
            pos.z = 0f;
            transform.position = pos;
        }

        private void ApplyFrameAt(float elapsed)
        {
            if (_frames == null || _frames.Count == 0) return;
            int idx = (int)(elapsed * _framesPerSecond) % _frames.Count;
            if (idx < 0) idx = 0;
            _renderer.sprite = _frames[idx];
        }
    }
}
