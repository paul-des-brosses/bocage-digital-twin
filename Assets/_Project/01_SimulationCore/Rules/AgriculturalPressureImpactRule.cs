using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Agricultural pressure (parcel consolidation, drainage, grubbing of
    /// hedges) erodes hedgerow density continuously. Linear in pressure:
    /// pressure = 1 corresponds to a sustained loss of about 5 m/ha per
    /// year, calibrated against typical Perche bocage decline rates.
    /// </summary>
    public sealed class AgriculturalPressureImpactRule : IRule
    {
        public string SubStreamId => "agricultural-pressure";

        private const double AnnualLossMetersPerHectarePerUnitPressure = 5.0;
        private const double DailyLossPerUnitPressure = AnnualLossMetersPerHectarePerUnitPressure / 365.0;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double pressure = scenario.AgriculturalPressure.Current;
            if (pressure <= 0.0) return;

            double loss = DailyLossPerUnitPressure * pressure;
            model.SetHedgerowDensity(model.HedgerowDensity - loss);
        }
    }
}
