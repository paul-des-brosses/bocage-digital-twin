namespace Bocage.SimulationCore
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

        /// <summary>Marge annualisée (€/ha/an) — somme de la décomposition. Peut être négative (année déficitaire).</summary>
        public static double AnnualMarginEurosPerHa(EcosystemModel model, ScenarioContext scenario)
            => Breakdown(model, scenario).TotalEurosPerHa;

        /// <summary>
        /// Décompose la marge en ses postes (revenu, coûts par levier, charges, et
        /// les paiements de services éco). Source unique de vérité : la marge totale
        /// EST la somme des postes — l'onglet Économie lit cette décompo, pas une
        /// recomputation parallèle.
        /// </summary>
        public static MarginBreakdown Breakdown(EcosystemModel model, ScenarioContext scenario)
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
            double carbonCredit = carbonAbove > 0.0 ? carbonAbove * CarbonPaymentEurosPerTonneAboveBaseline : 0.0;

            return new MarginBreakdown(revenue, costNitrogen, costPesticide, costTillage, costGrassland,
                BaseChargesEurosPerHaPerYear, PacBaseEurosPerHa, pse, maec, carbonCredit);
        }

        public void Apply(EcosystemModel model, ScenarioContext scenario)
        {
            double margin = AnnualMarginEurosPerHa(model, scenario);
            model.AddCapitalEurosPerHa(margin / DaysPerYear);
            model.SetLastAnnualMarginEurosPerHa(margin);
        }
    }

    /// <summary>
    /// Décomposition de la marge annualisée en postes (€/ha/an), affichée par
    /// l'onglet Économie. <see cref="TotalEurosPerHa"/> EST la marge — revenu moins
    /// tous les coûts plus tous les paiements de services écosystémiques.
    /// </summary>
    public readonly struct MarginBreakdown
    {
        public double RevenueEurosPerHa { get; }
        public double NitrogenCostEurosPerHa { get; }
        public double PesticideCostEurosPerHa { get; }
        public double TillageCostEurosPerHa { get; }
        public double GrasslandUpkeepEurosPerHa { get; }
        public double BaseChargesEurosPerHa { get; }
        public double PacEurosPerHa { get; }
        public double PseEurosPerHa { get; }
        public double MaecEurosPerHa { get; }
        public double CarbonCreditEurosPerHa { get; }

        public MarginBreakdown(double revenue, double nitrogenCost, double pesticideCost, double tillageCost,
            double grasslandUpkeep, double baseCharges, double pac, double pse, double maec, double carbonCredit)
        {
            RevenueEurosPerHa = revenue;
            NitrogenCostEurosPerHa = nitrogenCost;
            PesticideCostEurosPerHa = pesticideCost;
            TillageCostEurosPerHa = tillageCost;
            GrasslandUpkeepEurosPerHa = grasslandUpkeep;
            BaseChargesEurosPerHa = baseCharges;
            PacEurosPerHa = pac;
            PseEurosPerHa = pse;
            MaecEurosPerHa = maec;
            CarbonCreditEurosPerHa = carbonCredit;
        }

        /// <summary>Marge totale = revenu − (coûts intrants + entretien + charges) + (PAC + PSE + MAEC + crédit carbone).</summary>
        public double TotalEurosPerHa =>
            RevenueEurosPerHa
            - NitrogenCostEurosPerHa - PesticideCostEurosPerHa - TillageCostEurosPerHa
            - GrasslandUpkeepEurosPerHa - BaseChargesEurosPerHa
            + PacEurosPerHa + PseEurosPerHa + MaecEurosPerHa + CarbonCreditEurosPerHa;
    }
}
