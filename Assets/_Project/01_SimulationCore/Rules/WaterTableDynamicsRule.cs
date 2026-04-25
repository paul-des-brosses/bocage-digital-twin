using System;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Updates the water table depth from today's weather. Precipitation
    /// raises the water level (depth decreases). Warm temperatures drive
    /// evapotranspiration (depth increases). Climate stress amplifies the
    /// evaporation term.
    /// </summary>
    public sealed class WaterTableDynamicsRule : IRule
    {
        public string SubStreamId => "water-table";

        private const double InfiltrationFactor = 0.0001;   // depth metres lifted per mm of rain
        private const double EvaporationBase = 0.003;       // depth metres added per (T/30) degree-day
        private const double EvaporationStressGain = 1.0;   // multiplicative gain at stress = 1

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double climate = scenario.ClimateStress.Current;
            double rainTerm = -model.CurrentWeather.PrecipitationMillimeters * InfiltrationFactor;
            double tempNormalized = Math.Max(0.0, model.CurrentWeather.TemperatureCelsius / 30.0);
            double evapTerm = tempNormalized * EvaporationBase * (1.0 + EvaporationStressGain * climate);
            double change = rainTerm + evapTerm;

            model.SetWaterTableDepth(model.WaterTableDepth + change);
        }
    }
}
