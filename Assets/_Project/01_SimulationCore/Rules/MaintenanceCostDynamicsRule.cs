using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily update of the maintenance cost — the annualised expenditure
    /// on bocage upkeep (hedge trimming, replanting, pond clearance), in
    /// € per hectare per year. Linear function of
    /// <see cref="EcosystemModel.HedgerowDensity"/>, no inertia.
    /// <para>
    /// <b>Calibration (revision 2026-05-21)</b>
    /// Rate 1.0 €/m/yr derives from the Réseau Haies 2024 référentiel
    /// "coût moyen de gestion durable" of 3.69 €/ml (taille + entretien
    /// + replantation amortie), reduced to account for the share of
    /// labour the farmer self-absorbs (the published figure includes
    /// market-rate labour). 1.0 €/m/yr represents the out-of-pocket
    /// cost (fuel, équipement amorti, intrants secondaires).
    /// </para>
    /// </summary>
    public sealed class MaintenanceCostDynamicsRule : IRule
    {
        public string SubStreamId => "maintenance-cost";

        public const double EurosPerMeterPerYear = 1.0;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double cost = EurosPerMeterPerYear * model.HedgerowDensity;
            model.SetMaintenanceCost(cost);
        }
    }
}
