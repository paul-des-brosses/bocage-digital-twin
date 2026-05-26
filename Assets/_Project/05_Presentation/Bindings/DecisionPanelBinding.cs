using System.Collections.Generic;
using Bocage.Decision;
using Bocage.Decision.Recommendations;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Renders the « Recommandations en cours » button in the right-hand
    /// decision-panel, and the centred list popup that opens when the
    /// user clicks it. The list shows every pending recommendation (in
    /// the journal but not yet resolved, whether previously deferred
    /// via "Voir plus tard" or never shown). Clicking an entry in the
    /// list re-opens the full <see cref="DecisionPopupBinding"/>
    /// popup for that recommendation.
    /// <para>
    /// Button text follows the pending count : « Recommandations en
    /// cours (3) » when there's something to act on, « Aucune
    /// recommandation en cours » when the queue is empty. The button
    /// is enabled either way so the user can always open the list
    /// (which then displays an empty-state placeholder).
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DecisionPanelBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the journal and current day. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [SerializeField, Tooltip("Sibling binding that owns the single-recommendation popup. Click-through from the history list re-opens that popup.")]
        private DecisionPopupBinding recommendationPopup;

        [Header("UXML element names")]
        [SerializeField] private string openHistoryButtonName = "decision-history-open-button";
        [SerializeField] private string historyOverlayName = "decision-history-overlay";
        [SerializeField] private string historyListName = "decision-history-list";
        [SerializeField] private string historyEmptyLabelName = "decision-history-empty";
        [SerializeField] private string historyCloseButtonName = "decision-history-close-button";

        private const string HiddenClass = "hidden";

        private UIDocument _document;
        private Button _openHistoryButton;
        private VisualElement _historyOverlay;
        private VisualElement _historyList;
        private Label _historyEmptyLabel;
        private Button _historyCloseButton;

        private int _lastPendingCount = -1;
        private bool _wired;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveElements();
            WireCallbacks();
            HideHistoryOverlay();
            RefreshButtonLabel(force: true);
        }

        private void OnDisable()
        {
            UnwireCallbacks();
        }

        private void Update()
        {
            // Per-frame refresh: cheap text update when the pending
            // count changes between ticks (user resolving recos in
            // the popup, or new ones arriving). Also keeps the history
            // list in sync if currently open.
            RefreshButtonLabel(force: false);
            if (_historyOverlay != null && !_historyOverlay.ClassListContains(HiddenClass))
            {
                RebuildHistoryList();
            }
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _openHistoryButton = root.Q<Button>(openHistoryButtonName);
            _historyOverlay = root.Q<VisualElement>(historyOverlayName);
            _historyList = root.Q<VisualElement>(historyListName);
            _historyEmptyLabel = root.Q<Label>(historyEmptyLabelName);
            _historyCloseButton = root.Q<Button>(historyCloseButtonName);

            if (_openHistoryButton == null || _historyOverlay == null || _historyList == null)
            {
                SimLogger.DebugLog("[DecisionPanelBinding] history button or overlay not found — check UXML names");
            }
        }

        private void WireCallbacks()
        {
            if (_wired) return;
            if (_openHistoryButton != null) _openHistoryButton.clicked += OnOpenHistoryClicked;
            if (_historyCloseButton != null) _historyCloseButton.clicked += OnCloseHistoryClicked;
            _wired = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wired) return;
            if (_openHistoryButton != null) _openHistoryButton.clicked -= OnOpenHistoryClicked;
            if (_historyCloseButton != null) _historyCloseButton.clicked -= OnCloseHistoryClicked;
            _wired = false;
        }

        private void RefreshButtonLabel(bool force)
        {
            if (_openHistoryButton == null || runner == null || runner.DecisionJournal == null) return;
            int pending = runner.DecisionJournal.PendingEntries.Count;
            if (!force && pending == _lastPendingCount) return;
            _lastPendingCount = pending;
            _openHistoryButton.text = pending > 0
                ? "Recommandations en cours (" + pending + ")"
                : "Aucune recommandation en cours";
        }

        private void OnOpenHistoryClicked()
        {
            ShowHistoryOverlay();
            RebuildHistoryList();
        }

        private void OnCloseHistoryClicked()
        {
            HideHistoryOverlay();
        }

        private void ShowHistoryOverlay()
        {
            if (_historyOverlay != null) _historyOverlay.RemoveFromClassList(HiddenClass);
        }

        private void HideHistoryOverlay()
        {
            if (_historyOverlay != null) _historyOverlay.AddToClassList(HiddenClass);
        }

        private void RebuildHistoryList()
        {
            if (_historyList == null || runner == null || runner.DecisionJournal == null) return;
            var pending = runner.DecisionJournal.PendingEntries;

            _historyList.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                _historyList.Add(BuildHistoryRow(pending[i].Recommendation));
            }
            if (_historyEmptyLabel != null)
            {
                _historyEmptyLabel.EnableInClassList(HiddenClass, pending.Count > 0);
            }
        }

        private VisualElement BuildHistoryRow(IRecommendation rec)
        {
            var row = new VisualElement();
            row.AddToClassList("decision-history-row");

            var info = new VisualElement();
            info.AddToClassList("decision-history-row-info");

            var titleLabel = new Label(rec.Title);
            titleLabel.AddToClassList("decision-history-row-title");
            info.Add(titleLabel);

            // Sub-line: causal chain (sensor + event + day). Replaces
            // the previous standalone "Détectée au jour N" line which
            // had less context. Sub-étape 10a friction #2 fix.
            var provenanceText = RecommendationProvenance.Format(
                rec, runner != null ? runner.EventLog : null);
            var provenanceLabel = new Label(provenanceText);
            provenanceLabel.AddToClassList("decision-history-row-provenance");
            info.Add(provenanceLabel);

            row.Add(info);

            var openButton = new Button(() => OpenRecommendation(rec))
            {
                text = "Examiner"
            };
            openButton.AddToClassList("decision-history-row-button");
            row.Add(openButton);

            return row;
        }

        private void OpenRecommendation(IRecommendation rec)
        {
            if (recommendationPopup == null || rec == null) return;
            HideHistoryOverlay();
            recommendationPopup.ShowRecommendationFromHistory(rec);
        }
    }
}
