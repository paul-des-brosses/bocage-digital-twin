namespace Bocage.SimulationCore.Refonte
{
    /// <summary>Décomposition du bilan azoté d'un jour (kgN/ha), pour inspection/test.</summary>
    public readonly struct NitrogenFlux
    {
        /// <summary>Entrées : fertilisation + minéralisation + dépôt + fixation.</summary>
        public double InputsKgPerHa { get; }
        public double UptakeKgPerHa { get; }
        public double LeachingKgPerHa { get; }
        public double GaseousKgPerHa { get; }

        /// <summary>Variation nette du jour = entrées − prélèvement − lessivage − pertes gazeuses.</summary>
        public double NetKgPerHa => InputsKgPerHa - UptakeKgPerHa - LeachingKgPerHa - GaseousKgPerHa;

        public NitrogenFlux(double inputs, double uptake, double leaching, double gaseous)
        {
            InputsKgPerHa = inputs;
            UptakeKgPerHa = uptake;
            LeachingKgPerHa = leaching;
            GaseousKgPerHa = gaseous;
        }
    }

    /// <summary>
    /// Bilan d'azote minéral du sol (kgN/ha) — la variable physique qui porte
    /// l'arbitrage éco/écolo :
    /// <code>
    ///   ΔN = fert + N_min + dépôt + fixation − prélèvement − lessivage − pertes_gaz
    /// </code>
    /// <list type="bullet">
    ///   <item><b>N_min</b> : minéralisation de l'humus (pool vieux), couplée au
    ///   même facteur climat r_e que le carbone → le chaud flushe l'azote.</item>
    ///   <item><b>prélèvement</b> : demande de la culture (∝ rendement) sur la
    ///   fenêtre de croissance, plafonnée par la disponibilité (→ limitation Kn).</item>
    ///   <item><b>lessivage</b> : porté par le drainage du jour (couche eau) et
    ///   la concentration N/RU_max → downside qualité d'eau (mare).</item>
    ///   <item><b>fixation</b> : couverts légumineux ; <b>pertes_gaz</b> : fraction
    ///   volatilisée de l'apport.</item>
    /// </list>
    /// Le système est auto-stabilisant (les sorties croissent avec N), donc N
    /// reste positif sans écrêtage. Déterministe, sans I/O. Sources : COMIFER ;
    /// INRAE (lessivage) ; Justes et al. (couverts).
    /// </summary>
    public sealed class NitrogenDynamicsRule
    {
        public const double CarbonNitrogenRatio = 10.0;                     // C/N humus
        public const double AtmosphericDepositionKgPerHaPerYear = 15.0;
        public const double LegumeFixationKgPerHaPerYear = 80.0;            // à 100 % de couverts légumineux
        public const double NitrogenDemandKgPerTonne = 22.0;
        public const double MaxUptakeFractionPerDay = 0.5;                  // plafond d'accès journalier au pool
        public const double LeachableFraction = 0.5;                       // λ
        public const double VolatilizationFraction = 0.10;                 // pertes gazeuses sur l'apport

        // Calendrier agronomique (jour de l'année, 1-365).
        public const int FertilizationStartDay = 60;                       // ~mars
        public const int FertilizationEndDay = 150;                        // ~fin mai
        public const int CropDemandStartDay = 90;                          // ~avril
        public const int CropDemandEndDay = 210;                           // ~fin juillet

        private const double DaysPerYear = 365.0;

        private static bool InWindow(int day, int start, int end) => day >= start && day <= end;

        /// <summary>
        /// Calcule (sans muter) la décomposition du bilan azoté du jour pour le
        /// jour de l'année donné (1-365).
        /// </summary>
        public static NitrogenFlux ComputeFlux(EcosystemModel model, ScenarioContext scenario, int dayOfYear)
        {
            double n = model.MineralNitrogenKgPerHa;

            // --- Entrées ---
            double fertWindow = FertilizationEndDay - FertilizationStartDay + 1;
            double fert = InWindow(dayOfYear, FertilizationStartDay, FertilizationEndDay)
                ? scenario.NitrogenDoseKgPerHaPerYear / fertWindow : 0.0;

            double re = CarbonDynamicsRule.ClimateFactor(model.CurrentWeather.TMeanCelsius, model.SoilWaterMm);
            double oldDecayDaily = (CarbonDynamicsRule.DecayOldPerYear / DaysPerYear) * re * model.CarbonOldTPerHa;
            double nMin = oldDecayDaily / CarbonNitrogenRatio * 1000.0;   // tN/ha/j → kgN/ha/j

            double deposition = AtmosphericDepositionKgPerHaPerYear / DaysPerYear;
            double fixation = LegumeFixationKgPerHaPerYear * (scenario.CoverCropsCoveragePercent / 100.0) / DaysPerYear;

            double inputs = fert + nMin + deposition + fixation;

            // --- Sorties ---
            double demandWindow = CropDemandEndDay - CropDemandStartDay + 1;
            double demand = InWindow(dayOfYear, CropDemandStartDay, CropDemandEndDay)
                ? (model.CropYieldTPerHa * NitrogenDemandKgPerTonne) / demandWindow : 0.0;
            double uptakeCap = MaxUptakeFractionPerDay * n;
            double uptake = demand < uptakeCap ? demand : uptakeCap;

            double ruMax = WaterBalanceRule.SoilWaterCapacityMm(model.SoilCarbonTotalTPerHa);
            double leaching = LeachableFraction * model.LastDrainageMm * (n / ruMax);

            double gaseous = VolatilizationFraction * fert;

            return new NitrogenFlux(inputs, uptake, leaching, gaseous);
        }

        public void Apply(EcosystemModel model, ScenarioContext scenario, int dayOfYear)
        {
            NitrogenFlux f = ComputeFlux(model, scenario, dayOfYear);
            model.SetMineralNitrogenKgPerHa(model.MineralNitrogenKgPerHa + f.NetKgPerHa);
            model.SetLastNitrogenUptakeKgPerHa(f.UptakeKgPerHa);
            model.SetLastNitrogenLeachingKgPerHa(f.LeachingKgPerHa);
        }
    }
}
