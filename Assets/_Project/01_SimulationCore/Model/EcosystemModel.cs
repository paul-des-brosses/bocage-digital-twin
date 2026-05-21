using System;

namespace Bocage.SimulationCore.Model
{
    /// <summary>
    /// Minimal state container for the bocage ecosystem. Holds the variables
    /// the simulation core can read and rules can mutate, plus a simulated
    /// day counter. State is constrained at the setters so invariants
    /// (non-negative depth, non-negative density, ...) cannot be broken.
    /// Defaults are centred on Perche bocage realistic ranges (annual
    /// average temperature ≈ 12 °C, hedgerow density 60-110 m/ha,
    /// mixed cereal/oilseed yield ≈ 5.5 t/ha Eure-et-Loir/Orne,
    /// input cost ≈ 1200 €/ha/yr CIVAM grandes cultures).
    /// </summary>
    public sealed class EcosystemModel
    {
        public int CurrentDay { get; private set; }

        public Weather CurrentWeather { get; private set; }

        /// <summary>Distance from surface to top of the water table, in metres. 0 = at surface.</summary>
        public double WaterTableDepth { get; private set; }

        /// <summary>Hedgerow density expressed in metres of hedgerow per hectare.</summary>
        public double HedgerowDensity { get; private set; }

        /// <summary>
        /// Current estimate of the crop yield at harvest, in tonnes per
        /// hectare. Evolves daily under the influence of the windbreak
        /// effect of hedgerows, the water table depth, climate stress
        /// and agricultural pressure (cf. CropYieldDynamicsRule).
        /// Calibration source: weighted average of Eure-et-Loir wheat
        /// (7 t/ha) and rapeseed (3 t/ha) yields per Agreste 2015-2024.
        /// </summary>
        public double CropYield { get; private set; }

        /// <summary>
        /// Annualised cost of inputs (fertilisers, pesticides, fuel,
        /// seeds), in € per hectare per year. Calibration source:
        /// CIVAM and AFPF range 1100-2000 €/ha/yr for grandes cultures
        /// annuelles. Default 1200 = median conventional Perche mix.
        /// </summary>
        public double InputCost { get; private set; }

        /// <summary>
        /// Annualised cost of maintaining the bocage features (hedge
        /// trimming, replanting, pond clearance), in € per hectare per
        /// year. Linear in HedgerowDensity at 1.0 €/m/yr (cf.
        /// MaintenanceCostDynamicsRule sources).
        /// </summary>
        public double MaintenanceCost { get; private set; }

        /// <summary>
        /// Composite fauna abundance index, dimensionless. 1.0 represents
        /// the Perche bocage reference state (90 m/ha hedges, water table
        /// at 2 m, conventional input intensity). Values above 1 indicate
        /// richer fauna than reference (denser hedges, wetter, less
        /// intensive), values below 1 indicate impoverished fauna
        /// (intensification, drought, hedge removal).
        /// <para>
        /// Calibration sources: INRAE / OFB Vigie-Nature (−30 % oiseaux
        /// des milieux agricoles depuis 1989), Réseau Haies / Solagro
        /// (doublement des passereaux à 100+ m/ha), IPBES (−50 % insectes
        /// en 30 ans sous pression pesticides). Dynamics with a ~1-year
        /// time constant: fauna populations track habitat changes slowly.
        /// </para>
        /// </summary>
        public double FaunaPopulation { get; private set; }

        public EcosystemModel(
            int initialDay = 0,
            Weather initialWeather = default,
            double initialWaterTableDepth = 2.0,
            double initialHedgerowDensity = 90.0,
            double initialCropYield = 5.5,
            double initialInputCost = 1200.0,
            double initialMaintenanceCost = 90.0,
            double initialFaunaPopulation = 1.0)
        {
            CurrentDay = initialDay;
            CurrentWeather = initialWeather;
            WaterTableDepth = ClampNonNegative(initialWaterTableDepth);
            HedgerowDensity = ClampNonNegative(initialHedgerowDensity);
            CropYield = ClampNonNegative(initialCropYield);
            InputCost = ClampNonNegative(initialInputCost);
            MaintenanceCost = ClampNonNegative(initialMaintenanceCost);
            FaunaPopulation = ClampNonNegative(initialFaunaPopulation);
        }

        public void AdvanceDay()
        {
            CurrentDay++;
        }

        public void SetWeather(Weather weather)
        {
            CurrentWeather = weather;
        }

        public void SetWaterTableDepth(double depthInMeters)
        {
            WaterTableDepth = ClampNonNegative(depthInMeters);
        }

        public void SetHedgerowDensity(double metersPerHectare)
        {
            HedgerowDensity = ClampNonNegative(metersPerHectare);
        }

        public void SetCropYield(double tonnesPerHectare)
        {
            CropYield = ClampNonNegative(tonnesPerHectare);
        }

        public void SetInputCost(double eurosPerHectarePerYear)
        {
            InputCost = ClampNonNegative(eurosPerHectarePerYear);
        }

        public void SetMaintenanceCost(double eurosPerHectarePerYear)
        {
            MaintenanceCost = ClampNonNegative(eurosPerHectarePerYear);
        }

        public void SetFaunaPopulation(double index)
        {
            FaunaPopulation = ClampNonNegative(index);
        }

        private static double ClampNonNegative(double value)
        {
            return value < 0.0 ? 0.0 : value;
        }
    }
}
