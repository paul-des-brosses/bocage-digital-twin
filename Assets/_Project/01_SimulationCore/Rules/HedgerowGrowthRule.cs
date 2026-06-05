using System;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Hedgerows grow continuously when conditions allow. The daily growth is
    /// the product of a potential rate and two state-dependent factors, in
    /// line with the agroforestry literature (INRAE / AFAC-Agroforesteries):
    /// hedge productivity is driven by <b>water availability</b> and by
    /// <b>soil fertility / organic matter</b>.
    /// <list type="bullet">
    ///   <item><b>Water factor</b> — ample shallow water accelerates growth,
    ///         drought (deep water table) slows or stops it.</item>
    ///   <item><b>Fertility factor</b> — a richer, more living soil supports
    ///         faster hedge growth. Proxied by the soil carbon stock relative
    ///         to the Perche reference. This couples soil management
    ///         (couverts, résidus → soil carbon) to hedge dynamics.</item>
    /// </list>
    /// <para>
    /// Calibration : <c>AnnualGrowthMetersPerHectare = 0.5</c> est un proxy de
    /// densification fonctionnelle (allongement de discontinuités +
    /// densification visible), pas une mesure d'allongement linéaire stricto
    /// sensu. La fourchette AFAC pour la régénération naturelle en contexte
    /// favorable est 0,2–0,4 m/ha/an ; 0,5 est dans le haut de fourchette pour
    /// un bocage percheron bien géré. La <i>forme</i> de la modulation (eau,
    /// fertilité) est sourcée ; les seuils du facteur de fertilité sont une
    /// calibration assumée (référence sol = stock de carbone initial du site,
    /// 50 tC/ha). Voir docs/CALIBRATION.md et BACKLOG.md.
    /// </para>
    /// </summary>
    public sealed class HedgerowGrowthRule : IRule
    {
        public string SubStreamId => "hedgerow-growth";

        private const double AnnualGrowthMetersPerHectare = 0.5;
        private const double DailyGrowth = AnnualGrowthMetersPerHectare / 365.0;
        private const double IdealDepthMeters = 2.0;
        private const double DepthSensitivity = 0.2;

        // Fertility factor: soil carbon stock relative to the Perche
        // reference (the site's initial stock). At the reference the factor
        // is 1.0 (no effect); a degraded soil slows growth, a rich soil
        // (good management) speeds it up. Bounds keep the modulation modest.
        private const double SoilCarbonReferenceTonnesPerHectare = 50.0;
        private const double FertilityFloor = 0.3;
        private const double FertilityCap = 1.3;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double depthDelta = model.WaterTableDepth - IdealDepthMeters;
            double waterFactor = 1.0 - DepthSensitivity * depthDelta;
            if (waterFactor < 0.0) waterFactor = 0.0;
            if (waterFactor > 1.5) waterFactor = 1.5;

            double fertilityFactor = model.SoilCarbonStock / SoilCarbonReferenceTonnesPerHectare;
            if (fertilityFactor < FertilityFloor) fertilityFactor = FertilityFloor;
            if (fertilityFactor > FertilityCap) fertilityFactor = FertilityCap;

            double growth = DailyGrowth * waterFactor * fertilityFactor;
            model.SetHedgerowDensity(model.HedgerowDensity + growth);
        }
    }
}
