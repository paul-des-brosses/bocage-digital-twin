using UnityEngine;

namespace Bocage.Presentation.Scenario
{
    /// <summary>
    /// Preset de scénario « stratégie complète » (S2) : un asset = un climat + les
    /// 6 leviers agriculteur, appliqués en un clic via
    /// <see cref="Bocage.Presentation.Bindings.ScenarioPresetsBinding"/>.
    /// Contrairement à l'ancien design (cadre exogène seul), un preset pose
    /// désormais un <b>point de départ jouable complet</b> (Référence / Bas-intrants
    /// / Intensif / Sécheresse RCP4.5) que l'utilisateur ajuste ensuite librement
    /// aux sliders. Application instantanée (transitions §15 non retenues au MVP).
    /// </summary>
    [CreateAssetMenu(menuName = "Bocage/Scenario/Preset", fileName = "ScenarioPreset_New")]
    public sealed class ScenarioPresetDefinition : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField, Tooltip("Identifiant stable (minuscules, sans espace).")]
        private string id = "preset";
        [SerializeField, Tooltip("Libellé affiché sur le bouton.")]
        private string displayName = "Préréglage";
        [SerializeField, TextArea(2, 4), Tooltip("Description (tooltip du bouton).")]
        private string description = "";

        [Header("Climat (subi)")]
        [SerializeField, Range(-2f, 5f)] private float temperatureAnomalyC = 0f;
        [SerializeField, Range(-60f, 20f)] private float precipitationAnomalyPercent = 0f;

        [Header("Leviers agriculteur")]
        [SerializeField, Range(0f, 250f)] private float nitrogenDoseKgPerHa = 120f;
        [SerializeField, Range(0f, 2f)] private float pesticideIntensity = 1f;
        [SerializeField, Range(0f, 1f)] private float tillageIntensity = 1f;
        [SerializeField, Range(0f, 100f)] private float coverCropsPercent = 0f;
        [SerializeField, Range(-10f, 10f)] private float hedgeManagementMetersPerHaPerYear = 0f;
        [SerializeField, Range(0f, 1f)] private float grasslandFraction = 0f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public float TemperatureAnomalyC => temperatureAnomalyC;
        public float PrecipitationAnomalyPercent => precipitationAnomalyPercent;
        public float NitrogenDoseKgPerHa => nitrogenDoseKgPerHa;
        public float PesticideIntensity => pesticideIntensity;
        public float TillageIntensity => tillageIntensity;
        public float CoverCropsPercent => coverCropsPercent;
        public float HedgeManagementMetersPerHaPerYear => hedgeManagementMetersPerHaPerYear;
        public float GrasslandFraction => grasslandFraction;
    }
}
