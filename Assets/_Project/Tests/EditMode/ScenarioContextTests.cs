using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class ScenarioContextTests
    {
        [Test]
        public void DefaultsAreNeutral()
        {
            var ctx = new ScenarioContext();
            Assert.AreEqual(0.0, ctx.TemperatureAnomalyC.Current);
            Assert.AreEqual(0.0, ctx.PrecipitationAnomalyPercent.Current);
            Assert.AreEqual(0.0, ctx.HedgeRemovalRate.Current);
            Assert.AreEqual(1.0, ctx.InputIntensityFactor.Current); // 1 = reference
            Assert.AreEqual(0.0, ctx.MaecCoveragePercent.Current);
            Assert.AreEqual(0.0, ctx.PseSubsidyRate.Current);
            Assert.AreEqual(365, ctx.HorizonInDays);
        }

        [Test]
        public void TickAdvancesAllChildTransitions()
        {
            var ctx = new ScenarioContext();
            ctx.TemperatureAnomalyC.SetTarget(3.0, durationInDays: 10);
            ctx.HedgeRemovalRate.SetTarget(5.0, durationInDays: 10);

            for (int i = 0; i < 10; i++) ctx.Tick();

            Assert.AreEqual(3.0, ctx.TemperatureAnomalyC.Current, 1e-9);
            Assert.AreEqual(5.0, ctx.HedgeRemovalRate.Current, 1e-9);
        }
    }
}
