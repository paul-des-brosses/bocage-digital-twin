using System.Globalization;
using Bocage.Presentation.Refonte;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Wires the three "Conditions initiales du bocage" sliders
    /// (densité de haie, profondeur de nappe, biodiversité initiale) and the
    /// "Réinitialiser le bocage" button to
    /// <see cref="RefonteSimulationRunner.Rebuild"/>.
    /// <para>
    /// Sliders are editable only when
    /// <c>RefonteSimulationRunner.CurrentDay == 0</c>. Once the simulation has
    /// advanced past day 0, the sliders are disabled and a "verrouillé"
    /// hint appears — clicking Reset is the only way back to an
    /// editable state. The Reset button itself stays enabled at all
    /// times, so the user can rebuild the bocage with the current
    /// slider values from any state of the run.
    /// </para>
    /// <para>
    /// Sliders' default values mirror the EcosystemModel's hardcoded
    /// initial defaults (hedge 90 m/ha, depth 2 m, biodiversité 0.6). Le
    /// scénario (climat + 6 leviers) est préservé à travers un reset —
    /// seul l'état du bocage est remis à zéro.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class InitialConditionsBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source du jour courant et de l'API Rebuild. Glisse le GameObject portant le RefonteSimulationRunner.")]
        private RefonteSimulationRunner runner;

        [Header("UXML element names")]
        [SerializeField] private string hedgerowDensitySliderName = "initial-hedgerow-density-slider";
        [SerializeField] private string waterTableDepthSliderName = "initial-water-table-depth-slider";
        // Historiquement « abondance faune » ; ce slider pilote désormais la
        // biodiversité initiale [0,1] (initialBiodiversity du nouveau modèle).
        // Nom d'élément UXML conservé pour ne pas casser le câblage scène —
        // renommage complet prévu au cutover (étape 5).
        [SerializeField] private string faunaPopulationSliderName = "initial-fauna-population-slider";
        [SerializeField] private string resetButtonName = "initial-reset-button";
        [SerializeField] private string lockHintLabelName = "initial-lock-hint";

        [Header("UXML element names — value labels")]
        [SerializeField] private string hedgerowDensityValueLabelName = "initial-hedgerow-density-value";
        [SerializeField] private string waterTableDepthValueLabelName = "initial-water-table-depth-value";
        [SerializeField] private string faunaPopulationValueLabelName = "initial-fauna-population-value";

        private const string LaunchButtonText = "Lancer la simulation";
        private const string ResetButtonText = "Réinitialiser la simulation";

        private UIDocument _document;
        private Slider _hedgeSlider, _depthSlider, _faunaSlider;
        private Button _resetButton;
        private Label _hedgeLabel, _depthLabel, _faunaLabel, _lockHint;
        private bool _wired;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveElements();
            WireCallbacks();
            if (runner != null) runner.TickCompleted += OnTickCompleted;
            RefreshLockState();
            RefreshAllValueLabels();
        }

        private void OnDisable()
        {
            UnwireCallbacks();
            if (runner != null) runner.TickCompleted -= OnTickCompleted;
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _hedgeSlider = root.Q<Slider>(hedgerowDensitySliderName);
            _depthSlider = root.Q<Slider>(waterTableDepthSliderName);
            _faunaSlider = root.Q<Slider>(faunaPopulationSliderName);
            _resetButton = root.Q<Button>(resetButtonName);
            _hedgeLabel = root.Q<Label>(hedgerowDensityValueLabelName);
            _depthLabel = root.Q<Label>(waterTableDepthValueLabelName);
            _faunaLabel = root.Q<Label>(faunaPopulationValueLabelName);
            _lockHint = root.Q<Label>(lockHintLabelName);

            if (_hedgeSlider == null || _depthSlider == null || _faunaSlider == null || _resetButton == null)
            {
                SimLogger.DebugLog("[InitialConditionsBinding] one or more elements not found — check UXML names");
            }
        }

        private void WireCallbacks()
        {
            if (_wired) return;
            if (_hedgeSlider != null) _hedgeSlider.RegisterValueChangedCallback(OnHedgeChanged);
            if (_depthSlider != null) _depthSlider.RegisterValueChangedCallback(OnDepthChanged);
            if (_faunaSlider != null) _faunaSlider.RegisterValueChangedCallback(OnFaunaChanged);
            if (_resetButton != null) _resetButton.clicked += OnResetClicked;
            _wired = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wired) return;
            if (_hedgeSlider != null) _hedgeSlider.UnregisterValueChangedCallback(OnHedgeChanged);
            if (_depthSlider != null) _depthSlider.UnregisterValueChangedCallback(OnDepthChanged);
            if (_faunaSlider != null) _faunaSlider.UnregisterValueChangedCallback(OnFaunaChanged);
            if (_resetButton != null) _resetButton.clicked -= OnResetClicked;
            _wired = false;
        }

        private void OnHedgeChanged(ChangeEvent<float> evt)
        {
            if (_hedgeLabel != null) _hedgeLabel.text = FormatMetersPerHectare(evt.newValue);
        }

        private void OnDepthChanged(ChangeEvent<float> evt)
        {
            if (_depthLabel != null) _depthLabel.text = FormatMeters(evt.newValue);
        }

        private void OnFaunaChanged(ChangeEvent<float> evt)
        {
            if (_faunaLabel != null) _faunaLabel.text = FormatIndex(evt.newValue);
        }

        private void OnResetClicked()
        {
            if (runner == null || _hedgeSlider == null || _depthSlider == null || _faunaSlider == null) return;
            // Two behaviours depending on whether we're in the
            // "fresh start" state or in the middle of a run :
            //  - Fresh start (button reads "Lancer la simulation",
            //    day==0 && !IsRunning) → Rebuild with current slider
            //    values, set speed to ×1, then start ticking.
            //  - Mid-run (button reads "Réinitialiser la simulation")
            //    → stop ticking, rebuild back to day 0, STAY PAUSED.
            //    The user can then re-adjust any scenario slider and
            //    click "Lancer la simulation" to begin again.
            bool isFreshLaunch = runner.CurrentDay == 0 && !runner.IsRunning;

            runner.StopTicking();
            runner.Rebuild(_hedgeSlider.value, _depthSlider.value, _faunaSlider.value);

            if (isFreshLaunch)
            {
                runner.TicksPerSecond = 1f;
                runner.StartTicking();
            }

            RefreshLockState();
        }

        private void OnTickCompleted()
        {
            RefreshLockState();
        }

        private void RefreshLockState()
        {
            if (runner == null) return;
            bool editable = runner.CurrentDay == 0;
            bool freshStart = editable && !runner.IsRunning;
            SetSliderEnabled(_hedgeSlider, editable);
            SetSliderEnabled(_depthSlider, editable);
            SetSliderEnabled(_faunaSlider, editable);
            if (_lockHint != null)
            {
                _lockHint.text = freshStart
                    ? "Ajustez les valeurs ci-dessus puis lancez la simulation."
                    : (editable
                        ? "Ajustez puis cliquez Réinitialiser pour appliquer."
                        : "Verrouillé en cours de run — cliquer Réinitialiser pour repartir à zéro.");
            }
            if (_resetButton != null)
            {
                _resetButton.text = freshStart ? LaunchButtonText : ResetButtonText;
            }
        }

        private void RefreshAllValueLabels()
        {
            if (_hedgeSlider != null && _hedgeLabel != null) _hedgeLabel.text = FormatMetersPerHectare(_hedgeSlider.value);
            if (_depthSlider != null && _depthLabel != null) _depthLabel.text = FormatMeters(_depthSlider.value);
            if (_faunaSlider != null && _faunaLabel != null) _faunaLabel.text = FormatIndex(_faunaSlider.value);
        }

        private static void SetSliderEnabled(Slider slider, bool enabled)
        {
            if (slider == null) return;
            slider.SetEnabled(enabled);
            slider.EnableInClassList("initial-slider--locked", !enabled);
        }

        private static string FormatMetersPerHectare(float v) => v.ToString("0", Inv) + " m/ha";
        private static string FormatMeters(float v) => v.ToString("0.0", Inv) + " m";
        private static string FormatIndex(float v) => v.ToString("0.00", Inv); // biodiversité initiale [0,1]
    }
}
