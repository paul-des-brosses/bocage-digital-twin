using System.Collections.Generic;
using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.Presentation.Scene.Fauna;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Fills the five rows of the "Biodiversité" Niveau B panel (chantier E6
    /// / ADR #54): the composite index plus its three exposed factors
    /// (habitat / eau / intrants), all driven by their RCs' <c>OnChanged</c>,
    /// and a live count of the fauna species currently visible on screen,
    /// derived from <see cref="FaunaPool"/>.
    /// <para>
    /// The composite is displayed exactly like the Hero KPI
    /// (<c>round(Score × 100)</c> %); the three factors use their normalized
    /// 0-1 channel mapped to a percentage so the breakdown reads on the same
    /// scale. The species count is the one row not backed by an RC — it
    /// observes the actual pooled sprites, which are themselves gated by the
    /// measured biodiversity index (CLAUDE.md §9: derived from a measure,
    /// never the calendar).
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class OngletBiodivBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Composite biodiversity index container.")]
        private RC_BiodiversityComposite biodivComposite;
        [SerializeField, Tooltip("Habitat factor (derived from hedgerow density).")]
        private RC_FaunaFactorHabitat habitat;
        [SerializeField, Tooltip("Water factor (derived from water-table depth).")]
        private RC_FaunaFactorWater water;
        [SerializeField, Tooltip("Inputs factor (derived from input intensity).")]
        private RC_FaunaFactorInputs inputs;
        [SerializeField, Tooltip("Fauna pool the visible-species count is read from.")]
        private FaunaPool faunaPool;

        [SerializeField] private string compositeLabelName = "biodiv-composite-value";
        [SerializeField] private string habitatLabelName = "biodiv-habitat-value";
        [SerializeField] private string waterLabelName = "biodiv-water-value";
        [SerializeField] private string inputsLabelName = "biodiv-inputs-value";
        [SerializeField] private string speciesCountLabelName = "biodiv-species-count-value";

        private UIDocument _document;
        private Label _compositeLabel, _habitatLabel, _waterLabel, _inputsLabel, _speciesCountLabel;

        // Reused so the per-frame visible-species count never allocates (CLAUDE.md §6).
        private readonly List<FaunaSpeciesDefinition> _visibleBuffer = new List<FaunaSpeciesDefinition>(8);
        private readonly List<FaunaSpeciesDefinition> _distinctBuffer = new List<FaunaSpeciesDefinition>(4);
        private int _lastSpeciesCount = -1;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveLabels();
            if (biodivComposite != null) { biodivComposite.OnChanged += HandleCompositeChanged; HandleCompositeChanged(0f); }
            if (habitat != null) { habitat.OnChanged += HandleHabitatChanged; HandleHabitatChanged(0f); }
            if (water != null) { water.OnChanged += HandleWaterChanged; HandleWaterChanged(0f); }
            if (inputs != null) { inputs.OnChanged += HandleInputsChanged; HandleInputsChanged(0f); }

            if (biodivComposite == null || habitat == null || water == null || inputs == null)
                SimLogger.DebugLog("[OngletBiodivBinding] one or more containers not assigned on " + name);
            if (faunaPool == null)
                SimLogger.DebugLog("[OngletBiodivBinding] fauna pool not assigned on " + name);
        }

        private void OnDisable()
        {
            if (biodivComposite != null) biodivComposite.OnChanged -= HandleCompositeChanged;
            if (habitat != null) habitat.OnChanged -= HandleHabitatChanged;
            if (water != null) water.OnChanged -= HandleWaterChanged;
            if (inputs != null) inputs.OnChanged -= HandleInputsChanged;
        }

        private void Update()
        {
            if (faunaPool == null) return;
            int count = CountVisibleSpeciesRuntime();
            if (count == _lastSpeciesCount) return;
            _lastSpeciesCount = count;
            EnsureResolved();
            if (_speciesCountLabel != null)
                _speciesCountLabel.text = count.ToString(CultureInfo.InvariantCulture);
        }

        private void ResolveLabels()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _compositeLabel = root.Q<Label>(compositeLabelName);
            _habitatLabel = root.Q<Label>(habitatLabelName);
            _waterLabel = root.Q<Label>(waterLabelName);
            _inputsLabel = root.Q<Label>(inputsLabelName);
            _speciesCountLabel = root.Q<Label>(speciesCountLabelName);
        }

        private void EnsureResolved()
        {
            if (_compositeLabel == null || _habitatLabel == null || _waterLabel == null
                || _inputsLabel == null || _speciesCountLabel == null)
            {
                ResolveLabels();
            }
        }

        private void HandleCompositeChanged(float _)
        {
            EnsureResolved();
            if (_compositeLabel != null && biodivComposite != null)
                _compositeLabel.text = Mathf.RoundToInt(biodivComposite.Score * 100f).ToString(CultureInfo.InvariantCulture);
        }

        private void HandleHabitatChanged(float _)
        {
            EnsureResolved();
            if (_habitatLabel != null && habitat != null)
                _habitatLabel.text = Mathf.RoundToInt(habitat.Normalized01 * 100f).ToString(CultureInfo.InvariantCulture);
        }

        private void HandleWaterChanged(float _)
        {
            EnsureResolved();
            if (_waterLabel != null && water != null)
                _waterLabel.text = Mathf.RoundToInt(water.Normalized01 * 100f).ToString(CultureInfo.InvariantCulture);
        }

        private void HandleInputsChanged(float _)
        {
            EnsureResolved();
            if (_inputsLabel != null && inputs != null)
                _inputsLabel.text = Mathf.RoundToInt(inputs.Normalized01 * 100f).ToString(CultureInfo.InvariantCulture);
        }

        private int CountVisibleSpeciesRuntime()
        {
            _visibleBuffer.Clear();
            var pooled = faunaPool.PooledSprites;
            if (pooled != null)
            {
                for (int i = 0; i < pooled.Count; i++)
                {
                    var ps = pooled[i];
                    if (ps == null) continue;
                    bool visible = ps.StaticAppearance != null
                        ? ps.StaticAppearance.CurrentAlpha > 0.01f
                        : (ps.GameObject != null && ps.GameObject.activeSelf);
                    if (visible && ps.Species != null) _visibleBuffer.Add(ps.Species);
                }
            }
            return CountDistinctSpecies(_visibleBuffer, _distinctBuffer);
        }

        /// <summary>
        /// Counts the distinct species among <paramref name="visibleSpecies"/>
        /// (which may contain duplicates and nulls), using
        /// <paramref name="reuseBuffer"/> as scratch so the call never
        /// allocates. Pure and side-effect-free on its inputs beyond the
        /// scratch buffer — covered by EditMode tests.
        /// </summary>
        public static int CountDistinctSpecies(
            IReadOnlyList<FaunaSpeciesDefinition> visibleSpecies,
            List<FaunaSpeciesDefinition> reuseBuffer)
        {
            reuseBuffer.Clear();
            if (visibleSpecies == null) return 0;
            for (int i = 0; i < visibleSpecies.Count; i++)
            {
                var sp = visibleSpecies[i];
                if (sp != null && !reuseBuffer.Contains(sp)) reuseBuffer.Add(sp);
            }
            return reuseBuffer.Count;
        }
    }
}
