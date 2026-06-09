using System;
using Bocage.SimulationCore.Refonte;
using Bocage.Sensors.Refonte;
using SeededRandom = Bocage.SimulationCore.SeededRandom;

namespace Bocage.Decision.Refonte
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
            _eddyTower.ReadFluxKgCo2(m.LastCarbonRespirationTPerHa - m.LastCarbonInputTPerHa);

            _detector.Detect(m.CurrentDay, MeasuredHumidityFraction, _eddyTower.EstimatedCarbonStockTPerHa,
                MeasuredFauna, m.MineralNitrogenKgPerHa, m.LastAnnualMarginEurosPerHa, _eventLog);
        }

        public void Run(int days)
        {
            for (int d = 0; d < days; d++) Tick();
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

        /// <summary>Produit la meilleure recommandation pour un type d'événement (à la demande de l'UI), ou null.</summary>
        public Recommendation Recommend(EventKind kind)
            => _recommendationEngine.TryProduce(kind, _real.Model, _liveScenario, _masterSeed);
    }
}
