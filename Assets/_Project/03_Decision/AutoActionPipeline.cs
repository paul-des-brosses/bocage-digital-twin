using Bocage.Decision.Recommendations;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.Decision
{
    /// <summary>
    /// Applies the mechanical effect of every Accepted / AutoAccepted
    /// recommendation in a <see cref="DecisionJournal"/> to the real
    /// <see cref="EcosystemModel"/>. Idempotent via the journal's own
    /// applied-tracking (<see cref="DecisionJournal.MarkApplied"/>):
    /// each rec is applied exactly once even if the pipeline runs
    /// every tick.
    /// <para>
    /// Pure C#, no Unity. The Couche 5 <c>AutoActionApplier</c>
    /// wraps this pipeline in a MonoBehaviour that subscribes to
    /// <c>SimulationRunner.TickCompleted</c>.
    /// </para>
    /// <para>
    /// <b>Honesty note on the ReduceInputs action</b>. The "right" way to
    /// reduce input intensity would be to lower
    /// <c>ScenarioContext.InputIntensityFactor</c> — but that's the
    /// shared scenario, so changing it would also affect the shadow run
    /// and collapse the tech-delta KPI to zero. As a pragmatic
    /// alternative for sub-étape 8c.3, the action applies its visible
    /// effects directly on the real model: a fauna abundance boost
    /// (+0.05 index, representing the immediate insect rebound from
    /// pesticide cessation) and an input-cost reduction (−200 €/ha/yr,
    /// representing the savings). This is a one-shot model nudge, not
    /// a sustained scenario change — a future refactor could introduce
    /// a per-run "tech adjustment" axis on the model to express this
    /// cleanly without the shared-scenario tension.
    /// </para>
    /// </summary>
    public static class AutoActionPipeline
    {
        /// <summary>
        /// Walks the journal's resolved entries and applies the
        /// mechanical effect of any Accepted / AutoAccepted rec not yet
        /// marked applied. Returns the number of actions applied this
        /// pass (0 in the steady state). Safe to call every tick.
        /// </summary>
        public static int Apply(DecisionJournal journal, EcosystemModel model, ScenarioContext scenario, int currentDay)
        {
            if (journal == null || model == null) return 0;
            int applied = 0;

            var resolved = journal.ResolvedEntries;
            for (int i = 0; i < resolved.Count; i++)
            {
                var entry = resolved[i];
                if (entry.Verdict != DecisionVerdict.Accepted && entry.Verdict != DecisionVerdict.AutoAccepted) continue;
                if (journal.IsApplied(entry.Recommendation.Id)) continue;

                ApplyOne(entry.Recommendation, model, scenario);
                journal.MarkApplied(entry.Recommendation.Id, currentDay);
                applied++;
            }
            return applied;
        }

        /// <summary>
        /// Single-rec application. Exposed for tests that want to
        /// verify each action's mechanical effect in isolation.
        /// </summary>
        public static void ApplyOne(IRecommendation rec, EcosystemModel model, ScenarioContext scenario)
        {
            switch (rec)
            {
                case PlantHedgesRecommendation _:
                    model.SetHedgerowDensity(model.HedgerowDensity + PlantHedgesRecommendation.HedgeRestoreMetersPerHectare);
                    break;
                case IrrigationAdviceRecommendation _:
                    // Reduce depth (water rises). Floor at 0.5 m so the
                    // water table doesn't surface absurdly.
                    double newDepth = model.WaterTableDepth - IrrigationAdviceRecommendation.WaterReliefDepthMeters;
                    if (newDepth < 0.5) newDepth = 0.5;
                    model.SetWaterTableDepth(newDepth);
                    break;
                case ReduceInputsRecommendation _:
                    // Pragmatic shortcut: boost fauna + cut input cost
                    // directly on the model. See class docstring for
                    // the architectural rationale.
                    model.SetFaunaPopulation(model.FaunaPopulation + 0.05);
                    model.SetInputCost(model.InputCost - 200.0);
                    break;
            }
        }
    }
}
