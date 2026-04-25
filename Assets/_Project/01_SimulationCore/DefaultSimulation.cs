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
            ScenarioContext scenario = null)
        {
            model = model ?? new EcosystemModel();
            scenario = scenario ?? new ScenarioContext();

            var rules = new IRule[]
            {
                new WeatherUpdateRule(),
                new WaterTableDynamicsRule(),
                new HedgerowGrowthRule(),
                new AgriculturalPressureImpactRule(),
            };

            return new SimulationEngine(masterSeed, model, scenario, rules);
        }
    }
}
