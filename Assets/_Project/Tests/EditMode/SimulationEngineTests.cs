using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    public sealed class SimulationEngineTests
    {
        [Test]
        public void TickIncrementsDayCounter()
        {
            var engine = DefaultSimulation.Build(masterSeed: 1UL);
            for (int i = 0; i < 10; i++) engine.Tick();
            Assert.AreEqual(10, engine.Model.CurrentDay);
        }

        [Test]
        public void SameSeedAndInputsProduceIdenticalTrajectories()
        {
            var a = DefaultSimulation.Build(masterSeed: 12345UL);
            var b = DefaultSimulation.Build(masterSeed: 12345UL);

            for (int day = 0; day < 365; day++)
            {
                a.Tick();
                b.Tick();
                Assert.AreEqual(a.Model.CurrentDay, b.Model.CurrentDay);
                Assert.AreEqual(a.Model.HedgerowDensity, b.Model.HedgerowDensity);
                Assert.AreEqual(a.Model.WaterTableDepth, b.Model.WaterTableDepth);
                Assert.AreEqual(a.Model.CurrentWeather.TemperatureCelsius,
                                b.Model.CurrentWeather.TemperatureCelsius);
                Assert.AreEqual(a.Model.CurrentWeather.PrecipitationMillimeters,
                                b.Model.CurrentWeather.PrecipitationMillimeters);
            }
        }

        [Test]
        public void DifferentSeedsDivergeWithinAFewDays()
        {
            var a = DefaultSimulation.Build(masterSeed: 1UL);
            var b = DefaultSimulation.Build(masterSeed: 2UL);

            for (int day = 0; day < 30; day++)
            {
                a.Tick();
                b.Tick();
            }
            Assert.AreNotEqual(a.Model.HedgerowDensity, b.Model.HedgerowDensity);
            Assert.AreNotEqual(a.Model.WaterTableDepth, b.Model.WaterTableDepth);
        }

        [Test]
        public void RuleSubStreamsAreIndependentOfRuleOrdering()
        {
            // Build two engines with the same seed but rule lists in different
            // order, and check that each rule receives the same RNG sequence
            // regardless of position. Sub-stream isolation guarantees this.
            var modelA = new EcosystemModel();
            var ctxA = new ScenarioContext();
            var rulesA = new IRule[]
            {
                new WeatherUpdateRule(),
                new HedgerowGrowthRule(),
                new AgriculturalPressureImpactRule(),
                new WaterTableDynamicsRule(),
            };
            var engineA = new SimulationEngine(7UL, modelA, ctxA, rulesA);

            var modelB = new EcosystemModel();
            var ctxB = new ScenarioContext();
            var rulesB = new IRule[]
            {
                new WaterTableDynamicsRule(),
                new AgriculturalPressureImpactRule(),
                new WeatherUpdateRule(),
                new HedgerowGrowthRule(),
            };
            var engineB = new SimulationEngine(7UL, modelB, ctxB, rulesB);

            for (int day = 0; day < 10; day++)
            {
                engineA.Tick();
                engineB.Tick();
            }

            Assert.AreEqual(modelA.CurrentWeather.TemperatureCelsius,
                            modelB.CurrentWeather.TemperatureCelsius,
                "Weather rule should pull the same RNG values regardless of its position " +
                "in the rule list because each rule has its own sub-stream.");
            Assert.AreEqual(modelA.CurrentWeather.PrecipitationMillimeters,
                            modelB.CurrentWeather.PrecipitationMillimeters);
        }

        [Test]
        public void StateRemainsInPlausibleBoundsOver365DayRun()
        {
            var engine = DefaultSimulation.Build(masterSeed: 999UL);

            for (int day = 0; day < 365; day++)
            {
                engine.Tick();
                Assert.GreaterOrEqual(engine.Model.HedgerowDensity, 0.0);
                Assert.GreaterOrEqual(engine.Model.WaterTableDepth, 0.0);
            }
            // After a year of neutral scenario, hedgerow density should be in
            // a sensible neighbourhood of the initial value (no runaway).
            Assert.That(engine.Model.HedgerowDensity, Is.InRange(80.0, 100.0));
        }

        [Test]
        public void ScenarioTransitionsAdvanceWithEngine()
        {
            var ctx = new ScenarioContext();
            ctx.AgriculturalPressure.SetTarget(1.0, durationInDays: 10);
            var engine = DefaultSimulation.Build(masterSeed: 1UL, scenario: ctx);

            for (int i = 0; i < 10; i++) engine.Tick();

            Assert.AreEqual(1.0, ctx.AgriculturalPressure.Current, 1e-9);
            Assert.IsFalse(ctx.AgriculturalPressure.IsTransitioning);
        }
    }
}
