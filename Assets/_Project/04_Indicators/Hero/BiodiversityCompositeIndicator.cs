using Bocage.SimulationCore.Model;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Third Hero KPI: composite biodiversity score in <c>[0, 1]</c>,
    /// aggregating three habitat signals already exposed by the model:
    /// <list type="bullet">
    ///   <item><b>FaunaPopulation</b> — direct fauna abundance index.
    ///         Weight 0.5 because it's the closest proxy for the thing
    ///         being measured.</item>
    ///   <item><b>HedgerowDensity</b> — habitat connectivity & corridors.
    ///         Weight 0.3 (Réseau Haies / Solagro evidence on passerine
    ///         density doubling at 100+ m/ha).</item>
    ///   <item><b>WaterTableDepth (inverted)</b> — proxy for the
    ///         availability of wetland habitats (mares, fossés). Weight
    ///         0.2 (smallest of the three because it's an indirect
    ///         proxy).</item>
    /// </list>
    /// Per ARCHITECTURE.md §2.4, this layer never mutates the model: it
    /// only reads. Stateless, allocation-free, deterministic.
    /// <para>
    /// The weights come from the broad consensus in the agroecology
    /// literature (INRAE composite "biodiversity index" workshops) that
    /// fauna abundance is the leading indicator and hedge corridors
    /// dominate hedge-water habitats in temperate bocage. They are not a
    /// hard physical law — they're a defensible aggregation choice
    /// documented here so the score can be audited.
    /// </para>
    /// </summary>
    public static class BiodiversityCompositeIndicator
    {
        public const double FaunaWeight = 0.5;
        public const double HedgerowWeight = 0.3;
        public const double WaterWeight = 0.2;

        // Fauna normalisation bounds. 0 = collapsed, 1.5 = lush bocage
        // (Solagro reference). Anything beyond 1.5 saturates at 1.0 on
        // the normalised scale.
        public const double FaunaMinIndex = 0.0;
        public const double FaunaMaxIndex = 1.5;

        /// <summary>
        /// Returns the raw composite score in <c>[0, 1]</c> ready for
        /// the Hero KPI label and gauges. Same value as <see cref="Normalize"/>
        /// since the indicator is already unit-range by construction.
        /// </summary>
        public static double Compute(EcosystemModel model)
        {
            double faunaN = NormalizeFauna(model.FaunaPopulation);
            double hedgeN = HedgerowDensityIndicator.Normalize(model.HedgerowDensity);
            double waterN = WaterTableIndicator.Normalize(model.WaterTableDepth);

            return FaunaWeight * faunaN + HedgerowWeight * hedgeN + WaterWeight * waterN;
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
        /// Maps the dimensionless fauna index to <c>[0, 1]</c> using the
        /// <see cref="FaunaMinIndex"/> / <see cref="FaunaMaxIndex"/>
        /// bounds. Exposed publicly so tests can assert the contribution
        /// of each variable independently.
        /// </summary>
        public static double NormalizeFauna(double faunaIndex)
        {
            double range = FaunaMaxIndex - FaunaMinIndex;
            double t = (faunaIndex - FaunaMinIndex) / range;
            if (t < 0.0) return 0.0;
            if (t > 1.0) return 1.0;
            return t;
        }
    }
}
