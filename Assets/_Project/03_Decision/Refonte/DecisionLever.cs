using Bocage.SimulationCore.Refonte;

namespace Bocage.Decision.Refonte
{
    /// <summary>Les leviers de décision actionnables (MVP) que le moteur peut recommander.</summary>
    public enum DecisionLever
    {
        NitrogenDose,
        Pesticide,
        Tillage,
        CoverCrops,
        HedgeManagement
    }

    /// <summary>
    /// Plage, lecture et écriture de chaque levier sur un <see cref="ScenarioContext"/>.
    /// Conformément à la règle « reco ⊆ leviers », ces mêmes leviers sont pilotables
    /// directement (sliders) ; le moteur ne fait qu'en proposer un niveau optimal.
    /// (La part de prairie n'est pas encore câblée dans le modèle — exclue des
    /// candidats pour l'instant.)
    /// </summary>
    public static class DecisionLevers
    {
        public static (double Min, double Max) Range(DecisionLever lever)
        {
            switch (lever)
            {
                case DecisionLever.NitrogenDose: return (0.0, 250.0);
                case DecisionLever.Pesticide: return (0.0, 2.0);
                case DecisionLever.Tillage: return (0.0, 1.0);
                case DecisionLever.CoverCrops: return (0.0, 100.0);
                case DecisionLever.HedgeManagement: return (-10.0, 10.0);
                default: return (0.0, 1.0);
            }
        }

        public static double Get(ScenarioContext scenario, DecisionLever lever)
        {
            switch (lever)
            {
                case DecisionLever.NitrogenDose: return scenario.NitrogenDoseKgPerHaPerYear;
                case DecisionLever.Pesticide: return scenario.PesticideIntensity;
                case DecisionLever.Tillage: return scenario.TillageIntensity;
                case DecisionLever.CoverCrops: return scenario.CoverCropsCoveragePercent;
                case DecisionLever.HedgeManagement: return scenario.HedgeManagementMetersPerHaPerYear;
                default: return 0.0;
            }
        }

        public static void Set(ScenarioContext scenario, DecisionLever lever, double value)
        {
            switch (lever)
            {
                case DecisionLever.NitrogenDose: scenario.NitrogenDoseKgPerHaPerYear = value; break;
                case DecisionLever.Pesticide: scenario.PesticideIntensity = value; break;
                case DecisionLever.Tillage: scenario.TillageIntensity = value; break;
                case DecisionLever.CoverCrops: scenario.CoverCropsCoveragePercent = value; break;
                case DecisionLever.HedgeManagement: scenario.HedgeManagementMetersPerHaPerYear = value; break;
            }
        }
    }
}
