using System.Globalization;
using Bocage.Decision.Refonte;
using Bocage.Presentation.Refonte;
using Bocage.SimulationCore.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Câble les 6 sliders de leviers agriculteur + 2 sliders climat au
    /// <see cref="RefonteSimulationRunner"/>. Les leviers passent par
    /// <see cref="RefonteSimulationRunner.ApplyDecision"/> — exactement le chemin
    /// des recommandations (« reco ⊆ leviers ») ; le climat (exogène) par
    /// <see cref="RefonteSimulationRunner.SetClimate"/>, appliqué aux deux runs.
    /// Application instantanée : les variables d'état lentes (azote, carbone,
    /// biodiversité, densité) lissent l'effet ; les transitions douces §15 ne sont
    /// pas retenues au MVP. Couche 05 (Unity) — validée en Play Mode.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScenarioControlsBinding : MonoBehaviour
    {
        [SerializeField, Tooltip("Source du scénario. Glisse le GameObject portant le RefonteSimulationRunner.")]
        private RefonteSimulationRunner runner;

        [Header("UXML — sliders leviers")]
        [SerializeField] private string nitrogenSliderName = "nitrogen-dose-slider";
        [SerializeField] private string pesticideSliderName = "pesticide-slider";
        [SerializeField] private string tillageSliderName = "tillage-slider";
        [SerializeField] private string coverCropsSliderName = "cover-crops-slider";
        [SerializeField] private string hedgeSliderName = "hedge-management-slider";
        [SerializeField] private string grasslandSliderName = "grassland-slider";

        [Header("UXML — sliders climat")]
        [SerializeField] private string temperatureSliderName = "temperature-anomaly-slider";
        [SerializeField] private string precipitationSliderName = "precipitation-anomaly-slider";

        [Header("UXML — labels valeurs leviers")]
        [SerializeField] private string nitrogenValueName = "nitrogen-dose-value";
        [SerializeField] private string pesticideValueName = "pesticide-value";
        [SerializeField] private string tillageValueName = "tillage-value";
        [SerializeField] private string coverCropsValueName = "cover-crops-value";
        [SerializeField] private string hedgeValueName = "hedge-management-value";
        [SerializeField] private string grasslandValueName = "grassland-value";

        [Header("UXML — labels valeurs climat")]
        [SerializeField] private string temperatureValueName = "temperature-anomaly-value";
        [SerializeField] private string precipitationValueName = "precipitation-anomaly-value";

        private UIDocument _document;
        private Slider _nitrogen, _pesticide, _tillage, _cover, _hedge, _grassland, _temp, _precip;
        private Label _nitrogenL, _pesticideL, _tillageL, _coverL, _hedgeL, _grasslandL, _tempL, _precipL;
        private bool _wired;

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveElements();
            InitFromScenario();
            WireCallbacks();
            if (runner != null) runner.TickCompleted += OnTickSync;
        }

        private void OnDisable()
        {
            UnwireCallbacks();
            if (runner != null) runner.TickCompleted -= OnTickSync;
        }

        private void ResolveElements()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            var r = _document.rootVisualElement;
            _nitrogen = r.Q<Slider>(nitrogenSliderName);
            _pesticide = r.Q<Slider>(pesticideSliderName);
            _tillage = r.Q<Slider>(tillageSliderName);
            _cover = r.Q<Slider>(coverCropsSliderName);
            _hedge = r.Q<Slider>(hedgeSliderName);
            _grassland = r.Q<Slider>(grasslandSliderName);
            _temp = r.Q<Slider>(temperatureSliderName);
            _precip = r.Q<Slider>(precipitationSliderName);

            _nitrogenL = r.Q<Label>(nitrogenValueName);
            _pesticideL = r.Q<Label>(pesticideValueName);
            _tillageL = r.Q<Label>(tillageValueName);
            _coverL = r.Q<Label>(coverCropsValueName);
            _hedgeL = r.Q<Label>(hedgeValueName);
            _grasslandL = r.Q<Label>(grasslandValueName);
            _tempL = r.Q<Label>(temperatureValueName);
            _precipL = r.Q<Label>(precipitationValueName);

            if (_nitrogen == null || _pesticide == null || _tillage == null || _cover == null
                || _hedge == null || _grassland == null || _temp == null || _precip == null)
                SimLogger.DebugLog("[ScenarioControlsBinding] un ou plusieurs sliders introuvables — vérifier les noms UXML");
        }

        private void InitFromScenario()
        {
            var s = runner != null ? runner.Scenario : null;
            if (s == null) { SimLogger.DebugLog("[ScenarioControlsBinding] scénario indisponible, init ignorée"); return; }
            Set(_nitrogen, _nitrogenL, (float)s.NitrogenDoseKgPerHaPerYear, FormatN);
            Set(_pesticide, _pesticideL, (float)s.PesticideIntensity, FormatIft);
            Set(_tillage, _tillageL, (float)s.TillageIntensity, FormatTillage);
            Set(_cover, _coverL, (float)s.CoverCropsCoveragePercent, FormatPercent);
            Set(_hedge, _hedgeL, (float)s.HedgeManagementMetersPerHaPerYear, FormatHedge);
            Set(_grassland, _grasslandL, (float)(s.GrasslandFraction * 100.0), FormatPercent);
            Set(_temp, _tempL, (float)s.TemperatureAnomalyC, FormatTemp);
            Set(_precip, _precipL, (float)((s.PrecipitationFactor - 1.0) * 100.0), FormatPercentSigned);
        }

        private void WireCallbacks()
        {
            if (_wired) return;
            if (_nitrogen != null) _nitrogen.RegisterValueChangedCallback(OnNitrogen);
            if (_pesticide != null) _pesticide.RegisterValueChangedCallback(OnPesticide);
            if (_tillage != null) _tillage.RegisterValueChangedCallback(OnTillage);
            if (_cover != null) _cover.RegisterValueChangedCallback(OnCover);
            if (_hedge != null) _hedge.RegisterValueChangedCallback(OnHedge);
            if (_grassland != null) _grassland.RegisterValueChangedCallback(OnGrassland);
            if (_temp != null) _temp.RegisterValueChangedCallback(OnTemp);
            if (_precip != null) _precip.RegisterValueChangedCallback(OnPrecip);
            _wired = true;
        }

        private void UnwireCallbacks()
        {
            if (!_wired) return;
            if (_nitrogen != null) _nitrogen.UnregisterValueChangedCallback(OnNitrogen);
            if (_pesticide != null) _pesticide.UnregisterValueChangedCallback(OnPesticide);
            if (_tillage != null) _tillage.UnregisterValueChangedCallback(OnTillage);
            if (_cover != null) _cover.UnregisterValueChangedCallback(OnCover);
            if (_hedge != null) _hedge.UnregisterValueChangedCallback(OnHedge);
            if (_grassland != null) _grassland.UnregisterValueChangedCallback(OnGrassland);
            if (_temp != null) _temp.UnregisterValueChangedCallback(OnTemp);
            if (_precip != null) _precip.UnregisterValueChangedCallback(OnPrecip);
            _wired = false;
        }

        // ---- Leviers → ApplyDecision (run réel ; divergence vs fantôme = valeur de la décision) ----

        private void OnNitrogen(ChangeEvent<float> e)
        {
            if (_nitrogenL != null) _nitrogenL.text = FormatN(e.newValue);
            if (runner != null) runner.ApplyDecision(DecisionLever.NitrogenDose, e.newValue);
        }

        private void OnPesticide(ChangeEvent<float> e)
        {
            if (_pesticideL != null) _pesticideL.text = FormatIft(e.newValue);
            if (runner != null) runner.ApplyDecision(DecisionLever.Pesticide, e.newValue);
        }

        private void OnTillage(ChangeEvent<float> e)
        {
            if (_tillageL != null) _tillageL.text = FormatTillage(e.newValue);
            if (runner != null) runner.ApplyDecision(DecisionLever.Tillage, e.newValue);
        }

        private void OnCover(ChangeEvent<float> e)
        {
            if (_coverL != null) _coverL.text = FormatPercent(e.newValue);
            if (runner != null) runner.ApplyDecision(DecisionLever.CoverCrops, e.newValue);
        }

        private void OnHedge(ChangeEvent<float> e)
        {
            if (_hedgeL != null) _hedgeL.text = FormatHedge(e.newValue);
            if (runner != null) runner.ApplyDecision(DecisionLever.HedgeManagement, e.newValue);
        }

        private void OnGrassland(ChangeEvent<float> e)
        {
            if (_grasslandL != null) _grasslandL.text = FormatPercent(e.newValue);
            if (runner != null) runner.ApplyDecision(DecisionLever.Grassland, e.newValue / 100.0);
        }

        // ---- Climat → SetClimate (appliqué aux DEUX runs) ----

        private void OnTemp(ChangeEvent<float> e)
        {
            if (_tempL != null) _tempL.text = FormatTemp(e.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.SetClimate(e.newValue, runner.Scenario.PrecipitationFactor);
        }

        private void OnPrecip(ChangeEvent<float> e)
        {
            if (_precipL != null) _precipL.text = FormatPercentSigned(e.newValue);
            if (runner == null || runner.Scenario == null) return;
            runner.SetClimate(runner.Scenario.TemperatureAnomalyC, 1.0 + e.newValue / 100.0);
        }

        /// <summary>
        /// Re-synchronise tous les sliders depuis le scénario courant sans déclencher
        /// les callbacks. Appelé après l'application d'un preset, et à chaque tick
        /// pour refléter un changement venu d'ailleurs (ex. recommandation acceptée).
        /// Un slider que l'utilisateur vient de bouger reste où il l'a mis (le
        /// scénario y est déjà → snap sans effet).
        /// </summary>
        public void SyncAllFromScenario()
        {
            var s = runner != null ? runner.Scenario : null;
            if (s == null) return;
            Snap(_nitrogen, _nitrogenL, (float)s.NitrogenDoseKgPerHaPerYear, FormatN);
            Snap(_pesticide, _pesticideL, (float)s.PesticideIntensity, FormatIft);
            Snap(_tillage, _tillageL, (float)s.TillageIntensity, FormatTillage);
            Snap(_cover, _coverL, (float)s.CoverCropsCoveragePercent, FormatPercent);
            Snap(_hedge, _hedgeL, (float)s.HedgeManagementMetersPerHaPerYear, FormatHedge);
            Snap(_grassland, _grasslandL, (float)(s.GrasslandFraction * 100.0), FormatPercent);
            Snap(_temp, _tempL, (float)s.TemperatureAnomalyC, FormatTemp);
            Snap(_precip, _precipL, (float)((s.PrecipitationFactor - 1.0) * 100.0), FormatPercentSigned);
        }

        private void OnTickSync() => SyncAllFromScenario();

        // ---- Helpers ----

        private static void Set(Slider sl, Label lb, float v, System.Func<float, string> fmt)
        {
            if (sl != null) sl.SetValueWithoutNotify(v);
            if (lb != null) lb.text = fmt(v);
        }

        private static void Snap(Slider sl, Label lb, float v, System.Func<float, string> fmt)
        {
            if (sl == null) return;
            if (Mathf.Approximately(sl.value, v)) return;
            sl.SetValueWithoutNotify(v);
            if (lb != null) lb.text = fmt(v);
        }

        private static string FormatN(float v) => v.ToString("0", Inv) + " kgN/ha";
        private static string FormatIft(float v) => "IFT " + v.ToString("0.0", Inv);
        private static string FormatTillage(float v) => v >= 0.85f ? "labour" : (v <= 0.15f ? "semis direct" : v.ToString("0.0", Inv) + " (réduit)");
        private static string FormatPercent(float v) => v.ToString("0", Inv) + " %";
        private static string FormatPercentSigned(float v) => v.ToString("+0;-0;0", Inv) + " %";
        private static string FormatHedge(float v) => v.ToString("+0.0;-0.0;0.0", Inv) + " m/ha/an";
        private static string FormatTemp(float v) => v.ToString("+0.0;-0.0;0.0", Inv) + " °C";
    }
}
