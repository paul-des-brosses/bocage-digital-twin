using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="BiodiversityCompositeIndicator"/>. The
    /// indicator is a pure read of model state + a weighted sum, so the
    /// tests focus on (a) the weight algebra at known model states and
    /// (b) the monotonic responses to single-variable changes.
    /// </summary>
    public sealed class BiodiversityCompositeIndicatorTests
    {
        [Test]
        public void Compute_at_baseline_sits_near_reference_anchor()
        {
            // Default model: hedge 90, water 2, fauna 1.0.
            // normFauna = (1.0 - 0) / (1.5 - 0) ≈ 0.667
            // normHedge = (90 - 40) / (150 - 40) ≈ 0.4545
            // normWaterInv = 1 - (2 - 0.5) / (6 - 0.5) ≈ 0.727
            // composite = 0.5 × 0.667 + 0.3 × 0.4545 + 0.2 × 0.727 ≈ 0.6155
            var model = new EcosystemModel();
            double score = BiodiversityCompositeIndicator.Compute(model);
            Assert.That(score, Is.EqualTo(0.6155).Within(0.01),
                "Baseline composite should be ~0.62. Got " + score);
        }

        [Test]
        public void Compute_collapsed_state_returns_low_score()
        {
            // No hedges, deep water, fauna near zero.
            // normFauna ≈ 0, normHedge clamped 0, normWaterInv clamped 0.
            // composite ≈ 0.
            var model = new EcosystemModel(
                initialHedgerowDensity: 0.0,
                initialWaterTableDepth: 10.0,
                initialFaunaPopulation: 0.05);
            double score = BiodiversityCompositeIndicator.Compute(model);
            Assert.That(score, Is.LessThan(0.05),
                "Collapsed state should give a near-zero score. Got " + score);
        }

        [Test]
        public void Compute_lush_state_returns_high_score()
        {
            // Dense hedges, shallow water, lush fauna.
            // normFauna saturates at 1, normHedge at 1, normWaterInv at 1.
            // composite = 0.5 + 0.3 + 0.2 = 1.0.
            var model = new EcosystemModel(
                initialHedgerowDensity: 200.0,
                initialWaterTableDepth: 0.5,
                initialFaunaPopulation: 1.5);
            double score = BiodiversityCompositeIndicator.Compute(model);
            Assert.That(score, Is.EqualTo(1.0).Within(1e-6),
                "Lush state should saturate at 1.0. Got " + score);
        }

        [Test]
        public void Compute_monotonic_in_fauna()
        {
            var low = new EcosystemModel(initialFaunaPopulation: 0.3);
            var high = new EcosystemModel(initialFaunaPopulation: 1.2);
            Assert.Less(
                BiodiversityCompositeIndicator.Compute(low),
                BiodiversityCompositeIndicator.Compute(high));
        }

        [Test]
        public void Compute_monotonic_in_hedge_density()
        {
            var sparse = new EcosystemModel(initialHedgerowDensity: 50.0);
            var dense = new EcosystemModel(initialHedgerowDensity: 130.0);
            Assert.Less(
                BiodiversityCompositeIndicator.Compute(sparse),
                BiodiversityCompositeIndicator.Compute(dense));
        }

        [Test]
        public void Compute_monotonic_inverse_in_water_depth()
        {
            // Deeper water should DECREASE the composite (worse for fauna).
            var shallow = new EcosystemModel(initialWaterTableDepth: 1.0);
            var deep = new EcosystemModel(initialWaterTableDepth: 5.0);
            Assert.Greater(
                BiodiversityCompositeIndicator.Compute(shallow),
                BiodiversityCompositeIndicator.Compute(deep));
        }

        [Test]
        public void Weights_sum_to_one()
        {
            // The contract: the composite is unit-range by construction
            // because each normalised input is in [0,1] and weights sum
            // to exactly 1. Guard against future drift.
            double sum = BiodiversityCompositeIndicator.FaunaWeight
                       + BiodiversityCompositeIndicator.HedgerowWeight
                       + BiodiversityCompositeIndicator.WaterWeight;
            Assert.AreEqual(1.0, sum, 1e-9);
        }

        [Test]
        public void NormalizeFauna_at_max_index_returns_one()
        {
            Assert.AreEqual(1.0,
                BiodiversityCompositeIndicator.NormalizeFauna(
                    BiodiversityCompositeIndicator.FaunaMaxIndex),
                1e-9);
        }

        [Test]
        public void NormalizeFauna_clamps_above_max()
        {
            Assert.AreEqual(1.0,
                BiodiversityCompositeIndicator.NormalizeFauna(5.0),
                1e-9);
        }

        [Test]
        public void Normalize_clamps_out_of_range()
        {
            Assert.AreEqual(0.0, BiodiversityCompositeIndicator.Normalize(-0.5), 1e-9);
            Assert.AreEqual(1.0, BiodiversityCompositeIndicator.Normalize(2.0), 1e-9);
        }
    }
}
