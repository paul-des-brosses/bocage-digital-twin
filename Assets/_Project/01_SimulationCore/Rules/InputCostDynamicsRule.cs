using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily update of the input cost estimate (fertiliser, pesticide,
    /// fuel, seeds), in € per hectare per year. After the scenario
    /// refactor (sub-étape 7c.1/Option 3) the target is driven by
    /// three physical inputs: intensification factor, MAEC coverage,
    /// climate stress.
    /// <para>
    /// <b>Calibration sources (revision 2026-05-21)</b>
    /// <list type="bullet">
    ///   <item>Baseline 1200 €/ha/yr : plage CIVAM Haut-Bocage et
    ///         AFPF "grandes cultures annuelles" 1100-2000 €/ha/yr.
    ///         Médiane ≈ 1200 pour un mix bocager conventionnel.</item>
    ///   <item>MAEC réduction jusqu'à −30% à 100% de couverture :
    ///         CIVAM rapporte 76% d'économie sur fertilisants et 74%
    ///         sur phytos en système herbager extensif. Notre 30%
    ///         couvre un passage MAEC standard, pas la bascule complète
    ///         vers bio.</item>
    ///   <item>Climate surcharge : +20% sous combinaison chaleur+
    ///         sécheresse (compensation irrigation, replantation,
    ///         pesticides supplémentaires). Plage haute mais plausible
    ///         sous scénarios RCP8.5.</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class InputCostDynamicsRule : IRule
    {
        public string SubStreamId => "input-cost";

        public const double BaselineEurosPerHectarePerYear = 1200.0;
        private const double TransitionRatePerDay = 0.017;

        private const double MaecReductionPerPercent = 0.003; // 0.30 / 100%
        private const double HeatSurchargePerDegree = 0.04;   // 0.20 / 5°C
        private const double DroughtSurchargePerPercent = 0.00333; // 0.20 / 60%

        // Acute heat-stress surcharge driven by the WeatherStation daily
        // reading (chantier E2 / ADR #52). 0.5 %/canicule day with a
        // 30-day window caps the surcharge at 15 % — additive on top of
        // the scenario anomaly heat surcharge, so a worst-case scenario
        // with sustained +5 °C anomaly AND frequent 25 °C+ peaks pays
        // both penalties.
        private const double HeatStressSurchargePerDay = 0.005;
        private const double HeatStressMaxSurcharge = 0.15;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double intensityFactor = scenario.InputIntensityFactor.Current;
            if (intensityFactor < 0.5) intensityFactor = 0.5;
            if (intensityFactor > 2.0) intensityFactor = 2.0;

            double maecPct = scenario.MaecCoveragePercent.Current;
            if (maecPct < 0.0) maecPct = 0.0;
            if (maecPct > 100.0) maecPct = 100.0;
            double maecReduction = maecPct * MaecReductionPerPercent;

            double tempAnomalyC = scenario.TemperatureAnomalyC.Current;
            double precipAnomalyPct = scenario.PrecipitationAnomalyPercent.Current;
            double heatSurcharge = tempAnomalyC > 0.0 ? tempAnomalyC * HeatSurchargePerDegree : 0.0;
            if (heatSurcharge > 0.20) heatSurcharge = 0.20;
            double droughtSurcharge = precipAnomalyPct < 0.0 ? -precipAnomalyPct * DroughtSurchargePerPercent : 0.0;
            if (droughtSurcharge > 0.20) droughtSurcharge = 0.20;
            double heatStressSurcharge = ComputeHeatStressSurcharge(model.RecentHeatDayCount);
            double climateSurcharge = heatSurcharge + droughtSurcharge + heatStressSurcharge;

            double target = BaselineEurosPerHectarePerYear
                            * intensityFactor
                            * (1.0 - maecReduction)
                            * (1.0 + climateSurcharge);
            if (target < 0.0) target = 0.0;

            double current = model.InputCost;
            double next = current + TransitionRatePerDay * (target - current);
            model.SetInputCost(next);
        }

        /// <summary>
        /// Acute heat-stress surcharge driven by the rolling count of days
        /// above <see cref="EcosystemModel.HeatDayThresholdCelsius"/>
        /// (25 °C) over the last
        /// <see cref="EcosystemModel.HeatDayWindowDays"/> (30) days.
        /// Linear surcharge capped at 15 %.
        /// </summary>
        public static double ComputeHeatStressSurcharge(int recentHeatDayCount)
        {
            if (recentHeatDayCount <= 0) return 0.0;
            double surcharge = HeatStressSurchargePerDay * recentHeatDayCount;
            if (surcharge > HeatStressMaxSurcharge) surcharge = HeatStressMaxSurcharge;
            return surcharge;
        }
    }
}
