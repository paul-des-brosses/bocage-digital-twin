using Bocage.Data.RuntimeContainers;
using Bocage.Presentation.Refonte;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Scene.Fauna
{
    /// <summary>
    /// Probabilistic spawn driver for the fauna pool. Observes
    /// <see cref="RC_BiodiversityComposite"/> and, per-frame and
    /// per-pooled-sprite, rolls a Bernoulli trial whose rate is
    /// <c>λ_effective × Δt</c> with
    /// <c>λ_effective = λ_max × max(0, (biodiv − threshold) / (1 − threshold))</c>
    /// — linear above the species threshold, zero below. Successful
    /// rolls activate the GameObject and call
    /// <see cref="FaunaTraversalMotion.StartTraversal"/> with a random
    /// direction (50/50) and a uniform-random sin phase offset, both
    /// deterministic under <see cref="masterSeed"/> via
    /// <see cref="SeededRandom"/> sub-streams.
    /// <para>
    /// Honest design (CLAUDE.md §9): the binding subscribes to
    /// <see cref="RC_BiodiversityComposite.OnChanged"/> to cache the
    /// latest value cheaply; the spawn formula is the only translation
    /// from the model's biodiv signal to visible birds. No calendar,
    /// no scenic trigger.
    /// </para>
    /// <para>
    /// Deactivation: pooled sprites with <see cref="FaunaTraversalMotion.IsFinished"/>
    /// are stopped and disabled the same frame; their slot is then
    /// immediately eligible for a fresh roll (no cooldown beyond the
    /// Poisson rate itself).
    /// </para>
    /// </summary>
    public sealed class FaunaPoolBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Pool driving the per-trajectory pre-instantiated sprites.")]
        private FaunaPool pool;

        [SerializeField, Tooltip("Observable biodiv composite from Couche 04. Read every frame for spawn-rate computation.")]
        private RC_BiodiversityComposite biodivComposite;

        [SerializeField, Tooltip("Master seed for the binding's RNG sub-stream. Must match the simulation seed for cross-run determinism.")]
        private ulong masterSeed = 12345UL;

        private SeededRandom _spawnRng;
        private float _currentBiodiv;
        private bool _subscribed;

        private void Awake()
        {
            _spawnRng = new SeededRandom(masterSeed).DeriveSubStream("fauna_pool_binding");
        }

        private void OnEnable()
        {
            if (biodivComposite == null) return;
            biodivComposite.OnChanged += OnBiodivChanged;
            _currentBiodiv = biodivComposite.Normalized01;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed || biodivComposite == null) return;
            biodivComposite.OnChanged -= OnBiodivChanged;
            _subscribed = false;
        }

        private void OnBiodivChanged(float _)
        {
            _currentBiodiv = biodivComposite.Normalized01;
        }

        private void Update()
        {
            if (pool == null) return;
            float dt = Time.deltaTime;
            var pooled = pool.PooledSprites;

            for (int i = 0; i < pooled.Count; i++)
            {
                var p = pooled[i];

                if (p.Species.MotionMode == FaunaMotionMode.StaticAppearance)
                {
                    // Static sentinel: visible iff biodiv >= threshold.
                    // FaunaStaticAppearance handles the alpha lerp itself.
                    bool shouldBeVisible = _currentBiodiv >= p.Species.AppearanceThreshold;
                    p.StaticAppearance.SetVisible(shouldBeVisible);
                    continue;
                }

                // Traversal mode below.

                // 1) Finished traversal → deactivate, slot becomes eligible again.
                if (p.TraversalMotion.IsFinished)
                {
                    p.TraversalMotion.Stop();
                    p.GameObject.SetActive(false);
                    continue;
                }

                // 2) Currently traversing → skip.
                if (p.TraversalMotion.IsActive) continue;

                // 3) Inactive slot → probabilistic spawn roll. Suppressed while the
                //    simulated clock is paused, so no new birds appear (and queue
                //    up off-screen) during a pause.
                if (!RefonteSimulationRunner.IsTicking) continue;
                float lambdaEff = ComputeEffectiveSpawnRate(p.Species, _currentBiodiv);
                if (lambdaEff <= 0f) continue;

                float pSpawn = lambdaEff * dt;
                if ((float)_spawnRng.NextDouble() < pSpawn)
                {
                    var direction = _spawnRng.NextDouble() < 0.5
                        ? FaunaTraversalMotion.Direction.LeftToRight
                        : FaunaTraversalMotion.Direction.RightToLeft;
                    float phase = (float)(_spawnRng.NextDouble() * 2.0 * Mathf.PI);
                    p.GameObject.SetActive(true);
                    p.TraversalMotion.StartTraversal(direction, phase);
                }
            }
        }

        /// <summary>
        /// Pure compute exposed for EditMode tests: maps a biodiv value
        /// in [0, 1] to the effective Poisson rate (spawns/sec) for the
        /// given species, applying the threshold + linear-above-threshold
        /// formula.
        /// </summary>
        public static float ComputeEffectiveSpawnRate(FaunaSpeciesDefinition species, float biodivNormalized01)
        {
            if (species == null) return 0f;
            float threshold = species.AppearanceThreshold;
            float lambdaMax = species.SpawnRateAtMaxBiodiv;
            if (biodivNormalized01 <= threshold) return 0f;
            float denom = Mathf.Max(0.0001f, 1f - threshold);
            float t = Mathf.Clamp01((biodivNormalized01 - threshold) / denom);
            return lambdaMax * t;
        }
    }
}
