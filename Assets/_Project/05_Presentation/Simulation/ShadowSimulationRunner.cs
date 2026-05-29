using Bocage.SimulationCore;
using Bocage.SimulationCore.Logging;
using Bocage.SimulationCore.Model;
using UnityEngine;

namespace Bocage.Presentation.Simulation
{
    /// <summary>
    /// Runs a parallel "shadow" simulation alongside the real
    /// <see cref="SimulationRunner"/>. Same master seed, same shared
    /// <see cref="Bocage.SimulationCore.Scenario.ScenarioContext"/>
    /// reference, fresh independent <see cref="EcosystemModel"/>. The
    /// shadow advances one tick whenever the real runner fires its
    /// <see cref="SimulationRunner.TickCompleted"/> event, using
    /// <see cref="SimulationEngine.TickWithoutAdvancingScenario"/> so the
    /// scenario's <c>TransitioningParameter</c> values are only stepped
    /// once per simulated day (the real run owns the canonical
    /// scenario tick).
    /// <para>
    /// At sub-étape 8b the shadow trajectory is mathematically identical
    /// to the real trajectory because no decisions or auto-actions
    /// differentiate them yet. That makes <see cref="ShadowModel"/>
    /// equal to <see cref="SimulationRunner.Model"/> at every tick, and
    /// the tech-delta KPI displays 0 by construction. The component
    /// exists at 8b so the plumbing is in place; meaningful divergence
    /// emerges at sub-étape 8c when <c>AutoActions</c> are applied to
    /// the real engine but skipped on the shadow.
    /// </para>
    /// <para>
    /// Execution order: this runner has no DefaultExecutionOrder so its
    /// Awake/Start fire AFTER <see cref="SimulationRunner"/> (which is
    /// at -8000). At Start, the real runner's engine already exists and
    /// its scenario reference can be safely captured.
    /// </para>
    /// </summary>
    public sealed class ShadowSimulationRunner : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the seed and shared scenario reference. Drag the GameObject carrying the real SimulationRunner.")]
        private SimulationRunner realRunner;

        private SimulationEngine _engine;

        /// <summary>
        /// The shadow's <see cref="EcosystemModel"/>. Read-only from the
        /// outside — bindings should never write to it.
        /// </summary>
        public EcosystemModel ShadowModel => _engine?.Model;

        private void Start()
        {
            if (realRunner == null)
            {
                SimLogger.DebugLog("[ShadowSimulationRunner] realRunner not assigned; shadow run disabled");
                return;
            }

            // Fresh model for the shadow (independent state). Same scenario
            // reference so user inputs (sliders / presets) affect both
            // runs in lockstep. Same master seed so any rule that uses
            // its SubStream RNG produces an identical sequence — only
            // divergent decisions (8c) will move the two trajectories apart.
            var shadowModel = new EcosystemModel();
            _engine = DefaultSimulation.Build(realRunner.MasterSeed, shadowModel, realRunner.Scenario, realRunner.SeasonalWeather);

            realRunner.TickCompleted += OnRealTickCompleted;
            realRunner.Rebuilt += OnRealRebuilt;
        }

        private void OnDestroy()
        {
            if (realRunner != null)
            {
                realRunner.TickCompleted -= OnRealTickCompleted;
                realRunner.Rebuilt -= OnRealRebuilt;
            }
        }

        private void OnRealTickCompleted()
        {
            // Scenario has already been advanced by the real engine's
            // Tick(). Shadow only re-applies the rules to its own model
            // against the now-current scenario state, without
            // double-ticking the scenario.
            _engine?.TickWithoutAdvancingScenario();
        }

        private void OnRealRebuilt()
        {
            // Real has been reset to day 0 with new initial conditions.
            // Rebuild the shadow with an identical fresh model so the
            // two trajectories stay aligned at t=0 and TechDelta starts
            // from zero again. Reusing the shared scenario reference.
            var shadowModel = new EcosystemModel(
                initialWaterTableDepth: realRunner.Model.WaterTableDepth,
                initialHedgerowDensity: realRunner.Model.HedgerowDensity,
                initialFaunaPopulation: realRunner.Model.FaunaPopulation);
            _engine = DefaultSimulation.Build(realRunner.MasterSeed, shadowModel, realRunner.Scenario, realRunner.SeasonalWeather);
        }
    }
}
