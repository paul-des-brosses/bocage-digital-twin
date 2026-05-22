using System.Globalization;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Wires the seven scenario controls (sliders with visible numeric
    /// labels) to the simulation's
    /// <see cref="Bocage.SimulationCore.Scenario.ScenarioContext"/>.
    /// After the 2026-05-21 UX iteration the controls are
    /// <see cref="Slider"/>/<see cref="SliderInt"/> with concrete physical
    /// units displayed inline (°C, %, m/ha/yr, etc.) — the abstract [0,1]
    /// dials and free-typed FloatFields of the previous iterations are
    /// retired.
    /// <para>
    /// Each value is pushed to the ScenarioContext via
    /// <c>TransitioningParameter.SetTarget</c> with a transition over
    /// <see cref="transitionDurationDays"/> simulated days
    /// (CLAUDE.md §15). The horizon is applied directly (it's a deadline,
    /// not a smoothed setpoint).
    /// </para>
    /// <para>
    /// Per CLAUDE.md §5.5 Couche 5 may push user inputs to the
    /// ScenarioContext — the only allowed downstream write.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScenarioControlsBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the scenario context. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [SerializeField, Range(1, 30), Tooltip("Simulated days over which a user-set scenario value transitions. 7-14 per CLAUDE.md §15.")]
        private int transitionDurationDays = 10;

        [Header("UXML element names — sliders")]
        [SerializeField] private string temperatureSliderName = "temperature-anomaly-slider";
        [SerializeField] private string precipitationSliderName = "precipitation-anomaly-slider";
        [SerializeField] private string hedgeRemovalSliderName = "hedge-removal-slider";
        [SerializeField] private string inputIntensitySliderName = "input-intensity-slider";
        [SerializeField] private string maecCoverageSliderName = "maec-coverage-slider";
        [SerializeField] private string pseRateSliderName = "pse-rate-slider";
        [SerializeField] private string horizonSliderName = "horizon-slider";

        [Header("UXML element names — value labels")]
        [SerializeField] private string temperatureValueLabelName = "temperature-anomaly-value";
        [SerializeField] private string precipitationValueLabelName = "precipitation-anomaly-value";
        [SerializeField] private string hedgeRemovalValueLabelName = "hedge-removal-value";
        [SerializeField] private string inputIntensityValueLabelName = "input-intensity-value";
        [SerializeField] private string maecCoverageValueLabelName = "maec-coverage-value";
        [SerializeField] private string pseRateValueLabelName = "pse-rate-value";
        [SerializeField] private string horizonValueLabelName = "horizon-value";

        private UIDocument _document;
        private Slider _tempSlider, _precipSlider, _hedgeRemovalSlider, _inputIntensitySlider, _maecSlider, _pseSlider;
        private SliderInt _horizonSlider;
        private Label _tempLabel, _precipLabel, _hedgeRemovalLabel, _inputIntensityLabel, _maecLabel, _pseLabel, _horizonLabel;
        private bool _wiredCallbacks;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveElements();
            InitializeFromScenario();
            WireCallbacks();
        }

        private void OnDisable()
        {
            UnwireCallbacks();
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _tempSlider = root.Q<Slider>(temperatureSliderName);
            _precipSlider = root.Q<Slider>(precipitationSliderName);
            _hedgeRemovalSlider = root.Q<Slider>(hedgeRemovalSliderName);
            _inputIntensitySlider = root.Q<Slider>(inputIntensitySliderName);
            _maecSlider = root.Q<Slider>(maecCoverageSliderName);
            _pseSlider = root.Q<Slider>(pseRateSliderName);
            _horizonSlider = root.Q<SliderInt>(horizonSliderName);

            _tempLabel = root.Q<Label>(temperatureValueLabelName);
            _precipLabel = root.Q<Label>(precipitationValueLabelName);
            _hedgeRemovalLabel = root.Q<Label>(hedgeRemovalValueLabelName);
            _inputIntensityLabel = root.Q<Label>(inputIntensityValueLabelName);
            _maecLabel = root.Q<Label>(maecCoverageValueLabelName);
            _pseLabel = root.Q<Label>(pseRateValueLabelName);
            _horizonLabel = root.Q<Label>(horizonValueLabelName);

            if (_tempSlider == null || _precipSlider == null || _hedgeRemovalSlider == null
                || _inputIntensitySlider == null || _maecSlider == null || _pseSlider == null
                || _horizonSlider == null)
            {
                SimLogger.DebugLog("[ScenarioControlsBinding] one or more scenario sliders not found — check UXML names");
            }
        }

        private void InitializeFromScenario()
        {
            if (runner == null || runner.Scenario == null)
            {
                SimLogger.DebugLog("[ScenarioControlsBinding] runner or scenario not available, skipping init");
                return;
            }
            var s = runner.Scenario;
            SetSlider(_tempSlider, _tempLabel, (float)s.TemperatureAnomalyC.Current, FormatTemperature);
            SetSlider(_precipSlider, _precipLabel, (float)s.PrecipitationAnomalyPercent.Current, FormatPercentSigned);
            SetSlider(_hedgeRemovalSlider, _hedgeRemovalLabel, (float)s.HedgeRemovalRate.Current, FormatHedgeRemoval);
            SetSlider(_inputIntensitySlider, _inputIntensityLabel, (float)s.InputIntensityFactor.Current, FormatIntensity);
            SetSlider(_maecSlider, _maecLabel, (float)s.MaecCoveragePercent.Current, FormatPercent);
            SetSlider(_pseSlider, _pseLabel, (float)s.PseSubsidyRate.Current, FormatPseRate);
            SetSliderInt(_horizonSlider, _horizonLabel, s.HorizonInDays, FormatHorizon);
        }

        private void WireCallbacks()
        {
            if (_wiredCallbacks) return;
            if (_tempSlider != null) _tempSlider.RegisterValueChangedCallback(OnTempChanged);
            if (_precipSlider != null) _precipSlider.RegisterValueChangedCallback(OnPrecipChanged);
            if (_hedgeRemovalSlider != null) _hedgeRemovalSlider.RegisterValueChangedCallback(OnHedgeRemovalChanged);
            if (_inputIntensitySlider != null) _inputIntensitySlider.RegisterValueChangedCallback(OnInputIntensityChanged);
            if (_maecSlider != null) _maecSlider.RegisterValueChangedCallback(OnMaecChanged);
            if (_pseSlider != null) _pseSlider.RegisterValueChangedCallback(OnPseChanged);
            if (_horizonSlider != null) _horizonSlider.RegisterValueChangedCallback(OnHorizonChanged);
            _wiredCallbacks = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wiredCallbacks) return;
            if (_tempSlider != null) _tempSlider.UnregisterValueChangedCallback(OnTempChanged);
            if (_precipSlider != null) _precipSlider.UnregisterValueChangedCallback(OnPrecipChanged);
            if (_hedgeRemovalSlider != null) _hedgeRemovalSlider.UnregisterValueChangedCallback(OnHedgeRemovalChanged);
            if (_inputIntensitySlider != null) _inputIntensitySlider.UnregisterValueChangedCallback(OnInputIntensityChanged);
            if (_maecSlider != null) _maecSlider.UnregisterValueChangedCallback(OnMaecChanged);
            if (_pseSlider != null) _pseSlider.UnregisterValueChangedCallback(OnPseChanged);
            if (_horizonSlider != null) _horizonSlider.UnregisterValueChangedCallback(OnHorizonChanged);
            _wiredCallbacks = false;
        }

        // ---- Callbacks ----

        private void OnTempChanged(ChangeEvent<float> evt)
        {
            if (_tempLabel != null) _tempLabel.text = FormatTemperature(evt.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.Scenario.TemperatureAnomalyC.SetTarget(evt.newValue, transitionDurationDays);
        }

        private void OnPrecipChanged(ChangeEvent<float> evt)
        {
            if (_precipLabel != null) _precipLabel.text = FormatPercentSigned(evt.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.Scenario.PrecipitationAnomalyPercent.SetTarget(evt.newValue, transitionDurationDays);
        }

        private void OnHedgeRemovalChanged(ChangeEvent<float> evt)
        {
            if (_hedgeRemovalLabel != null) _hedgeRemovalLabel.text = FormatHedgeRemoval(evt.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.Scenario.HedgeRemovalRate.SetTarget(evt.newValue, transitionDurationDays);
        }

        private void OnInputIntensityChanged(ChangeEvent<float> evt)
        {
            if (_inputIntensityLabel != null) _inputIntensityLabel.text = FormatIntensity(evt.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.Scenario.InputIntensityFactor.SetTarget(evt.newValue, transitionDurationDays);
        }

        private void OnMaecChanged(ChangeEvent<float> evt)
        {
            if (_maecLabel != null) _maecLabel.text = FormatPercent(evt.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.Scenario.MaecCoveragePercent.SetTarget(evt.newValue, transitionDurationDays);
        }

        private void OnPseChanged(ChangeEvent<float> evt)
        {
            if (_pseLabel != null) _pseLabel.text = FormatPseRate(evt.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.Scenario.PseSubsidyRate.SetTarget(evt.newValue, transitionDurationDays);
        }

        private void OnHorizonChanged(ChangeEvent<int> evt)
        {
            if (_horizonLabel != null) _horizonLabel.text = FormatHorizon(evt.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.Scenario.HorizonInDays = evt.newValue;
        }

        /// <summary>
        /// Snaps the EXOGENOUS sliders (climate, public policy, horizon)
        /// to the supplied target values without triggering callbacks —
        /// so this method alone does NOT push anything to the
        /// ScenarioContext. Used by <see cref="ScenarioPresetsBinding"/>
        /// after it has applied a preset: visual snap of the climate /
        /// policy / horizon sliders while the model interpolates.
        /// <para>
        /// The farmer-controlled sliders (hedge removal, input intensity)
        /// are deliberately NOT touched here — they remain under user
        /// control regardless of which preset is loaded.
        /// </para>
        /// </summary>
        public void SnapToPresetExogenousValues(
            double temperatureAnomalyC,
            double precipitationAnomalyPercent,
            double maecCoveragePercent,
            double pseSubsidyRate,
            int horizonInDays)
        {
            SetSlider(_tempSlider, _tempLabel, (float)temperatureAnomalyC, FormatTemperature);
            SetSlider(_precipSlider, _precipLabel, (float)precipitationAnomalyPercent, FormatPercentSigned);
            SetSlider(_maecSlider, _maecLabel, (float)maecCoveragePercent, FormatPercent);
            SetSlider(_pseSlider, _pseLabel, (float)pseSubsidyRate, FormatPseRate);
            SetSliderInt(_horizonSlider, _horizonLabel, horizonInDays, FormatHorizon);
        }

        // ---- Formatting (InvariantCulture so decimal sep is locale-stable) ----

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private static string FormatTemperature(float v) => v.ToString("+0.0;-0.0;0.0", Inv) + " °C";
        private static string FormatPercentSigned(float v) => v.ToString("+0;-0;0", Inv) + " %";
        private static string FormatPercent(float v) => v.ToString("0", Inv) + " %";
        private static string FormatHedgeRemoval(float v) => v.ToString("0.0", Inv) + " m/ha/an";
        private static string FormatIntensity(float v) => v.ToString("0.0", Inv) + "× réf.";
        private static string FormatPseRate(float v) => v.ToString("0.00", Inv) + " €/m/an";
        private static string FormatHorizon(int v) => v.ToString(Inv) + " j";

        // ---- Helpers ----

        private static void SetSlider(Slider slider, Label label, float value, System.Func<float, string> format)
        {
            if (slider != null) slider.SetValueWithoutNotify(value);
            if (label != null) label.text = format(value);
        }

        private static void SetSliderInt(SliderInt slider, Label label, int value, System.Func<int, string> format)
        {
            if (slider != null) slider.SetValueWithoutNotify(value);
            if (label != null) label.text = format(value);
        }
    }
}
