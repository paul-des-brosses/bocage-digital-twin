using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// A biophysical rule applied once per simulated day. Rules read model
    /// state, scenario inputs, and may mutate the model. Each rule advertises
    /// a SubStreamId so the SimulationEngine can hand it a stable, isolated
    /// RNG sub-stream — that way adding or reordering rules does not shift
    /// other rules' random sequences.
    /// </summary>
    public interface IRule
    {
        string SubStreamId { get; }

        void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng);
    }
}
