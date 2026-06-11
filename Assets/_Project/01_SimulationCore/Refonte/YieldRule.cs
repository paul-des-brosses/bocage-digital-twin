namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Rendement Y (t/ha) modélisé comme une <b>récolte annuelle</b> : le stress
    /// est intégré sur la <b>saison de croissance</b> (printemps → récolte), puis
    /// la récolte fige Y pour l'année. C'est plus juste qu'un stress hydrique
    /// appliqué toute l'année (qui pénalisait le rendement avec la sécheresse
    /// d'août, alors que la culture est déjà récoltée), et ça préserve la cascade
    /// (la sécheresse frappe bien la saison de croissance).
    /// <code>
    ///   stress_jour = Ks(θ) · Kn(N) · K_chaleur · K_adventices   (chaque facteur ≤ 1)
    ///   Y_récolte   = Y_pot · moyenne(stress_jour sur la saison)
    /// </code>
    /// Y_pot = potentiel non stressé ; l'actuel en découle par les stress.
    /// Déterministe, sans I/O. Sources : FAO-56 (Doorenbos &amp; Kassam) ;
    /// Mitscherlich/COMIFER ; IPCC AR6 (chaleur).
    /// </summary>
    public sealed class YieldRule
    {
        public const double YieldPotentialTPerHa = 7.6;    // potentiel non stressé (blé atteignable Perche → ~5,5 actuel après stress)
        public const double NitrogenScaleKgPerHa = 15.0;   // Kn = 1 − exp(−N/scale)
        public const double HeatPenaltyPerDay = 0.003;
        public const double HeatPenaltyCap = 0.09;
        public const double WeedYieldPenalty = 0.3;
        public const int GrowingSeasonStartDay = 90;       // ~avril
        public const int GrowingSeasonEndDay = 210;        // ~fin juillet (récolte)

        private double _stressSum;
        private int _stressDays;

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
            => 1.0 - System.Math.Exp(-mineralNitrogenKgPerHa / NitrogenScaleKgPerHa);

        public static double HeatFactor(int recentHeatDayCount)
        {
            double penalty = HeatPenaltyPerDay * recentHeatDayCount;
            if (penalty > HeatPenaltyCap) penalty = HeatPenaltyCap;
            return 1.0 - penalty;
        }

        public static double WeedFactor(double weedPressure) => 1.0 - WeedYieldPenalty * weedPressure;

        /// <summary>Facteur de stress instantané du jour (produit des facteurs ≤ 1).</summary>
        public static double DailyStressFactor(EcosystemModel model)
            => WaterFactor(model)
               * NitrogenFactor(model.MineralNitrogenKgPerHa)
               * HeatFactor(model.RecentHeatDayCount)
               * WeedFactor(model.WeedPressure);

        /// <summary>
        /// Accumule le stress du jour sur la saison de croissance ; à la récolte
        /// (<see cref="GrowingSeasonEndDay"/>), fige le rendement de l'année.
        /// <paramref name="dayOfYear"/> ∈ [1, 365].
        /// </summary>
        public void Apply(EcosystemModel model, int dayOfYear)
        {
            if (dayOfYear == GrowingSeasonStartDay)
            {
                _stressSum = 0.0;
                _stressDays = 0;
            }
            if (dayOfYear >= GrowingSeasonStartDay && dayOfYear <= GrowingSeasonEndDay)
            {
                _stressSum += DailyStressFactor(model);
                _stressDays++;
            }
            if (dayOfYear == GrowingSeasonEndDay && _stressDays > 0)
            {
                double meanStress = _stressSum / _stressDays;
                model.SetCropYieldTPerHa(YieldPotentialTPerHa * meanStress);
            }
        }
    }
}
