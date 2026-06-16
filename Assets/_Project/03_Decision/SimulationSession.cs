using System;
using System.Collections.Generic;
using Bocage.SimulationCore;
using Bocage.Sensors;
using SeededRandom = Bocage.SimulationCore.SeededRandom;

namespace Bocage.Decision
{
    /// <summary>
    /// Orchestrateur de session (C# pur, testable — le « cerveau » du pont vers
    /// l'UI). Pilote en lockstep le <b>run réel</b> et le <b>run fantôme</b>
    /// (baseline gelée : même météo/seed, décisions agriculteur figées à leur
    /// valeur initiale), lit les capteurs sur la vérité du réel, détecte les
    /// événements sur les MESURES, et produit des recommandations à la demande.
    /// La divergence réel↔fantôme = l'effet des décisions (apport de la techno).
    /// Aucune I/O, aucun UnityEngine.
    /// </summary>
    public sealed class SimulationSession
    {
        private readonly SimulationEngine _real;
        private readonly SimulationEngine _shadow;
        private readonly ScenarioContext _liveScenario;     // muté par les décisions
        private readonly Climatology _climatology;
        private readonly ulong _masterSeed;

        private readonly WeatherStationReader _weatherStation;
        private readonly EddyTowerReader _eddyTower;
        private readonly FaunaSensorReader _faunaSensor;
        private readonly PiezometerReader _piezometer;
        private readonly EventDetector _detector = new EventDetector();
        private readonly EventLog _eventLog = new EventLog();
        private readonly RecommendationEngine _recommendationEngine;

        private double _totalInvestmentEurosPerHa;

        // --- Cycle de vie des recommandations ---
        public const int RecoActiveDays = 45;     // > cooldown détecteur (30 j) : une condition persistante reste « active »
        public const int RecoCooldownDays = 60;   // anti-spam : après résolution, pas de nouvelle reco du même type avant ce délai
        public const double LeverSatisfiedToleranceFraction = 0.02;  // reco « satisfaite » si le levier est à ±2 % de la plage du niveau recommandé
        private static readonly EventKind[] AllEventKinds =
        {
            EventKind.HydricStress, EventKind.SoilCarbonLow, EventKind.FaunaAnomaly,
            EventKind.NitrogenDeficiency, EventKind.NitrogenExcess, EventKind.LowProfitability
        };
        private readonly List<Recommendation> _pending = new List<Recommendation>();
        private readonly HashSet<Recommendation> _deferred = new HashSet<Recommendation>();
        private readonly Dictionary<EventKind, int> _recoCooldownUntilDay = new Dictionary<EventKind, int>();
        private readonly Dictionary<EventKind, int> _lastAttemptEventDay = new Dictionary<EventKind, int>();

        // --- Fenêtre glissante météo (365 j) + dernier flux Eddy (onglet Climat) ---
        private const int WeatherWindowDays = 365;
        private readonly double[] _tempWindow = new double[WeatherWindowDays];
        private readonly double[] _precipWindow = new double[WeatherWindowDays];
        private int _weatherWindowIndex;
        private int _weatherWindowCount;
        private double _tempSum;
        private double _precipSum;

        public EcosystemModel RealModel => _real.Model;
        public EcosystemModel ShadowModel => _shadow.Model;
        public ScenarioContext Scenario => _liveScenario;
        public EventLog Events => _eventLog;
        public int CurrentDay => _real.Model.CurrentDay;

        // Dernières mesures capteurs.
        public double MeasuredHumidityFraction { get; private set; }
        public double MeasuredFauna { get; private set; }
        public double MeasuredWaterTableDepthM { get; private set; }
        public double EstimatedCarbonTPerHa => _eddyTower.EstimatedCarbonStockTPerHa;

        // Données météo agrégées + dernier flux CO2 (onglet Climat / inspecteur capteurs).
        public double LastFluxKgCo2 { get; private set; }
        public double MeanRecentTemperatureC => _weatherWindowCount > 0 ? _tempSum / _weatherWindowCount : 0.0;
        public double RecentPrecipitationCumulMm => _precipSum;

        /// <summary>Apport de la techno = capital réel − capital fantôme − investissements.</summary>
        public double TechValueNetEurosPerHa
            => _real.Model.CapitalEurosPerHa - _shadow.Model.CapitalEurosPerHa - _totalInvestmentEurosPerHa;

