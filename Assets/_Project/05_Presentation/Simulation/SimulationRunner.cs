using System.Collections;
using Bocage.Data.RuntimeContainers;
using Bocage.Decision;
using Bocage.Decision.Recommendations;
using Bocage.Indicators.Hero;
using Bocage.Sensors;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Logging;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
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

        [Header("Climate")]
        [SerializeField, Tooltip("Seasonal weather authoring asset (chantier E2). If null, the engine falls back to the Mortagne-au-Perche normals hard-coded in SeasonalWeatherDataDefaults.")]
        private Bocage.Presentation.Weather.SeasonalWeatherDataAsset seasonalWeatherAsset;

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
        [SerializeField, Tooltip("Optional. Soil organic carbon Hero KPI (chantier E3 / ADR #48). Safe to leave null until the onglet Climat binding is wired in chantier E6.")]
        private RC_SoilCarbonStock soilCarbonContainer;

        [Header("Capital & horizon (chantier E5 / ADR #50)")]
        [SerializeField, Tooltip("Optional. Cumulative upfront capital invested via « Replanter haies » manual actions. Safe to leave null until the popup + Économie onglet bindings are wired.")]
        private RC_TotalInvestment totalInvestmentContainer;
        [SerializeField, Tooltip("Optional. Horizon de rentabilité (years to recoup the total investment via real-vs-shadow profit divergence). Safe to leave null until the popup + Économie onglet bindings are wired.")]
        private RC_InvestmentHorizon investmentHorizonContainer;

        [Header("Biodiv 3 facteurs (chantier E5 / ADR #51)")]
        [SerializeField, Tooltip("Optional. Habitat factor (derived from HedgerowDensity via FaunaDynamicsRule.ComputeHabitatFactor). Safe to leave null until the onglet Biodiv binding is wired in chantier E6.")]
        private RC_FaunaFactorHabitat faunaFactorHabitatContainer;
        [SerializeField, Tooltip("Optional. Water factor (derived from WaterTableDepth via FaunaDynamicsRule.ComputeWaterFactor). Safe to leave null until the onglet Biodiv binding is wired in chantier E6.")]
        private RC_FaunaFactorWater faunaFactorWaterContainer;
        [SerializeField, Tooltip("Optional. Inputs factor (derived from ScenarioContext.InputIntensityFactor via FaunaDynamicsRule.ComputeInputsFactor). Safe to leave null until the onglet Biodiv binding is wired in chantier E6.")]
        private RC_FaunaFactorInputs faunaFactorInputsContainer;

        [Header("Derived presentation channels (sub-étape 9α / 9β)")]
        [SerializeField, Tooltip("Optional. Soil-moisture proxy consumed by S_Meadow. Derived from WaterTableDepth (SoilMoistureIndicator). Safe to leave null if the meadow shader is not in the scene yet.")]
        private RC_SoilMoisture soilMoistureContainer;
        [SerializeField, Tooltip("Optional. Hedgerow-health proxy consumed by the _HealthT channel of the hedge shader. Derived from HedgerowDensity + recent stress events (HedgerowHealthIndicator). Safe to leave null until the shader exposes the property.")]
        private RC_HedgerowHealth hedgerowHealthContainer;

        [Header("Shadow run (sub-étape 8b)")]
        [SerializeField, Tooltip("Optional. If assigned, the shadow runner provides the second EcosystemModel against which the real run is compared for the cumulative tech-value KPI. If left null, the comparison degenerates to real vs real (delta = 0).")]
        private ShadowSimulationRunner shadowRunner;

        private SimulationEngine _engine;
        private Coroutine _tickRoutine;
        private int _currentDay;
        // Built once at Awake and reused on every Rebuild so the shadow
        // run consumes the exact same SeasonalWeatherData instance and
        // its weather noise stays in lockstep with the real run (their
        // divergence must come only from auto-actions, not from
        // independent re-roll of the seasonal asset's float→double cast).
        private Bocage.SimulationCore.Model.SeasonalWeatherData _seasonalWeather;
        private EventDetector _eventDetector;
        private EventLog _eventLog;
        private RecommendationEngine _recommendationEngine;
        private DecisionJournal _decisionJournal;
        private FaunaSensorReader _faunaSensorReader;
        private WeatherStationReader _weatherStationReader;
        private EddyTowerSensorReader _eddyTowerSensorReader;
        private PiezometerReader _piezometerReader;
        // Cumulative profit-delta integrator for the « horizon de
        // rentabilité » indicator (chantier E5 / ADR #50). Stateful by
        // nature — the integral cannot be derived from a snapshot —
        // so we own one instance per real run and reset it on Rebuild
        // so each trajectory starts with a clean cumul.
        private InvestmentHorizonIndicator _investmentHorizon;
        private CumulativeTechValueIndicator _techValue;
        // Per-type counters for manual actions (ADR #47). Disambiguates
        // multiple clicks on the same simulated day — each click gets a
        // unique recommendation id even though the day suffix collides.
        private int _manualPlantHedgesSeq;
        private int _manualIrrigationSeq;

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
        /// Immutable monthly normals consumed by the engine's
        /// <see cref="Bocage.SimulationCore.Rules.WeatherUpdateRule"/>.
        /// Exposed so the shadow runner can build its own engine against
        /// the exact same instance — sharing the reference avoids paying
        /// for two independent float→double conversions of the asset
        /// (which would otherwise produce different doubles by
        /// nondeterministic rounding and break weather lockstep between
        /// real and shadow runs).
        /// </summary>
        public Bocage.SimulationCore.Model.SeasonalWeatherData SeasonalWeather => _seasonalWeather;

        /// <summary>
        /// On-site WeatherStation sensor (chantier E2 / ADR #52). Owns
        /// the noisy reading of today's T° + precip plus a 365-day
        /// sliding window for the inspection panel that will be built in
        /// chantier E6 / ADR #53. Exposed so future bindings can read
        /// the recorded history; nothing else is wired to it yet.
        /// </summary>
        public WeatherStationReader WeatherStation => _weatherStationReader;

        /// <summary>
        /// On-site EddyTower sensor (chantier E3 / ADR #48). Owns the
        /// noisy reading of today's net CO2 flux derived from the
        /// day-over-day change in <see cref="EcosystemModel.SoilCarbonStock"/>,
        /// plus a 365-day sliding window for the inspection panel that
        /// will be built in chantier E6 / ADR #53. Exposed so future
        /// bindings can read the recorded flux history; nothing else
        /// consumes it yet.
        /// </summary>
        public EddyTowerSensorReader EddyTower => _eddyTowerSensorReader;

        /// <summary>
        /// On-site Piezometer sensor (chantier E6 / ADR #53). Owns the
        /// noisy reading of today's water-table depth plus a 365-day
        /// sliding window of paired (noisy, ground-truth) samples that
        /// the inspection panel binding reads. Indicators and rules
        /// continue to consume <see cref="EcosystemModel.WaterTableDepth"/>
        /// directly — bruiter la nappe partout serait une refonte
        /// scientifique non demandée par l'ADR.
        /// </summary>
        public PiezometerReader Piezometer => _piezometerReader;

        /// <summary>
        /// Fauna sensor composite (chantier E6 / ADR #53). Exposed so
        /// the inspection panel can drill into the two underlying
        /// channels via <c>FaunaSensor.Acoustic</c> and
        /// <c>FaunaSensor.Camera</c>, each owning its own 365-day
        /// history of paired (noisy, ground-truth) samples. The
        /// detector still consumes the fused estimate, which is what
        /// <see cref="FaunaSensorReader.ReadAndRecord"/> returns.
        /// </summary>
        public FaunaSensorReader FaunaSensor => _faunaSensorReader;

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

        /// <summary>
        /// Fired whenever the tick coroutine starts or stops (i.e. whenever
        /// <see cref="IsRunning"/> flips). Lets the speed-control UI mirror
        /// the runner's *actual* ticking state instead of inferring it at
        /// the wrong moment — e.g. « Lancer la simulation » rebuilds (which
        /// fires <see cref="Rebuilt"/> while the runner is still paused) and
        /// only then starts ticking at ×1. Without this signal the speed bar
        /// would stay highlighted on Pause while the engine runs at ×1.
        /// </summary>
        public event System.Action TickingStateChanged;

        private void Awake()
        {
            _seasonalWeather = seasonalWeatherAsset != null
                ? seasonalWeatherAsset.ToSeasonalWeatherData()
                : Bocage.SimulationCore.Model.SeasonalWeatherDataDefaults.MortagneAuPerche();
            _engine = DefaultSimulation.Build(masterSeed, seasonalWeather: _seasonalWeather);
            _eventDetector = new EventDetector();
            _eventLog = new EventLog();
            _recommendationEngine = new RecommendationEngine();
            _decisionJournal = new DecisionJournal();
            // Fresh SeededRandom built from the same master seed: the
            // engine owns its own master internally, so giving the fauna
            // sensor an independent SeededRandom (with the same seed)
            // means its derived "fauna-sensors" sub-stream is reproducible
            // and isolated from every other sub-system.
            _faunaSensorReader = new FaunaSensorReader(new SeededRandom(masterSeed));
            _weatherStationReader = new WeatherStationReader(new SeededRandom(masterSeed));
            _eddyTowerSensorReader = new EddyTowerSensorReader(new SeededRandom(masterSeed));
            // Piezometer (chantier E6 / ADR #53): observes the model's
            // water-table depth, records a 365-day noisy history for the
            // inspection panel. Indicators/rules still read truth from
            // the model — this reader is consumed only by the panel.
            _piezometerReader = new PiezometerReader(new SeededRandom(masterSeed));
            _investmentHorizon = new InvestmentHorizonIndicator();
            _techValue = new CumulativeTechValueIndicator();
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
            TickingStateChanged?.Invoke();
        }

        public void StopTicking()
        {
            if (_tickRoutine == null) return;
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
            TickingStateChanged?.Invoke();
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

                StepOneDay();
                PublishIndicators();
            }
        }

        /// <summary>
        /// Advances the engine by exactly one simulated day: ticks the model,
        /// records every sensor, runs detection + recommendations, then fires
        /// TickCompleted (so the shadow runner advances and auto-actions mutate
        /// the real model) and updates the investment horizon. Does NOT publish
        /// indicators — callers decide when (every tick in the live loop, once
        /// at the end of a fast-forward).
        /// </summary>
        private void StepOneDay()
        {
            _engine.Tick();
            _currentDay++;
            _eventDetector.Detect(
                _engine.Model,
                _eventLog,
                _faunaSensorReader.ReadAndRecord(_engine.Model.FaunaPopulation));
            _weatherStationReader.ReadAndRecord(_engine.Model.CurrentWeather);
            _eddyTowerSensorReader.ReadAndRecord(_engine.Model.SoilCarbonStock);
            _piezometerReader.ReadAndRecord(_engine.Model.WaterTableDepth);
            PublishRecommendations();
            // TickCompleted fires BEFORE the caller's PublishIndicators so the
            // shadow runner (subscriber) advances its own engine FIRST, and any
            // auto-actions (also subscribers) modify the real model — then
            // PublishIndicators reads the synchronized post-tick state of BOTH
            // engines. Without this order, TechDelta would drift by an
            // off-by-one tick under sustained scenario stress.
            TickCompleted?.Invoke();
            UpdateProfitAccumulators();
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
            _engine = DefaultSimulation.Build(
                masterSeed,
                model,
                _engine != null ? _engine.Scenario : null,
                _seasonalWeather);
            _currentDay = 0;
            _eventDetector = new EventDetector();
            _eventLog = new EventLog();
            _recommendationEngine = new RecommendationEngine();
            _decisionJournal = new DecisionJournal();
            _manualPlantHedgesSeq = 0;
            _manualIrrigationSeq = 0;
            // Re-instantiate so the fauna sensor noise sequence restarts
            // from day 0 in lockstep with the engine. Otherwise the
            // existing instance's internal state would have advanced and
            // the rebuilt trajectory would no longer be deterministic.
            _faunaSensorReader = new FaunaSensorReader(new SeededRandom(masterSeed));
            // Same reasoning as the fauna reader: the noisy weather sequence
            // restarts from day 0 in lockstep with the rebuilt engine so
            // the 365-day buffer is consistent with the new trajectory.
            _weatherStationReader = new WeatherStationReader(new SeededRandom(masterSeed));
            // Same lockstep reasoning for the EddyTower: the noisy flux
            // history restarts from day 0 with a fresh baseline so the
            // first recorded sample is taken against the new initial
            // SoilCarbonStock rather than against the previous run's
            // last stock.
            _eddyTowerSensorReader = new EddyTowerSensorReader(new SeededRandom(masterSeed));
            // Piezometer (chantier E6 / ADR #53): observes the model's
            // water-table depth, records a 365-day noisy history for the
            // inspection panel. Indicators/rules still read truth from
            // the model — this reader is consumed only by the panel.
            _piezometerReader = new PiezometerReader(new SeededRandom(masterSeed));
            _investmentHorizon = new InvestmentHorizonIndicator();
            _techValue = new CumulativeTechValueIndicator();

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
                StepOneDay();
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
        /// user via the « Replanter haies » manual button in the espace
        /// agriculteur. ADR #47 refactor: the action is journalled as a
        /// <see cref="PlantHedgesRecommendation"/> manual rec
        /// (<see cref="DecisionVerdict.AutoAccepted"/>, no triggering
        /// event), then <see cref="AutoActionPipeline.Apply"/> mutates
        /// the model — making the pipeline the single mutator. The
        /// effect is synchronous: by the time the call returns, the
        /// model has been updated and indicators republished.
        /// <para>
        /// The shadow run is not touched — that asymmetry is exactly
        /// what feeds the cumulative tech-value KPI
        /// (<see cref="Bocage.Indicators.Hero.CumulativeTechValueIndicator"/>).
        /// Manual actions are cumulable (per ADR #47): two clicks on the
        /// same simulated day produce two distinct journal entries via
        /// the per-type sequence counter, both applied.
        /// </para>
        /// </summary>
        public void ApplyManualPlantHedges(double metersPerHectare)
        {
            if (_engine == null || _engine.Model == null) return;
            double magnitude = metersPerHectare < 0 ? 0 : metersPerHectare;
            _manualPlantHedgesSeq++;
            var rec = PlantHedgesRecommendation.Manual(_currentDay, _manualPlantHedgesSeq, magnitude);
            _decisionJournal.Append(rec, _currentDay, magnitude);
            AutoActionPipeline.Apply(_decisionJournal, _engine.Model, _engine.Scenario, _currentDay);
            PublishIndicators();
            SimLogger.UserActionLog("manual: plant-hedges +" + magnitude.ToString("F1") + " m/ha (day " + _currentDay + ", id=" + rec.Id + ")");
        }

        /// <summary>
        /// Manual « Irrigation ponctuelle » intervention. ADR #47
        /// pathway — see <see cref="ApplyManualPlantHedges"/>. The
        /// water-table floor (0.5 m) is enforced inside
        /// <see cref="AutoActionPipeline.ApplyOne"/>; the runner
        /// therefore passes the raw magnitude.
        /// </summary>
        public void ApplyManualIrrigation(double depthMeters)
        {
            if (_engine == null || _engine.Model == null) return;
            double magnitude = depthMeters < 0 ? 0 : depthMeters;
            _manualIrrigationSeq++;
            var rec = IrrigationAdviceRecommendation.Manual(_currentDay, _manualIrrigationSeq, magnitude);
            _decisionJournal.Append(rec, _currentDay, magnitude);
            AutoActionPipeline.Apply(_decisionJournal, _engine.Model, _engine.Scenario, _currentDay);
            PublishIndicators();
            SimLogger.UserActionLog("manual: irrigation −" + magnitude.ToString("F2") + " m depth (day " + _currentDay + ", id=" + rec.Id + ")");
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
                double raw = BiodiversityCompositeIndicator.Compute(model, _engine.Scenario);
                double normalized = BiodiversityCompositeIndicator.Normalize(raw);
                biodiversityContainer.Set((float)raw, (float)normalized);
            }

            // ---- 3 fauna factors (chantier E5 / ADR #51) ----
            // Always compute when any consumer is wired; pre-E6 only the
            // composite was published, but the onglet Biodiv (E6) and the
            // future FaunaPoolBinding selectivity (E4) both need the
            // factor decomposition. Cheap pure functions, no allocation.
            if (faunaFactorHabitatContainer != null
                || faunaFactorWaterContainer != null
                || faunaFactorInputsContainer != null)
            {
                double habitatF = FaunaDynamicsRule.ComputeHabitatFactor(model.HedgerowDensity);
                double waterF = FaunaDynamicsRule.ComputeWaterFactor(model.WaterTableDepth);
                double inputsF = FaunaDynamicsRule.ComputeInputsFactor(_engine.Scenario.InputIntensityFactor.Current);
                if (faunaFactorHabitatContainer != null)
                {
                    faunaFactorHabitatContainer.Set(
                        (float)habitatF,
                        (float)BiodiversityCompositeIndicator.NormalizeHabitat(habitatF));
                }
                if (faunaFactorWaterContainer != null)
                {
                    faunaFactorWaterContainer.Set(
                        (float)waterF,
                        (float)BiodiversityCompositeIndicator.NormalizeWater(waterF));
                }
                if (faunaFactorInputsContainer != null)
                {
                    faunaFactorInputsContainer.Set(
                        (float)inputsF,
                        (float)BiodiversityCompositeIndicator.NormalizeInputs(inputsF));
                }
            }

            if (techDeltaContainer != null && _techValue != null)
            {
                // « Apport de la techno »: mirror the cumulative €/ha advantage
                // banked so far (the integral is advanced once per tick in
                // UpdateProfitAccumulators). Long-term and honest — a transient
                // action plateaus instead of collapsing back to 0.
                double cumulative = _techValue.CumulativeEurosPerHa;
                double normalized = CumulativeTechValueIndicator.Normalize(cumulative);
                techDeltaContainer.Set((float)cumulative, (float)normalized);
            }

            if (soilCarbonContainer != null)
            {
                double raw = SoilCarbonIndicator.Compute(model);
                double normalized = SoilCarbonIndicator.Normalize(raw);
                soilCarbonContainer.Set((float)raw, (float)normalized);
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

            // ---- Capital + horizon (chantier E5 / ADR #50) ----
            // _investmentHorizon is updated separately in TickLoop /
            // FastForwardTo so the per-tick integral is only counted
            // once per simulated day (not on manual-action publishes
            // that happen between ticks). PublishIndicators just mirrors
            // its current state to the observable containers.
            if (totalInvestmentContainer != null)
            {
                double total = _decisionJournal != null ? _decisionJournal.TotalInvestmentEurosPerHectare : 0.0;
                float norm = Mathf.Clamp01((float)total / RC_TotalInvestment.MaxEurosPerHectare);
                totalInvestmentContainer.Set((float)total, norm);
            }

            if (investmentHorizonContainer != null && _investmentHorizon != null)
            {
                investmentHorizonContainer.Set(
                    _investmentHorizon.IsHorizonReached,
                    (float)_investmentHorizon.HorizonYears,
                    (float)_investmentHorizon.CumulativeProfitDeltaEurosPerHa);
            }
        }

        /// <summary>
        /// Per-tick update of the two trajectory-based accumulators, from a
        /// single real/shadow profit computation. Called once per simulated
        /// day from the tick loops AFTER both engines have advanced, so the
        /// annualised profits are read on synchronised post-tick state.
        /// <list type="bullet">
        ///   <item>« Apport de la techno » (<c>CumulativeTechValueIndicator</c>):
        ///         integrated from day 0, ungated.</item>
        ///   <item>« Horizon de rentabilité » (<c>InvestmentHorizonIndicator</c>,
        ///         chantier E5 / ADR #50): only once an investment exists.</item>
        /// </list>
        /// Idempotent against a missing shadow runner: the comparison
        /// degenerates to <c>real − real == 0</c>, leaving both integrals idle.
        /// </summary>
        private void UpdateProfitAccumulators()
        {
            if (_engine == null) return;
            double realProfit = IntegratedProfitabilityIndicator.Compute(_engine.Model, _engine.Scenario);
            var shadowModel = shadowRunner != null && shadowRunner.ShadowModel != null
                ? shadowRunner.ShadowModel
                : _engine.Model;
            double shadowProfit = IntegratedProfitabilityIndicator.Compute(shadowModel, _engine.Scenario);

            // Cumulative « apport de la techno »: integrated from day 0,
            // ungated — every day's real-vs-shadow profit gap is banked.
            if (_techValue != null) _techValue.Update(realProfit, shadowProfit);

            // Payback horizon: only accumulates once an investment exists to
            // amortise (latches the first day the cumul covers the bill).
            if (_investmentHorizon != null && _decisionJournal != null)
            {
                double totalInvestment = _decisionJournal.TotalInvestmentEurosPerHectare;
                if (totalInvestment > 0.0)
                {
                    _investmentHorizon.Update(realProfit, shadowProfit, totalInvestment, _currentDay);
                }
            }
        }
    }
}
