using System.Collections.Generic;
using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.Presentation.Simulation;
using Bocage.Sensors;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    // The sibling namespace `Bocage.Presentation.Weather` shadows the simple
    // name `Weather` in the enclosing `Bocage.Presentation` scope, which
    // outranks any compilation-unit using-alias. Declaring the alias INSIDE
    // this namespace body gives it precedence, so `Weather` = the model type.
    using Weather = Bocage.SimulationCore.Model.Weather;

    /// <summary>
    /// Fills the four sensor-derived rows of the "Climat &amp; ressources"
    /// Niveau B panel (chantier E6 / ADR #54): the 365-day mean temperature
    /// and cumulative precipitation read from the WeatherStation sliding
    /// window, the soil carbon stock, and the latest net CO2 flux from the
    /// EddyTower window. The "Nappe phréatique" row is owned by the
    /// pre-existing <c>WaterTableDetailLabelBinding</c> and left untouched.
    /// <para>
    /// Sensor-derived rows refresh on the runner's <c>TickCompleted</c> — the
    /// readers' <c>ReadAndRecord</c> runs earlier in the same tick, so the
    /// window is already fresh. The soil carbon row is driven by
    /// <see cref="RC_SoilCarbonStock"/>.<c>OnChanged</c>, raised in
    /// <c>PublishIndicators</c> after <c>TickCompleted</c>; reading that RC
    /// inside the tick handler would be one tick stale.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class OngletClimatBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the model + sensor histories. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [SerializeField, Tooltip("Soil carbon stock container (tC/ha).")]
        private RC_SoilCarbonStock soilCarbon;

        [SerializeField] private string temperatureMeanLabelName = "climat-temp-mean-value";
        [SerializeField] private string precipitationCumulativeLabelName = "climat-precip-cumul-value";
        [SerializeField] private string soilCarbonLabelName = "climat-soil-carbon-value";
        [SerializeField] private string netCo2FluxLabelName = "climat-co2-flux-value";

        private UIDocument _document;
        private Label _temperatureMeanLabel, _precipitationCumulativeLabel, _soilCarbonLabel, _netCo2FluxLabel;

        // Reused across refreshes so copying the 365-day window never allocates (CLAUDE.md §6).
        private readonly List<Weather> _weatherBuffer = new List<Weather>(WeatherStationReader.HistoryWindowDays);

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveLabels();
            if (runner != null)
            {
                runner.TickCompleted += HandleTick;
                runner.Rebuilt += HandleTick;
            }
            else
            {
                SimLogger.DebugLog("[OngletClimatBinding] runner not assigned on " + name);
            }

            if (soilCarbon != null)
            {
                soilCarbon.OnChanged += HandleSoilCarbonChanged;
                HandleSoilCarbonChanged(soilCarbon.TonnesCarbonPerHectare);
            }

            HandleTick();
        }

        private void OnDisable()
        {
            if (runner != null)
            {
                runner.TickCompleted -= HandleTick;
                runner.Rebuilt -= HandleTick;
            }
            if (soilCarbon != null) soilCarbon.OnChanged -= HandleSoilCarbonChanged;
        }

        private void ResolveLabels()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _temperatureMeanLabel = root.Q<Label>(temperatureMeanLabelName);
            _precipitationCumulativeLabel = root.Q<Label>(precipitationCumulativeLabelName);
            _soilCarbonLabel = root.Q<Label>(soilCarbonLabelName);
            _netCo2FluxLabel = root.Q<Label>(netCo2FluxLabelName);
        }

        private void EnsureResolved()
        {
            if (_temperatureMeanLabel == null || _precipitationCumulativeLabel == null
                || _soilCarbonLabel == null || _netCo2FluxLabel == null)
            {
                ResolveLabels();
            }
        }

        private void HandleTick()
        {
            EnsureResolved();

            WeatherStationReader weatherStation = runner != null ? runner.WeatherStation : null;
            if (weatherStation != null && weatherStation.HistoryCount > 0)
            {
                weatherStation.CopyHistoryTo(_weatherBuffer);
                if (_temperatureMeanLabel != null)
                    _temperatureMeanLabel.text = MeanTemperatureCelsius(_weatherBuffer).ToString("F1", CultureInfo.InvariantCulture);
                if (_precipitationCumulativeLabel != null)
                    _precipitationCumulativeLabel.text = CumulativePrecipitationMm(_weatherBuffer).ToString("F0", CultureInfo.InvariantCulture);
            }

            EddyTowerSensorReader eddyTower = runner != null ? runner.EddyTower : null;
            if (eddyTower != null && eddyTower.TryGetLatest(out double netFlux) && _netCo2FluxLabel != null)
            {
                _netCo2FluxLabel.text = netFlux.ToString("F0", CultureInfo.InvariantCulture);
            }
        }

        private void HandleSoilCarbonChanged(float tonnesCarbonPerHectare)
        {
            EnsureResolved();
            if (_soilCarbonLabel != null)
                _soilCarbonLabel.text = tonnesCarbonPerHectare.ToString("F1", CultureInfo.InvariantCulture);
        }

        /// <summary>Mean daily temperature over the recorded window (°C); 0 when empty. Pure, tested.</summary>
        public static double MeanTemperatureCelsius(IReadOnlyList<Weather> history)
        {
            if (history == null || history.Count == 0) return 0.0;
            double sum = 0.0;
            for (int i = 0; i < history.Count; i++) sum += history[i].TemperatureCelsius;
            return sum / history.Count;
        }

        /// <summary>Cumulative precipitation over the recorded window (mm). Pure, tested.</summary>
        public static double CumulativePrecipitationMm(IReadOnlyList<Weather> history)
        {
            if (history == null) return 0.0;
            double sum = 0.0;
            for (int i = 0; i < history.Count; i++) sum += history[i].PrecipitationMillimeters;
            return sum;
        }
    }
}
