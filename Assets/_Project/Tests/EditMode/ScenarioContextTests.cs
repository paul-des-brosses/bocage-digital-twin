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
            Assert.AreEqual(0.0, ctx.ClimateStress.Current);
            Assert.AreEqual(0.0, ctx.AgriculturalPressure.Current);
            Assert.AreEqual(0.0, ctx.RegulatoryConstraints.Current);
            Assert.AreEqual(365, ctx.HorizonInDays);
        }

        [Test]
        public void TickAdvancesAllChildTransitions()
        {
            var ctx = new ScenarioContext();
            ctx.ClimateStress.SetTarget(1.0, durationInDays: 10);
            ctx.AgriculturalPressure.SetTarget(0.5, durationInDays: 10);

            for (int i = 0; i < 10; i++) ctx.Tick();

            Assert.AreEqual(1.0, ctx.ClimateStress.Current, 1e-9);
            Assert.AreEqual(0.5, ctx.AgriculturalPressure.Current, 1e-9);
        }
    }
}
