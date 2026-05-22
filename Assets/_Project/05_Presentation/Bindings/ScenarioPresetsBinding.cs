using System.Collections.Generic;
using Bocage.Presentation.Scenario;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Spawns one <see cref="Button"/> per <see cref="ScenarioPresetDefinition"/>
    /// in a UXML container, and applies the preset to the running
    /// <see cref="Bocage.SimulationCore.Scenario.ScenarioContext"/> on click
    /// — every continuous parameter goes through
    /// <c>TransitioningParameter.SetTarget(value, transitionDurationDays)</c>
    /// per CLAUDE.md §15, so applying a preset never produces a discontinuity.
    /// <para>
    /// The currently selected preset is mirrored into PlayerPrefs under
    /// <see cref="PrefsKey"/> so the next session re-applies it (CLAUDE.md §16:
    /// PlayerPrefs minimal — only presets and speed are persisted).
    /// </para>
    /// <para>
    /// This binding writes to <see cref="ScenarioContext"/> only — the single
    /// downstream write authorised by CLAUDE.md §5.5 for Couche 5.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScenarioPresetsBinding : MonoBehaviour
    {
        public const string PrefsKey = "Bocage.Scenario.LastPresetId";

        [SerializeField, Tooltip("Source of the scenario context. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [SerializeField, Tooltip("Sibling binding driving the per-parameter sliders. Used to visually snap sliders to the preset values after a click.")]
        private ScenarioControlsBinding controlsBinding;

        [SerializeField, Tooltip("Available presets, in display order. Author one ScriptableObject per preset in Assets/_Project/05_Presentation/Scenario/Presets/.")]
        private ScenarioPresetDefinition[] presets = new ScenarioPresetDefinition[0];

        [SerializeField, Range(1, 30), Tooltip("Simulated days over which a preset transitions in. 7-14 per CLAUDE.md §15.")]
        private int transitionDurationDays = 10;

        [SerializeField, Tooltip("Name of the VisualElement in the UXML that hosts the preset buttons (rendered as a horizontal flex row).")]
        private string presetsContainerName = "scenario-presets-row";

        [SerializeField, Tooltip("If true, the preset stored in PlayerPrefs is re-applied at startup. Disable to always start neutral.")]
        private bool applyPersistedPresetOnStart = true;

        private UIDocument _document;
        private VisualElement _container;
        private readonly List<Button> _buttons = new List<Button>();
        private string _activePresetId;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveContainer();
            BuildButtons();
            if (applyPersistedPresetOnStart)
            {
                TryApplyPersistedPreset();
            }
        }

        private void ResolveContainer()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _container = _document.rootVisualElement.Q<VisualElement>(presetsContainerName);
            if (_container == null)
            {
                SimLogger.DebugLog($"[ScenarioPresetsBinding] container '{presetsContainerName}' not found in UXML");
            }
        }

        private void BuildButtons()
        {
            _buttons.Clear();
            if (_container == null) return;
            _container.Clear();
            if (presets == null) return;

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                if (preset == null) continue;
                var button = new Button(() => ApplyPreset(preset))
                {
                    text = preset.DisplayName,
                    tooltip = string.IsNullOrEmpty(preset.Description) ? preset.DisplayName : preset.Description,
                    name = "scenario-preset-button-" + preset.Id
                };
                button.AddToClassList("scenario-preset-button");
                _container.Add(button);
                _buttons.Add(button);
            }
        }

        private void TryApplyPersistedPreset()
        {
            if (presets == null || presets.Length == 0) return;
            string persisted = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(persisted)) return;

            foreach (var preset in presets)
            {
                if (preset != null && preset.Id == persisted)
                {
                    ApplyPreset(preset);
                    return;
                }
            }
            SimLogger.DebugLog($"[ScenarioPresetsBinding] persisted preset id '{persisted}' not found in current presets list — ignored");
        }

        /// <summary>
        /// Applies the preset to the scenario context: each continuous
        /// parameter transitions over <see cref="transitionDurationDays"/>
        /// simulated days; the horizon is set directly. Then the sibling
        /// <see cref="controlsBinding"/> is asked to snap its sliders to
        /// the new target values for immediate visual feedback.
        /// </summary>
        public void ApplyPreset(ScenarioPresetDefinition preset)
        {
            if (preset == null) return;
            if (runner == null || runner.Scenario == null)
            {
                SimLogger.DebugLog("[ScenarioPresetsBinding] runner or scenario not available; preset ignored");
                return;
            }
            // Apply ONLY the exogenous fields (climate + public policy +
            // horizon). The farmer-controlled sliders (HedgeRemovalRate,
            // InputIntensityFactor) are deliberately left alone — a
            // preset is the "Cadre extérieur" the farmer is presented
            // with, not a substitute for his own choices.
            var s = runner.Scenario;
            s.TemperatureAnomalyC.SetTarget(preset.TemperatureAnomalyC, transitionDurationDays);
            s.PrecipitationAnomalyPercent.SetTarget(preset.PrecipitationAnomalyPercent, transitionDurationDays);
            s.MaecCoveragePercent.SetTarget(preset.MaecCoveragePercent, transitionDurationDays);
            s.PseSubsidyRate.SetTarget(preset.PseSubsidyRate, transitionDurationDays);
            s.HorizonInDays = preset.HorizonInDays;

            if (controlsBinding != null)
            {
                controlsBinding.SnapToPresetExogenousValues(
                    preset.TemperatureAnomalyC,
                    preset.PrecipitationAnomalyPercent,
                    preset.MaecCoveragePercent,
                    preset.PseSubsidyRate,
                    preset.HorizonInDays);
            }

            _activePresetId = preset.Id;
            PlayerPrefs.SetString(PrefsKey, preset.Id);
            PlayerPrefs.Save();

            UpdateActiveVisualState();
            SimLogger.UserActionLog($"scenario-preset-applied: id={preset.Id} name=\"{preset.DisplayName}\"");
        }

        private void UpdateActiveVisualState()
        {
            if (presets == null) return;
            int n = System.Math.Min(_buttons.Count, presets.Length);
            for (int i = 0; i < n; i++)
            {
                var btn = _buttons[i];
                if (btn == null) continue;
                bool isActive = presets[i] != null && presets[i].Id == _activePresetId;
                btn.EnableInClassList("scenario-preset-button--active", isActive);
            }
        }
    }
}
