using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Third Hero KPI: composite biodiversity score in <c>[0, 1]</c>,
    /// aggregating the three explicit habitat signals exposed by
    /// <see cref="FaunaDynamicsRule"/> since chantier E5 / ADR #51:
    /// <list type="bullet">
    ///   <item><b>Habitat</b> (40 %) — derived from
    ///         <see cref="EcosystemModel.HedgerowDensity"/>. Strongest
    ///         single driver in temperate bocage (Réseau Haies /
    ///         Solagro, Constant et al. 1976).</item>
    ///   <item><b>Intrants</b> (35 %) — derived from
    ///         <see cref="ScenarioContext.InputIntensityFactor"/>.
    ///         Weight moved up from the pre-E5 indicator to match
    ///         Hallmann 2017 / MNHN 2024 evidence on pesticide-driven
    ///         insect collapse.</item>
    ///   <item><b>Eau</b> (25 %) — derived from
    ///         <see cref="EcosystemModel.WaterTableDepth"/>. Slightly
    ///         downweighted versus the pre-E5 indicator because the
    ///         direct hedge/inputs effects dominate the published
    ///         literature.</item>
    /// </list>
    /// Per ARCHITECTURE.md §2.4, this layer never mutates the model: it
    /// only reads. Stateless, allocation-free, deterministic.
    /// <para>
    /// <b>Honesty note</b>. The pre-E5 indicator put 50 % weight on
    /// <see cref="EcosystemModel.FaunaPopulation"/>, which is a slow
    /// EMA-converging composite of the same three factors plus the
    /// canicule and soil-carbon modulators. Using fauna as a slot in
    /// the composite double-counted habitat and water; removing it
    /// keeps the Hero KPI reactive to scenario changes while
    /// <see cref="EcosystemModel.FaunaPopulation"/> continues to carry
    /// the slow trajectory for the visible faune of chantier E4. The
    /// canicule penalty and soil-carbon bonus modulate
    /// <see cref="EcosystemModel.FaunaPopulation"/> directly via the
    /// rule rather than entering the composite — keeps the composite
    /// a clean function of three RC observables that are themselves
    /// displayed in the onglet Biodiv (chantier E6).
    /// </para>
    /// </summary>
    public static class BiodiversityCompositeIndicator
    {
        public const double HabitatWeight = 0.40;
        public const double WaterWeight = 0.25;
        public const double InputsWeight = 0.35;

        // Normalisation bounds aligned with the natural range of each
        // FaunaDynamicsRule factor. Habitat: [0.5, 1.4] over the full
        // densité range 0 to 180 m/ha. Water: [0.5, 1.0] over depths
        // up to the 15 m+ saturation. Inputs: [0.4, 1.1] over intensity
        // 0.5 (bio) to 2.0+ (intensive).
        public const double HabitatFactorMin = 0.5;
        public const double HabitatFactorMax = 1.4;
        public const double WaterFactorMin = 0.5;
        public const double WaterFactorMax = 1.0;
        public const double InputsFactorMin = 0.4;
        public const double InputsFactorMax = 1.1;

        /// <summary>
        /// Returns the raw composite score in <c>[0, 1]</c> ready for
        /// the Hero KPI label and gauges. Same value as
        /// <see cref="Normalize"/> since the weighted sum is already
        /// unit-range by construction (weights sum to 1, each
        /// normalised factor sits in [0, 1]).
        /// </summary>
        public static double Compute(EcosystemModel model, ScenarioContext scenario)
        {
            double habitat = NormalizeHabitat(FaunaDynamicsRule.ComputeHabitatFactor(model.HedgerowDensity));
            double water = NormalizeWater(FaunaDynamicsRule.ComputeWaterFactor(model.WaterTableDepth));
            double intensity = scenario != null ? scenario.InputIntensityFactor.Current : 1.0;
            double inputs = NormalizeInputs(FaunaDynamicsRule.ComputeInputsFactor(intensity));
            return HabitatWeight * habitat + WaterWeight * water + InputsWeight * inputs;
        }

        /// <summary>
        /// Identity (the composite is already in <c>[0, 1]</c>) clamped
        /// defensively in case a future weight change pushes the sum
        /// outside the unit range.
        /// </summary>
        public static double Normalize(double composite)
        {
            if (composite < 0.0) return 0.0;
            if (composite > 1.0) return 1.0;
            return composite;
        }

        /// <summary>
        /// Maps the habitat factor to <c>[0, 1]</c> using
        /// <see cref="HabitatFactorMin"/> / <see cref="HabitatFactorMax"/>.
        /// At the Perche reference (90 m/ha → factor 1.0) the
        /// normalised value is ≈ 0.556.
        /// </summary>
        public static double NormalizeHabitat(double habitatFactor)
        {
            return ClampUnit((habitatFactor - HabitatFactorMin) / (HabitatFactorMax - HabitatFactorMin));
        }

        /// <summary>
        /// Maps the water factor to <c>[0, 1]</c> using
        /// <see cref="WaterFactorMin"/> / <see cref="WaterFactorMax"/>.
        /// Full habitat (depth ≤ 3 m → factor 1.0) maps to 1.0;
        /// extreme drought (depth ≥ 15 m → factor 0.5) maps to 0.0.
        /// </summary>
        public static double NormalizeWater(double waterFactor)
        {
            return ClampUnit((waterFactor - WaterFactorMin) / (WaterFactorMax - WaterFactorMin));
        }

        /// <summary>
        /// Maps the inputs factor to <c>[0, 1]</c> using
        /// <see cref="InputsFactorMin"/> / <see cref="InputsFactorMax"/>.
        /// Bio extensive (intensity 0.5 → factor 1.1) maps to 1.0;
        /// extreme intensification (intensity ≥ 2.4 → factor 0.4)
        /// maps to 0.0. Conventional (intensity 1.0 → factor 1.0)
        /// maps to ≈ 0.857.
        /// </summary>
        public static double NormalizeInputs(double inputsFactor)
        {
            return ClampUnit((inputsFactor - InputsFactorMin) / (InputsFactorMax - InputsFactorMin));
        }

        private static double ClampUnit(double t)
        {
            if (t < 0.0) return 0.0;
            if (t > 1.0) return 1.0;
            return t;
        }
    }
}
