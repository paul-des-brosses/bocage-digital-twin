using Bocage.Decision;
using Bocage.Decision.Recommendations;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests for the E9 surfacing classifier: a recommendation is win-win (popup)
    /// when neither projected dimension worsens, and a trade-off (passive list +
    /// « compromis » marker) otherwise. The economic counter-recommendations must
    /// classify as trade-offs; the ecological ones as win-wins.
    /// </summary>
    public sealed class RecommendationSurfacingTests
    {
        [Test]
        public void Economic_recommendations_classify_as_tradeoff()
        {
            Assert.AreEqual(RecommendationSurfacing.Kind.EconomicTradeoff,
                RecommendationSurfacing.Classify(new RaiseInputsRecommendation(1, "e")));
            Assert.AreEqual(RecommendationSurfacing.Kind.EconomicTradeoff,
                RecommendationSurfacing.Classify(new IncreaseHedgeRemovalRecommendation(1, "e")));
            Assert.IsTrue(RecommendationSurfacing.IsTradeoff(new RaiseInputsRecommendation(1, "e")));
            Assert.IsTrue(RecommendationSurfacing.IsTradeoff(new IncreaseHedgeRemovalRecommendation(1, "e")));
        }

        [Test]
        public void Ecological_recommendations_classify_as_winwin()
        {
            foreach (var rec in new IRecommendation[]
            {
                new ReduceInputsRecommendation(1, "e"),
                new SowCoverCropsRecommendation(1, "e"),
                new RestoreResidueRecommendation(1, "e"),
                new ReduceHedgeRemovalRecommendation(1, "e"),
                new PlantHedgesRecommendation(1, "e"),
                new IrrigationAdviceRecommendation(1, "e"),
            })
            {
                Assert.AreEqual(RecommendationSurfacing.Kind.WinWin,
                    RecommendationSurfacing.Classify(rec), rec.Id);
                Assert.IsFalse(RecommendationSurfacing.IsTradeoff(rec), rec.Id);
            }
        }

        [Test]
        public void WinWin_auto_pops_economic_tradeoff_does_not()
        {
            // Win-win interrupts regardless of biodiversity.
            Assert.IsTrue(RecommendationSurfacing.ShouldAutoPopup(
                new SowCoverCropsRecommendation(1, "e"), biodiversity: 0.6));
            // Economic trade-off stays in the list even when biodiversity is fine
            // AND even when it is critical (escalation only applies to ecological
            // trade-offs).
            Assert.IsFalse(RecommendationSurfacing.ShouldAutoPopup(
                new RaiseInputsRecommendation(1, "e"), biodiversity: 0.6));
            Assert.IsFalse(RecommendationSurfacing.ShouldAutoPopup(
                new RaiseInputsRecommendation(1, "e"), biodiversity: 0.1));
        }
    }
}
