using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bocage.Presentation.Scene.Sensors;
using Bocage.Presentation.Simulation;
using Bocage.Presentation.UI;
using Bocage.Sensors;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    // Same alias trick as OngletClimatBinding: the sibling
    // `Bocage.Presentation.Weather` namespace shadows the simple name
    // `Weather` in the enclosing `Bocage.Presentation` scope, which
    // outranks any compilation-unit using-alias. Declaring the alias
    // INSIDE this namespace body gives it precedence.
    using Weather = Bocage.SimulationCore.Model.Weather;

    /// <summary>
    /// Drives the sensor inspection modal popup (chantier E6 / ADR #53).
    /// Subscribes to <see cref="SensorClickedEventBus"/>; when the user
    /// clicks one of the 5 sensor sprites in the scene, the binding
    /// reconfigures the panel for that sensor's type (title, subtitle,
    /// chart series, thresholds, footer info) and removes the
    /// <c>.hidden</c> class from the overlay.
    /// <para>
    /// Each layout pulls fresh history straight from the reader exposed
    /// on the <see cref="SimulationRunner"/>: piezometer paired samples,
    /// weather window, eddy flux series, fauna acoustic + camera per-
    /// channel histories. Per-call buffers are pre-allocated members of
    /// this class — no per-frame allocation on the hot path (CLAUDE.md
    /// §6). Closing happens via the X button, click outside the card,
    /// or the Escape key — same pattern as
    /// <see cref="NiveauBModalsBinding"/>.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SensorInspectorPanelBinding : MonoBehaviour
    {
        public const string HiddenClass = "hidden";

        // Project palette (CLAUDE.md §11) — desaturated accents.
        private static readonly Color SeriesMeasuredColor = new Color(0.486f, 0.659f, 0.392f, 1.0f); // vert pâle
        private static readonly Color SeriesTruthColor = new Color(0.933f, 0.910f, 0.851f, 0.85f);  // crème
        private static readonly Color SeriesPrecipColor = new Color(0.580f, 0.741f, 0.890f, 1.0f);  // bleu pâle
        private static readonly Color ThresholdAlertColor = new Color(0.784f, 0.627f, 0.314f, 1.0f); // ocre
        private static readonly Color ThresholdCriticalColor = new Color(0.706f, 0.353f, 0.275f, 1.0f); // terre brûlée

        private const float SeriesLineWidth = 1.5f;
        private const float ThresholdLineWidth = 1.0f;

        // Piezometer thresholds (cf ADR #53 + EventDetector calibration).
        public const double PiezoAlertDepthMeters = 3.5;
        public const double PiezoCriticalDepthMeters = 5.0;

        [SerializeField, Tooltip("Source of the sensor histories. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        // UXML element names — defaults match the dashboard authoring.
        [SerializeField] private string overlayName = "sensor-inspector-overlay";
        [SerializeField] private string closeButtonName = "sensor-inspector-close";
        [SerializeField] private string titleLabelName = "sensor-inspector-title";
        [SerializeField] private string subtitleLabelName = "sensor-inspector-subtitle";
        [SerializeField] private string chart1CaptionName = "sensor-inspector-chart1-caption";
        [SerializeField] private string chart1HostName = "sensor-inspector-chart1-host";
        [SerializeField] private string chart1YMaxName = "sensor-inspector-chart1-ymax";
        [SerializeField] private string chart1YMinName = "sensor-inspector-chart1-ymin";
        [SerializeField] private string chart2CaptionName = "sensor-inspector-chart2-caption";
        [SerializeField] private string chart2RowName = "sensor-inspector-chart2-row";
        [SerializeField] private string chart2HostName = "sensor-inspector-chart2-host";
        [SerializeField] private string chart2YMaxName = "sensor-inspector-chart2-ymax";
        [SerializeField] private string chart2YMinName = "sensor-inspector-chart2-ymin";
        [SerializeField] private string footerInfoName = "sensor-inspector-footer-info";

        private UIDocument _document;
        private VisualElement _overlay;
        private Button _closeButton;
        private Label _titleLabel, _subtitleLabel;
        private Label _chart1Caption, _chart1YMax, _chart1YMin;
        private Label _chart2Caption, _chart2YMax, _chart2YMin;
        private VisualElement _chart1Host, _chart2Host, _chart2Row;
        private Label _footerLabel;
        private SensorTimeSeriesChart _chart1;
        private SensorTimeSeriesChart _chart2;

        private Coroutine _wireRoutine;
        private bool _escapeRegistered;
        private EventCallback<KeyDownEvent> _escapeHandler;
        // MouseDown (not ClickEvent): the panel can open via a SCENE
        // sprite OnMouseDown that fires BEFORE the matching mouse-up.
        // When the mouse-up lands on the now-visible overlay backdrop,
        // ClickEvent would treat it as a click-outside and close the
        // modal we just opened. MouseDown only fires on a NEW press, so
        // the residual mouse-up no longer triggers a false dismissal.
        private EventCallback<MouseDownEvent> _overlayMouseDownHandler;

        // Reusable scratch buffers — never allocated per-frame.
        private readonly List<SensorSample<double>> _paired = new List<SensorSample<double>>(365);
        private readonly List<double> _scalars = new List<double>(365);
        private readonly List<Weather> _weather = new List<Weather>(365);
        private readonly List<float> _floatsA = new List<float>(365);
        private readonly List<float> _floatsB = new List<float>(365);

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            _wireRoutine = StartCoroutine(InitRoutine());
            SensorClickedEventBus.SensorClicked += HandleSensorClicked;
        }

        private void OnDisable()
        {
            SensorClickedEventBus.SensorClicked -= HandleSensorClicked;
            if (_wireRoutine != null)
            {
                StopCoroutine(_wireRoutine);
                _wireRoutine = null;
            }
            if (_closeButton != null) _closeButton.clicked -= HandleClose;
            if (_overlay != null && _overlayMouseDownHandler != null) _overlay.UnregisterCallback(_overlayMouseDownHandler);
            if (_escapeRegistered && _document != null && _document.rootVisualElement != null && _escapeHandler != null)
            {
                _document.rootVisualElement.UnregisterCallback(_escapeHandler, TrickleDown.TrickleDown);
                _escapeRegistered = false;
                _escapeHandler = null;
            }
        }

        private IEnumerator InitRoutine()
        {
            const int maxAttempts = 300;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (TryWire())
                {
                    _wireRoutine = null;
                    yield break;
                }
                yield return null;
            }
            SimLogger.DebugLog("[SensorInspectorPanelBinding] could not wire after " + maxAttempts + " frames on " + name);
            _wireRoutine = null;
        }

        private bool TryWire()
        {
            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root == null) return false;
            _overlay = root.Q<VisualElement>(overlayName);
            if (_overlay == null) return false;

            _closeButton = root.Q<Button>(closeButtonName);
            _titleLabel = root.Q<Label>(titleLabelName);
            _subtitleLabel = root.Q<Label>(subtitleLabelName);
            _chart1Caption = root.Q<Label>(chart1CaptionName);
            _chart1Host = root.Q<VisualElement>(chart1HostName);
            _chart1YMax = root.Q<Label>(chart1YMaxName);
            _chart1YMin = root.Q<Label>(chart1YMinName);
            _chart2Caption = root.Q<Label>(chart2CaptionName);
            _chart2Row = root.Q<VisualElement>(chart2RowName);
            _chart2Host = root.Q<VisualElement>(chart2HostName);
            _chart2YMax = root.Q<Label>(chart2YMaxName);
            _chart2YMin = root.Q<Label>(chart2YMinName);
            _footerLabel = root.Q<Label>(footerInfoName);

            // Instantiate custom chart elements programmatically and insert
            // into their UXML hosts (avoids UxmlFactory boilerplate).
            if (_chart1Host != null && _chart1 == null)
            {
                _chart1 = new SensorTimeSeriesChart();
                _chart1Host.Add(_chart1);
            }
            if (_chart2Host != null && _chart2 == null)
            {
                _chart2 = new SensorTimeSeriesChart();
                _chart2Host.Add(_chart2);
            }

            if (_closeButton != null) _closeButton.clicked += HandleClose;
            // Click-outside dismissal via MouseDown (not Click) — see field
            // doc above. Only fires when the press target IS the overlay
            // itself (not bubbled up from the card or its descendants).
            _overlayMouseDownHandler = evt =>
            {
                if (evt.target == _overlay) HandleClose();
            };
            _overlay.RegisterCallback(_overlayMouseDownHandler);

            if (!_escapeRegistered)
            {
                _escapeHandler = OnKeyDown;
                root.RegisterCallback(_escapeHandler, TrickleDown.TrickleDown);
                _escapeRegistered = true;
            }
            return true;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape) HandleClose();
        }

        private void HandleSensorClicked(SensorType type)
        {
            if (_overlay == null) return;
            if (runner == null)
            {
                SimLogger.DebugLog("[SensorInspectorPanelBinding] runner not assigned; cannot populate panel for " + type);
                return;
            }
            switch (type)
            {
                case SensorType.Piezometer: ConfigureForPiezometer(); break;
                case SensorType.WeatherStation: ConfigureForWeatherStation(); break;
                case SensorType.EddyTower: ConfigureForEddyTower(); break;
                case SensorType.AcousticSensor: ConfigureForAcoustic(); break;
                case SensorType.CameraTrap: ConfigureForCameraTrap(); break;
                default: return;
            }
            // Defer the show by one frame so the scene sprite's legacy
            // OnMouseDown frame finishes processing BEFORE the overlay
            // becomes visible. Otherwise UI Toolkit sees the same
            // mouse-down event on the now-visible overlay (cursor was
            // on the sprite, sprite is on the same screen coordinates
            // as the overlay backdrop for 3 of 5 sensors) and our
            // MouseDownEvent handler dismisses the modal immediately.
            // ConfigurePanelFor* above runs sync so content is ready
            // when the overlay flips visible next frame.
            StartCoroutine(ShowOverlayNextFrame());
        }

        private IEnumerator ShowOverlayNextFrame()
        {
            yield return null;
            if (_overlay != null) _overlay.RemoveFromClassList(HiddenClass);
        }

        private void HandleClose()
        {
            if (_overlay == null) return;
            _overlay.AddToClassList(HiddenClass);
        }

        // ----- Per-sensor layout configurators ------------------------------

        private void ConfigureForPiezometer()
        {
            PiezometerReader r = runner.Piezometer;
            _titleLabel.text = "Piézomètre";
            _subtitleLabel.text = "Profondeur de la nappe phréatique mesurée par sonde à pression. " +
                                  "Bruit gaussien σ ≈ 5 cm.";
            _chart1Caption.text = "Profondeur sur 365 j — mesure (vert) vs vérité (crème) ; seuils alerte / critique.";

            r.CopyHistoryTo(_paired);
            ExtractMeasuredFloats(_paired, _floatsA);
            ExtractTruthFloats(_paired, _floatsB);

            const float yMin = 0f, yMax = 7f;
            _chart1.SetYBounds(yMin, yMax);
            _chart1.ClearThresholds();
            _chart1.AddThreshold(ThresholdAlertColor, ThresholdLineWidth, (float)PiezoAlertDepthMeters);
            _chart1.AddThreshold(ThresholdCriticalColor, ThresholdLineWidth, (float)PiezoCriticalDepthMeters);
            _chart1.ClearSeries();
            _chart1.AddSeries(SeriesTruthColor, SeriesLineWidth, _floatsB);
            _chart1.AddSeries(SeriesMeasuredColor, SeriesLineWidth, _floatsA);
            _chart1YMax.text = yMax.ToString("F1", CultureInfo.InvariantCulture) + " m";
            _chart1YMin.text = yMin.ToString("F1", CultureInfo.InvariantCulture) + " m";

            HideSecondChart();
            int trailing = ComputeTrailingDaysAboveThreshold(_paired, PiezoAlertDepthMeters);
            _footerLabel.text = "Jours consécutifs récents au-dessus du seuil alerte (3,5 m) : "
                + trailing.ToString(CultureInfo.InvariantCulture);
        }

        private void ConfigureForWeatherStation()
        {
            WeatherStationReader r = runner.WeatherStation;
            _titleLabel.text = "Station météo";
            _subtitleLabel.text = "Mesures T° + précipitations journalières. Bruit thermistor σ = 0,3 °C, " +
                                  "pluie σ = 5 %.";
            _chart1Caption.text = "Température journalière (365 j)";
            _chart2Caption.text = "Précipitations journalières (365 j)";

            r.CopyHistoryTo(_weather);
            ExtractTemperatureFloats(_weather, _floatsA);
            ExtractPrecipitationFloats(_weather, _floatsB);

            const float t1Min = -5f, t1Max = 35f;
            _chart1.SetYBounds(t1Min, t1Max);
            _chart1.ClearThresholds();
            _chart1.ClearSeries();
            _chart1.AddSeries(SeriesMeasuredColor, SeriesLineWidth, _floatsA);
            _chart1YMax.text = t1Max.ToString("F0", CultureInfo.InvariantCulture) + " °C";
            _chart1YMin.text = t1Min.ToString("F0", CultureInfo.InvariantCulture) + " °C";

            const float t2Min = 0f, t2Max = 40f;
            _chart2.SetYBounds(t2Min, t2Max);
            _chart2.ClearThresholds();
            _chart2.ClearSeries();
            _chart2.AddSeries(SeriesPrecipColor, SeriesLineWidth, _floatsB);
            _chart2YMax.text = t2Max.ToString("F0", CultureInfo.InvariantCulture) + " mm";
            _chart2YMin.text = t2Min.ToString("F0", CultureInfo.InvariantCulture) + " mm";
            ShowSecondChart();

            _footerLabel.text = BuildWeatherFooter(runner);
        }

        private void ConfigureForEddyTower()
        {
            EddyTowerSensorReader r = runner.EddyTower;
            _titleLabel.text = "Tour de covariance (EddyTower)";
            _subtitleLabel.text = "Flux net CO2 jour-à-jour dérivé du delta stock de carbone du sol. " +
                                  "Convention NEE : positif = émission, négatif = séquestration.";
            _chart1Caption.text = "Flux net CO2 (365 j, kgCO2/ha/jour)";

            r.CopyHistoryTo(_scalars);
            ExtractScalarFloats(_scalars, _floatsA);

            const float yMin = -40f, yMax = 40f;
            _chart1.SetYBounds(yMin, yMax);
            _chart1.ClearThresholds();
            _chart1.AddThreshold(SeriesTruthColor, ThresholdLineWidth, 0f); // baseline zero
            _chart1.ClearSeries();
            _chart1.AddSeries(SeriesMeasuredColor, SeriesLineWidth, _floatsA);
            _chart1YMax.text = yMax.ToString("F0", CultureInfo.InvariantCulture) + " kg";
            _chart1YMin.text = yMin.ToString("F0", CultureInfo.InvariantCulture) + " kg";

            HideSecondChart();
            _footerLabel.text = "Référence zéro = ligne crème (système à l'équilibre). " +
                                "Stock C courant : " + runner.Model.SoilCarbonStock.ToString("F1", CultureInfo.InvariantCulture) + " tC/ha.";
        }

        private void ConfigureForAcoustic()
        {
            AcousticSensorReader r = runner.FaunaSensor.Acoustic;
            _titleLabel.text = "Capteur acoustique faune";
            _subtitleLabel.text = "Recorder passif. Bruit Poisson : σ = 0,20 / √abondance — les espèces rares " +
                                  "génèrent des estimations plus dispersées.";
            _chart1Caption.text = "Abondance — mesure (vert) vs vérité modèle (crème), 365 j.";

            ConfigureFaunaChart(r);
            HideSecondChart();
            _footerLabel.text = "L'écart mesure-vérité visualise l'incertitude propre au capteur. " +
                                "Le détecteur d'événements lit la fusion acoustique + caméra, pas un canal seul.";
        }

        private void ConfigureForCameraTrap()
        {
            CameraTrapSensorReader r = runner.FaunaSensor.Camera;
            _titleLabel.text = "Piège photo (camera trap)";
            _subtitleLabel.text = "Détection visuelle nocturne. Même modèle de bruit Poisson que l'acoustique ; " +
                                  "séquence indépendante.";
            _chart1Caption.text = "Abondance — mesure (vert) vs vérité modèle (crème), 365 j.";

            ConfigureFaunaChart(r);
            HideSecondChart();
            _footerLabel.text = "Capteur indépendant de l'acoustique. La fusion arithmétique des deux divise " +
                                "l'écart-type effectif par √2.";
        }

        private void ConfigureFaunaChart(ISensorHistory<SensorSample<double>> r)
        {
            r.CopyHistoryTo(_paired);
            ExtractMeasuredFloats(_paired, _floatsA);
            ExtractTruthFloats(_paired, _floatsB);

            const float yMin = 0f, yMax = 1.5f;
            _chart1.SetYBounds(yMin, yMax);
            _chart1.ClearThresholds();
            _chart1.ClearSeries();
            _chart1.AddSeries(SeriesTruthColor, SeriesLineWidth, _floatsB);
            _chart1.AddSeries(SeriesMeasuredColor, SeriesLineWidth, _floatsA);
            _chart1YMax.text = yMax.ToString("F1", CultureInfo.InvariantCulture) + " × réf.";
            _chart1YMin.text = yMin.ToString("F1", CultureInfo.InvariantCulture) + " × réf.";
        }

        private void HideSecondChart()
        {
            if (_chart2Caption != null && !_chart2Caption.ClassListContains(HiddenClass)) _chart2Caption.AddToClassList(HiddenClass);
            if (_chart2Row != null && !_chart2Row.ClassListContains(HiddenClass)) _chart2Row.AddToClassList(HiddenClass);
        }

        private void ShowSecondChart()
        {
            if (_chart2Caption != null) _chart2Caption.RemoveFromClassList(HiddenClass);
            if (_chart2Row != null) _chart2Row.RemoveFromClassList(HiddenClass);
        }

        // ----- Static pure helpers (testable in EditMode) -------------------

        /// <summary>Copies the <c>Measured</c> field of each sample as a float into the reused buffer.</summary>
        public static void ExtractMeasuredFloats(IList<SensorSample<double>> source, List<float> destination)
        {
            destination.Clear();
            if (source == null) return;
            for (int i = 0; i < source.Count; i++) destination.Add((float)source[i].Measured);
        }

        /// <summary>Copies the <c>Truth</c> field of each sample as a float into the reused buffer.</summary>
        public static void ExtractTruthFloats(IList<SensorSample<double>> source, List<float> destination)
        {
            destination.Clear();
            if (source == null) return;
            for (int i = 0; i < source.Count; i++) destination.Add((float)source[i].Truth);
        }

        /// <summary>Casts <c>double</c> samples to <c>float</c> into the reused buffer.</summary>
        public static void ExtractScalarFloats(IList<double> source, List<float> destination)
        {
            destination.Clear();
            if (source == null) return;
            for (int i = 0; i < source.Count; i++) destination.Add((float)source[i]);
        }

        /// <summary>Pulls the temperature channel of each Weather sample as a float.</summary>
        public static void ExtractTemperatureFloats(IList<Weather> source, List<float> destination)
        {
            destination.Clear();
            if (source == null) return;
            for (int i = 0; i < source.Count; i++) destination.Add((float)source[i].TemperatureCelsius);
        }

        /// <summary>Pulls the precipitation channel of each Weather sample as a float.</summary>
        public static void ExtractPrecipitationFloats(IList<Weather> source, List<float> destination)
        {
            destination.Clear();
            if (source == null) return;
            for (int i = 0; i < source.Count; i++) destination.Add((float)source[i].PrecipitationMillimeters);
        }

        /// <summary>
        /// Counts the trailing run of consecutive most-recent samples whose
        /// <c>Measured</c> value exceeds <paramref name="threshold"/>. Used
        /// for the piezometer panel's « jours consécutifs au-dessus du
        /// seuil alerte » footer line. Pure — covered by EditMode tests.
        /// </summary>
        public static int ComputeTrailingDaysAboveThreshold(IList<SensorSample<double>> history, double threshold)
        {
            if (history == null) return 0;
            int run = 0;
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i].Measured > threshold) run++;
                else break;
            }
            return run;
        }

        private static string BuildWeatherFooter(SimulationRunner runner)
        {
            // Defensive: we can be called before the engine builds anything.
            var data = runner != null ? runner.SeasonalWeather : null;
            var model = runner != null ? runner.Model : null;
            var scenario = runner != null ? runner.Scenario : null;
            if (data == null || model == null || scenario == null)
                return "Normales mensuelles indisponibles.";

            int startingMonth = scenario.StartingMonth;
            int currentDay = runner.CurrentDay;
            int currentMonth = Bocage.SimulationCore.Model.SeasonalWeatherData.MonthIndexForDay(currentDay, startingMonth);
            int nextMonth = (currentMonth + 1) % 12;
            var cur = data.GetForMonth(currentMonth);
            var nxt = data.GetForMonth(nextMonth);
            double curPrecip = MonthlyExpectedPrecipitationMm(cur, currentMonth);
            double nxtPrecip = MonthlyExpectedPrecipitationMm(nxt, nextMonth);
            return "Normales — mois courant : "
                + cur.TemperatureMeanCelsius.ToString("F1", CultureInfo.InvariantCulture) + " °C, "
                + curPrecip.ToString("F0", CultureInfo.InvariantCulture) + " mm. "
                + "Mois suivant : "
                + nxt.TemperatureMeanCelsius.ToString("F1", CultureInfo.InvariantCulture) + " °C, "
                + nxtPrecip.ToString("F0", CultureInfo.InvariantCulture) + " mm.";
        }

        /// <summary>
        /// Reconstructs the expected monthly precipitation from the
        /// Markov + log-normal parameters (same identity as the test
        /// <c>DefaultMortagneAuPercheCalibrationIsInternallyConsistent</c>):
        /// daysInMonth × p_wet × exp(mu + sigma²/2). Pure — testable
        /// without Unity.
        /// </summary>
        public static double MonthlyExpectedPrecipitationMm(Bocage.SimulationCore.Model.MonthlyClimate climate, int monthIndex)
        {
            int daysIn = Bocage.SimulationCore.Model.SeasonalWeatherData.DaysIn(monthIndex);
            double expectedDaily = Math.Exp(climate.LogNormalMu + 0.5 * climate.LogNormalSigma * climate.LogNormalSigma);
            return daysIn * climate.ProbabilityWetDay * expectedDaily;
        }
    }
}
