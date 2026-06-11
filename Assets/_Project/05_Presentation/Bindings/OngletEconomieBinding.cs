using System.Globalization;
using Bocage.Presentation;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Remplit les 7 lignes de l'onglet « Économie » depuis
    /// <see cref="EconomyRule.Breakdown"/> (source unique : la somme des postes EST
    /// la marge) : rendement, coûts d'intrants, charges fixes, et les 4 paiements de
    /// services écosystémiques (PSE, PAC, MAEC, crédit carbone). Les deux anciennes
    /// lignes « investissement / horizon de rentabilité » sont repurposées en MAEC /
    /// crédit carbone — il n'y a plus d'investissement upfront, donc
    /// ces lignes auraient été « sans objet » (cf §17 : afficher de l'info utile).
    /// Rafraîchi sur TickCompleted / Rebuilt du runner. Couche 05 — Play Mode.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class OngletEconomieBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Glisse le GameObject portant le SimulationRunner.")]
        private SimulationRunner runner;

        [SerializeField] private string cropYieldLabelName = "eco-yield-value";
        [SerializeField] private string inputCostLabelName = "eco-input-cost-value";
        [SerializeField] private string baseChargesLabelName = "eco-maintenance-value";   // libellé UXML repurposé en « Charges fixes »
        [SerializeField] private string pseLabelName = "eco-pse-value";
        [SerializeField] private string pacLabelName = "eco-pac-value";
        [SerializeField] private string maecLabelName = "eco-total-investment-value";      // repurposé en « Paiement MAEC »
        [SerializeField] private string carbonCreditLabelName = "eco-horizon-value";       // repurposé en « Crédit carbone »

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private UIDocument _document;
        private Label _cropYield, _inputCost, _baseCharges, _pse, _pac, _maec, _carbonCredit;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveLabels();
            if (runner != null) { runner.TickCompleted += HandleTick; runner.Rebuilt += HandleTick; }
            else SimLogger.DebugLog("[OngletEconomieBinding] runner non assigné sur " + name);
            HandleTick();
        }

        private void OnDisable()
        {
            if (runner != null) { runner.TickCompleted -= HandleTick; runner.Rebuilt -= HandleTick; }
        }

        private void ResolveLabels()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _cropYield = root.Q<Label>(cropYieldLabelName);
            _inputCost = root.Q<Label>(inputCostLabelName);
            _baseCharges = root.Q<Label>(baseChargesLabelName);
            _pse = root.Q<Label>(pseLabelName);
            _pac = root.Q<Label>(pacLabelName);
            _maec = root.Q<Label>(maecLabelName);
            _carbonCredit = root.Q<Label>(carbonCreditLabelName);
        }

        private void EnsureResolved()
        {
            if (_cropYield == null || _inputCost == null || _baseCharges == null || _pse == null
                || _pac == null || _maec == null || _carbonCredit == null)
                ResolveLabels();
        }

        private void HandleTick()
        {
            EnsureResolved();
            var s = runner != null ? runner.Session : null;
            if (s == null) return;
            MarginBreakdown b = EconomyRule.Breakdown(s.RealModel, s.Scenario);

            if (_cropYield != null) _cropYield.text = s.RealModel.CropYieldTPerHa.ToString("F1", Inv);
            if (_inputCost != null)
                _inputCost.text = (b.NitrogenCostEurosPerHa + b.PesticideCostEurosPerHa + b.TillageCostEurosPerHa).ToString("F0", Inv);
            if (_baseCharges != null) _baseCharges.text = b.BaseChargesEurosPerHa.ToString("F0", Inv);
            if (_pse != null) _pse.text = b.PseEurosPerHa.ToString("F0", Inv);
            if (_pac != null) _pac.text = b.PacEurosPerHa.ToString("F0", Inv);
            if (_maec != null) _maec.text = b.MaecEurosPerHa.ToString("F0", Inv);
            if (_carbonCredit != null) _carbonCredit.text = b.CarbonCreditEurosPerHa.ToString("F0", Inv) + " €/ha";
        }
    }
}
