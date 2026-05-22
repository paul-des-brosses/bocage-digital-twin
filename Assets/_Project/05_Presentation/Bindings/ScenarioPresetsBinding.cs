using System.Collections.Generic;
using Bocage.Presentation.Scenario;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using Bocage.SimulationCore.Scenario;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Spawns one <see cref="Button"/> per <see cref="ScenarioPresetDefinition"/>
    /// in a UXML container, applies the preset to the running
    /// <see cref="ScenarioContext"/> on click, and continuously
    /// highlights the preset whose values EXACTLY match the current
    /// scenario state. Highlight is driven by state, not by click
    /// history — so as soon as the user drags any slider away from
    /// a preset's values, the green border disappears.
    /// <para>
    /// Matching is done against <c>TransitioningParameter.Target</c>
    /// (the user-intended value) rather than <c>.Current</c>, so the
    /// highlight tracks the user's intent without waiting for the
    /// 10-day transition to complete.
    /// </para>
    /// <para>
    /// This binding writes to the <see cref="ScenarioContext"/> only —
    /// the single downstream write authorised by CLAUDE.md §5.5 for
    /// Couche 5.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScenarioPresetsBinding : MonoBehaviour
    {
        public const string PrefsKey = "Bocage.Scenario.LastPresetId";

        // Tolerance for matching float parameters. Allows for tiny
        // numerical drift between slider values and serialized preset
        // values without falsely missing a match.
        private const double FloatMatchTolerance = 0.001;

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

        private UIDocument _document;
        private VisualElement _container;
        private readonly List<Button> _buttons = new List<Button>();
        private int _lastActiveIndex = -1;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveContainer();
            BuildButtons();
            RecomputeActivePreset();
        }

        private void Update()
        {
            // Polled re-evaluation: catches slider drags that happen
            // while the simulation is paused (no TickCompleted fires
            // then). The comparison is O(presets × 5 fields) — a
            // handful of float comparisons per frame, negligible.
            RecomputeActivePreset();
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

        /// <summary>
        /// Applies the preset to the scenario context. The active highlight
        /// will follow automatically (on the next Update poll) once the
        /// targets are set — no need to manage it from here.
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

            // PlayerPrefs is still written for telemetry / future
            // "remember last preset" feature, but never read back: the
            // highlight on next session is determined by the scenario
            // state, not by a persisted id.
            PlayerPrefs.SetString(PrefsKey, preset.Id);
            PlayerPrefs.Save();

            SimLogger.UserActionLog($"scenario-preset-applied: id={preset.Id} name=\"{preset.DisplayName}\"");
        }

        /// <summary>
        /// Walks the preset list, finds the first one whose target
        /// values match the scenario exactly, and highlights it. If
        /// none match (user dragged a slider off-preset), no button is
        /// highlighted.
        /// </summary>
        private void RecomputeActivePreset()
        {
            int matchIndex = -1;
            if (runner != null && runner.Scenario != null && presets != null)
            {
                for (int i = 0; i < presets.Length; i++)
                {
                    if (presets[i] != null && MatchesScenario(presets[i], runner.Scenario))
                    {
                        matchIndex = i;
                        break;
                    }
                }
            }

            if (matchIndex == _lastActiveIndex) return;
            _lastActiveIndex = matchIndex;

            int n = System.Math.Min(_buttons.Count, presets != null ? presets.Length : 0);
            for (int i = 0; i < n; i++)
            {
                var btn = _buttons[i];
                if (btn == null) continue;
                btn.EnableInClassList("scenario-preset-button--active", i == matchIndex);
            }
        }

        private static bool MatchesScenario(ScenarioPresetDefinition preset, ScenarioContext scenario)
        {
            return ApproxEqual(scenario.TemperatureAnomalyC.Target, preset.TemperatureAnomalyC)
                && ApproxEqual(scenario.PrecipitationAnomalyPercent.Target, preset.PrecipitationAnomalyPercent)
                && ApproxEqual(scenario.MaecCoveragePercent.Target, preset.MaecCoveragePercent)
                && ApproxEqual(scenario.PseSubsidyRate.Target, preset.PseSubsidyRate)
                && scenario.HorizonInDays == preset.HorizonInDays;
        }

        private static bool ApproxEqual(double a, double b)
        {
            double diff = a - b;
            if (diff < 0.0) diff = -diff;
            return diff < FloatMatchTolerance;
        }
    }
}
