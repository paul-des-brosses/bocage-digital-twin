using System.Collections;
using Bocage.Data.RuntimeContainers;
using Bocage.Decision.Refonte;
using Bocage.Sensors.Refonte;
using Bocage.Indicators.Refonte;
using Bocage.SimulationCore.Refonte;
using UnityEngine;

namespace Bocage.Presentation.Refonte
{
    /// <summary>
    /// Pont Unity de la refonte : possède une <see cref="SimulationSession"/>
    /// (run réel + fantôme + capteurs + événements + recos), la fait tourner sur
    /// une coroutine cadencée, et publie l'état dans les <c>RC_*</c> observables
    /// existants — donc l'UI d'affichage actuelle (Hero KPI, panneaux, shaders)
    /// fonctionne sans réécriture. Écrivain unique des conteneurs ; les bindings
    /// ne font que lire. Remplace l'ancien SimulationRunner au cutover.
    /// <para>
    /// Couche 05 (Unity) — non couverte par le harnais headless ; validation en
    /// Play Mode. La logique de simulation, elle, est entièrement testée (01-04).
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-8000)]
    public sealed class RefonteSimulationRunner : MonoBehaviour
    {
        [Header("Déterminisme")]
        [SerializeField, Tooltip("Seed maître. Même seed + même scénario => même trajectoire (run réel et fantôme partagés).")]
        private ulong masterSeed = 1UL;

        [Header("Cadence")]
        [SerializeField, Range(0.5f, 20f), Tooltip("Jours simulés par seconde réelle. 1 = x1, 10 = x10.")]
        private float ticksPerSecond = 1f;
        [SerializeField, Tooltip("Si vrai, le tick démarre à Start(). Sinon un contrôleur externe appelle StartTicking().")]
        private bool autoStart = true;

        [Header("Scénario initial (climat + leviers)")]
        [SerializeField, Tooltip("Anomalie de température (°C) appliquée au générateur.")]
        private float temperatureAnomalyC = 0f;
        [SerializeField, Range(0.2f, 1.5f), Tooltip("Facteur sur la pluie. 1 = baseline, 0.5 = −50 %.")]
        private float precipitationFactor = 1f;
        [SerializeField] private float initialNitrogenDoseKgPerHa = 120f;
        [SerializeField, Range(0f, 2f)] private float initialPesticideIntensity = 1f;
        [SerializeField, Range(0f, 1f)] private float initialTillageIntensity = 1f;
        [SerializeField, Range(0f, 100f)] private float initialCoverCropsPercent = 0f;

        [Header("Conteneurs observables (Hero KPI)")]
        [SerializeField] private RC_IntegratedProfitability profitabilityContainer;
        [SerializeField] private RC_BiodiversityComposite biodiversityContainer;
        [SerializeField] private RC_SoilCarbonStock soilCarbonContainer;
        [SerializeField] private RC_HedgerowDensity hedgerowDensityContainer;
        [SerializeField] private RC_WaterTableDepth waterTableContainer;
        [SerializeField] private RC_SoilMoisture soilMoistureContainer;
        [SerializeField] private RC_TechDelta techDeltaContainer;
        [SerializeField] private RC_Nitrogen nitrogenContainer;

        [Header("Biodiversité 3 facteurs")]
        [SerializeField] private RC_FaunaFactorHabitat faunaFactorHabitatContainer;
        [SerializeField] private RC_FaunaFactorWater faunaFactorWaterContainer;
        [SerializeField] private RC_FaunaFactorInputs faunaFactorInputsContainer;

        private SimulationSession _session;
        private Coroutine _tickRoutine;

        public static bool IsTicking { get; private set; }

        public SimulationSession Session => _session;
        public int CurrentDay => _session?.CurrentDay ?? 0;
        public bool IsRunning => _tickRoutine != null;

        public float TicksPerSecond
        {
            get => ticksPerSecond;
            set => ticksPerSecond = Mathf.Clamp(value, 0.01f, 200f);
        }

        public event System.Action TickCompleted;
        public event System.Action TickingStateChanged;

        private void Awake()
        {
            IsTicking = false;
            BuildSession();
            PublishIndicators();
        }

        private void Start()
        {
            if (autoStart) StartTicking();
        }

        private void OnDisable() => StopTicking();

        private void BuildSession()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext
            {
                TemperatureAnomalyC = temperatureAnomalyC,
                PrecipitationFactor = precipitationFactor,
                NitrogenDoseKgPerHaPerYear = initialNitrogenDoseKgPerHa,
                PesticideIntensity = initialPesticideIntensity,
                TillageIntensity = initialTillageIntensity,
                CoverCropsCoveragePercent = initialCoverCropsPercent
            };
            _session = new SimulationSession(model, scenario, TourouvreClimatology(), masterSeed);
        }

        public void StartTicking()
        {
            if (_tickRoutine != null) return;
            _tickRoutine = StartCoroutine(TickLoop());
            IsTicking = true;
            TickingStateChanged?.Invoke();
        }

        public void StopTicking()
        {
            if (_tickRoutine == null) return;
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
            IsTicking = false;
            TickingStateChanged?.Invoke();
        }

        private IEnumerator TickLoop()
        {
            while (true)
            {
                float interval = ticksPerSecond <= 0f ? 1f : 1f / ticksPerSecond;
                yield return new WaitForSecondsRealtime(interval);
                _session.Tick();
                PublishIndicators();
                TickCompleted?.Invoke();
            }
        }

        /// <summary>Avance la simulation de N jours d'un coup (skip-to-end), puis publie.</summary>
        public void FastForward(int days)
        {
            if (_session == null || days <= 0) return;
            bool wasRunning = IsRunning;
            StopTicking();
            _session.Run(days);
            PublishIndicators();
            if (wasRunning) StartTicking();
        }

        /// <summary>Applique une décision (lève un levier sur le run réel ; le fantôme reste gelé).</summary>
        public void ApplyDecision(DecisionLever lever, double level, double investmentEurosPerHa = 0.0)
            => _session?.ApplyDecision(lever, level, investmentEurosPerHa);

        /// <summary>Recommande pour un type d'événement (à la demande de l'UI), ou null.</summary>
        public Recommendation Recommend(EventKind kind) => _session?.Recommend(kind);

        private void PublishIndicators()
        {
            if (_session == null) return;
            EcosystemModel m = _session.RealModel;
            ScenarioContext s = _session.Scenario;

            if (profitabilityContainer != null)
            {
                double v = HeroIndicators.MarginEurosPerHa(m, s);
                profitabilityContainer.Set((float)v, (float)HeroIndicators.MarginNormalized(v));
            }
            if (biodiversityContainer != null)
            {
                double v = HeroIndicators.Biodiversity(m);
                biodiversityContainer.Set((float)v, (float)v);
            }
            if (soilCarbonContainer != null)
            {
                double v = HeroIndicators.CarbonTPerHa(m);
                soilCarbonContainer.Set((float)v, (float)HeroIndicators.CarbonNormalized(v));
            }
            if (hedgerowDensityContainer != null)
            {
                double v = m.HedgerowDensityMPerHa;
                hedgerowDensityContainer.Set((float)v, Mathf.Clamp01(((float)v - 40f) / 110f));
            }
            if (waterTableContainer != null)
            {
                double v = m.WaterTableDepthM;
                waterTableContainer.Set((float)v, Mathf.Clamp01(1f - ((float)v - 0.5f) / 5.5f));
            }
            if (soilMoistureContainer != null)
            {
                float norm = (float)HeroIndicators.WaterReserveNormalized(m);
                soilMoistureContainer.Set(norm, norm);
            }
            if (techDeltaContainer != null)
            {
                double v = _session.TechValueNetEurosPerHa;
                techDeltaContainer.Set((float)v, Mathf.Clamp01(((float)v + 500f) / 2000f));
            }
            if (nitrogenContainer != null)
            {
                double v = m.MineralNitrogenKgPerHa;
                nitrogenContainer.Set((float)v, Mathf.Clamp01((float)v / 200f));
            }

            if (faunaFactorHabitatContainer != null)
            {
                float v = (float)BiodiversityRule.HabitatFactor(m.HedgerowDensityMPerHa);
                faunaFactorHabitatContainer.Set(v, v);
            }
            if (faunaFactorWaterContainer != null)
            {
                float v = (float)BiodiversityRule.WaterFactor(m.SoilWaterMm);
                faunaFactorWaterContainer.Set(v, v);
            }
            if (faunaFactorInputsContainer != null)
            {
                float v = (float)BiodiversityRule.InputsFactor(m.MineralNitrogenKgPerHa, s.PesticideIntensity);
                faunaFactorInputsContainer.Set(v, v);
            }
        }

        /// <summary>Climatologie réelle de Tourouvre-au-Perche (sortie de tools/extract_weather_normals.py).</summary>
        private static Climatology TourouvreClimatology()
        {
            double[] tmean = { 3.97, 4.91, 7.23, 9.76, 13.05, 16.69, 18.61, 18.25, 15.53, 11.81, 7.66, 4.87 };
            double[] tstd = { 3.74, 3.72, 3.04, 3.19, 3.11, 3.14, 2.96, 3.01, 3.09, 3.19, 3.12, 3.69 };
            double[] diurn = { 5.25, 6.8, 8.52, 10.79, 10.82, 11.26, 12.38, 11.82, 10.92, 8.18, 6.13, 5.42 };
            double[] precip = { 72.6, 54.8, 58.9, 49.6, 64.7, 62.9, 51.6, 51.1, 54.1, 73.0, 74.4, 83.9 };
            double[] pwet = { 0.417, 0.385, 0.368, 0.286, 0.313, 0.316, 0.25, 0.269, 0.29, 0.372, 0.4, 0.441 };
            double[] p11 = { 0.616, 0.611, 0.648, 0.562, 0.494, 0.509, 0.366, 0.407, 0.531, 0.585, 0.581, 0.614 };
            double[] p01 = { 0.28, 0.243, 0.202, 0.178, 0.227, 0.228, 0.211, 0.22, 0.193, 0.245, 0.279, 0.301 };
            double[] mu = { 1.344, 1.273, 1.306, 1.366, 1.436, 1.439, 1.364, 1.345, 1.36, 1.403, 1.402, 1.43 };
            double[] sig = { 0.834, 0.793, 0.775, 0.829, 0.902, 0.902, 0.982, 0.881, 0.884, 0.858, 0.864, 0.838 };
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(tmean[i], tstd[i], diurn[i], precip[i], pwet[i], p11[i], p01[i], mu[i], sig[i]);
            return new Climatology(months, 0.75, 2.157);
        }
    }
}
