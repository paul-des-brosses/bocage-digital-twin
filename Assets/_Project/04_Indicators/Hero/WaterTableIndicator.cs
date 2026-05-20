using Bocage.SimulationCore.Model;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Second Hero KPI: water table depth in metres below surface, read
    /// directly from <see cref="EcosystemModel.WaterTableDepth"/>.
    /// Stateless, allocation-free, deterministic.
    /// <para>
    /// Sign convention: positive = depth below surface. A small value
    /// means the water table is close to the surface (good for plant
    /// transpiration, risk of waterlogging at &lt; 0.5 m), a large value
    /// means a deep table (drought stress at &gt; 4 m).
    /// </para>
    /// <para>
    /// Normalization is inverted on purpose: 1.0 = best (shallow, healthy
    /// table) and 0.0 = worst (deep, stressed). Downstream gauges and
    /// shaders therefore consume <c>Normalized01</c> with the same
    /// "higher is greener" semantics as the other Hero KPIs, which keeps
    /// the UI grammar consistent across the dashboard.
    /// </para>
    /// <para>
    /// Bounds correspond to the Perche bocage hydrology: 0.5 m (very
    /// high, late winter peak in a clay-bottomed valley) to 6.0 m
    /// (severely depressed, late-summer drought in a permeable upland).
    /// Out-of-range values are clamped — this is a presentation choice,
    /// not a model invariant.
    /// </para>
    /// </summary>
    public static class WaterTableIndicator
    {
        public const double MinDepthMeters = 0.5;  // shallow, optimal
        public const double MaxDepthMeters = 6.0;  // deep, drought-stressed

        /// <summary>Returns the current water table depth in metres below surface.</summary>
        public static double Compute(EcosystemModel model)
        {
            return model.WaterTableDepth;
        }

        /// <summary>
        /// Returns a normalized indicator in <c>[0,1]</c>, with 1.0 at
        /// the shallow (healthy) bound and 0.0 at the deep (stressed)
        /// bound. Inverted compared to raw depth so "higher is greener"
        /// matches the other Hero KPIs.
        /// </summary>
        public static double Normalize(double depthMeters)
        {
            double range = MaxDepthMeters - MinDepthMeters;
            double t = (depthMeters - MinDepthMeters) / range;
            if (t < 0.0) t = 0.0;
            else if (t > 1.0) t = 1.0;
            return 1.0 - t; // invert: shallow = 1.0
        }
    }
}
