using System;

namespace Bocage.Decision
{
    /// <summary>
    /// Distribution d'un résultat projeté sur plusieurs réalisations météo :
    /// pire cas, attendu (médiane) et meilleur cas. La bande worst→best vient de
    /// la variabilité inter-annuelle (réalisations seedées du générateur).
    /// </summary>
    public readonly struct OutcomeDistribution
    {
        public double Worst { get; }
        public double Expected { get; }
        public double Best { get; }

        public OutcomeDistribution(double worst, double expected, double best)
        {
            Worst = worst;
            Expected = expected;
            Best = best;
        }

        /// <summary>Construit la distribution depuis des échantillons (worst=min, expected=médiane, best=max).</summary>
        public static OutcomeDistribution FromSamples(double[] samples)
        {
            if (samples == null || samples.Length == 0)
                return new OutcomeDistribution(0.0, 0.0, 0.0);
            double[] sorted = (double[])samples.Clone();
            Array.Sort(sorted);
            return new OutcomeDistribution(sorted[0], sorted[sorted.Length / 2], sorted[sorted.Length - 1]);
        }
    }

    /// <summary>Résultat projeté d'un levier : Δmarge, Δbiodiversité, Δcarbone (chacun en distribution).</summary>
    public readonly struct LeverOutcome
    {
        public OutcomeDistribution DeltaMarginEurosPerHa { get; }
        public OutcomeDistribution DeltaBiodiversity { get; }
        public OutcomeDistribution DeltaCarbonTPerHa { get; }

        public LeverOutcome(OutcomeDistribution deltaMargin, OutcomeDistribution deltaBiodiversity,
            OutcomeDistribution deltaCarbon)
        {
            DeltaMarginEurosPerHa = deltaMargin;
            DeltaBiodiversity = deltaBiodiversity;
            DeltaCarbonTPerHa = deltaCarbon;
        }
    }
}