        public SimulationSession(EcosystemModel initialModel, ScenarioContext initialScenario,
            Climatology climatology, ulong masterSeed)
        {
            if (initialModel == null) throw new ArgumentNullException(nameof(initialModel));
            if (initialScenario == null) throw new ArgumentNullException(nameof(initialScenario));
            _climatology = climatology ?? throw new ArgumentNullException(nameof(climatology));
            _masterSeed = masterSeed;

            _liveScenario = new ScenarioContext(initialScenario);
            var frozenScenario = new ScenarioContext(initialScenario); // le fantôme reste figé là-dessus
            _real = MakeEngine(new EcosystemModel(initialModel), _liveScenario);
            _shadow = MakeEngine(new EcosystemModel(initialModel), frozenScenario);

            SeededRandom sensorRoot = new SeededRandom(masterSeed).DeriveSubStream("sensors");
            _weatherStation = new WeatherStationReader(sensorRoot.DeriveSubStream(WeatherStationReader.SubStreamId));
            _eddyTower = new EddyTowerReader(sensorRoot.DeriveSubStream(EddyTowerReader.SubStreamId),
                initialModel.SoilCarbonTotalTPerHa);
            _faunaSensor = new FaunaSensorReader(sensorRoot.DeriveSubStream(FaunaSensorReader.SubStreamId));
            _piezometer = new PiezometerReader(sensorRoot.DeriveSubStream(PiezometerReader.SubStreamId));

            _recommendationEngine = new RecommendationEngine(new ModelOutcomeProjector(climatology));
        }

        private SimulationEngine MakeEngine(EcosystemModel model, ScenarioContext scenario)
        {
            // Réel et fantôme partagent le seed météo → même météo (divergence = décisions).
            var weather = new WeatherGenerator(_climatology,
                new SeededRandom(_masterSeed).DeriveSubStream(WeatherGenerator.SubStreamId));
            return new SimulationEngine(model, scenario, weather);
        }

        public void Tick()
        {
            _real.Tick();
            _shadow.Tick();

            EcosystemModel m = _real.Model;
            double ruMax = WaterBalanceRule.SoilWaterCapacityMm(m.SoilCarbonTotalTPerHa);
            double humidityTruth = ruMax > 0.0 ? m.SoilWaterMm / ruMax : 0.0;

            MeasuredHumidityFraction = _weatherStation.ReadHumidityFraction(humidityTruth);
            MeasuredFauna = _faunaSensor.ReadBiodiversity(m.Biodiversity);
            MeasuredWaterTableDepthM = _piezometer.ReadDepthMeters(m.WaterTableDepthM);
            LastFluxKgCo2 = _eddyTower.ReadFluxKgCo2(m.LastCarbonRespirationTPerHa - m.LastCarbonInputTPerHa);
            RecordWeatherWindow(m.CurrentWeather.TMeanCelsius, m.CurrentWeather.PrecipMm);

            _detector.Detect(m.CurrentDay, MeasuredHumidityFraction, _eddyTower.EstimatedCarbonStockTPerHa,
                MeasuredFauna, m.MineralNitrogenKgPerHa, m.LastAnnualMarginEurosPerHa, _eventLog);

            UpdateRecommendations();
        }

        public void Run(int days)
        {
            for (int d = 0; d < days; d++) Tick();
        }

        // Fenêtre glissante O(1) : enregistre la T° + pluie (vérité) du jour et tient
        // la moyenne de T° et le cumul de pluie sur 365 jours (onglet Climat).
        private void RecordWeatherWindow(double temperatureC, double precipMm)
        {
            if (_weatherWindowCount >= WeatherWindowDays)
            {
                _tempSum -= _tempWindow[_weatherWindowIndex];
                _precipSum -= _precipWindow[_weatherWindowIndex];
            }
            else _weatherWindowCount++;
            _tempWindow[_weatherWindowIndex] = temperatureC;
            _precipWindow[_weatherWindowIndex] = precipMm;
            _tempSum += temperatureC;
            _precipSum += precipMm;
            _weatherWindowIndex = (_weatherWindowIndex + 1) % WeatherWindowDays;
        }

        /// <summary>
        /// Applique une décision : ajuste un levier sur le scénario RÉEL (le
        /// fantôme reste gelé) et cumule l'éventuel investissement.
        /// </summary>
        public void ApplyDecision(DecisionLever lever, double level, double investmentEurosPerHa = 0.0)
        {
            DecisionLevers.Set(_liveScenario, lever, level);
            if (investmentEurosPerHa > 0.0) _totalInvestmentEurosPerHa += investmentEurosPerHa;
        }

        /// <summary>
        /// Applique le forçage climatique exogène (ΔT additif, ×pluie) aux <b>deux</b>
        /// runs — réel et fantôme. Le climat n'est pas une décision : l'appliquer aux
        /// deux garantit que la divergence (l'apport de la techno) ne capte QUE les
        /// décisions de l'agriculteur, jamais le climat. À distinguer d'
        /// <see cref="ApplyDecision"/> (réel seul). Le mois de départ, lui, reste
        /// snapshoté à la construction des moteurs.
        /// </summary>
        public void SetClimate(double temperatureAnomalyC, double precipitationFactor)
        {
            _liveScenario.TemperatureAnomalyC = temperatureAnomalyC;
            _liveScenario.PrecipitationFactor = precipitationFactor;
            _shadow.Scenario.TemperatureAnomalyC = temperatureAnomalyC;
            _shadow.Scenario.PrecipitationFactor = precipitationFactor;
        }

