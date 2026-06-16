using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.Presentation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Remplit les lignes capteur de l'onglet « Climat &amp; ressources » sur le
    /// nouveau modèle : T° moyenne + cumul de pluie (fenêtre glissante 365 j de la
    /// <see cref="Bocage.Decision.SimulationSession"/>), stock de carbone du
    /// sol (RC), azote minéral disponible (RC), et dernier flux net CO2 (tour
    /// Eddy). La ligne « Nappe phréatique » reste pilotée par
    /// <c>WaterTableDetailLabelBinding</c>. Couche 05 — Play Mode.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class OngletClimatBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Glisse le GameObject portant le SimulationRunner.")]
        private SimulationRunner runner;
        [SerializeField, Tooltip("Stock de carbone du sol (tC/ha).")]
        private RC_SoilCarbonStock soilCarbon;
        [SerializeField, Tooltip("Azote minéral disponible (kgN/ha).")]
        private RC_Nitrogen nitrogen;

        [SerializeField] private string temperatureMeanLabelName = "climat-temp-mean-value";
        [SerializeField] private string precipitationCumulativeLabelName = "climat-precip-cumul-value";
        [SerializeField] private string soilCarbonLabelName = "climat-soil-carbon-value";
        [SerializeField] private string netCo2FluxLabelName = "climat-co2-flux-value";
        [SerializeField] private string nitrogenLabelName = "climat-nitrogen-value";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private UIDocument _document;
        private Label _temperatureMeanLabel, _precipitationCumulativeLabel, _soilCarbonLabel, _netCo2FluxLabel, _nitrogenLabel;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveLabels();
            if (runner != null) { runner.TickCompleted += HandleTick; runner.Rebuilt += HandleTick; }
            else SimLogger.DebugLog("[OngletClimatBinding] runner non assigné sur " + name);
            if (soilCarbon != null) { soilCarbon.OnChanged += HandleSoilCarbonChanged; HandleSoilCarbonChanged(soilCarbon.TonnesCarbonPerHectare); }
            if (nitrogen != null) { nitrogen.OnChanged += HandleNitrogenChanged; HandleNitrogenChanged(nitrogen.KgNPerHectare); }
            HandleTick();
        }

        private void OnDisable()
        {
            if (runner != null) { runner.TickCompleted -= HandleTick; runner.Rebuilt -= HandleTick; }
            if (soilCarbon != null) soilCarbon.OnChanged -= HandleSoilCarbonChanged;
            if (nitrogen != null) nitrogen.OnChanged -= HandleNitrogenChanged;
        }

        private void ResolveLabels()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _temperatureMeanLabel = root.Q<Label>(temperatureMeanLabelName);
            _precipitationCumulativeLabel = root.Q<Label>(precipitationCumulativeLabelName);
            _soilCarbonLabel = root.Q<Label>(soilCarbonLabelName);
            _netCo2FluxLabel = root.Q<Label>(netCo2FluxLabelName);
            _nitrogenLabel = root.Q<Label>(nitrogenLabelName);
        }

        private void EnsureResolved()
        {
            if (_temperatureMeanLabel == null || _precipitationCumulativeLabel == null
                || _soilCarbonLabel == null || _netCo2FluxLabel == null || _nitrogenLabel == null)
                ResolveLabels();
        }

        private void HandleTick()
        {
            EnsureResolved();
            var s = runner != null ? runner.Session : null;
            if (s == null) return;
            if (_temperatureMeanLabel != null) _temperatureMeanLabel.text = s.MeanRecentTemperatureC.ToString("F1", Inv);
            if (_precipitationCumulativeLabel != null) _precipitationCumulativeLabel.text = s.RecentPrecipitationCumulMm.ToString("F0", Inv);
            if (_netCo2FluxLabel != null) _netCo2FluxLabel.text = s.LastFluxKgCo2.ToString("F0", Inv);
        }

        private void HandleSoilCarbonChanged(float tonnesCarbonPerHectare)
        {
            EnsureResolved();
            if (_soilCarbonLabel != null) _soilCarbonLabel.text = tonnesCarbonPerHectare.ToString("F1", Inv);
        }

        private void HandleNitrogenChanged(float kgNPerHectare)
        {
            EnsureResolved();
            if (_nitrogenLabel != null) _nitrogenLabel.text = kgNPerHectare.ToString("F0", Inv);
        }
    }
}
