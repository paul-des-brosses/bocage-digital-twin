using UnityEngine;

namespace Bocage.Presentation.Scenario
{
    /// <summary>
    /// Data-driven scenario preset. One asset = one named combination of the
    /// six physical scenario parameters + horizon, applied in one click via
    /// <see cref="Bocage.Presentation.Bindings.ScenarioPresetsBinding"/>.
    /// <para>
    /// Presets exist to give the user reproducible reference points that
    /// match the four scenarios validated by
    /// <c>CalibrationScenarioValidationTests</c> in CALIBRATION.md — they
    /// are not a substitute for the per-parameter sliders, just a shortcut
    /// to a documented state.
    /// </para>
    /// <para>
    /// All continuous values are pushed via
    /// <c>TransitioningParameter.SetTarget(value, transitionDurationDays)</c>
    /// (CLAUDE.md §15), so applying a preset does not snap the simulation
    /// to a discontinuity — the model interpolates over ~10 simulated days.
    /// The horizon is applied directly (it's a deadline, not a setpoint).
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Scenario/Preset",
        fileName = "ScenarioPreset_New")]
    public sealed class ScenarioPresetDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Stable identifier persisted in PlayerPrefs. Keep short, lowercase, no spaces (e.g. 'reference', 'rcp45', 'bocage-bio', 'intensif').")]
        private string id = "preset";

        [SerializeField, Tooltip("Short label shown on the preset button.")]
        private string displayName = "Préréglage";

        [SerializeField, TextArea(2, 4), Tooltip("One-sentence description shown as tooltip on the button. Should explain when this preset is meaningful.")]
        private string description = "";

        // Note 2026-05-21 (8c.3 post-livraison polish): the
        // HedgeRemovalRate and InputIntensityFactor fields were removed
        // from preset definitions. A preset is a "Cadre extérieur"
        // (climate × public policy framework), NOT a bundle of farmer
        // decisions. The farmer-controlled sliders (arrachage haie,
        // intensité d'intrants) live separately in the right-hand
        // "Espace agriculteur" and are NEVER touched by a preset click —
        // the user keeps full control of those.

        [Header("Conditions naturelles (climat)")]
        [SerializeField, Range(-2f, 5f), Tooltip("Annual mean temperature anomaly in °C relative to Perche reference.")]
        private float temperatureAnomalyC = 0f;

        [SerializeField, Range(-60f, 20f), Tooltip("Annual precipitation anomaly in % relative to Perche reference (~730 mm/yr).")]
        private float precipitationAnomalyPercent = 0f;

        [Header("Politiques publiques (cadre)")]
        [SerializeField, Range(0f, 100f), Tooltip("Share of the farm under MAEC contracts (% of SAU). EU/state framework.")]
        private float maecCoveragePercent = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("Per-metre PSE subsidy paid for maintained hedges (€/m/yr). State/local PNR framework.")]
        private float pseSubsidyRate = 0f;

        [Header("Horizon")]
        [SerializeField, Range(30, 1825), Tooltip("Run horizon in simulated days. Used by skip-to-end.")]
        private int horizonInDays = 365;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public float TemperatureAnomalyC => temperatureAnomalyC;
        public float PrecipitationAnomalyPercent => precipitationAnomalyPercent;
        public float MaecCoveragePercent => maecCoveragePercent;
        public float PseSubsidyRate => pseSubsidyRate;
        public int HorizonInDays => horizonInDays;
    }
}
