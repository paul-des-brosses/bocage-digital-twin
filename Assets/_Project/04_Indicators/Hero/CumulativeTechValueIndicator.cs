namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// "Apport de la techno" Hero KPI: the running integral, in € / ha, of
    /// the integrated-profitability advantage of the real run (tech decisions
    /// applied) over the shadow run (same seed/scenario, no tech decisions),
    /// accumulated from day 0.
    /// <para>
    /// <b>Why cumulative, not instantaneous.</b> A one-shot action (a drought
    /// irrigation, an input-reduction pulse) creates a transient profit gap
    /// that the biophysical EMA rules erase over the following weeks, so an
    /// instantaneous delta spikes then collapses back to 0 — a misleading
    /// reading of "what the tech is worth". Integrating the daily gap instead
    /// banks the real value each action delivered: a transient action adds a
    /// finite bump that then plateaus, while a sustained strategy keeps the
    /// cumulative growing. The number stays honest (a real model difference
    /// against a fair counterfactual) and reflects the long term, which is
    /// the horizon on which a tech strategy is actually judged.
    /// </para>
    /// <para>
    /// Instance-stateful (the integral depends on the trajectory, not the
    /// current state). The owner (<c>SimulationRunner</c>) calls
    /// <see cref="Update"/> once per simulated day after both engines have
    /// advanced, and <see cref="Reset"/> at every Rebuild. Pure C#, no Unity
    /// dependency — Couche 04.
    /// </para>
    /// </summary>
    public sealed class CumulativeTechValueIndicator
    {
        public const double DaysPerYear = 365.0;

        // Display gauge bounds (€ / ha). Asymmetric: a good strategy is
        // expected to bank more upside than a poor one erodes.
        public const double MinEurosPerHectare = -500.0;
        public const double MaxEurosPerHectare = 1500.0;

        private double _cumulativeEurosPerHa;

        /// <summary>
        /// Running integral of <c>(real − shadow)</c> annualised profit / 365,
        /// in € / ha, since day 0. Stays at 0 until a tech action diverges
        /// the two runs.
        /// </summary>
        public double CumulativeEurosPerHa => _cumulativeEurosPerHa;

        /// <summary>
        /// Adds one simulated day's contribution. The two arguments are the
        /// annualised € / ha / yr outputs of
        /// <c>IntegratedProfitabilityIndicator.Compute</c> on the real and
        /// shadow models; the daily share is the difference divided by 365.
        /// </summary>
        public void Update(double realProfitAnnualised, double shadowProfitAnnualised)
        {
            _cumulativeEurosPerHa += (realProfitAnnualised - shadowProfitAnnualised) / DaysPerYear;
        }

        /// <summary>Wipes the integral back to 0 (called on Rebuild).</summary>
        public void Reset()
        {
            _cumulativeEurosPerHa = 0.0;
        }

        /// <summary>
        /// Maps a cumulative value to <c>[0,1]</c> for a centred gauge:
        /// 0 at <see cref="MinEurosPerHectare"/>, 1 at
        /// <see cref="MaxEurosPerHectare"/>, clamped outside the range.
        /// </summary>
        public static double Normalize(double eurosPerHa)
        {
            double range = MaxEurosPerHectare - MinEurosPerHectare;
            double t = (eurosPerHa - MinEurosPerHectare) / range;
            if (t < 0.0) return 0.0;
            if (t > 1.0) return 1.0;
            return t;
        }
    }
}
