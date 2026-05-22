using System.Collections.Generic;
using Bocage.Data.RuntimeContainers;
using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Pushes the water-table level into the <c>_WaterLevel</c> shader
    /// property of every pond <see cref="SpriteRenderer"/> spawned under
    /// <see cref="spawnRoot"/> by <c>SceneAssembler</c>. Same name-prefix
    /// scan pattern as <c>HedgerowShaderBinding</c> and
    /// <c>MeadowShaderBinding</c>.
    /// <para>
    /// Per CLAUDE.md §9 (sensor primacy), the pond's colour is a strict
    /// function of <see cref="RC_WaterTableDepth"/> (Normalized01),
    /// which itself maps to the piezometer sensor. High table = vibrant
    /// blue, deep table = muddy / shrunken.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PondShaderBinding : MonoBehaviour
    {
        [SerializeField] private RC_WaterTableDepth container;

        [SerializeField, Tooltip("Parent transform whose children are scanned to find pond sprites. Typically '_Scene_Visual > Composition'.")]
        private Transform spawnRoot;

        [SerializeField, Tooltip("A child SpriteRenderer is treated as the pond if its GameObject name starts with any of these prefixes (case-sensitive).")]
        private string[] pondNamePrefixes = new[] { "pond" };

        [SerializeField, Tooltip("Shader property name exposed by S_Pond.")]
        private string waterLevelProperty = "_WaterLevel";

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>(2);
        private MaterialPropertyBlock _block;
        private int _waterLevelPropertyId;
        private bool _subscribed;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _waterLevelPropertyId = Shader.PropertyToID(waterLevelProperty);
        }

        private void Start()
        {
            ScanRenderers();
            Subscribe();
            if (container != null)
            {
                Apply(container.Normalized01);
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
                SimLogger.DebugLog("[PondShaderBinding] no spawnRoot set on " + name + ", no pond will be tinted");
                return;
            }
            if (pondNamePrefixes == null || pondNamePrefixes.Length == 0)
            {
                SimLogger.DebugLog("[PondShaderBinding] no name prefixes configured, no pond will be tinted");
                return;
            }

            for (int i = 0; i < spawnRoot.childCount; i++)
            {
                var child = spawnRoot.GetChild(i);
                if (!MatchesAnyPrefix(child.name)) continue;
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) _renderers.Add(sr);
            }

            SimLogger.DebugLog("[PondShaderBinding] discovered " + _renderers.Count + " pond renderers under " + spawnRoot.name);
        }

        private bool MatchesAnyPrefix(string childName)
        {
            for (int i = 0; i < pondNamePrefixes.Length; i++)
            {
                var prefix = pondNamePrefixes[i];
                if (!string.IsNullOrEmpty(prefix) && childName.StartsWith(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        // The water-table container emits the RAW depth on OnChanged;
        // we read Normalized01 separately so the shader gets a stable
        // [0,1] signal regardless of the indicator's chosen bounds.
        private void HandleChanged(float _rawDepthMeters)
        {
            Apply(container.Normalized01);
        }

        private void Apply(float waterLevel01)
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetFloat(_waterLevelPropertyId, waterLevel01);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
