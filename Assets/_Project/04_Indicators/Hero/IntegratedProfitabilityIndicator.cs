using Bocage.SimulationCore.Model;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Hero KPI: integrated farm profitability in € per hectare per year,
    /// computed honestly from four model state variables introduced at
    /// sub-étape 7a:
    /// <list type="bullet">
    ///   <item><see cref="EcosystemModel.CropYield"/> × <see cref="CropPriceEurosPerTonne"/> — gross revenue</item>
    ///   <item>− <see cref="EcosystemModel.InputCost"/></item>
    ///   <item>− <see cref="EcosystemModel.MaintenanceCost"/></item>
    ///   <item>+ <see cref="EcosystemModel.HedgerowDensity"/> × <see cref="HedgerowPseRate"/> — PSE (paiement pour services environnementaux)</item>
    /// </list>
    /// Stateless, allocation-free, deterministic — pure C# function of
    /// the model snapshot. No invented data: every input is a model
    /// state variable evolved by an explicit biophysical or economic
    /// rule (CLAUDE.md §9 sensor primacy).
    /// <para>
    /// <b>Pricing constants</b>:
    /// <list type="bullet">
    ///   <item><c>CropPriceEurosPerTonne = 250</c> — averaged 2022 farm-gate
    ///         price for a mixed cereal/oilseed Perche farm.</item>
    ///   <item><c>HedgerowPseRate = 0.50 €/m/year</c> — order of
    ///         magnitude of MAEC linéaire and PNR du Perche bocage
    ///         maintenance contracts.</item>
    /// </list>
    /// Both treated as constants at this stage; future étapes may move
    /// them under scenario control if we expose price/policy presets.
    /// </para>
    /// <para>
    /// <b>Display bounds</b>: <c>[0, 2000] €/ha/yr</c> for the
    /// normalization channel. The raw label can go below 0 (struggling
    /// farm); the label binding shows the real number, only the
    /// gauge/normalized representation clamps.
    /// </para>
    /// </summary>
    public static class IntegratedProfitabilityIndicator
    {
        public const double MinEurosPerHectare = 0.0;
        public const double MaxEurosPerHectare = 2000.0;

        public const double CropPriceEurosPerTonne = 250.0;
        public const double HedgerowPseRate = 0.50;

        /// <summary>
        /// Returns the integrated profitability in € / hectare / year.
        /// Can be negative under bad conditions.
        /// </summary>
        public static double Compute(EcosystemModel model)
        {
            double revenue = model.CropYield * CropPriceEurosPerTonne;
            double pse = model.HedgerowDensity * HedgerowPseRate;
            return revenue - model.InputCost - model.MaintenanceCost + pse;
        }

        /// <summary>
        /// Returns the normalized profitability in <c>[0,1]</c>, clamping
        /// values below <see cref="MinEurosPerHectare"/> to 0 and above
        /// <see cref="MaxEurosPerHectare"/> to 1.
        /// </summary>
        public static double Normalize(double eurosPerHectare)
        {
            double range = MaxEurosPerHectare - MinEurosPerHectare;
            double t = (eurosPerHectare - MinEurosPerHectare) / range;
            if (t < 0.0) return 0.0;
            if (t > 1.0) return 1.0;
            return t;
        }
    }
}
