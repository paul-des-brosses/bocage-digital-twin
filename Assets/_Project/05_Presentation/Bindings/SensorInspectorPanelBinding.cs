using System.Collections;
using System.Globalization;
using Bocage.Presentation;
using Bocage.Presentation.Scene.Sensors;
using Bocage.Sensors;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Panneau d'inspection capteur (version <b>légère</b>, S4) : au
    /// clic sur un sprite capteur (<see cref="SensorClickedEventBus"/> → un
    /// <see cref="SensorType"/>), affiche le nom du capteur, son rôle (l'alerte
    /// qu'il arme), sa <b>valeur mesurée du jour</b> (lue sur la session), et son
    /// modèle de bruit (σ). Pas de graphe : les readers sont sans historique
    /// (décision S4). Ferme via le X, un clic sur le fond, ou Échap. Couche 05.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SensorInspectorPanelBinding : MonoBehaviour
    {
        public const string HiddenClass = "hidden";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [SerializeField, Tooltip("Glisse le GameObject portant le SimulationRunner.")]
        private SimulationRunner runner;

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
        [SerializeField] private string footerInfoName = "sensor-inspector-footer-info";

        private UIDocument _document;
        private VisualElement _overlay, _chart1Host, _chart2Row;
        private Button _closeButton;
        private Label _titleLabel, _subtitleLabel, _chart1Caption, _chart1YMax, _chart1YMin, _chart2Caption, _footerLabel;
        private Coroutine _initRoutine;
        private bool _wired;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            _initRoutine = StartCoroutine(InitRoutine());
            SensorClickedEventBus.SensorClicked += HandleSensorClicked;
        }

        private void OnDisable()
        {
            SensorClickedEventBus.SensorClicked -= HandleSensorClicked;
            if (_initRoutine != null) { StopCoroutine(_initRoutine); _initRoutine = null; }
            Unwire();
        }

        private IEnumerator InitRoutine()
        {
            while (_document == null || _document.rootVisualElement == null) yield return null;
            ResolveElements();
            Wire();
            _initRoutine = null;
        }

        private void ResolveElements()
        {
            var root = _document.rootVisualElement;
            _overlay = root.Q<VisualElement>(overlayName);
            _closeButton = root.Q<Button>(closeButtonName);
            _titleLabel = root.Q<Label>(titleLabelName);
            _subtitleLabel = root.Q<Label>(subtitleLabelName);
            _chart1Caption = root.Q<Label>(chart1CaptionName);
            _chart1Host = root.Q<VisualElement>(chart1HostName);
            _chart1YMax = root.Q<Label>(chart1YMaxName);
            _chart1YMin = root.Q<Label>(chart1YMinName);
            _chart2Caption = root.Q<Label>(chart2CaptionName);
            _chart2Row = root.Q<VisualElement>(chart2RowName);
            _footerLabel = root.Q<Label>(footerInfoName);
            if (_overlay == null) SimLogger.DebugLog("[SensorInspectorPanelBinding] overlay introuvable — vérifier les noms UXML");
        }

        private void Wire()
        {
            if (_wired) return;
            if (_closeButton != null) _closeButton.clicked += Close;
            if (_overlay != null) _overlay.RegisterCallback<MouseDownEvent>(OnOverlayMouseDown);
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _wired = true;
        }

        private void Unwire()
        {
            if (!_wired) return;
            if (_closeButton != null) _closeButton.clicked -= Close;
            if (_overlay != null) _overlay.UnregisterCallback<MouseDownEvent>(OnOverlayMouseDown);
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _wired = false;
        }

        private void HandleSensorClicked(SensorType type) => StartCoroutine(ShowAfterFrame(type));

        // Défère d'1 frame : le sprite ouvre via OnMouseDown ; sans ce délai, le
        // mouse-up résiduel sur le backdrop refermerait aussitôt le panneau.
        private IEnumerator ShowAfterFrame(SensorType type)
        {
            yield return null;
            if (_overlay == null) yield break;
            Configure(type);
            _overlay.RemoveFromClassList(HiddenClass);
        }

        private void OnOverlayMouseDown(MouseDownEvent evt)
        {
            if (evt.target == _overlay) Close(); // clic sur le fond (pas la carte)
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape && _overlay != null && !_overlay.ClassListContains(HiddenClass))
                Close();
        }

        private void Close()
        {
            if (_overlay != null) _overlay.AddToClassList(HiddenClass);
        }

        private void Configure(SensorType type)
        {
            var s = runner != null ? runner.Session : null;
            string title, role, value, noise;
            switch (type)
            {
                case SensorType.WeatherStation:
                    title = "Station météo";
                    role = "Arme l'alerte de stress hydrique (humidité du sol mesurée).";
                    value = s != null ? "Humidité du sol : " + (s.MeasuredHumidityFraction * 100.0).ToString("F0", Inv) + " % (θ/RU)" : "—";
                    noise = "Bruit gaussien σ = " + WeatherStationReader.HumidityNoiseSigma.ToString("0.00", Inv)
                            + " sur l'humidité (T° σ = " + WeatherStationReader.TemperatureNoiseSigmaC.ToString("0.0", Inv) + " °C).";
                    break;
                case SensorType.Piezometer:
                    title = "Piézomètre";
                    role = "Lecture de la profondeur de nappe (capteur secondaire).";
                    value = s != null ? "Profondeur nappe : " + s.MeasuredWaterTableDepthM.ToString("F2", Inv) + " m" : "—";
                    noise = "Bruit gaussien σ = " + PiezometerReader.DepthNoiseSigmaM.ToString("0.00", Inv) + " m.";
                    break;
                case SensorType.EddyTower:
                    title = "Tour à flux (Eddy)";
                    role = "Arme l'alerte de carbone bas (stock estimé, intégré du flux mesuré).";
                    value = s != null
                        ? "Carbone estimé : " + s.EstimatedCarbonTPerHa.ToString("F0", Inv) + " tC/ha   ·   Flux : " + s.LastFluxKgCo2.ToString("F0", Inv) + " kgCO₂/ha/j"
                        : "—";
                    noise = "Bruit gaussien σ = " + EddyTowerReader.FluxNoiseSigmaKgCo2.ToString("0.0", Inv) + " kgCO₂/ha/j sur le flux.";
                    break;
                default: // AcousticSensor / CameraTrap → capteur faune
                    title = type == SensorType.CameraTrap ? "Piège photographique (faune)" : "Capteur acoustique (faune)";
                    role = "Arme l'alerte d'anomalie faune (indice de biodiversité mesuré).";
                    value = s != null ? "Indice faune : " + s.MeasuredFauna.ToString("F2", Inv) : "—";
                    noise = "Bruit gaussien σ = " + FaunaSensorReader.ChannelNoiseSigma.ToString("0.00", Inv) + " par canal (acoustique + caméra moyennés).";
                    break;
            }

            if (_titleLabel != null) _titleLabel.text = title;
            if (_subtitleLabel != null) _subtitleLabel.text = role;
            if (_chart1Caption != null) _chart1Caption.text = "Mesure du jour";
            SetHostValue(value);
            if (_chart1YMax != null) _chart1YMax.text = "";
            if (_chart1YMin != null) _chart1YMin.text = "";
            if (_chart2Caption != null) _chart2Caption.AddToClassList(HiddenClass);
            if (_chart2Row != null) _chart2Row.AddToClassList(HiddenClass);
            if (_footerLabel != null)
                _footerLabel.text = noise + "  Primauté du capteur : l'alerte seuille la mesure, pas la vérité du modèle.";
        }

        private void SetHostValue(string value)
        {
            if (_chart1Host == null) return;
            _chart1Host.Clear();
            var label = new Label(value);
            label.style.fontSize = 18f;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexGrow = 1f;
            // Crème explicite (CLAUDE.md §11) : un Label ajouté en code n'hérite
            // d'aucune couleur (la racine n'en fixe pas) et tomberait sur le défaut
            // sombre du thème runtime → invisible sur le host sombre. Les autres
            // labels du panneau ont leur couleur via leur classe USS ; pas celui-ci.
            label.style.color = new Color(238f / 255f, 232f / 255f, 217f / 255f, 1f);
            _chart1Host.Add(label);
        }
    }
}
