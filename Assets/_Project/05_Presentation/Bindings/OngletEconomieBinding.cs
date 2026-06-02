using System.Globalization;
using Bocage.Data.RuntimeContainers;
using Bocage.Indicators.Hero;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Fills the seven rows of the "Économie" Niveau B panel (chantier E6 /
    /// ADR #54). Crop yield, input cost, maintenance cost, PSE and CAP are
    /// read from the model + scenario on the runner's <c>TickCompleted</c>;
    /// cumulative investment and the rentability horizon are driven by their
    /// RCs' <c>OnChanged</c> (raised after <c>TickCompleted</c>). PSE/CAP
    /// reuse the public constants of
    /// <see cref="IntegratedProfitabilityIndicator"/> so the breakdown shown
    /// here can never drift from the Hero profitability KPI.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class OngletEconomieBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the model + scenario. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [SerializeField, Tooltip("Cumulative investment container (€/ha).")]
        private RC_TotalInvestment totalInvestment;
        [SerializeField, Tooltip("Investment rentability horizon container.")]
        private RC_InvestmentHorizon investmentHorizon;

        [SerializeField] private string cropYieldLabelName = "eco-yield-value";
        [SerializeField] private string inputCostLabelName = "eco-input-cost-value";
        [SerializeField] private string maintenanceLabelName = "eco-maintenance-value";
        [SerializeField] private string pseLabelName = "eco-pse-value";
        [SerializeField] private string pacLabelName = "eco-pac-value";
        [SerializeField] private string totalInvestmentLabelName = "eco-total-investment-value";
        [SerializeField] private string horizonLabelName = "eco-horizon-value";

        private UIDocument _document;
        private Label _cropYieldLabel, _inputCostLabel, _maintenanceLabel, _pseLabel, _pacLabel, _totalInvestmentLabel, _horizonLabel;

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
                SimLogger.DebugLog("[OngletEconomieBinding] runner not assigned on " + name);
            }

            if (totalInvestment != null)
            {
                totalInvestment.OnChanged += HandleTotalInvestmentChanged;
                HandleTotalInvestmentChanged(totalInvestment.EurosPerHectare);
            }
            if (investmentHorizon != null)
            {
                investmentHorizon.OnChanged += HandleHorizonChanged;
                HandleHorizonChanged(investmentHorizon.IsReached);
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
            if (totalInvestment != null) totalInvestment.OnChanged -= HandleTotalInvestmentChanged;
            if (investmentHorizon != null) investmentHorizon.OnChanged -= HandleHorizonChanged;
        }

        private void ResolveLabels()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _cropYieldLabel = root.Q<Label>(cropYieldLabelName);
            _inputCostLabel = root.Q<Label>(inputCostLabelName);
            _maintenanceLabel = root.Q<Label>(maintenanceLabelName);
            _pseLabel = root.Q<Label>(pseLabelName);
            _pacLabel = root.Q<Label>(pacLabelName);
            _totalInvestmentLabel = root.Q<Label>(totalInvestmentLabelName);
            _horizonLabel = root.Q<Label>(horizonLabelName);
        }

        private void EnsureResolved()
        {
            if (_cropYieldLabel == null || _inputCostLabel == null || _maintenanceLabel == null
                || _pseLabel == null || _pacLabel == null || _totalInvestmentLabel == null || _horizonLabel == null)
            {
                ResolveLabels();
            }
        }

        private void HandleTick()
        {
            EnsureResolved();
            var model = runner != null ? runner.Model : null;
            if (model == null) return;
            var scenario = runner.Scenario;
            double pseRate = scenario != null ? scenario.PseSubsidyRate.Current : 0.0;

            if (_cropYieldLabel != null) _cropYieldLabel.text = model.CropYield.ToString("F1", CultureInfo.InvariantCulture);
            if (_inputCostLabel != null) _inputCostLabel.text = model.InputCost.ToString("F0", CultureInfo.InvariantCulture);
            if (_maintenanceLabel != null) _maintenanceLabel.text = model.MaintenanceCost.ToString("F0", CultureInfo.InvariantCulture);
            if (_pseLabel != null) _pseLabel.text = ComputePseEurosPerHectare(model.HedgerowDensity, pseRate).ToString("F0", CultureInfo.InvariantCulture);
            if (_pacLabel != null) _pacLabel.text = ComputePacEurosPerHectare(model.HedgerowDensity).ToString("F0", CultureInfo.InvariantCulture);
        }

        private void HandleTotalInvestmentChanged(float eurosPerHectare)
        {
            EnsureResolved();
            if (_totalInvestmentLabel != null)
                _totalInvestmentLabel.text = eurosPerHectare.ToString("F0", CultureInfo.InvariantCulture);
        }

        private void HandleHorizonChanged(bool _)
        {
            EnsureResolved();
            if (_horizonLabel == null) return;
            if (investmentHorizon != null && investmentHorizon.IsReached)
                _horizonLabel.text = investmentHorizon.HorizonYears.ToString("F1", CultureInfo.InvariantCulture) + " ans";
            else
                _horizonLabel.text = "Non atteint";
        }

        /// <summary>PSE payment (€/ha/yr): linear hedgerow density × the scenario PSE rate (€/m/yr). Pure, tested.</summary>
        public static double ComputePseEurosPerHectare(double hedgerowDensity, double pseSubsidyRate)
        {
            return hedgerowDensity * pseSubsidyRate;
        }

        /// <summary>
        /// CAP payment (€/ha/yr): the fixed basic support (DPB + redistributive
        /// + base eco-scheme) plus the per-hectare hedge bonus when hedgerows
        /// are present. Reuses the Hero KPI's public constants so the two views
        /// stay in lockstep. Pure, tested.
        /// </summary>
        public static double ComputePacEurosPerHectare(double hedgerowDensity)
        {
            double bonus = hedgerowDensity > 0.0 ? IntegratedProfitabilityIndicator.PacHedgeBonusEurosPerHectare : 0.0;
            return IntegratedProfitabilityIndicator.BasicCapPaymentEurosPerHectare + bonus;
        }
    }
}
