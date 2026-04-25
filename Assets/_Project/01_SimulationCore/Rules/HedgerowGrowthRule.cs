using System;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Hedgerows grow continuously when conditions allow. Growth rate is
    /// modulated by water table depth: ample shallow water accelerates
    /// growth, drought (deep water table) slows or stops it.
    /// </summary>
    public sealed class HedgerowGrowthRule : IRule
    {
        public string SubStreamId => "hedgerow-growth";

        private const double AnnualGrowthMetersPerHectare = 0.5;
        private const double DailyGrowth = AnnualGrowthMetersPerHectare / 365.0;
        private const double IdealDepthMeters = 2.0;
        private const double DepthSensitivity = 0.2;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double depthDelta = model.WaterTableDepth - IdealDepthMeters;
            double waterMultiplier = 1.0 - DepthSensitivity * depthDelta;
            if (waterMultiplier < 0.0) waterMultiplier = 0.0;
            if (waterMultiplier > 1.5) waterMultiplier = 1.5;

            double growth = DailyGrowth * waterMultiplier;
            model.SetHedgerowDensity(model.HedgerowDensity + growth);
        }
    }
}
