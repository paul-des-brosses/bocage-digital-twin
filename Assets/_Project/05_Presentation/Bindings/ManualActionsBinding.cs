using System.Globalization;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Wires the two « Interventions ponctuelles » buttons in the
    /// decision-panel (espace agriculteur) to the matching
    /// <see cref="SimulationRunner"/> manual-action methods. The buttons let
    /// the agriculteur trigger the same one-off effects as the algorithm-driven
    /// recommendations (PlantHedges, Irrigation) WITHOUT waiting for an event
    /// to fire. (Reducing input intensity is a sustained PRACTICE, not a
    /// one-off action, so it lives on the « Intensité d'intrants » daily
    /// decision slider — and the « anomalie faune » recommendation lowers that
    /// slider — instead of a punctual button.)
    /// <para>
    /// Each slider's value is rendered live in a sibling label; the button
    /// click sends the current magnitude to the runner, which journals it as
    /// an AutoAccepted manual entry (ADR #47) and logs a
    /// SimLogger.UserActionLog line for auditability. The binding itself keeps
    /// no state — it is a thin fire-and-forget dispatcher.
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

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private UIDocument _document;
        private Slider _plantHedgesSlider, _irrigationSlider;
        private Label _plantHedgesValue, _irrigationValue;
        private Button _plantHedgesButton, _irrigationButton;
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

            if (_plantHedgesButton == null || _irrigationButton == null)
            {
                SimLogger.DebugLog("[ManualActionsBinding] one or more buttons not found — check UXML element names");
            }
        }

        private void WireCallbacks()
        {
            if (_wired) return;
            if (_plantHedgesSlider != null) _plantHedgesSlider.RegisterValueChangedCallback(OnPlantHedgesValueChanged);
            if (_irrigationSlider != null) _irrigationSlider.RegisterValueChangedCallback(OnIrrigationValueChanged);
            if (_plantHedgesButton != null) _plantHedgesButton.clicked += OnPlantHedgesClicked;
            if (_irrigationButton != null) _irrigationButton.clicked += OnIrrigationClicked;
            _wired = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wired) return;
            if (_plantHedgesSlider != null) _plantHedgesSlider.UnregisterValueChangedCallback(OnPlantHedgesValueChanged);
            if (_irrigationSlider != null) _irrigationSlider.UnregisterValueChangedCallback(OnIrrigationValueChanged);
            if (_plantHedgesButton != null) _plantHedgesButton.clicked -= OnPlantHedgesClicked;
            if (_irrigationButton != null) _irrigationButton.clicked -= OnIrrigationClicked;
            _wired = false;
        }

        private void OnPlantHedgesValueChanged(ChangeEvent<float> evt) => RefreshPlantHedgesLabel(evt.newValue);
        private void OnIrrigationValueChanged(ChangeEvent<float> evt) => RefreshIrrigationLabel(evt.newValue);

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
    }
}
