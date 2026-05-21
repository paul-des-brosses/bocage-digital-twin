using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Hedge stock loss driven by sustained removal pressure. After the
    /// scenario refactor (sub-étape 7c.1/Option 3), the rate is a direct
    /// scenario input expressed in <i>metres of hedgerow per hectare per
    /// year</i>, removing the previous arbitrary
    /// <c>5 m/ha/yr × pressure[0,1]</c> mapping in favour of a number
    /// the user reads and writes in real units.
    /// </summary>
    public sealed class AgriculturalPressureImpactRule : IRule
    {
        public string SubStreamId => "agricultural-pressure";

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double removalRatePerYear = scenario.HedgeRemovalRate.Current;
            if (removalRatePerYear <= 0.0) return;

            double dailyLoss = removalRatePerYear / 365.0;
            model.SetHedgerowDensity(model.HedgerowDensity - dailyLoss);
        }
    }
}
