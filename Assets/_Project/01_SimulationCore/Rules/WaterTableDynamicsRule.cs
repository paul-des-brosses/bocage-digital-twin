using System;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Water-table depth from a GARDÉNIA-inspired reservoir balance, the
    /// lumped rainfall→aquifer-level model the BRGM uses for exactly this
    /// problem. The depth below surface IS the aquifer reservoir level.
    /// <list type="bullet">
    ///   <item><b>Effective rain</b> = precipitation minus a
    ///         temperature-driven potential evapotranspiration
    ///         (<c>P − ETP</c>). Only the surplus can reach the aquifer.</item>
    ///   <item><b>Recharge</b> = a fixed fraction of the effective rain
    ///         infiltrates and raises the table (depth decreases). A
    ///         millimetre of recharge raises the table by <c>1&#160;mm / S</c>
    ///         where <c>S</c> is the storage coefficient (specific yield).</item>
    ///   <item><b>Recession</b> = the aquifer drains laterally toward streams
    ///         (Maillet exponential recession), pulling the table back down
    ///         toward a deep dry-season baseline. Keeps the table bounded and
    ///         produces the seasonal swing (shallow wet winter, deep dry
    ///         summer).</item>
    /// </list>
    /// <para>
    /// <b>Sourced parameters</b> (BRGM / SIGES Seine-Normandie — aquifère de
    /// la craie ; Eau Seine-et-Marne — bilan pluie efficace) : storage
    /// coefficient of the chalk 5-10 % (midpoint 0.075) ; infiltration ≈ 21 %
    /// of total P, i.e. ≈ 58 % of the ≈ 36 % effective rain. <b>Calibrated
    /// parameters</b> (documented assumption, tuned on the headless model
    /// harness to a Perche valley/plain nappe : mean ≈ 2 m, seasonal battement
    /// ≈ 1 m, deeper equilibrium under warming) : the temperature-ET
    /// coefficient, the recession rate and the deep baseline. See
    /// docs/CALIBRATION.md §Nappe.
    /// </para>
    /// <para>
    /// Hedge transpiration on the table was evaluated and dropped: at the
    /// field scale it shifts the table by &lt; 0.2 m even at double the
    /// reference density (negligible on yield/biodiversity), and the real cost
    /// of dense hedges is already carried by the maintenance cost and the
    /// crop-yield bell curve. Adding it would have been a redundant mechanic.
    /// </para>
    /// </summary>
    public sealed class WaterTableDynamicsRule : IRule
    {
        public string SubStreamId => "water-table";

        // --- Sourced (BRGM / SIGES Seine-Normandie ; Eau Seine-et-Marne) ---
        /// <summary>Storage coefficient (specific yield) of the chalk
        /// aquifer, 5-10 % per SIGES Seine-Normandie ; midpoint retained.</summary>
        public const double StorageCoefficient = 0.075;
        /// <summary>Fraction of the effective rain that infiltrates to the
        /// aquifer (≈ 21 % of P over ≈ 36 % effective rain, Eau Seine-et-Marne).</summary>
        public const double InfiltrationFraction = 0.58;

        // --- Calibrated on the headless harness (documented assumption) ---
        /// <summary>Temperature-based potential ET, mm per day per °C.</summary>
        public const double EtCoefficientMmPerDegreeDay = 0.14;
        /// <summary>Maillet recession rate per day toward the deep baseline.</summary>
        public const double RecessionRatePerDay = 0.012;
        /// <summary>Dry-season baseline depth the recession pulls toward (m).</summary>
        public const double DeepEquilibriumDepthMeters = 3.0;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double temperature = model.CurrentWeather.TemperatureCelsius;
            double precipitationMm = model.CurrentWeather.PrecipitationMillimeters;

            // Effective rain = precipitation minus temperature-driven ETP.
            double potentialEtMm = EtCoefficientMmPerDegreeDay * Math.Max(0.0, temperature);
            double effectiveRainMm = Math.Max(0.0, precipitationMm - potentialEtMm);

            // Recharge raises the table: a mm of recharge lifts it by mm / S.
            double rechargeMeters = InfiltrationFraction * effectiveRainMm / 1000.0;
            double rechargeTerm = -rechargeMeters / StorageCoefficient;

            // Recession drains the aquifer toward the deep dry baseline.
            // depth < baseline (table high) → positive term (depth grows, falls).
            double recessionTerm =
                RecessionRatePerDay * (DeepEquilibriumDepthMeters - model.WaterTableDepth);

            model.SetWaterTableDepth(model.WaterTableDepth + rechargeTerm + recessionTerm);
        }
    }
}
