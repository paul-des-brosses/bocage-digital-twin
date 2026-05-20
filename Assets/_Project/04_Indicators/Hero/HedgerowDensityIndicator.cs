using Bocage.SimulationCore.Model;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// First Hero KPI: hedgerow density in metres of hedgerow per hectare,
    /// read directly from the <see cref="EcosystemModel"/>. Stateless,
    /// allocation-free, deterministic — produces the same output for the
    /// same input model.
    /// <para>
    /// Per the architecture contract (cf ARCHITECTURE.md §2.4), this layer
    /// never mutates the model: it only reads. The writing of the value
    /// into the observable <c>RC_HedgerowDensity</c> ScriptableObject is
    /// performed by a binding in Couche 5, which keeps Couche 4 free of
    /// any UnityEngine dependency.
    /// </para>
    /// <para>
    /// We also expose a normalized representation in <c>[0,1]</c> via
    /// <see cref="Normalize"/> so downstream visual bindings (shaders, UI
    /// gauges) can consume a unit-range value without having to know the
    /// Perche-specific range. Bounds correspond to a sparse remembrement
    /// landscape (40 m/ha) and a dense traditional bocage (150 m/ha) —
    /// values outside this range are clamped, which is a deliberate
    /// presentation choice rather than a model invariant.
    /// </para>
    /// </summary>
    public static class HedgerowDensityIndicator
    {
        public const double MinMetersPerHectare = 40.0;
        public const double MaxMetersPerHectare = 150.0;

        /// <summary>
        /// Returns the current hedgerow density in metres / hectare.
        /// </summary>
        public static double Compute(EcosystemModel model)
        {
            return model.HedgerowDensity;
        }

        /// <summary>
        /// Returns a normalized hedgerow density in <c>[0,1]</c>, with
        /// 0 at <see cref="MinMetersPerHectare"/> and 1 at
        /// <see cref="MaxMetersPerHectare"/>. Useful for driving shaders
        /// and unit-range UI widgets.
        /// </summary>
        public static double Normalize(double metersPerHectare)
        {
            double range = MaxMetersPerHectare - MinMetersPerHectare;
            double t = (metersPerHectare - MinMetersPerHectare) / range;
            if (t < 0.0) return 0.0;
            if (t > 1.0) return 1.0;
            return t;
        }
    }
}
