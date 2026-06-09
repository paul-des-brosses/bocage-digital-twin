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
    /// <para>La part de prairie g pondère le revenu et les coûts d'intrants
    /// (culture sur 1−g, fourrage résilient sur g) — un coût d'opportunité qui
    /// crée un optimum intérieur (S0a).</para>
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

        // --- Prairie permanente (part d'assolement g) ---
        public const double ForageGrossRevenueEurosPerHa = 900.0;   // produit brut fourrage en bocage d'élevage (fourrage + concentrés évités, Idele/CerFrance Normandie)
        public const double ForageWaterResilienceFloor = 0.6;       // le fourrage garde ≥60% du produit même en sécheresse (racines profondes, INRAE)
        public const double GrasslandUpkeepEurosPerHa = 150.0;      // fauche/clôture/sursemis amorti (CerFrance)

        private const double DaysPerYear = 365.0;

        /// <summary>Marge annualisée (€/ha/an) à l'état courant. Peut être négative (année déficitaire).</summary>
        public static double AnnualMarginEurosPerHa(EcosystemModel model, ScenarioContext scenario)
        {
            // Assolement : (1−g) en culture annuelle, g en prairie permanente.
            double g = scenario.GrasslandFraction;
            if (g < 0.0) g = 0.0; else if (g > 1.0) g = 1.0;
            double cropShare = 1.0 - g;

            // Revenu : culture sur (1−g) + fourrage sur g. Le fourrage est résilient
            // (plancher) mais pas immunisé à la sécheresse (via Ks = YieldRule.WaterFactor).
            double forageResilience = ForageWaterResilienceFloor
                + (1.0 - ForageWaterResilienceFloor) * YieldRule.WaterFactor(model);
            double revenue = cropShare * model.CropYieldTPerHa * CropPriceEurosPerTonne
                + g * ForageGrossRevenueEurosPerHa * forageResilience;

            // Coûts d'intrants : sur la seule part cultivée ; entretien sur la part en herbe.
            double costNitrogen = cropShare * scenario.NitrogenDoseKgPerHaPerYear * NitrogenPriceEurosPerKg;
            double costPesticide = cropShare * scenario.PesticideIntensity * PesticideCostEurosPerUnit;
            double costTillage = cropShare * scenario.TillageIntensity * TillageFuelCostEurosPerYear;
            double costGrassland = g * GrasslandUpkeepEurosPerHa;

            double pse = model.HedgerowDensityMPerHa * PseRateEurosPerMeter;
            double maec = scenario.PesticideIntensity <= MaecIftThreshold ? MaecPaymentEurosPerHa : 0.0;
            double carbonAbove = model.SoilCarbonTotalTPerHa - CarbonReferenceTPerHa;
            double carbonPayment = carbonAbove > 0.0 ? carbonAbove * CarbonPaymentEurosPerTonneAboveBaseline : 0.0;

            return revenue
                - costNitrogen - costPesticide - costTillage - costGrassland - BaseChargesEurosPerHaPerYear
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
