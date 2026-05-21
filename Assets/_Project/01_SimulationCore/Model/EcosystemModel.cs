using System;

namespace Bocage.SimulationCore.Model
{
    /// <summary>
    /// Minimal state container for the bocage ecosystem. Holds the variables
    /// the simulation core can read and rules can mutate, plus a simulated
    /// day counter. State is constrained at the setters so invariants
    /// (non-negative depth, non-negative density, ...) cannot be broken.
    /// Defaults are centred on Perche bocage realistic ranges (annual
    /// average temperature ≈ 12 °C, hedgerow density 60-130 m/ha,
    /// mixed cereal/oilseed yield ≈ 5 t/ha, input cost ≈ 400 €/ha/yr).
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
        /// effect of hedgerows, the water table depth, climate stress and
        /// agricultural pressure (cf. CropYieldDynamicsRule).
        /// Source range: 2 to 8 t/ha for a Perche mixed cereal/oilseed
        /// farm (Agreste Centre-Val-de-Loire 2022).
        /// </summary>
        public double CropYield { get; private set; }

        /// <summary>
        /// Annualised cost of inputs (fertilisers, pesticides, fuel,
        /// seeds), in € per hectare per year. Evolves daily toward a
        /// target driven by agricultural pressure, regulatory
        /// constraints and climate stress. Range observed in mixed
        /// bocage farms: 100 to 1200 €/ha/yr (réseau CIVAM, fermes
        /// mixtes bocagères Perche).
        /// </summary>
        public double InputCost { get; private set; }

        /// <summary>
        /// Annualised cost of maintaining the bocage features (hedge
        /// trimming, replanting, pond clearance), in € per hectare per
        /// year. Directly proportional to <see cref="HedgerowDensity"/>;
        /// computed each tick by the MaintenanceCostDynamicsRule using a
        /// unit rate of about 0.30 €/m/yr (MAEC linéaire "entretien de
        /// haies", PNR du Perche).
        /// </summary>
        public double MaintenanceCost { get; private set; }

        public EcosystemModel(
            int initialDay = 0,
            Weather initialWeather = default,
            double initialWaterTableDepth = 2.0,
            double initialHedgerowDensity = 90.0,
            double initialCropYield = 5.0,
            double initialInputCost = 400.0,
            double initialMaintenanceCost = 27.0)
        {
            CurrentDay = initialDay;
            CurrentWeather = initialWeather;
            WaterTableDepth = ClampNonNegative(initialWaterTableDepth);
            HedgerowDensity = ClampNonNegative(initialHedgerowDensity);
            CropYield = ClampNonNegative(initialCropYield);
            InputCost = ClampNonNegative(initialInputCost);
            MaintenanceCost = ClampNonNegative(initialMaintenanceCost);
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

        private static double ClampNonNegative(double value)
        {
            return value < 0.0 ? 0.0 : value;
        }
    }
}
