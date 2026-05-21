using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bocage.Data.RuntimeContainers;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Diagnostics
{
    /// <summary>
    /// Records every tick of the simulation as one CSV row capturing
    /// the full model state, the scenario inputs and the published KPI
    /// containers. Useful when KPI behaviour looks suspicious: the user
    /// runs a session, exports the trace, and we read the CSV to look
    /// for off-by-one factors, sign inversions, unit mismatches, or
    /// runaway accumulators that aren't visible at the UI cadence.
    /// <para>
    /// Subscribes to <see cref="SimulationRunner.TickCompleted"/> rather
    /// than polling Update(): we get exactly one row per simulated day,
    /// no skipping, no double counting, regardless of the runner's
    /// ticksPerSecond setting.
    /// </para>
    /// <para>
    /// The file is written under <c>{ProjectRoot}/Logs/</c> on
    /// OnDestroy (or when the inspector's "Export trace now" button is
    /// pressed). Naming convention <c>trace-{yyyyMMdd-HHmmss}.csv</c>;
    /// the folder is auto-created. CSV uses InvariantCulture so the
    /// decimal separator is a period, locale-independent.
    /// </para>
    /// <para>
    /// Allocations: one List grows during the run, one string per tick
    /// is built via interpolation. For a 365-day run that's ~365
    /// strings — negligible. A safety <see cref="maxRowsBeforeFlush"/>
    /// caps the in-memory buffer so a forgotten Play session at x20 for
    /// hours doesn't OOM the editor.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-7000)]
    public sealed class SimulationTraceRecorder : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the simulation tick. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [Header("Observable containers (KPI snapshots)")]
        [SerializeField] private RC_HedgerowDensity hedgerowDensityContainer;
        [SerializeField] private RC_WaterTableDepth waterTableContainer;
        [SerializeField] private RC_IntegratedProfitability profitabilityContainer;

        [Header("Output")]
        [SerializeField, Tooltip("Folder relative to the project root where the CSV is written.")]
        private string outputFolder = "Logs";

        [SerializeField, Tooltip("Hard cap on rows kept in memory. Beyond this the recorder stops appending.")]
        private int maxRowsBeforeFlush = 200000;

        [SerializeField, Tooltip("If true, the trace is automatically exported when the recorder is destroyed (Play stops, scene unloads).")]
        private bool autoExportOnDestroy = true;

        private List<string> _rows;
        private bool _subscribed;
        private bool _capped;

        private const string CsvHeader =
            "day,temp_C,precip_mm," +
            "hedgerow_density_m_per_ha,water_table_depth_m,crop_yield_t_per_ha,input_cost_eur_per_ha,maintenance_cost_eur_per_ha," +
            "scn_temp_anomaly_C,scn_precip_anomaly_pct,scn_hedge_removal_m_per_ha_per_yr,scn_input_intensity,scn_maec_pct,scn_pse_rate_eur_per_m_per_yr,scn_horizon_days," +
            "kpi_hedgerow_m_per_ha,kpi_water_depth_m,kpi_profitability_eur_per_ha";

        private void OnEnable()
        {
            _rows = new List<string>(1024) { CsvHeader };
            _capped = false;

            if (runner == null)
            {
                SimLogger.DebugLog("[SimulationTraceRecorder] runner not assigned — no trace will be captured");
                return;
            }

            runner.TickCompleted += OnTickCompleted;
            _subscribed = true;

            // Capture an initial snapshot at day 0 if the engine is built.
            // The runner publishes indicators in Awake, so by the time our
            // OnEnable runs the model has its initial state.
            if (runner.Model != null)
            {
                _rows.Add(BuildRow());
            }
        }

        private void OnDisable()
        {
            if (_subscribed && runner != null)
            {
                runner.TickCompleted -= OnTickCompleted;
                _subscribed = false;
            }
        }

        private void OnDestroy()
        {
            if (autoExportOnDestroy) ExportToFile();
        }

        private void OnTickCompleted()
        {
            if (_capped) return;
            if (_rows.Count >= maxRowsBeforeFlush)
            {
                _capped = true;
                SimLogger.DebugLog("[SimulationTraceRecorder] reached maxRowsBeforeFlush (" + maxRowsBeforeFlush + "), further ticks ignored");
                return;
            }
            _rows.Add(BuildRow());
        }

        private string BuildRow()
        {
            var model = runner.Model;
            var scenario = runner.Scenario;
            var inv = CultureInfo.InvariantCulture;

            string day = model.CurrentDay.ToString(inv);
            string tempC = model.CurrentWeather.TemperatureCelsius.ToString("F3", inv);
            string precip = model.CurrentWeather.PrecipitationMillimeters.ToString("F3", inv);
            string hedge = model.HedgerowDensity.ToString("F4", inv);
            string water = model.WaterTableDepth.ToString("F4", inv);
            string yield = model.CropYield.ToString("F4", inv);
            string inputs = model.InputCost.ToString("F3", inv);
            string maint = model.MaintenanceCost.ToString("F3", inv);

            string sTemp = scenario != null ? scenario.TemperatureAnomalyC.Current.ToString("F3", inv) : "";
            string sPrecip = scenario != null ? scenario.PrecipitationAnomalyPercent.Current.ToString("F3", inv) : "";
            string sHedge = scenario != null ? scenario.HedgeRemovalRate.Current.ToString("F3", inv) : "";
            string sIntensity = scenario != null ? scenario.InputIntensityFactor.Current.ToString("F3", inv) : "";
            string sMaec = scenario != null ? scenario.MaecCoveragePercent.Current.ToString("F3", inv) : "";
            string sPse = scenario != null ? scenario.PseSubsidyRate.Current.ToString("F4", inv) : "";
            string sHorizon = scenario != null ? scenario.HorizonInDays.ToString(inv) : "";

            string kHedge = hedgerowDensityContainer != null
                ? hedgerowDensityContainer.MetersPerHectare.ToString("F4", inv) : "";
            string kWater = waterTableContainer != null
                ? waterTableContainer.DepthMeters.ToString("F4", inv) : "";
            string kProfit = profitabilityContainer != null
                ? profitabilityContainer.EurosPerHectare.ToString("F3", inv) : "";

            return string.Concat(
                day, ",", tempC, ",", precip, ",",
                hedge, ",", water, ",", yield, ",", inputs, ",", maint, ",",
                sTemp, ",", sPrecip, ",", sHedge, ",", sIntensity, ",", sMaec, ",", sPse, ",", sHorizon, ",",
                kHedge, ",", kWater, ",", kProfit);
        }

        /// <summary>
        /// Writes the accumulated rows to a CSV file under
        /// <c>{ProjectRoot}/{outputFolder}/trace-{timestamp}.csv</c> and
        /// returns the absolute path. Safe to call at runtime or via
        /// inspector button.
        /// </summary>
        public string ExportToFile()
        {
            if (_rows == null || _rows.Count <= 1)
            {
                SimLogger.DebugLog("[SimulationTraceRecorder] nothing to export (no rows captured)");
                return null;
            }

            // Application.dataPath ends in /Assets — go one up to project root.
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string folder = Path.Combine(projectRoot, outputFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string filename = "trace-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv";
            string fullPath = Path.Combine(folder, filename);

            File.WriteAllLines(fullPath, _rows);
            SimLogger.SimulationLog("[SimulationTraceRecorder] exported " + (_rows.Count - 1) + " ticks → " + fullPath);
            return fullPath;
        }
    }
}
