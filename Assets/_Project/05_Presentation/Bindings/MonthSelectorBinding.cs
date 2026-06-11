using Bocage.Presentation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Câble le dropdown « Mois de démarrage » à
    /// <see cref="Bocage.SimulationCore.ScenarioContext.StartingMonth"/>
    /// (S0b). Le choix est écrit dans le scénario vivant ; le moteur le
    /// snapshote à la construction → effectif au <b>prochain Rebuild</b> (lancement
    /// ou réinitialisation), pas en cours de run (la saison reste continue).
    /// Couche 05 — Play Mode.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MonthSelectorBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Glisse le GameObject portant le SimulationRunner.")]
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

        private void Awake() => _document = GetComponent<UIDocument>();

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
                SimLogger.DebugLog("[MonthSelectorBinding] dropdown '" + dropdownName + "' introuvable — vérifier le nom UXML");
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

        private void OnTickCompleted() => RefreshHint();

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
