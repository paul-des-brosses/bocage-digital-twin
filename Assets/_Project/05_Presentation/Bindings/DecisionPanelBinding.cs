using System.Collections.Generic;
using System.Globalization;
using Bocage.Decision;
using Bocage.Decision.Outcomes;
using Bocage.Decision.Recommendations;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Renders the pending entries of <see cref="DecisionJournal"/> as
    /// a vertical stack of cards inside a UXML container, with Accept
    /// and Reject buttons that mutate the journal verdict on click.
    /// Each card also displays the two-horizon outcome projection
    /// (short 30 d, long 365 d) on both profit and biodiversity, with
    /// the worst/expected/best bracket inline.
    /// <para>
    /// The cards are rebuilt every tick (subscription to
    /// <see cref="SimulationRunner.TickCompleted"/>) to reflect new
    /// pending entries appearing as events get detected and the
    /// recommendation engine fires. A small diff would be more
    /// efficient; for the typical bocage run (≤ a few pending at
    /// once) the rebuild cost is negligible.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DecisionPanelBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the journal and current day. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [SerializeField, Tooltip("Name of the VisualElement that hosts the recommendation cards.")]
        private string cardsContainerName = "decision-cards";

        [SerializeField, Tooltip("Name of the Label showing the journal summary line.")]
        private string summaryLabelName = "decision-summary";

        private UIDocument _document;
        private VisualElement _container;
        private Label _summaryLabel;
        private readonly List<VisualElement> _cards = new List<VisualElement>();

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveElements();
            if (runner != null) runner.TickCompleted += OnTick;
            Rebuild();
        }

        private void OnDisable()
        {
            if (runner != null) runner.TickCompleted -= OnTick;
        }

        private void OnTick()
        {
            Rebuild();
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _container = _document.rootVisualElement.Q<VisualElement>(cardsContainerName);
            _summaryLabel = _document.rootVisualElement.Q<Label>(summaryLabelName);
            if (_container == null)
            {
                SimLogger.DebugLog("[DecisionPanelBinding] container '" + cardsContainerName + "' not found");
            }
        }

        private void Rebuild()
        {
            if (_container == null || runner == null || runner.DecisionJournal == null) return;
            var pending = runner.DecisionJournal.PendingEntries;

            // Update summary line.
            if (_summaryLabel != null)
            {
                int total = runner.DecisionJournal.Entries.Count;
                int applied = runner.DecisionJournal.AppliedCount;
                _summaryLabel.text = pending.Count == 0
                    ? "Aucune recommandation en attente — " + applied + " appliquée(s) sur " + total
                    : pending.Count + " en attente — " + applied + " appliquée(s) sur " + total;
            }

            // Naive rebuild: clear everything, rebuild from scratch. For
            // the typical bocage run (≤ a handful of pending at once)
            // the cost is negligible and the code stays simple.
            _container.Clear();
            _cards.Clear();
            for (int i = 0; i < pending.Count; i++)
            {
                _container.Add(BuildCard(pending[i]));
            }
        }

        private VisualElement BuildCard(DecisionJournal.Entry entry)
        {
            var card = new VisualElement();
            card.AddToClassList("decision-card");

            var title = new Label(entry.Recommendation.Title);
            title.AddToClassList("decision-card-title");
            card.Add(title);

            var rationale = new Label(entry.Recommendation.Rationale);
            rationale.AddToClassList("decision-card-rationale");
            card.Add(rationale);

            // Two-horizon outcome bracket.
            var outcomes = OutcomeProjector.Project(entry.Recommendation, runner.Model);
            for (int i = 0; i < outcomes.Length; i++)
            {
                card.Add(BuildOutcomeRow(outcomes[i]));
            }

            // Action buttons.
            var actionsRow = new VisualElement();
            actionsRow.AddToClassList("decision-card-actions");

            var acceptBtn = new Button(() => OnAccept(entry.Recommendation.Id))
            {
                text = "Accepter"
            };
            acceptBtn.AddToClassList("decision-card-button");
            acceptBtn.AddToClassList("decision-card-button--accept");
            actionsRow.Add(acceptBtn);

            var rejectBtn = new Button(() => OnReject(entry.Recommendation.Id))
            {
                text = "Rejeter"
            };
            rejectBtn.AddToClassList("decision-card-button");
            rejectBtn.AddToClassList("decision-card-button--reject");
            actionsRow.Add(rejectBtn);

            card.Add(actionsRow);
            _cards.Add(card);
            return card;
        }

        private static VisualElement BuildOutcomeRow(OutcomeDistribution outcome)
        {
            var row = new VisualElement();
            row.AddToClassList("decision-outcome-row");

            var horizonLabel = new Label(outcome.HorizonInDays + " j");
            horizonLabel.AddToClassList("decision-outcome-horizon");
            row.Add(horizonLabel);

            // Profit bracket: "profit -60 / +20 / +60 €/ha/an"
            var profitLabel = new Label(FormatBracket(
                outcome.ProfitDeltaWorstCase,
                outcome.ProfitDeltaExpected,
                outcome.ProfitDeltaBestCase,
                "€"));
            profitLabel.AddToClassList("decision-outcome-profit");
            row.Add(profitLabel);

            // Biodiv bracket as percent of composite.
            var biodivLabel = new Label(FormatBracket(
                outcome.BiodiversityDeltaWorstCase * 100,
                outcome.BiodiversityDeltaExpected * 100,
                outcome.BiodiversityDeltaBestCase * 100,
                "% biodiv"));
            biodivLabel.AddToClassList("decision-outcome-biodiv");
            row.Add(biodivLabel);

            return row;
        }

        private static string FormatBracket(double worst, double expected, double best, string suffix)
        {
            var inv = CultureInfo.InvariantCulture;
            return worst.ToString("+0;-0;0", inv) + " / "
                 + expected.ToString("+0;-0;0", inv) + " / "
                 + best.ToString("+0;-0;0", inv) + " " + suffix;
        }

        private void OnAccept(string recId)
        {
            if (runner == null) return;
            runner.DecisionJournal.SetVerdict(recId, DecisionVerdict.Accepted, runner.CurrentDay);
            SimLogger.UserActionLog("decision: ACCEPTED " + recId + " on day " + runner.CurrentDay);
            Rebuild();
        }

        private void OnReject(string recId)
        {
            if (runner == null) return;
            runner.DecisionJournal.SetVerdict(recId, DecisionVerdict.Rejected, runner.CurrentDay);
            SimLogger.UserActionLog("decision: REJECTED " + recId + " on day " + runner.CurrentDay);
            Rebuild();
        }
    }
}
