using Bocage.Decision.Outcomes;
using Bocage.Decision.Recommendations;
using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests the model-derived projector (chantier modèle vivant): it must
    /// simulate the lever forward and produce a state-dependent, sign-correct
    /// projection, with a worst &lt;= expected &lt;= best band.
    /// </summary>
    public sealed class ModelOutcomeProjectorTests
    {
        private static OutcomeDistribution[] Project(IRecommendation rec, ScenarioContext scenario)
        {
            var model = new EcosystemModel();
            var weather = SeasonalWeatherDataDefaults.MortagneAuPerche();
            return ModelOutcomeProjector.Project(
                rec, model, scenario, 123456789UL, weather,
                IntegratedProfitabilityIndicator.Compute,
                BiodiversityCompositeIndicator.Compute);
        }

        [Test]
        public void ReduceInputs_FromIntensiveState_ProjectsBiodiversityGainLongTerm()
        {
            var scenario = new ScenarioContext(initialInputIntensityFactor: 1.5);
            var longTerm = Project(new ReduceInputsRecommendation(0, "evt#0"), scenario)[1];

            Assert.That(longTerm.BiodiversityDeltaExpected, Is.GreaterThan(0.0),
                "Reducing inputs from an intensive baseline should project a positive long-term "
                + "biodiversity delta. Got " + longTerm.BiodiversityDeltaExpected);
        }

        [Test]
        public void RaiseInputs_FromExtensiveState_ProjectsBiodiversityLossLongTerm()
        {
            var scenario = new ScenarioContext(initialInputIntensityFactor: 0.6);
            var longTerm = Project(new RaiseInputsRecommendation(0, "evt#0"), scenario)[1];

            Assert.That(longTerm.BiodiversityDeltaExpected, Is.LessThan(0.0),
                "Raising inputs should project a negative long-term biodiversity delta. Got "
                + longTerm.BiodiversityDeltaExpected);
        }

        [Test]
        public void Projection_RespectsWorstExpectedBestOrdering()
        {
            var scenario = new ScenarioContext(initialInputIntensityFactor: 1.5);
            var outcomes = Project(new ReduceInputsRecommendation(0, "evt#0"), scenario);
            foreach (var o in outcomes)
            {
                Assert.That(o.ProfitDeltaWorstCase, Is.LessThanOrEqualTo(o.ProfitDeltaExpected));
                Assert.That(o.ProfitDeltaExpected, Is.LessThanOrEqualTo(o.ProfitDeltaBestCase));
                Assert.That(o.BiodiversityDeltaWorstCase, Is.LessThanOrEqualTo(o.BiodiversityDeltaExpected));
                Assert.That(o.BiodiversityDeltaExpected, Is.LessThanOrEqualTo(o.BiodiversityDeltaBestCase));
            }
        }
    }
}
