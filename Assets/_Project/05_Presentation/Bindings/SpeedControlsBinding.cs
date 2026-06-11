using Bocage.Presentation.Refonte;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Wires the four speed-control buttons (pause, play x1, play x10,
    /// skip-to-end) to the <see cref="RefonteSimulationRunner"/>'s
    /// <c>StartTicking</c>/<c>StopTicking</c>/<c>TicksPerSecond</c>/
    /// <c>FastForwardTo</c> surface. Also maintains a small day counter
    /// label that reads <see cref="RefonteSimulationRunner.CurrentDay"/> after
    /// every tick.
    /// <para>
    /// The last selected play-speed (x1 or x10) is persisted in
    /// PlayerPrefs under <see cref="PrefsKey"/> and re-applied at startup
    /// per CLAUDE.md §16 (PlayerPrefs minimal: only presets and speed).
    /// </para>
    /// <para>
    /// Skip-to-end: synchronously fast-forwards the engine up to
    /// <c>ScenarioContext.HorizonInDays</c>, then leaves the runner
    /// paused so the user can inspect the resulting state. UI freezes
    /// briefly during the loop (a 5-year skip is ~1825 ticks ≈ a few
    /// hundred ms on desktop).
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SpeedControlsBinding : MonoBehaviour
    {
        public const string PrefsKey = "Bocage.Speed.LastSpeed";

        // Four persisted play-speeds. Other values (e.g. used by skip-to-end's
        // internal loop) are not persisted — only steady-state user choices.
        // x1 to x20 covers the realistic range: x1 to observe finely, x5 for
        // modest acceleration, x10 for sustained climate stress runs, x20
        // when the user wants to skim several years quickly without
        // jumping to skip-to-end.
        private const float SpeedX1 = 1f;
        private const float SpeedX5 = 5f;
        private const float SpeedX10 = 10f;
        private const float SpeedX20 = 20f;

        [SerializeField, Tooltip("Source du moteur. Glisse le GameObject portant le RefonteSimulationRunner.")]
        private RefonteSimulationRunner runner;

        [Header("UXML element names — buttons")]
        [SerializeField] private string pauseButtonName = "speed-pause-button";
        [SerializeField] private string playX1ButtonName = "speed-x1-button";
        [SerializeField] private string playX5ButtonName = "speed-x5-button";
        [SerializeField] private string playX10ButtonName = "speed-x10-button";
        [SerializeField] private string playX20ButtonName = "speed-x20-button";
        [SerializeField] private string skipEndButtonName = "speed-skip-end-button";

        [Header("UXML element names — labels")]
        [SerializeField] private string dayCounterLabelName = "speed-day-counter";

        private UIDocument _document;
        private Button _pauseButton, _playX1Button, _playX5Button, _playX10Button, _playX20Button, _skipEndButton;
        private Label _dayLabel;
        private bool _wired;

        private enum SpeedState { Paused, X1, X5, X10, X20 }
        private SpeedState _state = SpeedState.X1;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            ResolveElements();
            WireCallbacks();
            if (runner != null)
            {
                runner.TickCompleted += OnTickCompleted;
                runner.Rebuilt += OnRunnerRebuilt;
                runner.TickingStateChanged += OnTickingStateChanged;
            }
            RefreshDayLabel();
        }

        private void Start()
        {
            // Always start paused, per UX intent: the user sees the
            // initial dashboard state and decides when to begin. We must
            // force-pause AFTER the runner's Start() has run (it carries
            // DefaultExecutionOrder(-8000) and autoStart=true, so it
            // launches the tick coroutine on its own); this component has
            // no DefaultExecutionOrder, so Unity's default ordering
            // guarantees our Start fires after the runner's.
            if (runner != null) runner.StopTicking();
            _state = SpeedState.Paused;
            UpdateActiveVisualState();
        }

        private void OnDisable()
        {
            UnwireCallbacks();
            if (runner != null)
            {
                runner.TickCompleted -= OnTickCompleted;
                runner.Rebuilt -= OnRunnerRebuilt;
                runner.TickingStateChanged -= OnTickingStateChanged;
            }
        }

        /// <summary>
        /// Called when the runner has been rebuilt externally (typically
        /// by <see cref="InitialConditionsBinding"/>, which calls
        /// <c>StartTicking</c> after the rebuild). Re-syncs the
        /// highlighted speed button to match the runner's actual
        /// ticking state so the UI doesn't lie about which speed is
        /// active.
        /// </summary>
        private void OnRunnerRebuilt()
        {
            if (runner == null) return;
            _state = !runner.IsRunning
                ? SpeedState.Paused
                : InferStateFromTicksPerSecond(runner.TicksPerSecond);
            UpdateActiveVisualState();
            RefreshDayLabel();
        }

        private static SpeedState InferStateFromTicksPerSecond(float tps)
        {
            // Snap to the closest of the four supported play-speeds.
            if (tps < 3f) return SpeedState.X1;
            if (tps < 7.5f) return SpeedState.X5;
            if (tps < 15f) return SpeedState.X10;
            return SpeedState.X20;
        }

        /// <summary>
        /// Mirrors the runner's actual ticking state onto the speed bar.
        /// Fired by <see cref="RefonteSimulationRunner.TickingStateChanged"/>
        /// whenever StartTicking/StopTicking flips IsRunning — notably the
        /// fresh « Lancer la simulation » path, where Rebuild fires the
        /// Rebuilt event while still paused and StartTicking(×1) runs only
        /// afterwards. Reusing the same inference as
        /// <see cref="OnRunnerRebuilt"/> keeps the highlight honest in
        /// every entry path (boot, launch, mid-run reset, manual speed).
        /// </summary>
        private void OnTickingStateChanged()
        {
            if (runner == null) return;
            _state = !runner.IsRunning
                ? SpeedState.Paused
                : InferStateFromTicksPerSecond(runner.TicksPerSecond);
            UpdateActiveVisualState();
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var root = _document.rootVisualElement;
            _pauseButton = root.Q<Button>(pauseButtonName);
            _playX1Button = root.Q<Button>(playX1ButtonName);
            _playX5Button = root.Q<Button>(playX5ButtonName);
            _playX10Button = root.Q<Button>(playX10ButtonName);
            _playX20Button = root.Q<Button>(playX20ButtonName);
            _skipEndButton = root.Q<Button>(skipEndButtonName);
            _dayLabel = root.Q<Label>(dayCounterLabelName);

            if (_pauseButton == null || _playX1Button == null || _playX5Button == null
                || _playX10Button == null || _playX20Button == null || _skipEndButton == null)
            {
                SimLogger.DebugLog("[SpeedControlsBinding] one or more speed buttons not found — check UXML names");
            }
        }

        private void WireCallbacks()
        {
            if (_wired) return;
            if (_pauseButton != null) _pauseButton.clicked += OnPause;
            if (_playX1Button != null) _playX1Button.clicked += OnPlayX1;
            if (_playX5Button != null) _playX5Button.clicked += OnPlayX5;
            if (_playX10Button != null) _playX10Button.clicked += OnPlayX10;
            if (_playX20Button != null) _playX20Button.clicked += OnPlayX20;
            if (_skipEndButton != null) _skipEndButton.clicked += OnSkipToEnd;
            _wired = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wired) return;
            if (_pauseButton != null) _pauseButton.clicked -= OnPause;
            if (_playX1Button != null) _playX1Button.clicked -= OnPlayX1;
            if (_playX5Button != null) _playX5Button.clicked -= OnPlayX5;
            if (_playX10Button != null) _playX10Button.clicked -= OnPlayX10;
            if (_playX20Button != null) _playX20Button.clicked -= OnPlayX20;
            if (_skipEndButton != null) _skipEndButton.clicked -= OnSkipToEnd;
            _wired = false;
        }

        // ---------- Callbacks ----------

        private void OnPause()
        {
            if (runner == null) return;
            runner.StopTicking();
            _state = SpeedState.Paused;
            UpdateActiveVisualState();
            SimLogger.UserActionLog("speed: paused");
        }

        private void OnPlayX1() => SetPlaySpeed(SpeedX1, SpeedState.X1);
        private void OnPlayX5() => SetPlaySpeed(SpeedX5, SpeedState.X5);
        private void OnPlayX10() => SetPlaySpeed(SpeedX10, SpeedState.X10);
        private void OnPlayX20() => SetPlaySpeed(SpeedX20, SpeedState.X20);

        private void SetPlaySpeed(float ticksPerSecond, SpeedState newState)
        {
            if (runner == null) return;
            runner.TicksPerSecond = ticksPerSecond;
            if (!runner.IsRunning) runner.StartTicking();
            _state = newState;
            PlayerPrefs.SetFloat(PrefsKey, ticksPerSecond);
            PlayerPrefs.Save();
            UpdateActiveVisualState();
            SimLogger.UserActionLog("speed: x" + ticksPerSecond.ToString("0.#"));
        }

        private void OnSkipToEnd()
        {
            if (runner == null) return;
            int horizon = runner.HorizonInDays;
            int from = runner.CurrentDay;
            // Skip-to-end is meaningful relative to "from now": one click
            // advances by exactly HorizonInDays, regardless of how many
            // days were already simulated. This matches user intent —
            // "simule mon horizon en partant d'ici" — and lets the user
            // re-press skip-to-end repeatedly to step through horizons.
            int target = from + horizon;
            runner.FastForwardTo(target);
            _state = SpeedState.Paused;
            UpdateActiveVisualState();
            RefreshDayLabel();
            SimLogger.UserActionLog(
                "speed: skip-to-end from day " + from + " to day " + target +
                " (+" + horizon + " days)");
        }

        // ---------- Visual state ----------

        private void UpdateActiveVisualState()
        {
            SetActiveClass(_pauseButton, _state == SpeedState.Paused);
            SetActiveClass(_playX1Button, _state == SpeedState.X1);
            SetActiveClass(_playX5Button, _state == SpeedState.X5);
            SetActiveClass(_playX10Button, _state == SpeedState.X10);
            SetActiveClass(_playX20Button, _state == SpeedState.X20);
            // Skip-to-end is a one-shot action, never "active".
        }

        private static void SetActiveClass(Button button, bool active)
        {
            if (button == null) return;
            button.EnableInClassList("speed-button--active", active);
        }

        private void OnTickCompleted()
        {
            RefreshDayLabel();
        }

        private void RefreshDayLabel()
        {
            if (_dayLabel == null || runner == null) return;
            _dayLabel.text = "Jour " + runner.CurrentDay;
        }

        // Note (2026-05-21): the PlayerPrefs key is still written by
        // SetPlaySpeed for telemetry / future "remember last speed"
        // feature, but it is no longer read at startup — the simulation
        // always boots paused regardless of last session's choice.
    }
}
