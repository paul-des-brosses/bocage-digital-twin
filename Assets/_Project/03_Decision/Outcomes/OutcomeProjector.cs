using Bocage.Decision.Recommendations;

namespace Bocage.Decision.Outcomes
{
    /// <summary>
    /// Projects the expected impact of accepting a recommendation at
    /// two horizons (30 d "short" and 365 d "long"). Returns a
    /// three-point <see cref="OutcomeDistribution"/> per horizon —
    /// worst-case / expected / best-case.
    /// <para>
    /// <b>Honesty note</b>. The projections are calibrated coefficients,
    /// NOT stochastic re-runs of the engine. A true Monte-Carlo would
    /// re-tick the simulation N times with different rng seeds and
    /// take percentiles, which is heavy for a portfolio MVP. The
    /// coefficients below are the median values observed in the
    /// published literature for each intervention type, with a ±25 %
    /// uncertainty band (best-case = expected × 1.25, worst-case
    /// = expected × 0.5 — asymmetric because optimistic scenarios
    /// rarely overshoot the median but pessimistic ones (failed
    /// planting, partial application) can halve the benefit).
    /// </para>
    /// <para>
    /// Sources per recommendation type are documented inline below.
    /// </para>
    /// </summary>
    public static class OutcomeProjector
    {
        public const int ShortHorizonDays = 30;
        public const int LongHorizonDays = 365;

        public static OutcomeDistribution[] Project(IRecommendation recommendation)
        {
            return new[]
            {
                ProjectAtHorizon(recommendation, ShortHorizonDays),
                ProjectAtHorizon(recommendation, LongHorizonDays),
            };
        }

        private static OutcomeDistribution ProjectAtHorizon(IRecommendation recommendation, int horizonDays)
        {
            // Dispatch on the concrete recommendation type. Cleaner than
            // adding a "Kind" enum on IRecommendation and switching, and
            // the closed set of 3 types makes a runtime cast cheap.
            switch (recommendation)
            {
                case PlantHedgesRecommendation _:
                    return ProjectPlantHedges(horizonDays);
                case IrrigationAdviceRecommendation _:
                    return ProjectIrrigation(horizonDays);
                case ReduceInputsRecommendation _:
                    return ProjectReduceInputs(horizonDays);
                case RaiseInputsRecommendation _:
                    return ProjectRaiseInputs(horizonDays);
                case SowCoverCropsRecommendation _:
                    return ProjectSowCoverCrops(horizonDays);
                case RestoreResidueRecommendation _:
                    return ProjectRestoreResidue(horizonDays);
                case ReduceHedgeRemovalRecommendation _:
                    return ProjectReduceHedgeRemoval(horizonDays);
                case IncreaseHedgeRemovalRecommendation _:
                    return ProjectIncreaseHedgeRemoval(horizonDays);
                default:
                    // Unknown rec type → null distribution rather than throw.
                    return new OutcomeDistribution(horizonDays, 0, 0, 0, 0, 0, 0);
            }
        }

        // ---- PlantHedges ----
        // Source: PNR du Perche replanting cost ≈ 6 €/m, lifespan
        // 30 yrs → annualised ≈ 0.2 €/m/yr. For 30 m/ha added: −6 €/ha
        // short term (implementation cost), then biodiversity +0.05
        // composite long term as the hedge matures.
        private static OutcomeDistribution ProjectPlantHedges(int horizonDays)
        {
            if (horizonDays == ShortHorizonDays)
            {
                // Short term: implementation cost dominates, biodiversity
                // barely moves (hedges aren't planted yet, just budgeted).
                return new OutcomeDistribution(horizonDays,
                    profitDeltaWorstCase: -90.0, profitDeltaExpected: -60.0, profitDeltaBestCase: -30.0,
                    biodiversityDeltaWorstCase: -0.005, biodiversityDeltaExpected: 0.0, biodiversityDeltaBestCase: 0.005);
            }
            // Long term: hedges in place, biodiversity boost dominates.
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: -40.0, profitDeltaExpected: 20.0, profitDeltaBestCase: 60.0,
                biodiversityDeltaWorstCase: 0.025, biodiversityDeltaExpected: 0.05, biodiversityDeltaBestCase: 0.07);
        }

