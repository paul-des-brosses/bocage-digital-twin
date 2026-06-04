using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public class ScenarioContextShadowTests
    {
        [Test]
        public void CreateFrozenShadowFrom_SharesExogenous_FreezesFarmerDecisions()
        {
            var real = new ScenarioContext(
                initialTemperatureAnomalyC: 1.5,
                initialPrecipitationAnomalyPercent: -10.0,
                initialHedgeRemovalRate: 4.0,
                initialInputIntensityFactor: 1.6,
                initialMaecCoveragePercent: 30.0,
                initialPseSubsidyRate: 0.5,
                initialCoverCropsCoveragePercent: 20.0,
                initialResidueRestitutionPercent: 50.0,
                startingMonth: 4,
                horizonInDays: 730);

            var shadow = ScenarioContext.CreateFrozenShadowFrom(real);

            // Exogenous (climate + policy): SAME instances, so they track the
            // real run in lockstep.
            Assert.AreSame(real.TemperatureAnomalyC, shadow.TemperatureAnomalyC);
            Assert.AreSame(real.PrecipitationAnomalyPercent, shadow.PrecipitationAnomalyPercent);
            Assert.AreSame(real.MaecCoveragePercent, shadow.MaecCoveragePercent);
            Assert.AreSame(real.PseSubsidyRate, shadow.PseSubsidyRate);

            // Farmer decisions: independent FROZEN copies at the launch value.
            Assert.AreNotSame(real.HedgeRemovalRate, shadow.HedgeRemovalRate);
            Assert.AreNotSame(real.InputIntensityFactor, shadow.InputIntensityFactor);
            Assert.AreNotSame(real.CoverCropsCoveragePercent, shadow.CoverCropsCoveragePercent);
            Assert.AreNotSame(real.ResidueRestitutionPercent, shadow.ResidueRestitutionPercent);

            Assert.AreEqual(4.0, shadow.HedgeRemovalRate.Current, 1e-9);
            Assert.AreEqual(1.6, shadow.InputIntensityFactor.Current, 1e-9);
            Assert.AreEqual(20.0, shadow.CoverCropsCoveragePercent.Current, 1e-9);
            Assert.AreEqual(50.0, shadow.ResidueRestitutionPercent.Current, 1e-9);

            // Run-level values copied.
            Assert.AreEqual(4, shadow.StartingMonth);
            Assert.AreEqual(730, shadow.HorizonInDays);
        }
    }
}
