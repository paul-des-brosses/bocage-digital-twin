using System;

namespace Bocage.SimulationCore.Model
{
    /// <summary>
    /// Minimal state container for the bocage ecosystem. Holds the variables
    /// the simulation core can read and rules can mutate, plus a simulated
    /// day counter. State is constrained at the setters so invariants
    /// (non-negative depth, non-negative density, ...) cannot be broken.
    /// Defaults are centred on Perche bocage realistic ranges (annual
    /// average temperature ≈ 12 °C, hedgerow density 60-130 m/ha).
    /// </summary>
    public sealed class EcosystemModel
    {
        public int CurrentDay { get; private set; }

        public Weather CurrentWeather { get; private set; }

        /// <summary>Distance from surface to top of the water table, in metres. 0 = at surface.</summary>
        public double WaterTableDepth { get; private set; }

        /// <summary>Hedgerow density expressed in metres of hedgerow per hectare.</summary>
        public double HedgerowDensity { get; private set; }

        public EcosystemModel(
            int initialDay = 0,
            Weather initialWeather = default,
            double initialWaterTableDepth = 2.0,
            double initialHedgerowDensity = 90.0)
        {
            CurrentDay = initialDay;
            CurrentWeather = initialWeather;
            WaterTableDepth = ClampNonNegative(initialWaterTableDepth);
            HedgerowDensity = ClampNonNegative(initialHedgerowDensity);
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

        private static double ClampNonNegative(double value)
        {
            return value < 0.0 ? 0.0 : value;
        }
    }
}
