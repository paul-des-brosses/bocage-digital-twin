using Bocage.SimulationCore;
using UnityEngine;

namespace Bocage.Presentation.Scene.Fauna
{
    /// <summary>
    /// Static-visibility component for fauna species that use
    /// <see cref="FaunaMotionMode.StaticAppearance"/>. The GameObject
    /// stays active at its <see cref="FaunaSpeciesDefinition.StaticPosition"/>;
    /// visibility is driven by alpha fade-in / fade-out from
    /// <see cref="FaunaPoolBinding"/> via <see cref="SetVisible"/>.
    /// No motion, no frame swap, no flip.
    /// <para>
    /// Designed for sentinel-style species (héron): present when the
    /// biodiv composite is high, fades out gracefully when it drops
    /// under the species threshold.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FaunaStaticAppearance : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private float _fadeDurationSec = 1.5f;
        private float _currentAlpha;
        private float _targetAlpha;
        private bool _configured;

        // Head-turn state (optional — only used when restSprite + alertSprite
        // are both provided + meanSecondsBetweenHeadTurns > 0).
        private Sprite _restSprite;
        private Sprite _alertSprite;
        private float _meanSecondsBetweenHeadTurns;
        private float _headTurnHoldSec;
        private SeededRandom _rng;
        private bool _isAlert;
        private float _alertElapsedSec;

        public bool IsConfigured => _configured;
        public float CurrentAlpha => _currentAlpha;
        public float TargetAlpha => _targetAlpha;
        public bool IsAlert => _isAlert;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            // Start invisible — binding flips us on when biodiv crosses
            // the species threshold.
            _currentAlpha = 0f;
            _targetAlpha = 0f;
            var c = _renderer.color;
            c.a = 0f;
            _renderer.color = c;
        }

        public void Configure(
            float fadeDurationSec,
            Sprite restSprite = null,
            Sprite alertSprite = null,
            float meanSecondsBetweenHeadTurns = 0f,
            float headTurnHoldSec = 0f,
            ulong seed = 0UL)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

            _fadeDurationSec = Mathf.Max(0.01f, fadeDurationSec);
            _restSprite = restSprite;
            _alertSprite = alertSprite;
            _meanSecondsBetweenHeadTurns = meanSecondsBetweenHeadTurns;
            _headTurnHoldSec = headTurnHoldSec;
            _rng = new SeededRandom(seed == 0UL ? 1UL : seed);
            _isAlert = false;
            _alertElapsedSec = 0f;
            _configured = true;

            // Set initial sprite to rest if provided (also useful in
            // EditMode tests where Awake didn't run yet).
            if (_renderer != null && _restSprite != null)
            {
                _renderer.sprite = _restSprite;
            }
        }

        public void SetVisible(bool visible)
        {
            _targetAlpha = visible ? 1f : 0f;
        }

        private void Update()
        {
            ApplyFade(Time.deltaTime);
            // Head turn only matters once the heron is visible enough —
            // avoids invisible-alert flickers during fade-in.
            if (_alertSprite != null && _meanSecondsBetweenHeadTurns > 0f && _currentAlpha > 0.1f)
            {
                ApplyHeadTurn(Time.deltaTime);
            }
        }

        private void ApplyFade(float deltaTime)
        {
            if (Mathf.Approximately(_currentAlpha, _targetAlpha)) return;
            float step = deltaTime / _fadeDurationSec;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, step);
            if (_renderer != null)
            {
                var c = _renderer.color;
                c.a = _currentAlpha;
                _renderer.color = c;
            }
        }

        private void ApplyHeadTurn(float deltaTime)
        {
            if (_isAlert)
            {
                _alertElapsedSec += deltaTime;
                if (_alertElapsedSec >= _headTurnHoldSec)
                {
                    _isAlert = false;
                    _alertElapsedSec = 0f;
                    if (_renderer != null && _restSprite != null) _renderer.sprite = _restSprite;
                }
                return;
            }

            // Bernoulli roll : p = Δt / mean → expected 1 event per `mean` seconds.
            float p = deltaTime / _meanSecondsBetweenHeadTurns;
            if (_rng != null && (float)_rng.NextDouble() < p)
            {
                TriggerHeadTurnInternal();
            }
        }

        private void TriggerHeadTurnInternal()
        {
            _isAlert = true;
            _alertElapsedSec = 0f;
            if (_renderer != null && _alertSprite != null) _renderer.sprite = _alertSprite;
        }

        /// <summary>
        /// Pure compute helper exposed for EditMode tests: advance the
        /// fade by an explicit deltaTime instead of relying on
        /// Time.deltaTime in an Update loop.
        /// </summary>
        public void TickFade(float deltaTime)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            ApplyFade(deltaTime);
        }

        /// <summary>
        /// Pure compute helper exposed for EditMode tests: advance the
        /// head-turn state machine by deltaTime, bypassing the
        /// Bernoulli roll (use <see cref="TriggerHeadTurnForTest"/> to
        /// force-enter the alert state).
        /// </summary>
        public void TickHeadTurn(float deltaTime)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_isAlert)
            {
                _alertElapsedSec += deltaTime;
                if (_alertElapsedSec >= _headTurnHoldSec)
                {
                    _isAlert = false;
                    _alertElapsedSec = 0f;
                    if (_renderer != null && _restSprite != null) _renderer.sprite = _restSprite;
                }
            }
        }

        /// <summary>
        /// EditMode test hook: force-enter the alert state without
        /// rolling the Bernoulli. Sprite swaps to alert immediately.
        /// </summary>
        public void TriggerHeadTurnForTest()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            TriggerHeadTurnInternal();
        }
    }
}
