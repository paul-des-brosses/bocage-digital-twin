using System.Globalization;
using Bocage.Decision.Refonte;
using Bocage.Presentation.Refonte;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Popup modal centré affichant la prochaine recommandation à auto-ouvrir
    /// (win-win ou urgence écologique) et mettant la simulation en pause. La
    /// <see cref="SimulationSession"/> (Couche 03) gère déjà la file, le dédup, le
    /// cooldown et le surfacing : ce binding ne fait qu'<b>afficher</b> et relayer
    /// Valider / Ignorer / Plus tard vers <c>Accept/Dismiss/Defer</c>. « Valider »
    /// pose le levier à son niveau optimal (reco ⊆ leviers) ; l'utilisateur peut
    /// ensuite l'affiner via le slider du levier. Couche 05 (Unity) — Play Mode.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DecisionPopupBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Glisse le GameObject portant le RefonteSimulationRunner.")]
        private RefonteSimulationRunner runner;

        [Header("UXML element names")]
        [SerializeField] private string overlayName = "decision-popup-overlay";
        [SerializeField] private string titleLabelName = "decision-popup-title";
        [SerializeField] private string sourceEventLabelName = "decision-popup-source-event";
        [SerializeField] private string rationaleLabelName = "decision-popup-rationale";
        [SerializeField] private string outcomesContainerName = "decision-popup-outcomes";
        [SerializeField] private string validateButtonName = "decision-popup-validate-button";
        [SerializeField] private string ignoreButtonName = "decision-popup-ignore-button";
        [SerializeField] private string deferButtonName = "decision-popup-defer-button";

        private const string HiddenClass = "hidden";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private UIDocument _document;
        private VisualElement _overlay, _outcomesContainer;
        private Label _title, _sourceEvent, _rationale;
        private Button _validateButton, _ignoreButton, _deferButton;

        private Recommendation _current;
        private bool _wasRunningBeforePopup;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveElements();
            WireCallbacks();
            HideOverlay();
            if (runner != null)
            {
                runner.TickCompleted += OnTickCompleted;
                runner.Rebuilt += OnRebuilt;
            }
        }

        private void OnDisable()
        {
            UnwireCallbacks();
            if (runner != null)
            {
                runner.TickCompleted -= OnTickCompleted;
                runner.Rebuilt -= OnRebuilt;
            }
        }

        // Reconstruction de session (Rebuild) → les recos repartent de zéro.
        private void OnRebuilt() => HideOverlay();

        private void Update() { if (_current == null) TryShowNext(); }
        private void OnTickCompleted() { if (_current == null) TryShowNext(); }

        private SimulationSession Session => runner != null ? runner.Session : null;

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var r = _document.rootVisualElement;
            _overlay = r.Q<VisualElement>(overlayName);
            _title = r.Q<Label>(titleLabelName);
            _sourceEvent = r.Q<Label>(sourceEventLabelName);
            _rationale = r.Q<Label>(rationaleLabelName);
            _outcomesContainer = r.Q<VisualElement>(outcomesContainerName);
            _validateButton = r.Q<Button>(validateButtonName);
            _ignoreButton = r.Q<Button>(ignoreButtonName);
            _deferButton = r.Q<Button>(deferButtonName);
            if (_overlay == null || _validateButton == null || _ignoreButton == null || _deferButton == null)
                SimLogger.DebugLog("[DecisionPopupBinding] éléments popup introuvables — vérifier les noms UXML");
        }

        private void WireCallbacks()
        {
            if (_validateButton != null) _validateButton.clicked += OnValidate;
            if (_ignoreButton != null) _ignoreButton.clicked += OnIgnore;
            if (_deferButton != null) _deferButton.clicked += OnDefer;
        }

        private void UnwireCallbacks()
        {
            if (_validateButton != null) _validateButton.clicked -= OnValidate;
            if (_ignoreButton != null) _ignoreButton.clicked -= OnIgnore;
            if (_deferButton != null) _deferButton.clicked -= OnDefer;
        }

        private void TryShowNext()
        {
            SimulationSession s = Session;
            if (s == null) return;
            Recommendation reco = s.NextAutoPopupRecommendation();
            if (reco != null) ShowPopupFor(reco);
        }

        /// <summary>Ré-ouvre la popup pour une reco choisie dans le panneau (hors gate d'auto-popup).</summary>
        public void ShowRecommendationFor(Recommendation reco)
        {
            if (reco != null) ShowPopupFor(reco);
        }

        public bool IsPopupVisible => _current != null;

        private void ShowPopupFor(Recommendation reco)
        {
            if (reco == null || _overlay == null) return;
            _current = reco;
            _wasRunningBeforePopup = runner != null && runner.IsRunning;
            if (runner != null) runner.StopTicking();

            if (_title != null) _title.text = "Recommandation : " + RecommendationDisplay.LeverLabel(reco.Lever);
            if (_sourceEvent != null) _sourceEvent.text = "Déclencheur : " + RecommendationDisplay.EventLabel(reco.TriggeredBy);
            if (_rationale != null)
                _rationale.text = "Passer « " + RecommendationDisplay.LeverLabel(reco.Lever) + " » de "
                    + RecommendationDisplay.LeverValue(reco.Lever, reco.CurrentLevel) + " à "
                    + RecommendationDisplay.LeverValue(reco.Lever, reco.RecommendedLevel) + ".  "
                    + RecommendationDisplay.ClassLabel(reco.Class) + ".";

            BuildOutcomes(reco);
            _overlay.RemoveFromClassList(HiddenClass);
        }

        private void HideOverlay()
        {
            _current = null;
            if (_overlay != null) _overlay.AddToClassList(HiddenClass);
        }

        private void OnValidate()
        {
            SimulationSession s = Session;
            if (_current == null || s == null) return;
            s.AcceptRecommendation(_current);
            SimLogger.UserActionLog("decision: VALIDER " + _current.Lever + " -> " + _current.RecommendedLevel.ToString("0.##", Inv));
            DismissAndAdvance();
        }

        private void OnIgnore()
        {
            SimulationSession s = Session;
            if (_current == null || s == null) return;
            s.DismissRecommendation(_current);
            SimLogger.UserActionLog("decision: IGNORER " + _current.Lever);
            DismissAndAdvance();
        }

        private void OnDefer()
        {
            SimulationSession s = Session;
            if (_current == null || s == null) return;
            s.DeferRecommendation(_current);
            SimLogger.UserActionLog("decision: PLUS TARD " + _current.Lever);
            DismissAndAdvance();
        }

        private void DismissAndAdvance()
        {
            HideOverlay();
            SimulationSession s = Session;
            Recommendation next = s != null ? s.NextAutoPopupRecommendation() : null;
            if (next != null) ShowPopupFor(next);
            else if (_wasRunningBeforePopup && runner != null && !runner.IsRunning) runner.StartTicking();
        }

        // ---- Bloc d'outcome projeté (réutilise les classes USS existantes) ----

        private void BuildOutcomes(Recommendation reco)
        {
            if (_outcomesContainer == null) return;
            _outcomesContainer.Clear();
            LeverOutcome o = reco.Outcome;

            var block = new VisualElement();
            block.AddToClassList("decision-outcome-block");

            var header = new Label("Projeté sur 3 ans (pire / attendu / meilleur)");
            header.AddToClassList("decision-outcome-horizon");
            block.Add(header);

            block.Add(MetricRow("Rentabilité", Bracket(o.DeltaMarginEurosPerHa, 1.0, "€/ha/an"), "decision-outcome-profit"));
            block.Add(MetricRow("Biodiversité", Bracket(o.DeltaBiodiversity, 100.0, "% index"), "decision-outcome-biodiv"));
            block.Add(MetricRow("Carbone sol", Bracket(o.DeltaCarbonTPerHa, 1.0, "tC/ha"), "decision-outcome-biodiv"));

            _outcomesContainer.Add(block);
        }

        private static VisualElement MetricRow(string label, string value, string valueClass)
        {
            var row = new VisualElement();
            row.AddToClassList("decision-outcome-metric-row");
            var lbl = new Label(label);
            lbl.AddToClassList("decision-outcome-metric-label");
            row.Add(lbl);
            var val = new Label(value);
            val.AddToClassList("decision-outcome-metric-value");
            val.AddToClassList(valueClass);
            row.Add(val);
            return row;
        }

        private static string Bracket(OutcomeDistribution d, double scale, string suffix)
            => (d.Worst * scale).ToString("+0;-0;0", Inv) + " / "
               + (d.Expected * scale).ToString("+0;-0;0", Inv) + " / "
               + (d.Best * scale).ToString("+0;-0;0", Inv) + " " + suffix;
    }
}
