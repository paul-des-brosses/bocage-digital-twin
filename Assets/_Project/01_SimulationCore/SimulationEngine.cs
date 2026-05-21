using System;
using System.Collections.Generic;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore
{
    /// <summary>
    /// Orchestrates a single simulation run. Holds the model, the scenario
    /// context, a list of rules and a master RNG. On construction, derives
    /// one independent RNG sub-stream per rule from the master seed so each
    /// rule has its own reproducible random sequence even if rules are
    /// added or reordered later.
    ///
    /// The simulation is driven externally: callers invoke Tick() once per
    /// simulated day. Couche 5 wires this to a Unity coroutine; tests call
    /// it directly.
    /// </summary>
    public sealed class SimulationEngine
    {
        private readonly IReadOnlyList<IRule> _rules;
        private readonly SeededRandom _master;
        private readonly SeededRandom[] _ruleStreams;

        public EcosystemModel Model { get; }
        public ScenarioContext Scenario { get; }
        public ulong MasterSeed => _master.MasterSeed;

        public SimulationEngine(
            ulong masterSeed,
            EcosystemModel model,
            ScenarioContext scenario,
            IReadOnlyList<IRule> rules)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            Model = model;
            Scenario = scenario;
            _rules = rules;
            _master = new SeededRandom(masterSeed);

            _ruleStreams = new SeededRandom[rules.Count];
            for (int i = 0; i < rules.Count; i++)
            {
                _ruleStreams[i] = _master.DeriveSubStream(rules[i].SubStreamId);
            }
        }

        public void Tick()
        {
            TickInternal(advanceScenario: true);
        }

        /// <summary>
        /// Same as <see cref="Tick"/> but does NOT advance the shared
        /// <see cref="ScenarioContext"/>. Used by the shadow run so that
        /// the scenario's <see cref="TransitioningParameter{T}"/> values
        /// are only stepped once per simulated day — the real run owns
        /// the canonical tick, the shadow just consumes the updated
        /// scenario state. Without this guard, sharing a scenario by
        /// reference across two engines would double-tick transitions
        /// and shorten their durations by half.
        /// </summary>
        public void TickWithoutAdvancingScenario()
        {
            TickInternal(advanceScenario: false);
        }

        private void TickInternal(bool advanceScenario)
        {
            if (advanceScenario) Scenario.Tick();

            for (int i = 0; i < _rules.Count; i++)
            {
                _rules[i].Apply(Model, Scenario, _ruleStreams[i]);
            }

            Model.AdvanceDay();
        }
    }
}
