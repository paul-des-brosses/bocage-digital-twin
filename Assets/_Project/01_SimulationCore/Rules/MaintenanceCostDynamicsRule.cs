using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily update of the maintenance cost — the annualised expenditure
    /// on bocage upkeep (hedge trimming, replanting, pond clearance), in
    /// € per hectare per year. Computed each tick as a direct linear
    /// function of <see cref="EcosystemModel.HedgerowDensity"/>: no
    /// inertia, because the maintenance contract is recomputed each
    /// time the hedge stock is reassessed.
    /// <para>
    /// <b>Calibration</b>: 0.30 €/m/yr matches the per-linear-metre
    /// rate used by the MAEC linéaire "entretien de haies" and the
    /// maintenance plans of the PNR du Perche. The output figure
    /// (about 27 €/ha/yr at 90 m/ha) is small compared to InputCost,
    /// which is structurally correct — bocage maintenance is much
    /// cheaper than crop inputs.
    /// </para>
    /// </summary>
    public sealed class MaintenanceCostDynamicsRule : IRule
    {
        public string SubStreamId => "maintenance-cost";

        private const double EurosPerMeterPerYear = 0.30;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double cost = EurosPerMeterPerYear * model.HedgerowDensity;
            model.SetMaintenanceCost(cost);
        }
    }
}
