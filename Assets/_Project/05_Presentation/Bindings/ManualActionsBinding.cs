using System.Globalization;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Wires the three « Interventions ponctuelles » buttons in the
    /// decision-panel (espace agriculteur) to the matching
    /// <see cref="SimulationRunner"/> manual-action methods. The
    /// buttons let the agriculteur trigger the same mechanical effects
    /// as the algorithm-driven recommendations (PlantHedges, Irrigation,
    /// ReduceInputs) WITHOUT waiting for an event to fire.
    /// <para>
    /// Sub-étape 10a friction fix : audit revealed that the 3 reco
    /// actions were only accessible through algorithm prompts, which
    /// left the agriculteur passive between events. Manual buttons
    /// give the user agency to test what-if scenarios and stress
    /// the TechDelta KPI on demand.
    /// </para>
    /// <para>
    /// Each slider's value is rendered live in a sibling label; the
    /// button click sends the current magnitude to the runner, which
    /// journals it as an AutoAccepted manual entry (ADR #47) and logs a
    /// SimLogger.UserActionLog line for auditability. The binding itself
    /// keeps no state — it is a thin fire-and-forget dispatcher.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ManualActionsBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [Header("UXML — Plant hedges")]
        [SerializeField] private string plantHedgesSliderName = "manual-plant-hedges-slider";
        [SerializeField] private string plantHedgesValueLabelName = "manual-plant-hedges-value";
        [SerializeField] private string plantHedgesButtonName = "manual-plant-hedges-button";

        [Header("UXML — Irrigation")]
        [SerializeField] private string irrigationSliderName = "manual-irrigation-slider";
        [SerializeField] private string irrigationValueLabelName = "manual-irrigation-value";
        [SerializeField] private string irrigationButtonName = "manual-irrigation-button";

        [Header("UXML — Reduce inputs (pulse)")]
        [SerializeField] private string reduceInputsSliderName = "manual-reduce-inputs-slider";
        [SerializeField] private string reduceInputsValueLabelName = "manual-reduce-inputs-value";
        [SerializeField] private string reduceInputsButtonName = "manual-reduce-inputs-button";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private UIDocument _document;
        private Slider _plantHedgesSlider, _irrigationSlider, _reduceInputsSlider;
        private Label _plantHedgesValue, _irrigationValue, _reduceInputsValue;
        private Button _plantHedgesButton, _irrigationButton, _reduceInputsButton;
        private bool _wired;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveElements();
            WireCallbacks();
            // Push the slider initial values into the labels so the UI
            // reads correctly before the user touches anything.
            if (_plantHedgesSlider != null) RefreshPlantHedgesLabel(_plantHedgesSlider.value);
            if (_irrigationSlider != null) RefreshIrrigationLabel(_irrigationSlider.value);
            if (_reduceInputsSlider != null) RefreshReduceInputsLabel(_reduceInputsSlider.value);
        }

        private void OnDisable()
        {
            UnwireCallbacks();
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;

            _plantHedgesSlider = root.Q<Slider>(plantHedgesSliderName);
            _plantHedgesValue = root.Q<Label>(plantHedgesValueLabelName);
            _plantHedgesButton = root.Q<Button>(plantHedgesButtonName);

            _irrigationSlider = root.Q<Slider>(irrigationSliderName);
            _irrigationValue = root.Q<Label>(irrigationValueLabelName);
            _irrigationButton = root.Q<Button>(irrigationButtonName);

            _reduceInputsSlider = root.Q<Slider>(reduceInputsSliderName);
            _reduceInputsValue = root.Q<Label>(reduceInputsValueLabelName);
            _reduceInputsButton = root.Q<Button>(reduceInputsButtonName);

            if (_plantHedgesButton == null || _irrigationButton == null || _reduceInputsButton == null)
            {
                SimLogger.DebugLog("[ManualActionsBinding] one or more buttons not found — check UXML element names");
            }
        }

        private void WireCallbacks()
        {
            if (_wired) return;
            if (_plantHedgesSlider != null) _plantHedgesSlider.RegisterValueChangedCallback(OnPlantHedgesValueChanged);
            if (_irrigationSlider != null) _irrigationSlider.RegisterValueChangedCallback(OnIrrigationValueChanged);
            if (_reduceInputsSlider != null) _reduceInputsSlider.RegisterValueChangedCallback(OnReduceInputsValueChanged);
            if (_plantHedgesButton != null) _plantHedgesButton.clicked += OnPlantHedgesClicked;
            if (_irrigationButton != null) _irrigationButton.clicked += OnIrrigationClicked;
            if (_reduceInputsButton != null) _reduceInputsButton.clicked += OnReduceInputsClicked;
            _wired = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wired) return;
            if (_plantHedgesSlider != null) _plantHedgesSlider.UnregisterValueChangedCallback(OnPlantHedgesValueChanged);
            if (_irrigationSlider != null) _irrigationSlider.UnregisterValueChangedCallback(OnIrrigationValueChanged);
            if (_reduceInputsSlider != null) _reduceInputsSlider.UnregisterValueChangedCallback(OnReduceInputsValueChanged);
            if (_plantHedgesButton != null) _plantHedgesButton.clicked -= OnPlantHedgesClicked;
            if (_irrigationButton != null) _irrigationButton.clicked -= OnIrrigationClicked;
            if (_reduceInputsButton != null) _reduceInputsButton.clicked -= OnReduceInputsClicked;
            _wired = false;
        }

        private void OnPlantHedgesValueChanged(ChangeEvent<float> evt) => RefreshPlantHedgesLabel(evt.newValue);
        private void OnIrrigationValueChanged(ChangeEvent<float> evt) => RefreshIrrigationLabel(evt.newValue);
        private void OnReduceInputsValueChanged(ChangeEvent<float> evt) => RefreshReduceInputsLabel(evt.newValue);

        private void RefreshPlantHedgesLabel(float v)
        {
            if (_plantHedgesValue != null)
                _plantHedgesValue.text = v.ToString("0", Inv) + " m/ha";
        }
        private void RefreshIrrigationLabel(float v)
        {
            if (_irrigationValue != null)
                _irrigationValue.text = v.ToString("0.0", Inv) + " m";
        }
        private void RefreshReduceInputsLabel(float v)
        {
            if (_reduceInputsValue != null)
                _reduceInputsValue.text = v.ToString("0.00", Inv) + "× réf.";
        }

        private void OnPlantHedgesClicked()
        {
            if (runner == null || _plantHedgesSlider == null) return;
            runner.ApplyManualPlantHedges(_plantHedgesSlider.value);
        }

        private void OnIrrigationClicked()
        {
            if (runner == null || _irrigationSlider == null) return;
            runner.ApplyManualIrrigation(_irrigationSlider.value);
        }

        private void OnReduceInputsClicked()
        {
            if (runner == null || _reduceInputsSlider == null) return;
            runner.ApplyManualReduceInputs(_reduceInputsSlider.value);
        }
    }
}
