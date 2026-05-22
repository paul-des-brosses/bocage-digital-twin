namespace Bocage.Decision.Outcomes
{
    /// <summary>
    /// Three-point projection of the expected impact of accepting a
    /// recommendation at a given horizon. Worst-case / expected / best-case
    /// values bracket the uncertainty without claiming a true probability
    /// distribution — they're calibrated coefficients, not Monte-Carlo
    /// samples. The honesty note is in
    /// <see cref="OutcomeProjector"/>.
    /// <para>
    /// Two dimensions are projected:
    /// <list type="bullet">
    ///   <item><b>ProfitDelta</b> in €/ha/yr — change in integrated
    ///         profitability vs the do-nothing trajectory.</item>
    ///   <item><b>BiodiversityDelta</b> in unit-range [-1, +1] — change
    ///         in the composite biodiversity score.</item>
    /// </list>
    /// </para>
    /// </summary>
    public readonly struct OutcomeDistribution
    {
        public int HorizonInDays { get; }

        public double ProfitDeltaWorstCase { get; }
        public double ProfitDeltaExpected { get; }
        public double ProfitDeltaBestCase { get; }

        public double BiodiversityDeltaWorstCase { get; }
        public double BiodiversityDeltaExpected { get; }
        public double BiodiversityDeltaBestCase { get; }

        public OutcomeDistribution(
            int horizonInDays,
            double profitDeltaWorstCase,
            double profitDeltaExpected,
            double profitDeltaBestCase,
            double biodiversityDeltaWorstCase,
            double biodiversityDeltaExpected,
            double biodiversityDeltaBestCase)
        {
            HorizonInDays = horizonInDays;
            ProfitDeltaWorstCase = profitDeltaWorstCase;
            ProfitDeltaExpected = profitDeltaExpected;
            ProfitDeltaBestCase = profitDeltaBestCase;
            BiodiversityDeltaWorstCase = biodiversityDeltaWorstCase;
            BiodiversityDeltaExpected = biodiversityDeltaExpected;
            BiodiversityDeltaBestCase = biodiversityDeltaBestCase;
        }
    }
}
