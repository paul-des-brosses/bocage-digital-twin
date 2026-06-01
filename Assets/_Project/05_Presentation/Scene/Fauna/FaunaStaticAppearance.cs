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

        public bool IsConfigured => _configured;
        public float CurrentAlpha => _currentAlpha;
        public float TargetAlpha => _targetAlpha;

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

        public void Configure(float fadeDurationSec)
        {
            _fadeDurationSec = Mathf.Max(0.01f, fadeDurationSec);
            _configured = true;
        }

        public void SetVisible(bool visible)
        {
            _targetAlpha = visible ? 1f : 0f;
        }

        private void Update()
        {
            if (Mathf.Approximately(_currentAlpha, _targetAlpha)) return;
            float step = Time.deltaTime / _fadeDurationSec;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, step);
            var c = _renderer.color;
            c.a = _currentAlpha;
            _renderer.color = c;
        }

        /// <summary>
        /// Pure compute helper exposed for EditMode tests: advance the
        /// fade by an explicit deltaTime instead of relying on
        /// Time.deltaTime in an Update loop.
        /// </summary>
        public void TickFade(float deltaTime)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
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
    }
}
