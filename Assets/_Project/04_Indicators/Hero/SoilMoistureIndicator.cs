using Bocage.SimulationCore.Model;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Derived indicator: a [0,1] proxy for top-soil moisture, intended to
    /// drive the meadow shader (sub-étape 9α). Today the proxy is a strict
    /// function of <see cref="EcosystemModel.WaterTableDepth"/> — shallow
    /// table = moist top soil, deep table = dry top soil. Semantically
    /// equivalent to <see cref="WaterTableIndicator.Normalize"/> at the
    /// current model resolution, but exposed as its own indicator so that
    /// a future revision of the model (precipitation smoothing, runoff,
    /// evapotranspiration) can refine the formula here without touching
    /// the consumer side (meadow material, binding, RC).
    /// <para>
    /// Honest design (CLAUDE.md §9 sensor primacy): no calendar input, no
    /// scenic ambient cue. The meadow visual variation is a strict
    /// function of a model variable that itself maps to a deployed sensor
    /// (the piezometer).
    /// </para>
    /// <para>
    /// Stateless, allocation-free, deterministic. No Unity reference.
    /// </para>
    /// </summary>
    public static class SoilMoistureIndicator
    {
        /// <summary>
        /// Computes the soil moisture proxy in [0,1]. 1.0 = very moist
        /// (shallow water table), 0.0 = very dry (deep water table at the
        /// drought threshold or worse).
        /// </summary>
        public static double Compute(EcosystemModel model)
        {
            // Reuse the water-table normalization so the two indicators
            // never diverge by accident at the calibration bounds. Future
            // refinement: blend in a smoothed precipitation channel here.
            return WaterTableIndicator.Normalize(model.WaterTableDepth);
        }

        /// <summary>
        /// Identity normalize: <see cref="Compute"/> already returns a
        /// [0,1] value. Kept for symmetry with sibling indicators whose
        /// raw value carries a unit (m, t/ha, €/ha/yr) and needs a
        /// separate normalisation step.
        /// </summary>
        public static double Normalize(double moisture01)
        {
            if (moisture01 < 0.0) return 0.0;
            if (moisture01 > 1.0) return 1.0;
            return moisture01;
        }
    }
}
