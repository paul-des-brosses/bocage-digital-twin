using Bocage.Decision;
using Bocage.Presentation.Simulation;
using Bocage.SimulationCore.Logging;
using UnityEngine;

namespace Bocage.Presentation.Decision
{
    /// <summary>
    /// Couche 5 wrapper around <see cref="AutoActionPipeline"/>. Hooks
    /// into <see cref="SimulationRunner.TickCompleted"/> and applies
    /// any newly-accepted recommendation to the real engine's model
    /// and scenario. The shadow engine is never touched — that's
    /// precisely the asymmetry that gives the
    /// <see cref="Bocage.Indicators.Hero.TechDeltaIndicator"/> a
    /// non-zero value once the user (or auto-accept config) accepts
    /// a recommendation.
    /// <para>
    /// Order of operations per tick on the real run:
    /// <list type="number">
    ///   <item>Engine ticks (rules apply to real model).</item>
    ///   <item>EventDetector scans the model, appends to log.</item>
    ///   <item>RecommendationEngine produces pending recs into journal.</item>
    ///   <item>User (or AutoAccept config) sets verdicts on pending recs.</item>
    ///   <item>This component applies Accepted/AutoAccepted recs to
    ///         the real model.</item>
    /// </list>
    /// The journal's <c>IsApplied</c> guard ensures each accepted rec
    /// is applied exactly once.
    /// </para>
    /// </summary>
    public sealed class AutoActionApplier : MonoBehaviour
    {
        [SerializeField, Tooltip("Source of the real engine, decision journal and current day. Drag the GameObject carrying the SimulationRunner.")]
        private SimulationRunner runner;

        private void OnEnable()
        {
            if (runner == null) return;
            runner.TickCompleted += OnTick;
        }

        private void OnDisable()
        {
            if (runner == null) return;
            runner.TickCompleted -= OnTick;
        }

        private void OnTick()
        {
            if (runner == null || runner.DecisionJournal == null) return;
            int applied = AutoActionPipeline.Apply(
                runner.DecisionJournal,
                runner.Model,
                runner.Scenario,
                runner.CurrentDay);
            if (applied > 0)
            {
                SimLogger.UserActionLog("auto-action: applied " + applied
                    + " recommendation(s) on day " + runner.CurrentDay);
            }
        }
    }
}
