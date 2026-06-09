using System;

namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Dynamique du rendement Y (t/ha). Y relaxe (EMA, ~saison) vers une cible
    /// produit de facteurs de stress ≤ 1 appliqués au potentiel :
    /// <code>
    ///   Y_target = Y_pot · Ks(θ) · Kn(N) · K_chaleur · K_adventices
    /// </code>
    /// <list type="bullet">
    ///   <item><b>Ks(θ)</b> : stress hydrique FAO-56 — la sécheresse mord le rendement
    ///   (couplage `θ` → Y) ;</item>
    ///   <item><b>Kn(N)</b> : limitation azotée saturante (Mitscherlich) — plateau au-delà
    ///   de l'optimum (fertiliser plus ne gagne plus rien) ;</item>
    ///   <item><b>K_chaleur</b> : pénalité des jours chauds accumulés ;</item>
    ///   <item><b>K_adventices</b> : pénalité du salissement W.</item>
    /// </list>
    /// Y alimente les résidus (carbone) et la marge (économie). Déterministe,
    /// sans I/O. Sources : FAO-56 (Doorenbos &amp; Kassam) ; Mitscherlich/COMIFER ;
    /// IPCC AR6 (chaleur).
    /// </summary>
    public sealed class YieldRule
    {
        public const double YieldPotentialTPerHa = 5.5;
        public const double NitrogenScaleKgPerHa = 15.0;   // Kn = 1 − exp(−N/scale) ; plateau ~ 60-90 kgN
        public const double HeatPenaltyPerDay = 0.003;
        public const double HeatPenaltyCap = 0.09;
        public const double WeedYieldPenalty = 0.3;
        public const double RelaxationDays = 100.0;

        /// <summary>Stress hydrique Ks = clamp(θ / (p·RU_max), 0, 1) — FAO-56.</summary>
        public static double WaterFactor(EcosystemModel model)
        {
            double ruMax = WaterBalanceRule.SoilWaterCapacityMm(model.SoilCarbonTotalTPerHa);
            double ks = model.SoilWaterMm / (WaterBalanceRule.ReadilyAvailableFraction * ruMax);
            if (ks < 0.0) ks = 0.0;
            else if (ks > 1.0) ks = 1.0;
            return ks;
        }

        public static double NitrogenFactor(double mineralNitrogenKgPerHa)
            => 1.0 - Math.Exp(-mineralNitrogenKgPerHa / NitrogenScaleKgPerHa);

        public static double HeatFactor(int recentHeatDayCount)
        {
            double penalty = HeatPenaltyPerDay * recentHeatDayCount;
            if (penalty > HeatPenaltyCap) penalty = HeatPenaltyCap;
            return 1.0 - penalty;
        }

        public static double WeedFactor(double weedPressure) => 1.0 - WeedYieldPenalty * weedPressure;

        public static double Target(EcosystemModel model)
            => YieldPotentialTPerHa
               * WaterFactor(model)
               * NitrogenFactor(model.MineralNitrogenKgPerHa)
               * HeatFactor(model.RecentHeatDayCount)
               * WeedFactor(model.WeedPressure);

        public void Apply(EcosystemModel model)
        {
            double target = Target(model);
            double y = model.CropYieldTPerHa;
            model.SetCropYieldTPerHa(y + (target - y) / RelaxationDays);
        }
    }
}
