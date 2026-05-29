using Bocage.Indicators.Hero;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the post-E5 <see cref="BiodiversityCompositeIndicator"/>,
    /// which aggregates the three FaunaDynamicsRule factors (habitat,
    /// eau, intrants) with weights 40/25/35 (ADR #51). Tests target
    /// (a) the weight algebra at known scenario states, (b) the
    /// monotonic responses to each individual factor, and (c) the
    /// three normalisation helpers.
    /// </summary>
    public sealed class BiodiversityCompositeIndicatorTests
    {
        [Test]
        public void Compute_at_baseline_sits_near_reference_anchor()
        {
            // Default model + default scenario:
            //   habitat factor = ComputeHabitatFactor(90)  = 1.000 → norm ≈ 0.5556
            //   water factor   = ComputeWaterFactor(2)     = 1.000 → norm  = 1.0
            //   inputs factor  = ComputeInputsFactor(1.0)  = 1.000 → norm ≈ 0.8571
            //   composite = 0.40 × 0.5556 + 0.25 × 1.0 + 0.35 × 0.8571 ≈ 0.7722
            var model = new EcosystemModel();
            var scenario = new ScenarioContext();
            double score = BiodiversityCompositeIndicator.Compute(model, scenario);
            Assert.That(score, Is.EqualTo(0.7722).Within(0.005),
                "Baseline composite should be ~0.77. Got " + score);
        }

        [Test]
        public void Compute_full_collapse_returns_low_score()
        {
            // Habitat and water collapsed AND intensive farming → all 3
            // factors at their floor: habitat 0.5 → norm 0, water 0.5
            // → norm 0, inputs 0.4 → norm 0. Composite ≈ 0.
            var model = new EcosystemModel(
                initialHedgerowDensity: 0.0,
                initialWaterTableDepth: 15.0,
                initialFaunaPopulation: 0.05);
            var scenario = new ScenarioContext(initialInputIntensityFactor: 5.0);
            double score = BiodiversityCompositeIndicator.Compute(model, scenario);
            Assert.That(score, Is.LessThan(0.05),
                "Full-collapse state should give a near-zero score. Got " + score);
        }

        [Test]
        public void Compute_full_bocage_saturates_at_one()
        {
            // Hyper-bocage + shallow water + bio extensive intensity 0.5:
            //   habitat 1.4 → norm 1.0
            //   water 1.0 → norm 1.0
            //   inputs 1.1 → norm 1.0
            //   composite = 1.0
            var model = new EcosystemModel(
                initialHedgerowDensity: 200.0,
                initialWaterTableDepth: 0.5,
                initialFaunaPopulation: 1.5);
            var scenario = new ScenarioContext(initialInputIntensityFactor: 0.5);
            double score = BiodiversityCompositeIndicator.Compute(model, scenario);
            Assert.That(score, Is.EqualTo(1.0).Within(1e-6),
                "Hyper-bocage + bio state should saturate at 1.0. Got " + score);
        }

        [Test]
        public void Compute_monotonic_in_hedge_density()
        {
            var sparse = new EcosystemModel(initialHedgerowDensity: 50.0);
            var dense = new EcosystemModel(initialHedgerowDensity: 130.0);
            var scenario = new ScenarioContext();
            Assert.Less(
                BiodiversityCompositeIndicator.Compute(sparse, scenario),
                BiodiversityCompositeIndicator.Compute(dense, scenario));
        }

        [Test]
        public void Compute_monotonic_inverse_in_water_depth()
        {
            // Deeper water should DECREASE the composite — the water
            // factor declines linearly past the 3 m critical depth.
            var shallow = new EcosystemModel(initialWaterTableDepth: 2.0);
            var deep = new EcosystemModel(initialWaterTableDepth: 9.0);
            var scenario = new ScenarioContext();
            Assert.Greater(
                BiodiversityCompositeIndicator.Compute(shallow, scenario),
                BiodiversityCompositeIndicator.Compute(deep, scenario));
        }

        [Test]
        public void Compute_monotonic_inverse_in_input_intensity()
        {
            // Higher input intensity should DECREASE the composite —
            // matches the post-E5 weight shift toward intrants (35 %).
            var model = new EcosystemModel();
            var bio = new ScenarioContext(initialInputIntensityFactor: 0.5);
            var conventional = new ScenarioContext(initialInputIntensityFactor: 1.0);
            var intensive = new ScenarioContext(initialInputIntensityFactor: 2.0);

            double sBio = BiodiversityCompositeIndicator.Compute(model, bio);
            double sConv = BiodiversityCompositeIndicator.Compute(model, conventional);
            double sInt = BiodiversityCompositeIndicator.Compute(model, intensive);

            Assert.Greater(sBio, sConv, "Bio should score higher than conventional");
            Assert.Greater(sConv, sInt, "Conventional should score higher than intensive");
        }

        [Test]
        public void Weights_sum_to_one()
        {
            // The contract: the composite is unit-range by construction
            // because each normalised input is in [0,1] and weights sum
            // to exactly 1. Guard against future drift.
            double sum = BiodiversityCompositeIndicator.HabitatWeight
                       + BiodiversityCompositeIndicator.WaterWeight
                       + BiodiversityCompositeIndicator.InputsWeight;
            Assert.AreEqual(1.0, sum, 1e-9);
        }

        [Test]
        public void NormalizeHabitat_at_baseline_returns_just_above_half()
        {
            // Habitat factor at the Perche reference 90 m/ha is 1.0,
            // which lands at (1.0 − 0.5) / (1.4 − 0.5) ≈ 0.5556 on the
            // normalised scale.
            Assert.AreEqual(0.5556, BiodiversityCompositeIndicator.NormalizeHabitat(1.0), 1e-3);
        }

        [Test]
        public void NormalizeWater_at_full_factor_returns_one()
        {
            // Water factor 1.0 maps to 1.0 (full habitat).
            Assert.AreEqual(1.0, BiodiversityCompositeIndicator.NormalizeWater(1.0), 1e-9);
        }

        [Test]
        public void NormalizeWater_at_floor_returns_zero()
        {
            // Water factor 0.5 (extreme drought floor) maps to 0.0.
            Assert.AreEqual(0.0, BiodiversityCompositeIndicator.NormalizeWater(0.5), 1e-9);
        }

        [Test]
        public void NormalizeInputs_at_neutral_returns_just_below_nine_tenths()
        {
            // Inputs factor at intensity 1.0 is 1.0, which maps to
            // (1.0 − 0.4) / (1.1 − 0.4) ≈ 0.8571 on the normalised scale.
            Assert.AreEqual(0.8571, BiodiversityCompositeIndicator.NormalizeInputs(1.0), 1e-3);
        }

        [Test]
        public void NormalizeInputs_at_bio_saturates_at_one()
        {
            // Inputs factor 1.1 (bio extensive at intensity 0.5) is the
            // top of the normalisation range and maps to exactly 1.0.
            Assert.AreEqual(1.0, BiodiversityCompositeIndicator.NormalizeInputs(1.1), 1e-9);
        }

        [Test]
        public void Normalize_clamps_out_of_range()
        {
            Assert.AreEqual(0.0, BiodiversityCompositeIndicator.Normalize(-0.5), 1e-9);
            Assert.AreEqual(1.0, BiodiversityCompositeIndicator.Normalize(2.0), 1e-9);
        }
    }
}
