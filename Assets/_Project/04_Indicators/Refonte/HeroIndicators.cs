using Bocage.SimulationCore.Refonte;

namespace Bocage.Indicators.Refonte
{
    /// <summary>
    /// Les 5 Hero KPI (Couche 04, refonte) + l'apport de la techno. Chaque KPI
    /// suit la chaîne <c>état → valeur métier → normalisation [0,1]</c> (la
    /// normalisation ne sert qu'à colorer la jauge). Lecture seule du modèle ;
    /// réutilise les calculs des règles (pas de duplication). Aucune I/O.
    /// </summary>
    public static class HeroIndicators
    {
        // ---- 1. Marge / rentabilité (€/ha/an) ----
        public const double ProfitNormalizeMin = -500.0;
        public const double ProfitNormalizeMax = 1500.0;

        public static double MarginEurosPerHa(EcosystemModel model, ScenarioContext scenario)
            => EconomyRule.AnnualMarginEurosPerHa(model, scenario);

        public static double MarginNormalized(double eurosPerHa)
            => Clamp01((eurosPerHa - ProfitNormalizeMin) / (ProfitNormalizeMax - ProfitNormalizeMin));

        // ---- 2. Rendement (t/ha) ----
        public const double YieldNormalizeMax = 8.4; // ~1,2 × potentiel

        public static double YieldTPerHa(EcosystemModel model) => model.CropYieldTPerHa;
        public static double YieldNormalized(double tPerHa) => Clamp01(tPerHa / YieldNormalizeMax);

        // ---- 3. Biodiversité [0,1] : état laggé + pression instantanée (doc 10 §B.4) ----
        public static double Biodiversity(EcosystemModel model) => model.Biodiversity;

        public static double BiodiversityPressure(EcosystemModel model, ScenarioContext scenario)
            => BiodiversityRule.Target(model, scenario);

        // ---- 4. Carbone du sol (tC/ha) + trajectoire ----
        public const double CarbonNormalizeMin = 30.0;
        public const double CarbonNormalizeMax = 100.0;

        public static double CarbonTPerHa(EcosystemModel model) => model.SoilCarbonTotalTPerHa;
        public static double CarbonNormalized(double tPerHa)
            => Clamp01((tPerHa - CarbonNormalizeMin) / (CarbonNormalizeMax - CarbonNormalizeMin));

        /// <summary>
        /// Équilibre vers lequel le carbone tend aux conditions courantes —
        /// la <b>trajectoire</b> : si &lt; stock actuel, le sol perd lentement du
        /// carbone (rend lisible la dynamique lente, cf caveat doc 10 §0).
        /// <c>C* = (i / r_e) · (1/k_y + h_hum/k_o)</c>.
        /// </summary>
        public static double CarbonEquilibriumTPerHa(EcosystemModel model, ScenarioContext scenario)
        {
            double re = CarbonDynamicsRule.ClimateFactor(model.CurrentWeather.TMeanCelsius, model.SoilWaterMm);
            if (re < 1e-6) re = 1e-6;
            double inputs = CarbonDynamicsRule.CarbonInputsTPerHaPerYear(model, scenario);
            double poolFactor = 1.0 / CarbonDynamicsRule.DecayYoungPerYear
                + CarbonDynamicsRule.HumificationFraction / CarbonDynamicsRule.DecayOldPerYear;
            return inputs / re * poolFactor;
        }

        // ---- 5. Réserve en eau (% RU) ----
        public static double WaterReservePercent(EcosystemModel model)
        {
            double ruMax = WaterBalanceRule.SoilWaterCapacityMm(model.SoilCarbonTotalTPerHa);
            return ruMax > 0.0 ? model.SoilWaterMm / ruMax * 100.0 : 0.0;
        }

        public static double WaterReserveNormalized(EcosystemModel model)
            => Clamp01(WaterReservePercent(model) / 100.0);

        // ---- Apport de la techno (réel vs run fantôme), net d'investissement ----
        public static double TechValueNetEurosPerHa(double realCapitalEurosPerHa,
            double shadowCapitalEurosPerHa, double investmentsEurosPerHa)
            => realCapitalEurosPerHa - shadowCapitalEurosPerHa - investmentsEurosPerHa;

        private static double Clamp01(double value) => value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
    }
}
