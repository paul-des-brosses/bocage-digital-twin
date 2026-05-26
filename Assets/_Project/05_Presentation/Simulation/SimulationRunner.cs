using System.Collections;
using Bocage.Data.RuntimeContainers;
using Bocage.Decision;
using Bocage.Decision.Recommendations;
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

        [Header("Derived presentation channels (sub-étape 9α / 9β)")]
        [SerializeField, Tooltip("Optional. Soil-moisture proxy consumed by S_Meadow. Derived from WaterTableDepth (SoilMoistureIndicator). Safe to leave null if the meadow shader is not in the scene yet.")]
        private RC_SoilMoisture soilMoistureContainer;
        [SerializeField, Tooltip("Optional. Hedgerow-health proxy consumed by the _HealthT channel of the hedge shader. Derived from HedgerowDensity + recent stress events (HedgerowHealthIndicator). Safe to leave null until the shader exposes the property.")]
        private RC_HedgerowHealth hedgerowHealthContainer;

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

        /// <summary>
        /// Fired after a successful <see cref="Rebuild"/>: the engine and
        /// the journal have been wiped and reconstructed from new
        /// initial conditions. The shadow runner subscribes to this so
        /// it can rebuild its own engine in lockstep — otherwise it
        /// would keep ticking the old trajectory while the real moved
        /// to a fresh day-0 state, immediately blowing up TechDelta.
        /// </summary>
        public event System.Action Rebuilt;

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
        /// Wipes the simulation back to day 0 with a fresh
        /// <see cref="EcosystemModel"/> built from the supplied initial
        /// conditions. The current
        /// <see cref="Bocage.SimulationCore.Scenario.ScenarioContext"/>
        /// is RE-USED (so the user's preset / slider choices for
        /// climate, policies and horizon survive a reset), but the
        /// event log, decision journal and detector state are all
        /// reinitialised. After this call:
        /// <list type="bullet">
        ///   <item><see cref="CurrentDay"/> == 0</item>
        ///   <item><see cref="EventLog"/> is empty</item>
        ///   <item><see cref="DecisionJournal"/> is empty</item>
        ///   <item>Hero KPIs have been republished with the new state</item>
        ///   <item>The <see cref="Rebuilt"/> event has fired so the
        ///         shadow runner rebuilds in lockstep</item>
        /// </list>
        /// The runner is left in its current ticking state — if it was
        /// ticking, it continues ticking the new model; if paused, it
        /// stays paused.
        /// </summary>
        public void Rebuild(double initialHedgerowDensity, double initialWaterTableDepth, double initialFaunaPopulation)
        {
            // Build a fresh model with the supplied initial conditions.
            // Non-overridden state (CropYield, InputCost, MaintenanceCost)
            // falls back to the default Perche calibration baseline so the
            // economic KPIs don't snap to weird values at t=0.
            var model = new EcosystemModel(
                initialWaterTableDepth: initialWaterTableDepth,
                initialHedgerowDensity: initialHedgerowDensity,
                initialFaunaPopulation: initialFaunaPopulation);
            _engine = DefaultSimulation.Build(masterSeed, model, _engine != null ? _engine.Scenario : null);
            _currentDay = 0;
            _eventDetector = new EventDetector();
            _eventLog = new EventLog();
            _recommendationEngine = new RecommendationEngine();
            _decisionJournal = new DecisionJournal();

            PublishIndicators();
            // Rebuild does NOT touch the ticking state — the caller
            // decides whether to start, stop or leave as-is.
            // - Lancer la simulation (fresh start): caller stops first,
            //   rebuilds, then starts ticking at ×1.
            // - Réinitialiser la simulation (mid-run reset): caller
            //   stops first, rebuilds, leaves PAUSED so the user can
            //   re-adjust sliders and click Lancer again.
            // This separation keeps the Rebuild API a pure state-reset
            // operation with no implicit side effects on the coroutine.
            Rebuilt?.Invoke();
            SimLogger.UserActionLog(
                "[SimulationRunner] rebuilt at day 0 — hedge=" + initialHedgerowDensity.ToString("F1")
                + " m/ha, depth=" + initialWaterTableDepth.ToString("F1")
                + " m, fauna=" + initialFaunaPopulation.ToString("F2"));
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
        /// Applies a one-off "plant hedges" intervention requested by the
        /// user OUTSIDE the recommendation pathway (manual action button
        /// in the espace agriculteur). Sub-étape 10a friction fix —
        /// before this, the 3 reco actions could only be triggered by
        /// an algorithm prompt, leaving the user passive between events.
        /// <para>
        /// Implementation: the same mechanical effect as
        /// <see cref="Bocage.Decision.AutoActionPipeline.ApplyOne"/>,
        /// applied directly on the real model (the shadow run is not
        /// touched — that asymmetry is exactly what feeds TechDelta).
        /// We do NOT journal the action: the journal carries
        /// recommendation-arbitrage history, and a manual button click
        /// has no triggering event nor outcome bracket. The action
        /// nonetheless surfaces in <see cref="SimLogger.UserActionLog"/>
        /// for telemetry.
        /// </para>
        /// </summary>
        public void ApplyManualPlantHedges(double metersPerHectare)
        {
            if (_engine == null || _engine.Model == null) return;
            double magnitude = metersPerHectare < 0 ? 0 : metersPerHectare;
            _engine.Model.SetHedgerowDensity(_engine.Model.HedgerowDensity + magnitude);
            PublishIndicators();
            SimLogger.UserActionLog("manual: plant-hedges +" + magnitude.ToString("F1") + " m/ha (day " + _currentDay + ")");
        }

        /// <summary>
        /// Manual irrigation intervention: raises the water table by the
        /// chosen depth (clamped so the table doesn't surface absurdly).
        /// See <see cref="ApplyManualPlantHedges"/> for the design
        /// rationale.
        /// </summary>
        public void ApplyManualIrrigation(double depthMeters)
        {
            if (_engine == null || _engine.Model == null) return;
            double magnitude = depthMeters < 0 ? 0 : depthMeters;
            double newDepth = _engine.Model.WaterTableDepth - magnitude;
            if (newDepth < 0.5) newDepth = 0.5;
            _engine.Model.SetWaterTableDepth(newDepth);
            PublishIndicators();
            SimLogger.UserActionLog("manual: irrigation −" + magnitude.ToString("F2") + " m depth (day " + _currentDay + ")");
        }

        /// <summary>
        /// Manual "reduce inputs" pulse: applies the same one-shot fauna
        /// boost + input-cost reduction that the
        /// <see cref="Bocage.Decision.Recommendations.ReduceInputsRecommendation"/>
        /// applies, scaled by the chosen intensity cut. Note this is a
        /// punctual nudge — for sustained reduction, the agriculteur
        /// uses the continuous "Intensité d'intrants" slider instead.
        /// </summary>
        public void ApplyManualReduceInputs(double intensityCut)
        {
            if (_engine == null || _engine.Model == null) return;
            double magnitude = intensityCut < 0 ? 0 : intensityCut;
            double reference = ReduceInputsRecommendation.IntensityCutPerStep;
            double ratio = reference > 0 ? magnitude / reference : 0;
            _engine.Model.SetFaunaPopulation(_engine.Model.FaunaPopulation + 0.05 * ratio);
            _engine.Model.SetInputCost(_engine.Model.InputCost - 200.0 * ratio);
            PublishIndicators();
            SimLogger.UserActionLog("manual: reduce-inputs −" + magnitude.ToString("F2") + " intensity (ratio=" + ratio.ToString("F2") + ", day " + _currentDay + ")");
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

            // ---- Derived presentation channels (sub-étape 9α / 9β) ----
            // These are read by the shader bindings (S_Meadow, S_Pond,
            // SG_Hedgerow). Both are unit-range, so raw == normalized.
            if (soilMoistureContainer != null)
            {
                double moisture = SoilMoistureIndicator.Compute(model);
                double normalized = SoilMoistureIndicator.Normalize(moisture);
                soilMoistureContainer.Set((float)moisture, (float)normalized);
            }

            if (hedgerowHealthContainer != null)
            {
                double health = HedgerowHealthIndicator.Compute(model, _eventLog);
                double normalized = HedgerowHealthIndicator.Normalize(health);
                hedgerowHealthContainer.Set((float)health, (float)normalized);
            }
        }
    }
}