        /// <summary>Produit la meilleure recommandation pour un type d'événement (à la demande de l'UI), ou null.</summary>
        public Recommendation Recommend(EventKind kind)
            => _recommendationEngine.TryProduce(kind, _real.Model, _liveScenario, _masterSeed);

        // ===== Cycle de vie des recommandations =====

        /// <summary>Recommandations en attente (affichées dans le panneau de décision).</summary>
        public IReadOnlyList<Recommendation> PendingRecommendations => _pending;

        /// <summary>Vrai si la reco a été différée (« Plus tard ») → ne plus l'auto-popup.</summary>
        public bool IsDeferred(Recommendation r) => r != null && _deferred.Contains(r);

        /// <summary>Valider : applique le levier au niveau recommandé (reco ⊆ leviers), puis résout la reco.</summary>
        public void AcceptRecommendation(Recommendation r)
        {
            if (r == null) return;
            ApplyDecision(r.Lever, r.RecommendedLevel);
            ResolveRecommendation(r);
        }

        /// <summary>Ignorer : résout la reco sans rien appliquer (+ cooldown anti-spam).</summary>
        public void DismissRecommendation(Recommendation r) => ResolveRecommendation(r);

        /// <summary>Plus tard : la reco reste dans la liste mais n'auto-popup plus.</summary>
        public void DeferRecommendation(Recommendation r)
        {
            if (r != null && _pending.Contains(r)) _deferred.Add(r);
        }

        /// <summary>Prochaine reco à auto-ouvrir (win-win ou urgence écologique), ou null.</summary>
        public Recommendation NextAutoPopupRecommendation()
        {
            double biodiversity = _real.Model.Biodiversity;
            for (int i = 0; i < _pending.Count; i++)
            {
                Recommendation r = _pending[i];
                if (_deferred.Contains(r)) continue;
                if (RecommendationSurfacing.ShouldAutoPopup(r, biodiversity)) return r;
            }
            return null;
        }

        private void ResolveRecommendation(Recommendation r)
        {
            if (r == null) return;
            _pending.Remove(r);
            _deferred.Remove(r);
            _recoCooldownUntilDay[r.TriggeredBy] = _real.Model.CurrentDay + RecoCooldownDays;
        }

        /// <summary>
        /// Met à jour la liste des recos (chaque tick) : retire les recos <b>satisfaites</b>
        /// (levier déjà au niveau recommandé ; Valider/Ignorer les retirent aussi), puis
        /// produit une reco par événement récent sans reco active — la projection ne
        /// tourne qu'<b>une seule fois</b> par déclenchement d'événement (coûteuse). Une
        /// reco non traitée <b>persiste</b> (boîte de réception) : elle n'expire pas sur
        /// l'ancienneté de l'événement (sinon une reco passive ne vivait que ~45 j,
        /// injouable à vitesse rapide).
        /// </summary>
        private void UpdateRecommendations()
        {
            int day = _real.Model.CurrentDay;

            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                Recommendation r = _pending[i];
                if (IsSatisfied(r))
                {
                    _pending.RemoveAt(i);
                    _deferred.Remove(r);
                }
            }

            foreach (EventKind kind in AllEventKinds)
            {
                if (HasPendingForKind(kind)) continue;
                if (day < CooldownUntil(kind)) continue;
                DetectedEvent? latest = _eventLog.LatestOfKind(kind);
                if (latest == null) continue;
                int eventDay = latest.Value.Day;
                if (day - eventDay > RecoActiveDays) continue;                                         // périmé
                if (_lastAttemptEventDay.TryGetValue(kind, out int lastTry) && eventDay <= lastTry) continue; // déjà tenté pour cet événement
                _lastAttemptEventDay[kind] = eventDay;
                Recommendation produced = _recommendationEngine.TryProduce(kind, _real.Model, _liveScenario, _masterSeed);
                if (produced != null) _pending.Add(produced);
            }
        }

        private bool IsSatisfied(Recommendation r)
        {
            double current = DecisionLevers.Get(_liveScenario, r.Lever);
            (double min, double max) = DecisionLevers.Range(r.Lever);
            double tol = LeverSatisfiedToleranceFraction * (max - min);
            return Math.Abs(current - r.RecommendedLevel) <= tol;
        }

        private bool HasPendingForKind(EventKind kind)
        {
            for (int i = 0; i < _pending.Count; i++)
                if (_pending[i].TriggeredBy == kind) return true;
            return false;
        }

        private int CooldownUntil(EventKind kind)
            => _recoCooldownUntilDay.TryGetValue(kind, out int d) ? d : 0;
    }
}
