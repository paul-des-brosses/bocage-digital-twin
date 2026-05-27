using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Hero KPI: integrated farm profitability in € per hectare per year.
    /// Honest composite read from four model state variables and the
    /// PSE subsidy rate exposed by the scenario:
    /// <para>
    ///   <c>profit = CropYield × CropPrice</c><br/>
    ///   <c>       − InputCost</c><br/>
    ///   <c>       − MaintenanceCost</c><br/>
    ///   <c>       + HedgerowDensity × scenario.PseSubsidyRate</c><br/>
    ///   <c>       + PacHedgeBonus × hectare (forfait PAC 2025)</c>
    /// </para>
    /// <para>
    /// <b>Pricing constants (revision 2026-05-21)</b>
    /// <list type="bullet">
    ///   <item><c>CropPriceEurosPerTonne = 250</c> : prix farm-gate
    ///         pondéré blé/colza Eure-et-Loir 2022 (blé 230-270 €/t,
    ///         colza 400-550 €/t en année moyenne, mix 70/30).</item>
    ///   <item><c>PacHedgeBonusEurosPerHectare = 20</c> : Bonus haie PAC
    ///         2025 (Chambre Agriculture Pays de la Loire),
    ///         indépendant de la densité linéaire. Forfait par hectare
    ///         de SAU si haies présentes.</item>
    /// </list>
    /// <b>Note honnêteté</b> : le baseline yield (5.5 t/ha) inclut déjà
    /// l'effet brise-vent moyen du bocage. La bell curve dans
    /// CropYieldDynamicsRule pénalise les écarts à la densité optimale,
    /// elle ne booste plus le pic. Évite double-comptage.
    /// </para>
    /// <para>
    /// <b>Display bounds</b>: <c>[-500, 1000] €/ha/yr</c>. Le profit
    /// peut être négatif (ferme déficitaire sous mauvaises conditions
    /// et intrants chers), atteindre +500-800 sous conditions
    /// optimales avec PSE et MAEC actifs. Marges réelles fermes
    /// grandes cultures Perche : 100-400 €/ha/yr en année moyenne.
    /// </para>
    /// </summary>
    public static class IntegratedProfitabilityIndicator
    {
        public const double MinEurosPerHectare = -500.0;
        public const double MaxEurosPerHectare = 1500.0;

        public const double CropPriceEurosPerTonne = 250.0;
        public const double PacHedgeBonusEurosPerHectare = 20.0;

        /// <summary>
        /// CAP base support : DPB (Droit Paiement de Base) + paiement
        /// redistributif + écorégime base. Le paiement vert PAC 2014-2020
        /// est supprimé depuis 2022 et remplacé par l'écorégime à partir
        /// de 2023 (Légifrance, arrêté du 25 novembre 2025).
        /// Montants 2025 : DPB Hexagone ≈ 127,67 €/ha, paiement redistributif
        /// (52 premiers ha) ≈ 48 €/ha, écorégime base ≈ 45 €/ha.
        /// Total ≈ 220 €/ha/yr. Sources : Légifrance + Leandri Conseils 2025.
        /// </summary>
        public const double BasicCapPaymentEurosPerHectare = 220.0;

        /// <summary>
        /// Returns the integrated profitability in € / hectare / year.
        /// Includes production margin + CAP basic payment + PAC hedge
        /// bonus + PSE. Can be negative under severe stress combined
        /// with high inputs and absence of subsidies.
        /// </summary>
        public static double Compute(EcosystemModel model, ScenarioContext scenario)
        {
            double pseRate = scenario != null ? scenario.PseSubsidyRate.Current : 0.0;
            double revenue = model.CropYield * CropPriceEurosPerTonne;
            double pse = model.HedgerowDensity * pseRate;
            double pacBonus = model.HedgerowDensity > 0.0 ? PacHedgeBonusEurosPerHectare : 0.0;
            return revenue - model.InputCost - model.MaintenanceCost
                   + pse + pacBonus + BasicCapPaymentEurosPerHectare;
        }

        /// <summary>
        /// Returns the normalized profitability in <c>[0,1]</c>, mapping
        /// the display range [Min, Max] linearly. Centred so 0.5
        /// corresponds to break-even profit ≈ 250 €/ha/yr.
        /// </summary>
        public static double Normalize(double eurosPerHectare)
        {
            double range = MaxEurosPerHectare - MinEurosPerHectare;
            double t = (eurosPerHectare - MinEurosPerHectare) / range;
            if (t < 0.0) return 0.0;
            if (t > 1.0) return 1.0;
            return t;
        }
    }
}
