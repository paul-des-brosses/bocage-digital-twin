using System.Collections.Generic;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Pushes the normalized hedgerow density into the
    /// <c>_Density</c> shader property of every hedge
    /// <see cref="SpriteRenderer"/> spawned under <see cref="spawnRoot"/>
    /// by <c>SceneAssembler</c>. Uses a shared
    /// <see cref="MaterialPropertyBlock"/> so the per-renderer values
    /// do not cause material instancing.
    /// <para>
    /// Per CLAUDE.md §9 (sensor primacy), the colour of the hedges is
    /// not driven by a clock or an ambience cue: it is a strict
    /// function of the simulated model variable
    /// <c>EcosystemModel.HedgerowDensity</c>, computed by the indicator
    /// and exposed via the observable container.
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
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class HedgerowShaderBinding : MonoBehaviour
    {
        [SerializeField] private RC_HedgerowDensity container;

        [SerializeField, Tooltip("Parent transform whose children are scanned to find hedge sprites. Typically '_Scene_Visual > Composition' (the same root SceneAssembler spawns into).")]
        private Transform spawnRoot;

        [SerializeField, Tooltip("A child SpriteRenderer is treated as a hedge if its GameObject name starts with any of these prefixes (case-sensitive). Defaults to 'hedge_'.")]
        private string[] hedgeNamePrefixes = new[] { "hedge_" };

        [SerializeField, Tooltip("Shader property name exposed by SG_Hedgerow. Defaults to '_Density'.")]
        private string densityProperty = "_Density";

        private readonly List<SpriteRenderer> _hedgeRenderers = new List<SpriteRenderer>(8);
        private MaterialPropertyBlock _block;
        private int _densityPropertyId;
        private bool _subscribed;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _densityPropertyId = Shader.PropertyToID(densityProperty);
        }

        private void Start()
        {
            ScanRenderers();
            Subscribe();
            if (container != null)
            {
                ApplyDensity(container.Normalized01);
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || container == null) return;
            container.OnChanged += HandleChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || container == null) return;
            container.OnChanged -= HandleChanged;
            _subscribed = false;
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

        private void HandleChanged(float metersPerHectare)
        {
            ApplyDensity(container.Normalized01);
        }

        private void ApplyDensity(float normalized01)
        {
            for (int i = 0; i < _hedgeRenderers.Count; i++)
            {
                var renderer = _hedgeRenderers[i];
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block);
                _block.SetFloat(_densityPropertyId, normalized01);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
