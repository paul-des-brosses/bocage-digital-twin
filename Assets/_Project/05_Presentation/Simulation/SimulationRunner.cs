using System.Collections;
using Bocage.Data.RuntimeContainers;
using Bocage.Decision;
using Bocage.Indicators.Hero;
using Bocage.Sensors;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Logging;
using Bocage.SimulationCore.Model;
using UnityEngine;

namespace Bocage.Presentation.Simulation
{
    /// <summary>
    /// Owns the real-run <see cref="SimulationEngine"/> and ticks it on a
    /// Unity coroutine. After every tick, queries the relevant indicators
    /// (Couche 4) and pushes their values into the observable
    /// <c>RC_*</c> ScriptableObjects so UI bindings and shader bindings
    /// in Couche 5 receive an <c>OnChanged</c> notification.
    /// <para>
    /// This component is the single writer for the runtime containers it
    /// references: bindings only ever read. That keeps the data flow
    /// strictly one-way (simulation → containers → presentation).
    /// </para>
    /// <para>
    /// Tick cadence is controlled by <see cref="ticksPerSecond"/>; the
    /// coroutine uses <see cref="WaitForSecondsRealtime"/> to stay
    /// independent of <c>Time.timeScale</c>, matching ARCHITECTURE.md §5
    /// (simulated clock decoupled from cosmetic clock). Subsequent
    /// étapes will plug play/pause/x10/skip-to-end on top of this.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-8000)]
    public sealed class SimulationRunner : MonoBehaviour
    {
        [Header("Determinism")]
        [SerializeField, Tooltip("Master seed for the SeededRandom. Same seed + same scenario => same trajectory.")]
        private ulong masterSeed = 1UL;

        [Header("Cadence")]
        [SerializeField, Range(0.5f, 20f), Tooltip("Simulated days per real-time second. 1 = x1, 10 = x10.")]
        private float ticksPerSecond = 1f;

        [SerializeField, Tooltip("If true, ticking starts at Start(). If false, an external controller will call StartTicking() (e.g. the future play/pause UI of Étape 7).")]
        private bool autoStart = true;

        [Header("Observable containers")]
        [SerializeField] private RC_HedgerowDensity hedgerowDensityContainer;
        [SerializeField] private RC_WaterTableDepth waterTableContainer;
        [SerializeField] private RC_IntegratedProfitability profitabilityContainer;
        [SerializeField] private RC_BiodiversityComposite biodiversityContainer;
        [SerializeField] private RC_TechDelta techDeltaContainer;

        [Header("Shadow run (sub-étape 8b)")]
        [SerializeField, Tooltip("Optional. If assigned, the shadow runner provides the second EcosystemModel against which the real run is compared by TechDeltaIndicator. If left null, the tech-delta KPI is computed against the real run itself (delta = 0).")]
        private ShadowSimulationRunner shadowRunner;

        private SimulationEngine _engine;
        private Coroutine _tickRoutine;
        private int _currentDay;
        private EventDetector _eventDetector;
        private EventLog _eventLog;
        private RecommendationEngine _recommendationEngine;
        private DecisionJournal _decisionJournal;

        public EcosystemModel Model => _engine?.Model;
        public Bocage.SimulationCore.Scenario.ScenarioContext Scenario => _engine?.Scenario;
        public bool IsRunning => _tickRoutine != null;

        /// <summary>
        /// Master seed used to build the engine. Exposed so the shadow
        /// runner can build its own engine with the SAME seed (and the
        /// shared scenario reference) so the two trajectories only
        /// diverge through tech actions, not through RNG.
        /// </summary>
        public ulong MasterSeed => masterSeed;

        /// <summary>
        /// Append-only history of events emitted by the Couche 2
        /// <see cref="EventDetector"/> during this run. Read by the
        /// Couche 3 recommendation engine (sub-étape 8c.2) and by the
        /// Couche 5 decision panel (sub-étape 8c.3). At sub-étape 8c.1
        /// the log is populated but no UI consumes it yet.
        /// </summary>
        public EventLog EventLog => _eventLog;

        /// <summary>
        /// Append-only history of recommendations and their verdicts.
        /// Read by the Couche 5 decision panel (sub-étape 8c.3) and by
        /// the auto-actions consumer that applies accepted entries to
        /// the real engine. The shadow run does NOT see this journal —
        /// that asymmetry is exactly what makes the tech-delta KPI
        /// meaningful.
        /// </summary>
        public DecisionJournal DecisionJournal => _decisionJournal;

        /// <summary>
        /// Number of simulated days that have elapsed since startup. Used
        /// by the speed-control UI (sub-étape 7c.3) to render the
        /// "skip-to-end" stop condition and by any future "session report"
        /// to know how long the run lasted.
        /// </summary>
        public int CurrentDay => _currentDay;

        /// <summary>
        /// Tick cadence in simulated days per real-time second. Mutable at
        /// runtime by the speed controls. Clamped to ]0, 200] to keep the
        /// coroutine's WaitForSecondsRealtime sane.
        /// </summary>
        public float TicksPerSecond
        {
            get => ticksPerSecond;
            set => ticksPerSecond = Mathf.Clamp(value, 0.01f, 200f);
        }

        /// <summary>
        /// Fired after every Tick + PublishIndicators pass. Used by
        /// diagnostic recorders (cf. <c>SimulationTraceRecorder</c>) to
        /// snapshot model and scenario state without re-running the
        /// simulation. Also useful for any future binding that needs a
        /// reliable "post-tick" hook rather than polling Update().
        /// </summary>
        public event System.Action TickCompleted;

