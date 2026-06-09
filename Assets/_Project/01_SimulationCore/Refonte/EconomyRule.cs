namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Économie : marge annualisée et capital cumulé. La marge décompose le
    /// revenu, les coûts des leviers (azote, phyto, travail du sol) et les
    /// charges fixes, plus les paiements qui <b>monétisent les services
    /// écosystémiques</b> — PAC de base, PSE (haies/flore), MAEC (récompense le
    /// bas-phyto), crédit carbone (carbone au-dessus de la baseline) :
    /// <code>
    ///   marge = Y·prix − coût_N − coût_phyto − coût_travail − charges
    ///         + PAC + PSE + MAEC + crédit_carbone
    /// </code>
    /// L'écologie « paie » donc en euros traçables. Déterministe, sans I/O.
    /// Sources : Agreste/CerFrance (prix, charges), PAC 2025, CIVAM (MAEC),
    /// Label Bas-Carbone (crédit).
    /// </summary>
    public sealed class EconomyRule
    {
        public const double CropPriceEurosPerTonne = 250.0;
        public const double NitrogenPriceEurosPerKg = 1.2;
        public const double PesticideCostEurosPerUnit = 80.0;
        public const double TillageFuelCostEurosPerYear = 60.0;
        public const double BaseChargesEurosPerHaPerYear = 1000.0;
        public const double PacBaseEurosPerHa = 220.0;
        public const double PseRateEurosPerMeter = 0.5;            // monétise la densité de haie
        public const double MaecIftThreshold = 0.7;               // IFT ≤ seuil → paiement MAEC
        public const double MaecPaymentEurosPerHa = 90.0;
        public const double CarbonReferenceTPerHa = 50.0;
        public const double CarbonPaymentEurosPerTonneAboveBaseline = 6.0; // crédit carbone (services)

        private const double DaysPerYear = 365.0;

        /// <summary>Marge annualisée (€/ha/an) à l'état courant. Peut être négative (année déficitaire).</summary>
        public static double AnnualMarginEurosPerHa(EcosystemModel model, ScenarioContext scenario)
        {
            double revenue = model.CropYieldTPerHa * CropPriceEurosPerTonne;

            double costNitrogen = scenario.NitrogenDoseKgPerHaPerYear * NitrogenPriceEurosPerKg;
            double costPesticide = scenario.PesticideIntensity * PesticideCostEurosPerUnit;
            double costTillage = scenario.TillageIntensity * TillageFuelCostEurosPerYear;

            double pse = model.HedgerowDensityMPerHa * PseRateEurosPerMeter;
            double maec = scenario.PesticideIntensity <= MaecIftThreshold ? MaecPaymentEurosPerHa : 0.0;
            double carbonAbove = model.SoilCarbonTotalTPerHa - CarbonReferenceTPerHa;
            double carbonPayment = carbonAbove > 0.0 ? carbonAbove * CarbonPaymentEurosPerTonneAboveBaseline : 0.0;

            return revenue
                - costNitrogen - costPesticide - costTillage - BaseChargesEurosPerHaPerYear
                + PacBaseEurosPerHa + pse + maec + carbonPayment;
        }

        public void Apply(EcosystemModel model, ScenarioContext scenario)
        {
            double margin = AnnualMarginEurosPerHa(model, scenario);
            model.AddCapitalEurosPerHa(margin / DaysPerYear);
            model.SetLastAnnualMarginEurosPerHa(margin);
        }
    }
}
