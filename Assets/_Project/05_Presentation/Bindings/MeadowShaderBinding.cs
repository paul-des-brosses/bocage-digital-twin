using System.Collections.Generic;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Pushes the soil-moisture proxy into the <c>_Moisture</c> shader
    /// property of every meadow <see cref="SpriteRenderer"/> spawned
    /// under <see cref="spawnRoot"/> by <c>SceneAssembler</c>. Companion
    /// to <c>HedgerowShaderBinding</c> — same scan-by-name-prefix pattern,
    /// same single <see cref="MaterialPropertyBlock"/> to avoid material
    /// instancing per renderer.
    /// <para>
    /// Per CLAUDE.md §9 (sensor primacy), the meadow's tint is not
    /// driven by a calendar or an ambient scenic cue: it is a strict
    /// function of <see cref="RC_SoilMoisture"/>, itself derived from
    /// <c>EcosystemModel.WaterTableDepth</c> (cf. SoilMoistureIndicator).
    /// </para>
    /// <para>
    /// Default prefix is <c>"grass_"</c> to cover <c>grass_border</c>
    /// and any future <c>grass_field_*</c> variants. Extend the prefix
    /// list in the inspector when new meadow sprites are added.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class MeadowShaderBinding : MonoBehaviour
    {
        [SerializeField] private RC_SoilMoisture container;

        [SerializeField, Tooltip("Parent transform whose children are scanned to find meadow sprites. Typically '_Scene_Visual > Composition'.")]
        private Transform spawnRoot;

        [SerializeField, Tooltip("A child SpriteRenderer is treated as a meadow tile if its GameObject name starts with any of these prefixes (case-sensitive).")]
        private string[] meadowNamePrefixes = new[] { "grass_" };

        [SerializeField, Tooltip("Shader property name exposed by S_Meadow.")]
        private string moistureProperty = "_Moisture";

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>(8);
        private MaterialPropertyBlock _block;
        private int _moisturePropertyId;
        private bool _subscribed;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _moisturePropertyId = Shader.PropertyToID(moistureProperty);
        }

        private void Start()
        {
            ScanRenderers();
            Subscribe();
            if (container != null)
            {
                Apply(container.Moisture01);
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
            _renderers.Clear();

            if (spawnRoot == null)
            {
                SimLogger.DebugLog("[MeadowShaderBinding] no spawnRoot set on " + name + ", no meadow tile will be tinted");
                return;
            }
            if (meadowNamePrefixes == null || meadowNamePrefixes.Length == 0)
            {
                SimLogger.DebugLog("[MeadowShaderBinding] no name prefixes configured, no meadow tile will be tinted");
                return;
            }

            for (int i = 0; i < spawnRoot.childCount; i++)
            {
                var child = spawnRoot.GetChild(i);
                if (!MatchesAnyPrefix(child.name)) continue;
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) _renderers.Add(sr);
            }

            SimLogger.DebugLog("[MeadowShaderBinding] discovered " + _renderers.Count + " meadow renderers under " + spawnRoot.name);
        }

        private bool MatchesAnyPrefix(string childName)
        {
            for (int i = 0; i < meadowNamePrefixes.Length; i++)
            {
                var prefix = meadowNamePrefixes[i];
                if (!string.IsNullOrEmpty(prefix) && childName.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleChanged(float moisture01)
        {
            Apply(moisture01);
        }

        private void Apply(float moisture01)
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetFloat(_moisturePropertyId, moisture01);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
