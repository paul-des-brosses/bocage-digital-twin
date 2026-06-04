namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Stateful latch for the « horizon de rentabilité » metric
    /// (chantier E5 / ADR #50, refondu E8). Watches the NET tech value
    /// — the cumulative operational gain of the real run over the
    /// shadow run (<see cref="CumulativeTechValueIndicator"/>) MINUS the
    /// cumulative upfront action investment
    /// (<see cref="Bocage.Decision.DecisionJournal.TotalInvestmentEurosPerHectare"/>)
    /// — and latches the first simulated day on which it reaches
    /// break-even (NET ≥ 0), provided at least one investment exists to
    /// amortise.
    /// <para>
    /// <b>Why a latch, not a second integral.</b> Before E8 this class
    /// kept its OWN gated copy of the <c>(real − shadow) / 365</c>
    /// integral, duplicating <see cref="CumulativeTechValueIndicator"/>.
    /// The two could silently diverge (the Hero KPI integrates from day 0,
    /// this one only after the first investment), so the payback day and
    /// the displayed NET could disagree. Now this indicator owns no
    /// integral: it is fed the very NET the Hero KPI shows, so payback
    /// and displayed value can never contradict each other.
    /// </para>
    /// <para>
    /// Instance-stateful (the latch depends on the trajectory, not the
    /// current state). The owner (<c>SimulationRunner</c>) calls
    /// <see cref="Update"/> once per simulated day after both engines
    /// have advanced. The
    /// « payback » concept is the FIRST day the NET matched the bill,
    /// not a moving target: once latched, <see cref="HorizonReachedOnDay"/>
    /// stays fixed even if the NET later regresses negative. Pure C#, no
    /// Unity dependency — Couche 04.
    /// </para>
    /// </summary>
    public sealed class InvestmentHorizonIndicator
    {
        public const double DaysPerYear = 365.0;

        private int _horizonReachedOnDay = -1; // sentinel: -1 == not reached

        /// <summary>
        /// First simulated day on which the NET tech value first reached
        /// break-even (≥ 0) while an investment existed, or <c>-1</c> if
        /// not yet reached. Latched on first crossing.
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
        /// raw read, but UI bindings must show « Non atteint » (or
        /// « Sans objet » when no investment exists) instead of a numeric
        /// value while the flag is false.
        /// </summary>
        public double HorizonYears => _horizonReachedOnDay >= 0 ? _horizonReachedOnDay / DaysPerYear : 0.0;

        /// <summary>
        /// Advances the latch by one tick. <paramref name="netTechValueEurosPerHa"/>
        /// is the NET tech value displayed by the Hero KPI (cumulative
        /// operational gain minus total action investment).
        /// <paramref name="totalInvestmentEurosPerHa"/> gates the latch:
        /// while it is zero there is no capital to amortise, so the
        /// horizon stays « Sans objet » no matter how positive the NET is
        /// (a positive NET with no investment is pure free-slider value,
        /// not a payback). Once an investment exists, the first day the
        /// NET is non-negative is latched.
        /// </summary>
        public void Update(double netTechValueEurosPerHa, double totalInvestmentEurosPerHa, int currentDay)
        {
            if (totalInvestmentEurosPerHa <= 0.0) return;
            if (_horizonReachedOnDay < 0 && netTechValueEurosPerHa >= 0.0)
            {
                _horizonReachedOnDay = currentDay;
            }
        }

        /// <summary>
        /// Wipes the latched horizon back to « not reached », for a clean
        /// restart of the trajectory.
        /// </summary>
        public void Reset()
        {
            _horizonReachedOnDay = -1;
        }
    }
}
