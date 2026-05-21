using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily update of the running input-cost estimate (fertiliser,
    /// pesticide, fuel, seeds), in € per hectare per year. The target
    /// is driven by the scenario:
    /// <list type="bullet">
    ///   <item>Agricultural pressure increases inputs (up to +50% at pressure = 1).</item>
    ///   <item>Regulatory constraints reduce inputs (up to -30% at constraints = 1).</item>
    ///   <item>Climate stress requires additional inputs (irrigation, replanting), up to +20%.</item>
    /// </list>
    /// EMA toward target with a ~60-day time constant (k = 0.017).
    /// <para>
    /// <b>Calibration</b>: baseline 400 €/ha/yr matches the order of
    /// magnitude reported by the réseau CIVAM for mixed-farm bocage
    /// systems in the Perche. Bounds [100, 1200] €/ha/yr applied at
    /// the model setter level via <c>ClampNonNegative</c> (the upper
    /// cap is a presentation choice handled by the indicator).
    /// </para>
    /// </summary>
    public sealed class InputCostDynamicsRule : IRule
    {
        public string SubStreamId => "input-cost";

        private const double BaselineEurosPerHectarePerYear = 400.0;
        private const double TransitionRatePerDay = 0.017; // EMA, ~60-day time constant

        private const double PressureMultiplier = 0.50;
        private const double RegulatoryReduction = 0.30;
        private const double ClimateStressSurcharge = 0.20;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double pressure = Clamp01(scenario.AgriculturalPressure.Current);
            double constraints = Clamp01(scenario.RegulatoryConstraints.Current);
            double climate = Clamp01(scenario.ClimateStress.Current);

            double target = BaselineEurosPerHectarePerYear
                            * (1.0 + PressureMultiplier * pressure)
                            * (1.0 - RegulatoryReduction * constraints)
                            * (1.0 + ClimateStressSurcharge * climate);
            if (target < 0.0) target = 0.0;

            double current = model.InputCost;
            double next = current + TransitionRatePerDay * (target - current);
            model.SetInputCost(next);
        }

        private static double Clamp01(double v)
        {
            if (v < 0.0) return 0.0;
            if (v > 1.0) return 1.0;
            return v;
        }
    }
}
