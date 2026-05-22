using System.Collections.Generic;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.Serialization;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Pushes the normalized hedgerow density into the
    /// <c>_Density</c> shader property AND the hedgerow health proxy
    /// into the <c>_HealthT</c> property of every hedge
    /// <see cref="SpriteRenderer"/> spawned under <see cref="spawnRoot"/>
    /// by <c>SceneAssembler</c>. Uses a shared
    /// <see cref="MaterialPropertyBlock"/> so the per-renderer values
    /// do not cause material instancing.
    /// <para>
    /// Per CLAUDE.md §9 (sensor primacy), the colour of the hedges is
    /// not driven by a clock or an ambience cue: it is a strict
    /// function of the simulated model variable
    /// <c>EcosystemModel.HedgerowDensity</c> (modulated by
    /// <c>HedgerowHealthIndicator</c> for the healthT channel, which
    /// blends in recent stress events from the Couche 2 EventLog).
    /// </para>
    /// <para>
    /// Renderers are discovered at <see cref="Start"/> by name-prefix
    /// scan under <see cref="spawnRoot"/>. We deliberately don't accept
    /// hard-coded SpriteRenderer references because the SceneAssembler
    /// destroys and re-instantiates the composition's children at Awake;
    /// any reference captured in Edit Mode would become a null pointer
    /// at Play. The scan happens once, after the Assembler has finished
    /// (SceneAssembler runs at execution order -9000, this script at
    /// the default 0, so Start fires safely after).
    /// </para>
    /// <para>
    /// CLAUDE.md §6 forbids <c>FindObjectsOfType</c>. We respect that:
    /// the scan is bounded to the children of an explicit transform
    /// passed by the inspector, which is just a transform traversal.
    /// </para>
    /// <para>
    /// <c>_HealthT</c> is pushed even if the underlying shader does not
    /// yet read it: SetPropertyBlock silently ignores unknown shader
    /// properties. This lets us land the data path independently from
    /// the Shader Graph extension (cf. BACKLOG.md "SG_Hedgerow healthT
    /// node").
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class HedgerowShaderBinding : MonoBehaviour
    {
        // FormerlySerializedAs preserves the prefab/scene wiring after
        // the 9β rename from "container" to "densityContainer" — without
        // it, any existing scene reference would be cleared on first
        // import after this commit (silent breakage).
        [SerializeField, FormerlySerializedAs("container")] private RC_HedgerowDensity densityContainer;

        [SerializeField, Tooltip("Optional. Health proxy (RC_HedgerowHealth) for the _HealthT shader property. If null, _HealthT is not pushed. Safe to leave empty until the shader exposes the property.")]
        private RC_HedgerowHealth healthContainer;

        [SerializeField, Tooltip("Parent transform whose children are scanned to find hedge sprites. Typically '_Scene_Visual > Composition' (the same root SceneAssembler spawns into).")]
        private Transform spawnRoot;

        [SerializeField, Tooltip("A child SpriteRenderer is treated as a hedge if its GameObject name starts with any of these prefixes (case-sensitive). Defaults to 'hedge_'.")]
        private string[] hedgeNamePrefixes = new[] { "hedge_" };

        [SerializeField, Tooltip("Shader property name exposed by SG_Hedgerow. Defaults to '_Density'.")]
        private string densityProperty = "_Density";

        [SerializeField, Tooltip("Shader property name for the hedge health [0,1] channel. Defaults to '_HealthT'. The property is pushed even if the shader does not (yet) read it — SetPropertyBlock is a silent no-op for unknown properties.")]
        private string healthProperty = "_HealthT";

        private readonly List<SpriteRenderer> _hedgeRenderers = new List<SpriteRenderer>(8);
        private MaterialPropertyBlock _block;
        private int _densityPropertyId;
        private int _healthPropertyId;
        private bool _subscribedDensity;
        private bool _subscribedHealth;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _densityPropertyId = Shader.PropertyToID(densityProperty);
            _healthPropertyId = Shader.PropertyToID(healthProperty);
        }

        private void Start()
        {
            ScanRenderers();
            SubscribeDensity();
            SubscribeHealth();
            // Push initial values so the materials are correct before
            // the first tick lands.
            ApplyAll();
        }

        private void OnDestroy()
        {
            UnsubscribeDensity();
            UnsubscribeHealth();
        }

        private void SubscribeDensity()
        {
            if (_subscribedDensity || densityContainer == null) return;
            densityContainer.OnChanged += HandleDensityChanged;
            _subscribedDensity = true;
        }

        private void UnsubscribeDensity()
        {
            if (!_subscribedDensity || densityContainer == null) return;
            densityContainer.OnChanged -= HandleDensityChanged;
            _subscribedDensity = false;
        }

        private void SubscribeHealth()
        {
            if (_subscribedHealth || healthContainer == null) return;
            healthContainer.OnChanged += HandleHealthChanged;
            _subscribedHealth = true;
        }

        private void UnsubscribeHealth()
        {
            if (!_subscribedHealth || healthContainer == null) return;
            healthContainer.OnChanged -= HandleHealthChanged;
            _subscribedHealth = false;
        }

        private void ScanRenderers()
        {
            _hedgeRenderers.Clear();

            if (spawnRoot == null)
            {
                SimLogger.DebugLog("[HedgerowShaderBinding] no spawnRoot set on " + name + ", no hedges will be tinted");
                return;
            }
            if (hedgeNamePrefixes == null || hedgeNamePrefixes.Length == 0)
            {
                SimLogger.DebugLog("[HedgerowShaderBinding] no name prefixes configured, no hedges will be tinted");
                return;
            }

            for (int i = 0; i < spawnRoot.childCount; i++)
            {
                var child = spawnRoot.GetChild(i);
                if (!MatchesAnyPrefix(child.name)) continue;
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) _hedgeRenderers.Add(sr);
            }

            SimLogger.DebugLog("[HedgerowShaderBinding] discovered " + _hedgeRenderers.Count + " hedge renderers under " + spawnRoot.name);
        }

        private bool MatchesAnyPrefix(string childName)
        {
            for (int i = 0; i < hedgeNamePrefixes.Length; i++)
            {
                var prefix = hedgeNamePrefixes[i];
                if (!string.IsNullOrEmpty(prefix) && childName.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleDensityChanged(float metersPerHectare)
        {
            ApplyAll();
        }

        private void HandleHealthChanged(float health01)
        {
            ApplyAll();
        }

        /// <summary>
        /// Reads the current values from both containers and pushes them
        /// in a single per-renderer property block update. Two channels
        /// in one block keeps the GPU upload cost flat (one
        /// <c>SetPropertyBlock</c> per hedge per change), regardless of
        /// which container fired.
        /// </summary>
        private void ApplyAll()
        {
            float density01 = densityContainer != null ? densityContainer.Normalized01 : 0.5f;
            float health01 = healthContainer != null ? healthContainer.Health01 : 1f;

            for (int i = 0; i < _hedgeRenderers.Count; i++)
            {
                var renderer = _hedgeRenderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetFloat(_densityPropertyId, density01);
                _block.SetFloat(_healthPropertyId, health01);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
