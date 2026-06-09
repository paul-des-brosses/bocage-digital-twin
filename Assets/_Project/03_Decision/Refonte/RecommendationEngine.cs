using System;
using Bocage.SimulationCore.Refonte;
using Bocage.Sensors.Refonte;

namespace Bocage.Decision.Refonte
{
    /// <summary>
    /// Moteur de recommandation (Couche 03, refonte). Pour un événement, il liste
    /// les leviers candidats, <b>cherche pour chacun le niveau optimal</b> (balaie
    /// la plage, projette, retient l'argmax de l'objectif — le fix du P1 : la reco
    /// donne le bon taux, pas un pas fixe), classe par utilité, et ne surface le
    /// meilleur que s'il améliore réellement l'objectif. Aucune I/O.
    /// </summary>
    public sealed class RecommendationEngine
    {
        public const int DoseSearchLevels = 5;
        public const double MinLevelChange = 1e-6;

        private readonly ModelOutcomeProjector _projector;

        public RecommendationEngine(ModelOutcomeProjector projector)
        {
            _projector = projector ?? throw new ArgumentNullException(nameof(projector));
        }

        /// <summary>Leviers candidats pour un type d'événement.</summary>
        public static DecisionLever[] CandidatesFor(EventKind kind)
        {
            switch (kind)
            {
                case EventKind.HydricStress:
                    return new[] { DecisionLever.CoverCrops, DecisionLever.Tillage };
                case EventKind.SoilCarbonLow:
                    return new[] { DecisionLever.CoverCrops, DecisionLever.Tillage };
                case EventKind.FaunaAnomaly:
                    return new[] { DecisionLever.Pesticide, DecisionLever.NitrogenDose, DecisionLever.HedgeManagement };
                case EventKind.NitrogenDeficiency:
                    return new[] { DecisionLever.NitrogenDose };
                case EventKind.NitrogenExcess:
                    return new[] { DecisionLever.NitrogenDose };
                case EventKind.LowProfitability:
                    return new[] { DecisionLever.NitrogenDose, DecisionLever.Pesticide };
                default:
                    return Array.Empty<DecisionLever>();
            }
        }

        /// <summary>
        /// Produit la meilleure recommandation pour l'événement, ou null si rien
        /// d'utile (objectif amélioré ET le niveau bouge).
        /// </summary>
        public Recommendation TryProduce(EventKind kind, EcosystemModel model, ScenarioContext scenario, ulong masterSeed)
        {
            Recommendation best = null;
            foreach (DecisionLever lever in CandidatesFor(kind))
            {
                (double level, LeverOutcome outcome, double utility) = FindOptimalLevel(model, scenario, masterSeed, lever);
                if (best == null || utility > best.Utility)
                {
                    best = new Recommendation(lever, DecisionLevers.Get(scenario, lever), level, outcome,
                        RecommendationSurfacing.Classify(outcome), kind, utility);
                }
            }

            if (best != null && best.Utility > 0.0
                && Math.Abs(best.RecommendedLevel - best.CurrentLevel) > MinLevelChange)
            {
                return best;
            }
            return null;
        }

        /// <summary>
        /// Cherche le niveau du levier qui maximise l'objectif (balayage + projection).
        /// </summary>
        public (double Level, LeverOutcome Outcome, double Utility) FindOptimalLevel(
            EcosystemModel model, ScenarioContext scenario, ulong masterSeed, DecisionLever lever)
        {
            (double min, double max) = DecisionLevers.Range(lever);
            double bestLevel = DecisionLevers.Get(scenario, lever);
            LeverOutcome bestOutcome = default;
            double bestUtility = double.NegativeInfinity;

            for (int k = 0; k < DoseSearchLevels; k++)
            {
                double level = min + (max - min) * k / (DoseSearchLevels - 1);
                LeverOutcome outcome = _projector.Project(model, scenario, masterSeed,
                    s => DecisionLevers.Set(s, lever, level));
                double utility = FarmerObjective.Utility(outcome);
                if (utility > bestUtility)
                {
                    bestUtility = utility;
                    bestLevel = level;
                    bestOutcome = outcome;
                }
            }
            return (bestLevel, bestOutcome, bestUtility);
        }
    }
}
