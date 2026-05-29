using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Wires the "Mois de démarrage" dropdown (chantier E2 / ADR #52) in
    /// the "Conditions initiales du bocage" section of the dashboard to
    /// <see cref="Bocage.SimulationCore.Scenario.ScenarioContext.StartingMonth"/>.
    /// <para>
    /// The dropdown writes the chosen month (1 = January … 12 = December)
    /// into the scenario as soon as the user changes it. The active
    /// engine ignores the new value — the engine's
    /// <see cref="Bocage.SimulationCore.Rules.WeatherUpdateRule"/>
    /// snapshots the starting month at construction so the seasonal
    /// cycle stays continuous across the run. The next call to
    /// <see cref="SimulationRunner.Rebuild"/> picks up the user's new
    /// choice via <see cref="Bocage.SimulationCore.DefaultSimulation.Build"/>.
    /// This is the contract documented in ROADMAP §E2: "Reset only at
    /// CurrentDay == 0".
    /// </para>
    /// <para>
    /// A small hint label flips between two messages depending on the
    /// run state (mirrors the lock-hint pattern of
    /// <see cref="InitialConditionsBinding"/>):
    /// fresh start ("appliqué immédiatement au lancement") vs mid-run
    /// ("effectif à la prochaine réinitialisation").
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MonthSelectorBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the scenario context and run state. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [Header("UXML element names")]
        [SerializeField] private string dropdownName = "starting-month-dropdown";
        [SerializeField] private string hintLabelName = "starting-month-hint";

        private static readonly string[] MonthLabels =
        {
            "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
            "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
        };

        private UIDocument _document;
        private DropdownField _dropdown;
        private Label _hintLabel;
        private bool _wired;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveElements();
            InitializeFromScenario();
            WireCallbacks();
            if (runner != null) runner.TickCompleted += OnTickCompleted;
            RefreshHint();
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
            _dropdown = root.Q<DropdownField>(dropdownName);
            _hintLabel = root.Q<Label>(hintLabelName);
            if (_dropdown == null)
            {
                SimLogger.DebugLog("[MonthSelectorBinding] dropdown '" + dropdownName + "' not found — check UXML name");
                return;
            }
            _dropdown.choices = new System.Collections.Generic.List<string>(MonthLabels);
        }

        private void InitializeFromScenario()
        {
            if (_dropdown == null || runner == null || runner.Scenario == null) return;
            int monthOneBased = runner.Scenario.StartingMonth;
            if (monthOneBased < 1 || monthOneBased > 12) monthOneBased = 1;
            _dropdown.SetValueWithoutNotify(MonthLabels[monthOneBased - 1]);
        }

        private void WireCallbacks()
        {
            if (_wired || _dropdown == null) return;
            _dropdown.RegisterValueChangedCallback(OnDropdownChanged);
            _wired = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wired || _dropdown == null) return;
            _dropdown.UnregisterValueChangedCallback(OnDropdownChanged);
            _wired = false;
        }

        private void OnDropdownChanged(ChangeEvent<string> evt)
        {
            if (runner == null || runner.Scenario == null) return;
            int monthIndex = System.Array.IndexOf(MonthLabels, evt.newValue);
            if (monthIndex < 0) return;
            runner.Scenario.StartingMonth = monthIndex + 1;
            RefreshHint();
        }

        private void OnTickCompleted()
        {
            // The dropdown selection itself doesn't need refreshing — the
            // engine's snapshot of StartingMonth is locked, and the user's
            // choice in scenario.StartingMonth is what we display. We only
            // refresh the hint label so the "mid-run" wording kicks in
            // exactly as the run starts.
            RefreshHint();
        }

        private void RefreshHint()
        {
            if (_hintLabel == null || runner == null) return;
            bool freshStart = runner.CurrentDay == 0 && !runner.IsRunning;
            _hintLabel.text = freshStart
                ? "Appliqué immédiatement au lancement."
                : "Effectif à la prochaine réinitialisation.";
        }
    }
}
