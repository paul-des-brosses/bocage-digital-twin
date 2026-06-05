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
    /// <b>How ReduceInputs is applied.</b> Lowering input intensity is a
    /// sustained PRACTICE change, so the action lowers the real run's
    /// <c>ScenarioContext.InputIntensityFactor</c> (the same value the
    /// « Intensité d'intrants » slider drives) by the chosen magnitude, via a
    /// CLAUDE.md §15 transition, floored at the organic-extensive end
    /// (<see cref="ReduceInputsRecommendation.MinInputIntensityFactor"/>). The
    /// shadow run keeps its frozen baseline intensity (chantier E8), so the
    /// resulting profit gap is exactly what the tech-value KPI measures — no
    /// shared-scenario collapse, no one-shot model nudge.
    /// </para>
    /// </summary>
    public static class AutoActionPipeline
    {
        // Shared transition window for every daily-practice change a reco can
        // apply (input intensity, cover crops, residues, hedge removal rate),
        // over a CLAUDE.md §15 transition. No abrupt mutation.
        private const int PracticeTransitionDays = 10;

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

                ApplyOne(entry.Recommendation, model, scenario, entry.AppliedMagnitude);
                journal.MarkApplied(entry.Recommendation.Id, currentDay);
                applied++;
            }
            return applied;
        }

        /// <summary>
        /// Single-rec application with a caller-chosen magnitude. The
        /// magnitude is the user-set value from the decision popup
        /// slider (e.g. 25 m/ha planted out of a 0-50 range with default
        /// 30). For ReduceInputs the magnitude is the intensity cut;
        /// fauna boost and input-cost reduction scale linearly with the
        /// ratio (magnitude / reference cut).
        /// </summary>
        public static void ApplyOne(IRecommendation rec, EcosystemModel model, ScenarioContext scenario, double magnitude)
        {
            switch (rec)
            {
                case PlantHedgesRecommendation _:
                    model.SetHedgerowDensity(model.HedgerowDensity + magnitude);
                    break;
                case IrrigationAdviceRecommendation _:
                    // Reduce depth (water rises). Floor so the water table
                    // doesn't surface absurdly — same constant as the rationale
                    // text (IrrigationAdviceRecommendation): single source of truth.
                    double floor = IrrigationAdviceRecommendation.WaterTableFloorMeters;
                    double newDepth = model.WaterTableDepth - magnitude;
                    if (newDepth < floor) newDepth = floor;
                    model.SetWaterTableDepth(newDepth);
                    break;
                case ReduceInputsRecommendation _:
                    if (scenario == null) break;
                    // A sustained PRACTICE change: lower the real run's input
                    // intensity (the slider) by the chosen magnitude. The
                    // effect - lower input cost, recovering fauna, adjusted
                    // yield - then flows through the rules over the transition.
                    // The shadow keeps its frozen baseline intensity, so this
                    // is what the tech-value KPI measures. Floored at the
                    // slider's organic-extensive end.
                    double floorIntensity = ReduceInputsRecommendation.MinInputIntensityFactor;
                    double targetIntensity = scenario.InputIntensityFactor.Current - magnitude;
                    if (targetIntensity < floorIntensity) targetIntensity = floorIntensity;
                    scenario.InputIntensityFactor.SetTarget(targetIntensity, PracticeTransitionDays);
                    break;
                case RaiseInputsRecommendation _:
                    if (scenario == null) break;
                    // Economic counterpart: nudge intensity UP. The engine only
                    // issued this reco because the forward projection showed the
                    // raise pays (it gates out raises past the profit optimum), so
                    // here we just apply it, clamped at the physical intensive cap.
                    double riTarget = scenario.InputIntensityFactor.Current + magnitude;
                    double riCeil = RaiseInputsRecommendation.MaxInputIntensityFactor;
                    if (riTarget > riCeil) riTarget = riCeil;
                    scenario.InputIntensityFactor.SetTarget(riTarget, PracticeTransitionDays);
                    break;
                case SowCoverCropsRecommendation _:
                    if (scenario == null) break;
                    double ccTarget = scenario.CoverCropsCoveragePercent.Current + magnitude;
                    if (ccTarget > SowCoverCropsRecommendation.MaxCoveragePercent)
                        ccTarget = SowCoverCropsRecommendation.MaxCoveragePercent;
                    scenario.CoverCropsCoveragePercent.SetTarget(ccTarget, PracticeTransitionDays);
                    break;
                case RestoreResidueRecommendation _:
                    if (scenario == null) break;
                    double rrTarget = scenario.ResidueRestitutionPercent.Current + magnitude;
                    if (rrTarget > RestoreResidueRecommendation.MaxRestitutionPercent)
                        rrTarget = RestoreResidueRecommendation.MaxRestitutionPercent;
                    scenario.ResidueRestitutionPercent.SetTarget(rrTarget, PracticeTransitionDays);
                    break;
                case ReduceHedgeRemovalRecommendation _:
                    if (scenario == null) break;
                    double hrTarget = scenario.HedgeRemovalRate.Current - magnitude;
                    if (hrTarget < 0.0) hrTarget = 0.0;
                    scenario.HedgeRemovalRate.SetTarget(hrTarget, PracticeTransitionDays);
                    break;
                case IncreaseHedgeRemovalRecommendation _:
                    if (scenario == null) break;
                    double ihTarget = scenario.HedgeRemovalRate.Current + magnitude;
                    scenario.HedgeRemovalRate.SetTarget(ihTarget, PracticeTransitionDays);
                    break;
            }
        }
    }
}
