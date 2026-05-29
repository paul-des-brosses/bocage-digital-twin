using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore
{
    /// <summary>
    /// Convenience factory wiring the default rule set in the order required
    /// for physical consistency (weather first so downstream rules read
    /// today's weather; water-table dynamics before hedgerow growth so the
    /// growth rule sees the up-to-date depth; agricultural pressure applied
    /// last as it is independent of water).
    /// </summary>
    public static class DefaultSimulation
    {
        public static SimulationEngine Build(
            ulong masterSeed,
            EcosystemModel model = null,
            ScenarioContext scenario = null,
            SeasonalWeatherData seasonalWeather = null)
        {
            model = model ?? new EcosystemModel();
            scenario = scenario ?? new ScenarioContext();
            // Default = Mortagne-au-Perche monthly normals encoded in
            // SeasonalWeatherDataDefaults (chantier E2). Callers that
            // build the engine with an authoring asset (e.g.
            // SimulationRunner with a SeasonalWeatherDataAsset) pass
            // the result of ToSeasonalWeatherData() here instead.
            seasonalWeather = seasonalWeather ?? SeasonalWeatherDataDefaults.MortagneAuPerche();

            var rules = new IRule[]
            {
                new WeatherUpdateRule(seasonalWeather, scenario.StartingMonth),
                new WaterTableDynamicsRule(),
                new HedgerowGrowthRule(),
                new AgriculturalPressureImpactRule(),
                // Economic rules applied after hedge stock is updated so the
                // maintenance cost reads the latest hedgerow density.
                new CropYieldDynamicsRule(),
                new InputCostDynamicsRule(),
                new MaintenanceCostDynamicsRule(),
                // Fauna depends on the up-to-date habitat state (hedgerow
                // density and water table) so it runs after both have been
                // updated for the day.
                new FaunaDynamicsRule(),
            };

            return new SimulationEngine(masterSeed, model, scenario, rules);
        }
    }
}
