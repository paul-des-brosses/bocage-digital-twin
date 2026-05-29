namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Stateful accumulator for the « horizon de rentabilité » metric
    /// (chantier E5 / ADR #50). Each tick after the first investment
    /// landed, integrates the per-day share of the integrated
    /// profitability delta between the real run (with tech actions
    /// applied) and the shadow run (same seed, no tech actions), and
    /// reports the first simulated day on which the cumulated delta
    /// matches or exceeds the cumulative <c>TotalInvestment</c> from
    /// <see cref="Bocage.Decision.DecisionJournal.TotalInvestmentEurosPerHectare"/>.
    /// <para>
    /// The indicator is INSTANCE-stateful rather than static because
    /// the cumulative integral cannot be derived from the current
    /// model state alone — it depends on the trajectory since
    /// <c>cumulProfitDelta == 0</c>. The owner (typically
    /// <c>SimulationRunner</c>) calls <see cref="Update"/> once per
    /// tick after both engines have advanced, and <see cref="Reset"/>
    /// at every Rebuild so day-0 starts from a clean cumul. Pure C#,
    /// no Unity dependency — Couche 04.
    /// </para>
    /// <para>
    /// <b>Conventions</b>. <c>IntegratedProfitabilityIndicator.Compute</c>
    /// returns an annualised value in € / ha / year. The per-day
    /// contribution to the cumulative integral is therefore
    /// <c>(realAnnualised − shadowAnnualised) / 365</c>. Once the
    /// cumul crosses the total investment, <see cref="HorizonReachedOnDay"/>
    /// is latched on the crossing day and stays fixed even if the
    /// delta later regresses — the « payback » concept is the FIRST
    /// time the integral matches the bill, not a moving target. If
    /// <c>TotalInvestment == 0</c> (no investment yet) the accumulator
    /// stays idle.
    /// </para>
    /// </summary>
    public sealed class InvestmentHorizonIndicator
    {
        public const double DaysPerYear = 365.0;

        private double _cumulativeProfitDeltaEurosPerHa;
        private int _horizonReachedOnDay = -1; // sentinel: -1 == not reached

        /// <summary>
        /// Running integral of <c>(realProfitAnnualised − shadowProfitAnnualised) / 365</c>
        /// since the first investment landed, in € / ha. Monotonic only
        /// when the real run consistently outperforms the shadow — in
        /// realistic runs it oscillates around zero before crossing.
        /// </summary>
        public double CumulativeProfitDeltaEurosPerHa => _cumulativeProfitDeltaEurosPerHa;

        /// <summary>
        /// First simulated day on which the cumul caught up with the
        /// total investment, or <c>-1</c> if not yet reached. Latched
        /// on first crossing.
        /// </summary>
        public int HorizonReachedOnDay => _horizonReachedOnDay;

        /// <summary>
        /// True when <see cref="HorizonReachedOnDay"/> has been latched.
        /// </summary>
        public bool IsHorizonReached => _horizonReachedOnDay >= 0;

        /// <summary>
        /// Horizon in simulated years (<see cref="HorizonReachedOnDay"/>
        /// divided by 365). Defined only when <see cref="IsHorizonReached"/>;
        /// callers should branch on that flag before consuming.
        /// Returns 0 when not reached so subscribers don't blow up on a
        /// raw read, but UI bindings must show « Non encore atteint »
        /// instead of a numeric value while the flag is false.
        /// </summary>
        public double HorizonYears => _horizonReachedOnDay >= 0 ? _horizonReachedOnDay / DaysPerYear : 0.0;

        /// <summary>
        /// Advances the accumulator by one tick. <paramref name="realProfitAnnualised"/>
        /// and <paramref name="shadowProfitAnnualised"/> are the
        /// outputs of <see cref="IntegratedProfitabilityIndicator.Compute"/>
        /// on the real and shadow models respectively. The contribution
        /// of this tick to the integral is divided by 365 to convert
        /// from annualised to daily. The accumulator stays idle while
        /// <paramref name="totalInvestmentEurosPerHa"/> is zero — no
        /// investment to amortise.
        /// </summary>
        public void Update(double realProfitAnnualised, double shadowProfitAnnualised,
            double totalInvestmentEurosPerHa, int currentDay)
        {
            if (totalInvestmentEurosPerHa <= 0.0) return;
            _cumulativeProfitDeltaEurosPerHa += (realProfitAnnualised - shadowProfitAnnualised) / DaysPerYear;
            if (_horizonReachedOnDay < 0 && _cumulativeProfitDeltaEurosPerHa >= totalInvestmentEurosPerHa)
            {
                _horizonReachedOnDay = currentDay;
            }
        }

        /// <summary>
        /// Wipes the cumul and the latched horizon back to day 0. Called
        /// from <c>SimulationRunner.Rebuild</c> so a fresh trajectory
        /// starts with a clean integral.
        /// </summary>
        public void Reset()
        {
            _cumulativeProfitDeltaEurosPerHa = 0.0;
            _horizonReachedOnDay = -1;
        }
    }
}
