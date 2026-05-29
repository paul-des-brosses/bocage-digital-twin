using Bocage.SimulationCore.Model;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Soil organic carbon Hero KPI (chantier E3 / ADR #48): tonnes of
    /// carbon per hectare, read directly from
    /// <see cref="EcosystemModel.SoilCarbonStock"/>. Stateless,
    /// allocation-free, deterministic.
    /// <para>
    /// Normalization bounds reflect the realistic envelope for cultivated
    /// bocage soils in the Perche: <c>MinTonnesPerHectare = 30</c>
    /// (degraded soils, intensive cropping without cover crops or hedges)
    /// to <c>MaxTonnesPerHectare = 100</c> (living soils, near the upper
    /// equilibrium attainable under high inputs + dense bocage). The
    /// default 50 tC/ha (BDAT INRAE reference) sits at <c>Normalized01 ≈
    /// 0.29</c>, in line with "average degraded baseline" framing.
    /// Higher = greener, consistent with the other Hero KPIs.
    /// </para>
    /// <para>
    /// Per the architecture contract (cf ARCHITECTURE.md §2.4), this
    /// layer never mutates the model. The writing of the value into the
    /// observable <c>RC_SoilCarbonStock</c> is performed by the
    /// Couche 05 runner.
    /// </para>
    /// </summary>
    public static class SoilCarbonIndicator
    {
        public const double MinTonnesPerHectare = 30.0;
        public const double MaxTonnesPerHectare = 100.0;

        /// <summary>Returns the current soil organic carbon stock in tC/ha.</summary>
        public static double Compute(EcosystemModel model)
        {
            return model.SoilCarbonStock;
        }

        /// <summary>
        /// Returns a normalized indicator in <c>[0,1]</c>, with 0 at the
        /// degraded bound (30 tC/ha) and 1 at the living-soil bound
        /// (100 tC/ha). Higher is greener, matching the other Hero KPIs.
        /// </summary>
        public static double Normalize(double tonnesCarbonPerHectare)
        {
            double range = MaxTonnesPerHectare - MinTonnesPerHectare;
            double t = (tonnesCarbonPerHectare - MinTonnesPerHectare) / range;
            if (t < 0.0) return 0.0;
            if (t > 1.0) return 1.0;
            return t;
        }
    }
}
