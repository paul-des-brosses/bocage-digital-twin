using System.Globalization;
using Bocage.Decision;
using Bocage.Sensors;

namespace Bocage.Presentation.Bindings
{
    /// <summary>
    /// Libellés FR + formats pour afficher une <see cref="Recommendation"/> (popup
    /// + panneau). Centralisé pour que le popup et le panneau parlent le même
    /// langage. Couche 05, présentation pure (aucune logique de décision).
    /// </summary>
    internal static class RecommendationDisplay
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string LeverLabel(DecisionLever lever)
        {
            switch (lever)
            {
                case DecisionLever.NitrogenDose: return "Dose d'azote";
                case DecisionLever.Pesticide: return "Traitements phyto (IFT)";
                case DecisionLever.Tillage: return "Travail du sol";
                case DecisionLever.CoverCrops: return "Couverts d'interculture";
                case DecisionLever.HedgeManagement: return "Gestion des haies";
                case DecisionLever.Grassland: return "Part de prairie";
                default: return lever.ToString();
            }
        }

        public static string LeverValue(DecisionLever lever, double value)
        {
            switch (lever)
            {
                case DecisionLever.NitrogenDose: return value.ToString("0", Inv) + " kgN/ha";
                case DecisionLever.Pesticide: return "IFT " + value.ToString("0.0", Inv);
                case DecisionLever.Tillage: return value.ToString("0.0", Inv);
                case DecisionLever.CoverCrops: return value.ToString("0", Inv) + " %";
                case DecisionLever.HedgeManagement: return value.ToString("+0.0;-0.0;0.0", Inv) + " m/ha/an";
                case DecisionLever.Grassland: return (value * 100.0).ToString("0", Inv) + " %";
                default: return value.ToString("0.##", Inv);
            }
        }

        public static string EventLabel(EventKind kind)
        {
            switch (kind)
            {
                case EventKind.HydricStress: return "Stress hydrique prolongé";
                case EventKind.SoilCarbonLow: return "Carbone du sol bas";
                case EventKind.FaunaAnomaly: return "Anomalie faune (biodiversité)";
                case EventKind.NitrogenDeficiency: return "Carence azotée";
                case EventKind.NitrogenExcess: return "Excès d'azote";
                case EventKind.LowProfitability: return "Rentabilité faible";
                default: return kind.ToString();
            }
        }

        public static string ClassLabel(RecommendationClass cls)
        {
            switch (cls)
            {
                case RecommendationClass.WinWin: return "Gain net (économie + écologie)";
                case RecommendationClass.EconomicTradeoff: return "Compromis : gain économique, coût écologique";
                case RecommendationClass.EcologicalTradeoff: return "Compromis : gain écologique, coût économique";
                default: return "Défavorable";
            }
        }

        public static bool IsTradeoff(RecommendationClass cls) => cls != RecommendationClass.WinWin;
    }
}