        private void Awake()
        {
            _engine = DefaultSimulation.Build(masterSeed);
            _eventDetector = new EventDetector();
            _eventLog = new EventLog();
            _recommendationEngine = new RecommendationEngine();
            _decisionJournal = new DecisionJournal();
            SimLogger.SimulationLog(
                "[SimulationRunner] engine built seed=" + masterSeed +
                " initialHedgerowDensity=" + _engine.Model.HedgerowDensity.ToString("F1") + " m/ha");

            // Push initial values immediately so bindings that subscribe on Enable
            // already see a consistent state before the first tick fires.
            PublishIndicators();
        }

        private void Start()
        {
            if (autoStart)
            {
                StartTicking();
            }
        }

        private void OnDisable()
        {
            StopTicking();
        }

        public void StartTicking()
        {
            if (_tickRoutine != null) return;
            _tickRoutine = StartCoroutine(TickLoop());
        }

        public void StopTicking()
        {
            if (_tickRoutine == null) return;
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
        }

        private IEnumerator TickLoop()
        {
            // Local cached WaitForSecondsRealtime would be ideal, but ticksPerSecond
            // can change at runtime (Étape 7 speed control); recreate the wait per
            // tick. This is one allocation per tick, far from a hot-path concern.
            while (true)
            {
                float interval = ticksPerSecond <= 0f ? 1f : 1f / ticksPerSecond;
                yield return new WaitForSecondsRealtime(interval);

                _engine.Tick();
                _currentDay++;
                _eventDetector.Detect(_engine.Model, _eventLog);
                PublishRecommendations();
                // TickCompleted fires BEFORE PublishIndicators so that
                // the shadow runner (subscriber) advances its own engine
                // FIRST, and any auto-actions (also subscribers) modify
                // the real model — then PublishIndicators reads the
                // synchronized post-tick state of BOTH engines.
                // Without this order, TechDelta would drift by an
                // off-by-one tick under sustained scenario stress.
                TickCompleted?.Invoke();
                PublishIndicators();
            }
        }

        /// <summary>
        /// Synchronously ticks the engine until <see cref="CurrentDay"/>
        /// reaches <paramref name="targetDay"/> (or already past it, in
        /// which case it does nothing). The coroutine is stopped before
        /// the loop so no concurrent tick fires, then restarted is up to
        /// the caller (typically the speed controls leave the runner in
        /// the paused state after a skip-to-end). The publishing pass is
        /// done at the end only to avoid 1825 binding updates for a 5-year
        /// skip; we still fire <see cref="TickCompleted"/> per tick so
        /// diagnostic recorders capture every day.
        /// </summary>
        public void FastForwardTo(int targetDay)
        {
            int safetyCap = 100000; // ≈ 273 years sim, hard ceiling.
            int budget = Mathf.Min(safetyCap, Mathf.Max(0, targetDay - _currentDay));
            if (budget == 0) return;

            bool wasRunning = IsRunning;
            StopTicking();

            for (int i = 0; i < budget; i++)
            {
                _engine.Tick();
                _currentDay++;
                _eventDetector.Detect(_engine.Model, _eventLog);
                PublishRecommendations();
                TickCompleted?.Invoke();
            }
            PublishIndicators();
            SimLogger.SimulationLog(
                "[SimulationRunner] fast-forward done, " + budget + " ticks, now day=" + _currentDay);
            // We intentionally do NOT restart ticking — sub-étape 7c.3
            // skip-to-end ends in a paused state so the user can inspect.
            // The caller is free to call StartTicking() again if needed.
        }

        /// <summary>
        /// Asks the <see cref="RecommendationEngine"/> for fresh
        /// recommendations against the current event log + journal,
        /// and appends new ones to the journal. The journal's own
        /// de-dup guard means calling this every tick is safe even
        /// though the same events linger in the log.
        /// </summary>
        private void PublishRecommendations()
        {
            var pending = _recommendationEngine.ProduceRecommendations(_eventLog, _decisionJournal);
            for (int i = 0; i < pending.Count; i++)
            {
                _decisionJournal.Append(pending[i], _currentDay);
            }
        }

        private void PublishIndicators()
        {
            var model = _engine.Model;

            if (hedgerowDensityContainer != null)
            {
                double raw = HedgerowDensityIndicator.Compute(model);
                double normalized = HedgerowDensityIndicator.Normalize(raw);
                hedgerowDensityContainer.Set((float)raw, (float)normalized);
            }

            if (waterTableContainer != null)
            {
                double raw = WaterTableIndicator.Compute(model);
                double normalized = WaterTableIndicator.Normalize(raw);
                waterTableContainer.Set((float)raw, (float)normalized);
            }

            if (profitabilityContainer != null)
            {
                double raw = IntegratedProfitabilityIndicator.Compute(model, _engine.Scenario);
                double normalized = IntegratedProfitabilityIndicator.Normalize(raw);
                profitabilityContainer.Set((float)raw, (float)normalized);
            }

            if (biodiversityContainer != null)
            {
                double raw = BiodiversityCompositeIndicator.Compute(model);
                double normalized = BiodiversityCompositeIndicator.Normalize(raw);
                biodiversityContainer.Set((float)raw, (float)normalized);
            }

            if (techDeltaContainer != null)
            {
                // If no shadow runner is wired, the comparison degenerates
                // to "real vs real" → delta = 0. This is honest reporting:
                // we publish the value that the indicator computes from
                // whatever shadow model is available.
                var shadowModel = shadowRunner != null && shadowRunner.ShadowModel != null
                    ? shadowRunner.ShadowModel
                    : model;
                double raw = TechDeltaIndicator.Compute(model, shadowModel, _engine.Scenario);
                double normalized = TechDeltaIndicator.Normalize(raw);
                techDeltaContainer.Set((float)raw, (float)normalized);
            }
        }
    }
}