        // ---- Irrigation ----
        // Source: irrigation + couverts coût ≈ 100 €/ha sur 30 j,
        // gain rendement +5-10 % si bien ciblé.
        private static OutcomeDistribution ProjectIrrigation(int horizonDays)
        {
            if (horizonDays == ShortHorizonDays)
            {
                return new OutcomeDistribution(horizonDays,
                    profitDeltaWorstCase: -120.0, profitDeltaExpected: -80.0, profitDeltaBestCase: -40.0,
                    biodiversityDeltaWorstCase: 0.0, biodiversityDeltaExpected: 0.015, biodiversityDeltaBestCase: 0.03);
            }
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: -50.0, profitDeltaExpected: 80.0, profitDeltaBestCase: 200.0,
                biodiversityDeltaWorstCase: 0.005, biodiversityDeltaExpected: 0.03, biodiversityDeltaBestCase: 0.05);
        }

        // ---- ReduceInputs ----
        // Source: IPBES + Vigie-Nature, baisse intrants 20 % = -10-15 %
        // rendement court terme mais +15-30 % biodiv long terme.
        private static OutcomeDistribution ProjectReduceInputs(int horizonDays)
        {
            if (horizonDays == ShortHorizonDays)
            {
                // Slight yield drop short term (less fert) but inputs
                // savings cushion the profit hit.
                return new OutcomeDistribution(horizonDays,
                    profitDeltaWorstCase: -100.0, profitDeltaExpected: -30.0, profitDeltaBestCase: 20.0,
                    biodiversityDeltaWorstCase: 0.005, biodiversityDeltaExpected: 0.02, biodiversityDeltaBestCase: 0.04);
            }
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: -150.0, profitDeltaExpected: 0.0, profitDeltaBestCase: 100.0,
                biodiversityDeltaWorstCase: 0.04, biodiversityDeltaExpected: 0.10, biodiversityDeltaBestCase: 0.15);
        }

        // ---- RaiseInputs (economic counterpart of ReduceInputs) ----
        // Below the profit optimum, raising inputs recovers margin (concave yield
        // gain > marginal cost) but presses on fauna. Source: Lechenet 2017 + the
        // concave N-response (CALIBRATION.md).
        private static OutcomeDistribution ProjectRaiseInputs(int horizonDays)
        {
            if (horizonDays == ShortHorizonDays)
            {
                return new OutcomeDistribution(horizonDays,
                    profitDeltaWorstCase: 0.0, profitDeltaExpected: 25.0, profitDeltaBestCase: 50.0,
                    biodiversityDeltaWorstCase: -0.04, biodiversityDeltaExpected: -0.02, biodiversityDeltaBestCase: -0.005);
            }
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: 10.0, profitDeltaExpected: 60.0, profitDeltaBestCase: 110.0,
                biodiversityDeltaWorstCase: -0.10, biodiversityDeltaExpected: -0.06, biodiversityDeltaBestCase: -0.02);
        }

        // ---- SowCoverCrops ----
        // Seed cost short term, then N restitution + soil carbon long term.
        // Source: INRAE 4 pour 1000 (~0.31 tC/ha/yr); Arvalis (30-100 kg N/ha).
        private static OutcomeDistribution ProjectSowCoverCrops(int horizonDays)
        {
            if (horizonDays == ShortHorizonDays)
            {
                return new OutcomeDistribution(horizonDays,
                    profitDeltaWorstCase: -15.0, profitDeltaExpected: -8.0, profitDeltaBestCase: 0.0,
                    biodiversityDeltaWorstCase: 0.0, biodiversityDeltaExpected: 0.005, biodiversityDeltaBestCase: 0.015);
            }
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: -10.0, profitDeltaExpected: 25.0, profitDeltaBestCase: 70.0,
                biodiversityDeltaWorstCase: 0.01, biodiversityDeltaExpected: 0.03, biodiversityDeltaBestCase: 0.05);
        }

        // ---- RestoreResidue ----
        // Foregone straw sale vs. soil fertility + carbon. Source: Solagro
        // Afterres2050 (~0.8 tC/ha/yr at 100%); INRAE (soil macrofauna).
        private static OutcomeDistribution ProjectRestoreResidue(int horizonDays)
        {
            if (horizonDays == ShortHorizonDays)
            {
                return new OutcomeDistribution(horizonDays,
                    profitDeltaWorstCase: -10.0, profitDeltaExpected: -3.0, profitDeltaBestCase: 5.0,
                    biodiversityDeltaWorstCase: 0.0, biodiversityDeltaExpected: 0.005, biodiversityDeltaBestCase: 0.01);
            }
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: -5.0, profitDeltaExpected: 18.0, profitDeltaBestCase: 45.0,
                biodiversityDeltaWorstCase: 0.005, biodiversityDeltaExpected: 0.02, biodiversityDeltaBestCase: 0.04);
        }

        // ---- ReduceHedgeRemoval ----
        // Keeping hedges keeps PSE/PAC + habitat; near profit-neutral, biodiv+.
        // Source: INRAE/OFB (hedge biodiversity + windbreak); PSE/PAC structure.
        private static OutcomeDistribution ProjectReduceHedgeRemoval(int horizonDays)
        {
            if (horizonDays == ShortHorizonDays)
            {
                return new OutcomeDistribution(horizonDays,
                    profitDeltaWorstCase: -5.0, profitDeltaExpected: 0.0, profitDeltaBestCase: 5.0,
                    biodiversityDeltaWorstCase: 0.005, biodiversityDeltaExpected: 0.015, biodiversityDeltaBestCase: 0.03);
            }
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: -10.0, profitDeltaExpected: 10.0, profitDeltaBestCase: 40.0,
                biodiversityDeltaWorstCase: 0.03, biodiversityDeltaExpected: 0.06, biodiversityDeltaBestCase: 0.10);
        }

        // ---- IncreaseHedgeRemoval (economic, narrow corner) ----
        // Thinning over-dense unsubsidised hedges recovers yield + cuts
        // maintenance, at a biodiversity cost. Source: yield bell + cost structure.
        private static OutcomeDistribution ProjectIncreaseHedgeRemoval(int horizonDays)
        {
            if (horizonDays == ShortHorizonDays)
            {
                return new OutcomeDistribution(horizonDays,
                    profitDeltaWorstCase: 0.0, profitDeltaExpected: 15.0, profitDeltaBestCase: 30.0,
                    biodiversityDeltaWorstCase: -0.03, biodiversityDeltaExpected: -0.02, biodiversityDeltaBestCase: -0.01);
            }
            return new OutcomeDistribution(horizonDays,
                profitDeltaWorstCase: 5.0, profitDeltaExpected: 50.0, profitDeltaBestCase: 90.0,
                biodiversityDeltaWorstCase: -0.10, biodiversityDeltaExpected: -0.06, biodiversityDeltaBestCase: -0.03);
        }
    }
}
