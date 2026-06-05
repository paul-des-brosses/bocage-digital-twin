using Bocage.Decision;
using Bocage.Decision.Outcomes;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests for the E9 surfacing classifier. With the model-derived projector,
    /// Surfacing is a pure function of the projected long-term outcome: it maps
    /// the (profit sign, biodiversity sign) pair to a Kind and decides whether to
    /// interrupt. These tests drive it with hand-built outcome distributions so
    /// they target the classification logic itself; the rec → projected-sign
    /// mapping is covered by <see cref="ModelOutcomeProjectorTests"/> and
    /// <see cref="BalancedRecommendationsTests"/>.
    /// </summary>
    public sealed class RecommendationSurfacingTests
    {
        // A flat long-horizon distribution with the given expected deltas
        // (worst = expected = best, which trivially respects the ordering).
        private static OutcomeDistribution Long(double profit, double biodiversity)
            => new OutcomeDistribution(365, profit, profit, profit, biodiversity, biodiversity, biodiversity);

        [Test]
        public void Classify_winwin_when_neither_dimension_worsens()
        {
            Assert.AreEqual(RecommendationSurfacing.Kind.WinWin, RecommendationSurfacing.Classify(Long(50, 0.05)));
            Assert.AreEqual(RecommendationSurfacing.Kind.WinWin, RecommendationSurfacing.Classify(Long(0, 0)));
            Assert.IsFalse(RecommendationSurfacing.IsTradeoff(Long(50, 0.05)));
        }

        [Test]
        public void Classify_economic_tradeoff_when_profit_up_biodiversity_down()
        {
            Assert.AreEqual(RecommendationSurfacing.Kind.EconomicTradeoff, RecommendationSurfacing.Classify(Long(50, -0.05)));
            Assert.IsTrue(RecommendationSurfacing.IsTradeoff(Long(50, -0.05)));
        }

        [Test]
        public void Classify_ecological_tradeoff_when_biodiversity_up_profit_down()
        {
            Assert.AreEqual(RecommendationSurfacing.Kind.EcologicalTradeoff, RecommendationSurfacing.Classify(Long(-50, 0.05)));
        }

        [Test]
        public void Classify_loselose_when_both_worsen()
        {
            Assert.AreEqual(RecommendationSurfacing.Kind.LoseLose, RecommendationSurfacing.Classify(Long(-50, -0.05)));
        }

        [Test]
        public void WinWin_auto_pops_regardless_of_biodiversity()
        {
            Assert.IsTrue(RecommendationSurfacing.ShouldAutoPopup(Long(50, 0.05), biodiversity: 0.6));
            Assert.IsTrue(RecommendationSurfacing.ShouldAutoPopup(Long(50, 0.05), biodiversity: 0.1));
        }

        [Test]
        public void Economic_tradeoff_never_auto_pops()
        {
            Assert.IsFalse(RecommendationSurfacing.ShouldAutoPopup(Long(50, -0.05), biodiversity: 0.6));
            Assert.IsFalse(RecommendationSurfacing.ShouldAutoPopup(Long(50, -0.05), biodiversity: 0.1));
        }

        [Test]
        public void Ecological_tradeoff_auto_pops_only_when_biodiversity_critical()
        {
            // Eco trade-off (biodiv up, profit down) escalates to a popup ONLY when
            // biodiversity is below the critical threshold (durable-damage tipping
            // point); above it, it waits passively in the list.
            Assert.IsFalse(RecommendationSurfacing.ShouldAutoPopup(Long(-50, 0.05), biodiversity: 0.6));
            Assert.IsTrue(RecommendationSurfacing.ShouldAutoPopup(Long(-50, 0.05),
                biodiversity: RecommendationEngine.BiodiversityCriticalThreshold - 0.01));
        }
    }
}
