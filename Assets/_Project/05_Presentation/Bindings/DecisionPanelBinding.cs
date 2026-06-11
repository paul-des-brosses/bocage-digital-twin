using Bocage.Decision;
using Bocage.Presentation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Bouton « Recommandations en cours (N) » + liste modale des recos en attente
    /// (<see cref="SimulationSession.PendingRecommendations"/>). Badge « compromis »
    /// pour les recos non win-win — lu directement sur <c>Recommendation.Class</c>,
    /// sans projection (la session l'a déjà calculée). « Examiner » ré-ouvre la
    /// popup pour la reco. Couche 05 (Unity) — Play Mode.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DecisionPanelBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Glisse le GameObject portant le SimulationRunner.")]
        private SimulationRunner runner;
        [SerializeField, Tooltip("Le DecisionPopupBinding voisin (ré-ouverture d'une reco depuis la liste).")]
        private DecisionPopupBinding recommendationPopup;

        [Header("UXML element names")]
        [SerializeField] private string openHistoryButtonName = "decision-history-open-button";
        [SerializeField] private string historyOverlayName = "decision-history-overlay";
        [SerializeField] private string historyListName = "decision-history-list";
        [SerializeField] private string historyEmptyLabelName = "decision-history-empty";
        [SerializeField] private string historyCloseButtonName = "decision-history-close-button";

        private const string HiddenClass = "hidden";

        private UIDocument _document;
        private Button _openHistoryButton, _historyCloseButton;
        private VisualElement _historyOverlay, _historyList;
        private Label _historyEmptyLabel;
        private int _lastPendingCount = -1;
        private bool _wired;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveElements();
            WireCallbacks();
            HideHistoryOverlay();
            RefreshButtonLabel(true);
        }

        private void OnDisable() => UnwireCallbacks();

        private SimulationSession Session => runner != null ? runner.Session : null;
        private int PendingCount => Session != null ? Session.PendingRecommendations.Count : 0;

        private void Update()
        {
            int pending = PendingCount;
            bool changed = pending != _lastPendingCount;
            RefreshButtonLabel(false);
            // Reconstruit la liste OUVERTE seulement quand son contenu change
            // (sinon on recrée les lignes chaque frame → le bouton Examiner est
            // détruit entre pointer-down et pointer-up et ne fire jamais).
            if (changed && _historyOverlay != null && !_historyOverlay.ClassListContains(HiddenClass))
                RebuildHistoryList();
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var r = _document.rootVisualElement;
            _openHistoryButton = r.Q<Button>(openHistoryButtonName);
            _historyOverlay = r.Q<VisualElement>(historyOverlayName);
            _historyList = r.Q<VisualElement>(historyListName);
            _historyEmptyLabel = r.Q<Label>(historyEmptyLabelName);
            _historyCloseButton = r.Q<Button>(historyCloseButtonName);
            if (_openHistoryButton == null || _historyOverlay == null || _historyList == null)
                SimLogger.DebugLog("[DecisionPanelBinding] bouton/overlay historique introuvable — vérifier les noms UXML");
        }

        private void WireCallbacks()
        {
            if (_wired) return;
            if (_openHistoryButton != null) _openHistoryButton.clicked += OnOpenHistory;
            if (_historyCloseButton != null) _historyCloseButton.clicked += OnCloseHistory;
            _wired = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wired) return;
            if (_openHistoryButton != null) _openHistoryButton.clicked -= OnOpenHistory;
            if (_historyCloseButton != null) _historyCloseButton.clicked -= OnCloseHistory;
            _wired = false;
        }

        private void RefreshButtonLabel(bool force)
        {
            if (_openHistoryButton == null) return;
            int pending = PendingCount;
            if (!force && pending == _lastPendingCount) return;
            _lastPendingCount = pending;
            _openHistoryButton.text = pending > 0
                ? "Recommandations en cours (" + pending + ")"
                : "Aucune recommandation en cours";
        }

        private void OnOpenHistory() { ShowHistoryOverlay(); RebuildHistoryList(); }
        private void OnCloseHistory() => HideHistoryOverlay();
        private void ShowHistoryOverlay() { if (_historyOverlay != null) _historyOverlay.RemoveFromClassList(HiddenClass); }
        private void HideHistoryOverlay() { if (_historyOverlay != null) _historyOverlay.AddToClassList(HiddenClass); }

        private void RebuildHistoryList()
        {
            if (_historyList == null) return;
            SimulationSession s = Session;
            _historyList.Clear();
            int count = 0;
            if (s != null)
            {
                var pending = s.PendingRecommendations;
                count = pending.Count;
                for (int i = 0; i < pending.Count; i++)
                    _historyList.Add(BuildRow(pending[i]));
            }
            if (_historyEmptyLabel != null) _historyEmptyLabel.EnableInClassList(HiddenClass, count > 0);
        }

        private VisualElement BuildRow(Recommendation reco)
        {
            var row = new VisualElement();
            row.AddToClassList("decision-history-row");

            var info = new VisualElement();
            info.AddToClassList("decision-history-row-info");

            var title = new Label(RecommendationDisplay.LeverLabel(reco.Lever));
            title.AddToClassList("decision-history-row-title");
            info.Add(title);

            if (RecommendationDisplay.IsTradeoff(reco.Class))
            {
                var badge = new Label("compromis");
                badge.AddToClassList("decision-history-row-badge");
                badge.style.fontSize = 10f;
                badge.style.color = new StyleColor(new Color(0.80f, 0.66f, 0.42f));
                info.Add(badge);
            }

            var provenance = new Label("Déclencheur : " + RecommendationDisplay.EventLabel(reco.TriggeredBy));
            provenance.AddToClassList("decision-history-row-provenance");
            info.Add(provenance);

            row.Add(info);

            var openButton = new Button(() => OpenReco(reco)) { text = "Examiner" };
            openButton.AddToClassList("decision-history-row-button");
            row.Add(openButton);
            return row;
        }

        private void OpenReco(Recommendation reco)
        {
            if (recommendationPopup == null || reco == null) return;
            HideHistoryOverlay();
            recommendationPopup.ShowRecommendationFor(reco);
        }
    }
}
