using System.Collections.Generic;
using System.Globalization;
using Bocage.Decision;
using Bocage.Decision.Outcomes;
using Bocage.Decision.Recommendations;
using Bocage.Indicators.Hero;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Renders pending recommendations as a centred modal popup that
    /// pauses the simulation while visible. One recommendation at a
    /// time (FIFO queue); the popup re-opens with the next pending
    /// after the current one is resolved. The simulation auto-resumes
    /// at its previous speed once the pending queue is empty.
    /// <para>
    /// The popup body is rebuilt for each new pending entry:
    /// title + rationale + two-horizon outcome bracket (same content
    /// as the earlier inline cards) PLUS a slider letting the user
    /// pick the magnitude of the action before validating. On
    /// "Valider", the chosen magnitude is recorded in the journal
    /// (<see cref="DecisionJournal.SetVerdict"/> with the
    /// <c>appliedMagnitude</c> overload) and the auto-action pipeline
    /// applies it on the next tick. "Ignorer" resolves the entry as
    /// Rejected with magnitude 0.
    /// </para>
    /// <para>
    /// The backdrop is intentionally NOT clickable — the user must
    /// explicitly choose Valider or Ignorer. No accidental dismissal.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DecisionPopupBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the decision journal and the runner (for auto-pause / resume). Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        [Header("UXML element names")]
        [SerializeField] private string overlayName = "decision-popup-overlay";
        [SerializeField] private string titleLabelName = "decision-popup-title";
        [SerializeField] private string sourceEventLabelName = "decision-popup-source-event";
        [SerializeField] private string rationaleLabelName = "decision-popup-rationale";
        [SerializeField] private string outcomesContainerName = "decision-popup-outcomes";
        [SerializeField] private string magnitudeLabelName = "decision-popup-magnitude-label";
        [SerializeField] private string magnitudeSliderName = "decision-popup-magnitude-slider";
        [SerializeField] private string magnitudeValueLabelName = "decision-popup-magnitude-value";
        [SerializeField] private string investmentLabelName = "decision-popup-investment";
        [SerializeField] private string validateButtonName = "decision-popup-validate-button";
        [SerializeField] private string ignoreButtonName = "decision-popup-ignore-button";
        [SerializeField] private string deferButtonName = "decision-popup-defer-button";

        private const string HiddenClass = "hidden";

        private UIDocument _document;
        private VisualElement _overlay;
        private Label _title, _sourceEvent, _rationale, _magnitudeLabel, _magnitudeValueLabel;
        private Label _investmentLabel;
        private VisualElement _outcomesContainer;
        private Slider _magnitudeSlider;
        private Button _validateButton, _ignoreButton, _deferButton;

        // Recommendations the user explicitly deferred via "Voir plus
        // tard" during this session. Auto-popup skips them; they can
        // still be re-opened from the history list popup. Cleared at
        // OnEnable so a fresh play session starts without skips.
        private readonly HashSet<string> _skippedRecommendationIds = new HashSet<string>();

        // Recommendation TYPES the user has rejected this session via
        // "Ignorer". Auto-popup suppresses any new recommendation of
        // these types (e.g. all future "reduce-inputs#N" recs). The
        // recos remain in pending and accessible via the history list,
        // but they no longer interrupt the simulation flow — matches
        // the user's stated intent on Ignorer ("non définitif").
        // Without this guard, a 30-day event-detector cooldown means a
        // rejected reco re-pops every 1.5 seconds real-time at
        // ×20 speed, breaking the UX.
        private readonly HashSet<string> _ignoredRecommendationTypes = new HashSet<string>();

        // Memoised model-derived outcome projections per rec.Id. Each projection
        // is a forward simulation (thousands of ticks), so it must NEVER run on a
        // per-frame path: compute once on first need and reuse. Cleared on Enable
        // and on Rebuild (the model resets, old projections become meaningless).
        private readonly Dictionary<string, OutcomeDistribution[]> _projectionCache = new Dictionary<string, OutcomeDistribution[]>();

        // Currently-displayed recommendation (null = popup hidden).
        private IRecommendation _currentRecommendation;
        // Was the runner ticking before the popup opened ?
        // Used to decide whether to resume on close.
        private bool _wasRunningBeforePopup;
        // Slider's current magnitude unit, kept for the value label.
        private string _currentUnit = "";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveElements();
            WireCallbacks();
            HideOverlay();
            _projectionCache.Clear();
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

        private void OnRebuilt()
        {
            // The engine + journal were wiped and the model reset to day 0; every
            // cached projection is now stale. Drop them so new recos project fresh.
            _projectionCache.Clear();
        }

        private void Update()
        {
            // Polled check: a new pending entry can appear between
            // ticks (e.g. if the user re-clicks Lancer right after a
            // rebuild). We also catch the case where the popup is not
            // visible but the journal already has pending — defensive.
            if (_currentRecommendation == null) TryShowNextPending();
        }

        private void OnTickCompleted()
        {
            // The pipeline of detection → reco production → journal
            // append runs DURING the tick. By the time TickCompleted
            // fires, new pending entries are visible to us.
            if (_currentRecommendation == null) TryShowNextPending();
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _overlay = root.Q<VisualElement>(overlayName);
            _title = root.Q<Label>(titleLabelName);
            _sourceEvent = root.Q<Label>(sourceEventLabelName);
            _rationale = root.Q<Label>(rationaleLabelName);
            _outcomesContainer = root.Q<VisualElement>(outcomesContainerName);
            _magnitudeLabel = root.Q<Label>(magnitudeLabelName);
            _magnitudeSlider = root.Q<Slider>(magnitudeSliderName);
            _magnitudeValueLabel = root.Q<Label>(magnitudeValueLabelName);
            _investmentLabel = root.Q<Label>(investmentLabelName);
            _validateButton = root.Q<Button>(validateButtonName);
            _ignoreButton = root.Q<Button>(ignoreButtonName);
            _deferButton = root.Q<Button>(deferButtonName);

            if (_overlay == null || _magnitudeSlider == null || _validateButton == null || _ignoreButton == null || _deferButton == null)
            {
                SimLogger.DebugLog("[DecisionPopupBinding] one or more popup elements not found — check UXML names");
            }
        }

        private void WireCallbacks()
        {
            if (_validateButton != null) _validateButton.clicked += OnValidate;
            if (_ignoreButton != null) _ignoreButton.clicked += OnIgnore;
            if (_deferButton != null) _deferButton.clicked += OnDefer;
            if (_magnitudeSlider != null) _magnitudeSlider.RegisterValueChangedCallback(OnMagnitudeChanged);
        }

        private void UnwireCallbacks()
        {
            if (_validateButton != null) _validateButton.clicked -= OnValidate;
            if (_ignoreButton != null) _ignoreButton.clicked -= OnIgnore;
            if (_deferButton != null) _deferButton.clicked -= OnDefer;
            if (_magnitudeSlider != null) _magnitudeSlider.UnregisterValueChangedCallback(OnMagnitudeChanged);
        }

        private void TryShowNextPending()
        {
            if (runner == null || runner.DecisionJournal == null) return;
            var pending = runner.DecisionJournal.PendingEntries;
            for (int i = 0; i < pending.Count; i++)
            {
                var rec = pending[i].Recommendation;
                if (ShouldAutoSkip(rec)) continue;
                if (!ShouldAutoSurface(rec)) continue; // trade-off -> waits in the list
                ShowPopupFor(rec);
                return;
            }
        }

        /// <summary>
        /// True if the recommendation should not auto-popup because the
        /// user previously deferred it (same instance) or ignored a
        /// recommendation of the same type this session.
        /// </summary>
        private bool ShouldAutoSkip(IRecommendation rec)
        {
            if (rec == null) return true;
            if (_skippedRecommendationIds.Contains(rec.Id)) return true;
            string type = ExtractTypePrefix(rec.Id);
            if (type != null && _ignoredRecommendationTypes.Contains(type)) return true;
            return false;
        }

        /// <summary>
        /// E9 surfacing: a trade-off recommendation (economic, or any with a
        /// worsening projected dimension) does NOT auto-open a popup — it waits
        /// passively in the decision list. An ecological trade-off escalates to a
        /// popup only when biodiversity is critical. Win-win always pops.
        /// </summary>
        private bool ShouldAutoSurface(IRecommendation rec)
        {
            var outcomes = GetProjection(rec);
            if (outcomes == null || outcomes.Length == 0) return true; // no projection yet → don't suppress
            double biodiversity = runner != null && runner.Model != null
                ? BiodiversityCompositeIndicator.Compute(runner.Model, runner.Scenario)
                : 1.0;
            return RecommendationSurfacing.ShouldAutoPopup(outcomes[outcomes.Length - 1], biodiversity);
        }

        /// <summary>
        /// Model-derived outcome projection for a recommendation, memoised by id.
        /// Returns null while the runner state isn't available. The projection is
        /// a forward simulation, so this MUST stay behind the cache on any path
        /// that runs every frame (e.g. <see cref="ShouldAutoSurface"/>).
        /// </summary>
        private OutcomeDistribution[] GetProjection(IRecommendation rec)
        {
            if (rec == null || runner == null || runner.Model == null || runner.Scenario == null) return null;
            if (_projectionCache.TryGetValue(rec.Id, out var cached)) return cached;
            var outcomes = ModelOutcomeProjector.Project(
                rec, runner.Model, runner.Scenario, runner.MasterSeed, runner.SeasonalWeather,
                IntegratedProfitabilityIndicator.Compute, BiodiversityCompositeIndicator.Compute);
            _projectionCache[rec.Id] = outcomes;
            return outcomes;
        }

        /// <summary>
        /// Whether the recommendation is a trade-off (not a clean win-win), from
        /// its model-derived projection. Exposed so the decision-list panel can
        /// badge « compromis » WITHOUT running its own forward simulation — it
        /// reuses this binding's memoised projection.
        /// </summary>
        public bool IsTradeoff(IRecommendation rec)
        {
            var outcomes = GetProjection(rec);
            if (outcomes == null || outcomes.Length == 0) return false;
            return RecommendationSurfacing.IsTradeoff(outcomes[outcomes.Length - 1]);
        }

        /// <summary>
        /// Recommendation ids follow the pattern <c>type#dayOrSalt</c>
        /// (cf. PlantHedgesRecommendation.Id and friends). Strip the
        /// suffix to compare by type.
        /// </summary>
        private static string ExtractTypePrefix(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            int sep = id.IndexOf('#');
            return sep < 0 ? id : id.Substring(0, sep);
        }

        /// <summary>
        /// Re-opens the popup for the supplied recommendation, clearing
        /// any "Voir plus tard" skip flag it may have had. Called by
        /// the history list popup when the user picks an entry to
        /// revisit.
        /// </summary>
        public void ShowRecommendationFromHistory(IRecommendation rec)
        {
            if (rec == null) return;
            _skippedRecommendationIds.Remove(rec.Id);
            ShowPopupFor(rec);
        }

        /// <summary>True if no popup is currently displayed.</summary>
        public bool IsPopupVisible => _currentRecommendation != null;

        private void ShowPopupFor(IRecommendation rec)
        {
            if (rec == null || _overlay == null) return;

            _currentRecommendation = rec;
            // Capture the runner's running state so we can resume on close.
            // If popup opens while paused (user paused manually before
            // recos arrived), we won't auto-resume on close.
            _wasRunningBeforePopup = runner != null && runner.IsRunning;
            if (runner != null) runner.StopTicking();

            if (_title != null) _title.text = rec.Title;
            // Surface the causal chain: which sensor caught what, on
            // which day. Falls back to a generic line if the event log
            // lookup fails (cf. RecommendationProvenance.Format).
            if (_sourceEvent != null)
            {
                _sourceEvent.text = RecommendationProvenance.Format(rec, runner != null ? runner.EventLog : null);
            }
            if (_rationale != null) _rationale.text = rec.Rationale;

            BuildOutcomesInto(_outcomesContainer, rec);
            ConfigureMagnitudeSlider(rec);

            _overlay.RemoveFromClassList(HiddenClass);
        }

        private void HideOverlay()
        {
            _currentRecommendation = null;
            if (_overlay != null) _overlay.AddToClassList(HiddenClass);
        }

        private void OnValidate()
        {
            if (_currentRecommendation == null || runner == null) return;
            double magnitude = _magnitudeSlider != null ? _magnitudeSlider.value : 0.0;
            runner.DecisionJournal.SetVerdict(
                _currentRecommendation.Id,
                DecisionVerdict.Accepted,
                runner.CurrentDay,
                appliedMagnitude: magnitude);
            // Validation overrides any prior Ignorer on this type: the
            // user is back in active engagement, future events of the
            // same type should pop the modal again.
            string type = ExtractTypePrefix(_currentRecommendation.Id);
            if (type != null) _ignoredRecommendationTypes.Remove(type);
            SimLogger.UserActionLog("decision: VALIDATED " + _currentRecommendation.Id
                + " magnitude=" + magnitude.ToString("F2", Inv));
            DismissAndAdvance();
        }

        private void OnIgnore()
        {
            if (_currentRecommendation == null || runner == null) return;
            runner.DecisionJournal.SetVerdict(
                _currentRecommendation.Id,
                DecisionVerdict.Rejected,
                runner.CurrentDay,
                appliedMagnitude: 0.0);
            // Suppress AUTO-popping of any future recommendation of the
            // same type during this session. The user said no to this
            // kind of action; we don't want the detector cooldown to
            // re-pop the same advice every couple seconds at high
            // speed. The recos still appear in the history list for
            // revisit.
            string type = ExtractTypePrefix(_currentRecommendation.Id);
            if (type != null) _ignoredRecommendationTypes.Add(type);
            SimLogger.UserActionLog("decision: IGNORED " + _currentRecommendation.Id
                + " (auto-popup suppressed for type=" + type + ")");
            DismissAndAdvance();
        }

        private void OnDefer()
        {
            // "Voir plus tard" → keep the entry in Pending state, but
            // mark it as session-skipped so auto-popup doesn't keep
            // re-firing on it. User can revisit via the history popup.
            if (_currentRecommendation == null || runner == null) return;
            _skippedRecommendationIds.Add(_currentRecommendation.Id);
            SimLogger.UserActionLog("decision: DEFERRED " + _currentRecommendation.Id);
            DismissAndAdvance();
        }

        private void DismissAndAdvance()
        {
            HideOverlay();
            // Try to surface the next non-skipped pending; if none,
            // resume ticking at the previously-captured running state.
            IRecommendation next = null;
            if (runner != null && runner.DecisionJournal != null)
            {
                var pending = runner.DecisionJournal.PendingEntries;
                for (int i = 0; i < pending.Count; i++)
                {
                    var rec = pending[i].Recommendation;
                    if (ShouldAutoSkip(rec)) continue;
                    if (!ShouldAutoSurface(rec)) continue; // trade-off -> waits in the list
                    next = rec;
                    break;
                }
            }
            if (next != null)
            {
                ShowPopupFor(next);
            }
            else if (_wasRunningBeforePopup && runner != null && !runner.IsRunning)
            {
                runner.StartTicking();
            }
        }

        // ---------- Magnitude slider configuration per rec type ----------

        private void ConfigureMagnitudeSlider(IRecommendation rec)
        {
            if (_magnitudeSlider == null) return;
            double min, max, def;
            string label, unit;
            ResolveMagnitudeRange(rec, out min, out max, out def, out label, out unit);

            _magnitudeSlider.lowValue = (float)min;
            _magnitudeSlider.highValue = (float)max;
            _magnitudeSlider.SetValueWithoutNotify((float)def);
            _currentUnit = unit;

            if (_magnitudeLabel != null) _magnitudeLabel.text = label;
            if (_magnitudeValueLabel != null) _magnitudeValueLabel.text = FormatMagnitude((float)def);
            RefreshInvestmentLabel(rec, def);
        }

        private void OnMagnitudeChanged(ChangeEvent<float> evt)
        {
            if (_magnitudeValueLabel != null) _magnitudeValueLabel.text = FormatMagnitude(evt.newValue);
            RefreshInvestmentLabel(_currentRecommendation, evt.newValue);
        }

        /// <summary>
        /// Shows / hides and refreshes the « Coût upfront estimé : X €/ha »
        /// line below the magnitude slider (chantier E5 / ADR #50). Only
        /// <see cref="PlantHedgesRecommendation"/> contributes a non-zero
        /// cost — the line is hidden for Irrigation and ReduceInputs whose
        /// expense is folded into <c>InputCost</c> / <c>WaterTableDepth</c>.
        /// Called from both <see cref="ConfigureMagnitudeSlider"/> (initial
        /// default magnitude) and <see cref="OnMagnitudeChanged"/> (live).
        /// </summary>
        private void RefreshInvestmentLabel(IRecommendation rec, double magnitude)
        {
            if (_investmentLabel == null) return;
            if (rec is PlantHedgesRecommendation)
            {
                double cost = PlantHedgesRecommendation.ComputeInvestmentCost(magnitude);
                _investmentLabel.text = "Coût upfront estimé : " + cost.ToString("F0", Inv) + " €/ha";
                _investmentLabel.RemoveFromClassList(HiddenClass);
            }
            else
            {
                _investmentLabel.text = "";
                _investmentLabel.AddToClassList(HiddenClass);
            }
        }

        private string FormatMagnitude(float value)
        {
            return value.ToString("0.##", Inv) + " " + _currentUnit;
        }

        private static void ResolveMagnitudeRange(
            IRecommendation rec, out double min, out double max, out double def, out string label, out string unit)
        {
            switch (rec)
            {
                case PlantHedgesRecommendation _:
                    min = 0; max = 50; def = PlantHedgesRecommendation.HedgeRestoreMetersPerHectare;
                    label = "Mètres de haies replantés"; unit = "m/ha";
                    break;
                case IrrigationAdviceRecommendation _:
                    min = 0; max = 3; def = IrrigationAdviceRecommendation.WaterReliefDepthMeters;
                    label = "Profondeur de nappe regagnée"; unit = "m";
                    break;
                case ReduceInputsRecommendation _:
                    min = 0; max = 0.5; def = ReduceInputsRecommendation.IntensityCutPerStep;
                    label = "Baisse d'intensité d'intrants"; unit = "× réf.";
                    break;
                case RaiseInputsRecommendation _:
                    min = 0; max = 0.5; def = RaiseInputsRecommendation.IntensityRaisePerStep;
                    label = "Hausse d'intensité d'intrants"; unit = "× réf.";
                    break;
                case SowCoverCropsRecommendation _:
                    min = 0; max = 50; def = SowCoverCropsRecommendation.CoverageRaisePerStep;
                    label = "Couverts d'interculture en plus"; unit = "%";
                    break;
                case RestoreResidueRecommendation _:
                    min = 0; max = 50; def = RestoreResidueRecommendation.RestitutionRaisePerStep;
                    label = "Résidus restitués en plus"; unit = "%";
                    break;
                case ReduceHedgeRemovalRecommendation _:
                    min = 0; max = 15; def = ReduceHedgeRemovalRecommendation.RemovalCutPerStep;
                    label = "Baisse du rythme d'arrachage"; unit = "m/ha/an";
                    break;
                case IncreaseHedgeRemovalRecommendation _:
                    min = 0; max = 15; def = IncreaseHedgeRemovalRecommendation.RemovalRaisePerStep;
                    label = "Hausse du rythme d'arrachage"; unit = "m/ha/an";
                    break;
                default:
                    min = 0; max = 1; def = 0; label = "Magnitude"; unit = "";
                    break;
            }
        }

        // ---------- Outcome block builder (shared structure) ----------

        private void BuildOutcomesInto(VisualElement container, IRecommendation rec)
        {
            if (container == null) return;
            container.Clear();

            // The simulation is paused while the popup is open (ShowPopupFor stops
            // ticking first), so the memoised projection reflects the current state.
            var outcomes = GetProjection(rec);
            if (outcomes == null) return;
            for (int i = 0; i < outcomes.Length; i++)
            {
                container.Add(BuildOutcomeBlock(outcomes[i]));
            }
        }

        private static VisualElement BuildOutcomeBlock(OutcomeDistribution outcome)
        {
            var block = new VisualElement();
            block.AddToClassList("decision-outcome-block");

            string horizonText = outcome.HorizonInDays == 30
                ? "Sur 30 jours (court terme)"
                : "Sur " + outcome.HorizonInDays + " jours (long terme)";
            var horizonHeader = new Label(horizonText);
            horizonHeader.AddToClassList("decision-outcome-horizon");
            block.Add(horizonHeader);

            block.Add(BuildMetricRow("Rentabilité",
                FormatBracket(outcome.ProfitDeltaWorstCase, outcome.ProfitDeltaExpected, outcome.ProfitDeltaBestCase, "€/ha/an"),
                "decision-outcome-profit"));
            block.Add(BuildMetricRow("Biodiversité",
                FormatBracket(outcome.BiodiversityDeltaWorstCase * 100, outcome.BiodiversityDeltaExpected * 100, outcome.BiodiversityDeltaBestCase * 100, "% index"),
                "decision-outcome-biodiv"));

            return block;
        }

        private static VisualElement BuildMetricRow(string label, string bracketValue, string valueClass)
        {
            var row = new VisualElement();
            row.AddToClassList("decision-outcome-metric-row");
            var lbl = new Label(label);
            lbl.AddToClassList("decision-outcome-metric-label");
            row.Add(lbl);
            var val = new Label(bracketValue);
            val.AddToClassList("decision-outcome-metric-value");
            val.AddToClassList(valueClass);
            row.Add(val);
            return row;
        }

        private static string FormatBracket(double worst, double expected, double best, string suffix)
        {
            return worst.ToString("+0;-0;0", Inv) + " / "
                 + expected.ToString("+0;-0;0", Inv) + " / "
                 + best.ToString("+0;-0;0", Inv) + " " + suffix;
        }
    }
}
