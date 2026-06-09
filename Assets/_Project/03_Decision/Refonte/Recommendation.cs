using Bocage.Sensors.Refonte;

namespace Bocage.Decision.Refonte
{
    /// <summary>Classification d'une recommandation selon le signe de ses effets projetés.</summary>
    public enum RecommendationClass
    {
        WinWin,             // marge ≥ 0 ET biodiversité ≥ 0
        EconomicTradeoff,   // marge ≥ 0, biodiversité < 0
        EcologicalTradeoff, // biodiversité ≥ 0, marge < 0
        LoseLose
    }

    /// <summary>
    /// Une recommandation produite par le moteur : le levier à ajuster, son
    /// <b>niveau optimal</b> (la dose qui maximise l'objectif — fix du P1), l'effet
    /// projeté (ΔKPI + bande), la classe, et l'événement déclencheur. Le payload
    /// d'affichage (provenance, etc.) se construit à partir de ces champs.
    /// </summary>
    public sealed class Recommendation
    {
        public DecisionLever Lever { get; }
        public double CurrentLevel { get; }
        public double RecommendedLevel { get; }
        public LeverOutcome Outcome { get; }
        public RecommendationClass Class { get; }
        public EventKind TriggeredBy { get; }
        public double Utility { get; }

        public Recommendation(DecisionLever lever, double currentLevel, double recommendedLevel,
            LeverOutcome outcome, RecommendationClass recommendationClass, EventKind triggeredBy, double utility)
        {
            Lever = lever;
            CurrentLevel = currentLevel;
            RecommendedLevel = recommendedLevel;
            Outcome = outcome;
            Class = recommendationClass;
            TriggeredBy = triggeredBy;
            Utility = utility;
        }
    }

    /// <summary>Classe une recommandation à partir des signes de ses Δ projetés (doc 10 §C.7).</summary>
    public static class RecommendationSurfacing
    {
        /// <summary>Sous ce seuil de biodiversité, un compromis écologique devient une urgence qui interrompt.</summary>
        public const double CriticalBiodiversityThreshold = 0.35;

        public static RecommendationClass Classify(LeverOutcome outcome)
        {
            bool economic = outcome.DeltaMarginEurosPerHa.Expected >= 0.0;
            bool ecological = outcome.DeltaBiodiversity.Expected >= 0.0;
            if (economic && ecological) return RecommendationClass.WinWin;
            if (economic) return RecommendationClass.EconomicTradeoff;
            if (ecological) return RecommendationClass.EcologicalTradeoff;
            return RecommendationClass.LoseLose;
        }

        /// <summary>
        /// Gate d'auto-popup : on n'interrompt l'utilisateur que pour un <b>win-win</b>,
        /// ou une <b>urgence écologique</b> (compromis écologique alors que la
        /// biodiversité est sous le seuil critique). Les compromis ordinaires
        /// patientent dans la liste du panneau de décision.
        /// </summary>
        public static bool ShouldAutoPopup(Recommendation recommendation, double biodiversity)
        {
            if (recommendation == null) return false;
            if (recommendation.Class == RecommendationClass.WinWin) return true;
            return recommendation.Class == RecommendationClass.EcologicalTradeoff
                   && biodiversity < CriticalBiodiversityThreshold;
        }
    }
}
