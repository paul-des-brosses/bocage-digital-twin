using System.Collections.Generic;
using Bocage.Decision;
using Bocage.Presentation;
using Bocage.Presentation.Scenario;
using Bocage.SimulationCore.Logging;
using Bocage.SimulationCore;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Crée un bouton par <see cref="ScenarioPresetDefinition"/>, applique le preset
    /// complet en un clic — climat via <see cref="SimulationRunner.SetClimate"/>
    /// (les deux runs) + les 6 leviers via <see cref="SimulationRunner.ApplyDecision"/>
    /// (run réel) — puis re-synchronise les sliders. Surligne le preset dont les
    /// valeurs correspondent exactement au scénario courant : dès que l'utilisateur
    /// bouge un slider, le surlignage disparaît. Application instantanée (S2).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScenarioPresetsBinding : MonoBehaviour
    {
        public const string PrefsKey = "Bocage.Scenario.LastPresetId";
        private const double FloatMatchTolerance = 0.001;

        [SerializeField, Tooltip("Source du scénario. Glisse le GameObject portant le SimulationRunner.")]
        private SimulationRunner runner;
        [SerializeField, Tooltip("Le ScenarioControlsBinding voisin (pour re-synchroniser les sliders après un preset).")]
        private ScenarioControlsBinding controlsBinding;
        [SerializeField, Tooltip("Presets disponibles, dans l'ordre d'affichage (tableau des 4 assets ScenarioPreset_*).")]
        private ScenarioPresetDefinition[] presets = new ScenarioPresetDefinition[0];
        [SerializeField, Tooltip("Nom du VisualElement UXML qui héberge les boutons de preset.")]
        private string presetsContainerName = "scenario-presets-row";

        private UIDocument _document;
        private VisualElement _container;
        private readonly List<Button> _buttons = new List<Button>();
        private int _lastActiveIndex = -1;

        private void Awake() => _document = GetComponent<UIDocument>();

        private void OnEnable()
        {
            ResolveContainer();
            BuildButtons();
            RecomputeActivePreset();
        }

        // Re-évaluation par poll : capte les drags de slider même en pause
        // (aucun TickCompleted ne fire alors). Quelques comparaisons float/frame.
        private void Update() => RecomputeActivePreset();

        private void ResolveContainer()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _container = _document.rootVisualElement.Q<VisualElement>(presetsContainerName);
            if (_container == null)
                SimLogger.DebugLog($"[ScenarioPresetsBinding] conteneur '{presetsContainerName}' introuvable dans l'UXML");
        }

        private void BuildButtons()
        {
            _buttons.Clear();
            if (_container == null) return;
            _container.Clear();
            if (presets == null) return;
            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                if (preset == null) continue;
                var button = new Button(() => ApplyPreset(preset))
                {
                    text = preset.DisplayName,
                    tooltip = string.IsNullOrEmpty(preset.Description) ? preset.DisplayName : preset.Description,
                    name = "scenario-preset-button-" + preset.Id
                };
                button.AddToClassList("scenario-preset-button");
                _container.Add(button);
                _buttons.Add(button);
            }
        }

        /// <summary>Applique le preset complet : climat (deux runs) + les 6 leviers (run réel), puis snap des sliders.</summary>
        public void ApplyPreset(ScenarioPresetDefinition preset)
        {
            if (preset == null || runner == null || runner.Scenario == null)
            {
                SimLogger.DebugLog("[ScenarioPresetsBinding] runner/scénario indisponible ; preset ignoré");
                return;
            }
            runner.SetClimate(preset.TemperatureAnomalyC, 1.0 + preset.PrecipitationAnomalyPercent / 100.0);
            runner.ApplyDecision(DecisionLever.NitrogenDose, preset.NitrogenDoseKgPerHa);
            runner.ApplyDecision(DecisionLever.Pesticide, preset.PesticideIntensity);
            runner.ApplyDecision(DecisionLever.Tillage, preset.TillageIntensity);
            runner.ApplyDecision(DecisionLever.CoverCrops, preset.CoverCropsPercent);
            runner.ApplyDecision(DecisionLever.HedgeManagement, preset.HedgeManagementMetersPerHaPerYear);
            runner.ApplyDecision(DecisionLever.Grassland, preset.GrasslandFraction);

            if (controlsBinding != null) controlsBinding.SyncAllFromScenario();

            PlayerPrefs.SetString(PrefsKey, preset.Id);
            PlayerPrefs.Save();
            SimLogger.UserActionLog($"scenario-preset-applied: id={preset.Id} name=\"{preset.DisplayName}\"");
        }

        private void RecomputeActivePreset()
        {
            int matchIndex = -1;
            var s = runner != null ? runner.Scenario : null;
            if (s != null && presets != null)
            {
                for (int i = 0; i < presets.Length; i++)
                {
                    if (presets[i] != null && MatchesScenario(presets[i], s)) { matchIndex = i; break; }
                }
            }
            if (matchIndex == _lastActiveIndex) return;
            _lastActiveIndex = matchIndex;
            int n = System.Math.Min(_buttons.Count, presets != null ? presets.Length : 0);
            for (int i = 0; i < n; i++)
            {
                var btn = _buttons[i];
                if (btn != null) btn.EnableInClassList("scenario-preset-button--active", i == matchIndex);
            }
        }

        private static bool MatchesScenario(ScenarioPresetDefinition p, ScenarioContext s)
        {
            return ApproxEqual(s.TemperatureAnomalyC, p.TemperatureAnomalyC)
                && ApproxEqual(s.PrecipitationFactor, 1.0 + p.PrecipitationAnomalyPercent / 100.0)
                && ApproxEqual(s.NitrogenDoseKgPerHaPerYear, p.NitrogenDoseKgPerHa)
                && ApproxEqual(s.PesticideIntensity, p.PesticideIntensity)
                && ApproxEqual(s.TillageIntensity, p.TillageIntensity)
                && ApproxEqual(s.CoverCropsCoveragePercent, p.CoverCropsPercent)
                && ApproxEqual(s.HedgeManagementMetersPerHaPerYear, p.HedgeManagementMetersPerHaPerYear)
                && ApproxEqual(s.GrasslandFraction, p.GrasslandFraction);
        }

        private static bool ApproxEqual(double a, double b)
        {
            double diff = a - b;
            if (diff < 0.0) diff = -diff;
            return diff < FloatMatchTolerance;
        }
    }
}
