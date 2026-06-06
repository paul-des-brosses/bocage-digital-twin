using System;
using System.Collections.Generic;
using Bocage.Sensors;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using Bocage.SimulationCore.Scenario;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the soil carbon chain (chantier E3 / ADR #48):
    /// the 1-pool dynamics rule in Couche 01 and the EddyTower sensor in
    /// Couche 02. Calibration parameters (k = 1/40 yr⁻¹, input ceilings)
    /// are verified against the values documented in
    /// <c>docs/CALIBRATION.md §Carbone sol</c>.
    /// </summary>
    public sealed class SoilCarbonDynamicsRuleTests
    {
        // Tolerances:
        // - Floor of 0.01 on equilibrium stability tests where the daily
        //   delta is ~1e-5 tC/ha.
        // - Wider tolerance on convergence tests where the absolute target
        //   is in the tens of tC/ha but the integrator is just an Euler
        //   step (no closed-form solver).
        private const double EquilibriumStockTolerance = 0.01;

        [Test]
        public void EquilibriumStockIsStableWhenInputsEqualMineralisation()
        {
            // With couverts 100 %, résidus 100 % and HedgerowDensity = 90 (ref),
            // annual inputs = 1.2 + 0.8 + 0.4 = 2.4 tC/ha/yr.
            // k = 1/40, so C_eq = 2.4 × 40 = 96 tC/ha.
            // Starting at exactly C_eq, the stock must remain stable.
            const double expectedCeq = 96.0;
            var rule = new SoilCarbonDynamicsRule();
            var model = new EcosystemModel(initialHedgerowDensity: 90.0, initialSoilCarbonStock: expectedCeq);
            var ctx = new ScenarioContext(initialCoverCropsCoveragePercent: 100.0, initialResidueRestitutionPercent: 100.0);
            var rng = new SeededRandom(0UL);

            for (int i = 0; i < 365; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.SoilCarbonStock, Is.EqualTo(expectedCeq).Within(EquilibriumStockTolerance),
                "At C = C_eq the daily ΔC should be ≈ 0 → stock stays at " + expectedCeq + ". Got " + model.SoilCarbonStock);
        }

        [Test]
        public void DecayHalfLifeMatchesPhysicalCalibration()
        {
            // With no inputs at all (couverts 0, résidus 0, HedgerowDensity 0),
            // the pool decays as C(t) = C0 × exp(−k·t). Half-life t½ = ln(2)/k
            // = 0.693 × 40 ≈ 27.7 years. After ~10120 days the stock must have
            // halved.
            var rule = new SoilCarbonDynamicsRule();
            var model = new EcosystemModel(initialHedgerowDensity: 0.0, initialSoilCarbonStock: 50.0);
            var ctx = new ScenarioContext(initialCoverCropsCoveragePercent: 0.0, initialResidueRestitutionPercent: 0.0);
            var rng = new SeededRandom(0UL);

            int halfLifeDays = (int)Math.Round(Math.Log(2.0) / SoilCarbonDynamicsRule.MineralisationRatePerYear * 365.0);
            for (int i = 0; i < halfLifeDays; i++) rule.Apply(model, ctx, rng);

            Assert.That(model.SoilCarbonStock, Is.EqualTo(25.0).Within(0.3),
                "After one half-life (~27.7 years) of pure decay, the stock should be ≈ 25 tC/ha. Got " + model.SoilCarbonStock);
        }

        [Test]
        public void CoverCropsLeverIncreasesStockOverTime()
        {
            // Two runs, identical except for the couverts lever. The run
            // with couverts active must end up with a higher stock after
            // 5 simulated years.
            var ruleA = new SoilCarbonDynamicsRule();
            var modelA = new EcosystemModel(initialHedgerowDensity: 90.0, initialSoilCarbonStock: 50.0);
            var ctxA = new ScenarioContext(initialCoverCropsCoveragePercent: 0.0, initialResidueRestitutionPercent: 0.0);

            var ruleB = new SoilCarbonDynamicsRule();
            var modelB = new EcosystemModel(initialHedgerowDensity: 90.0, initialSoilCarbonStock: 50.0);
            var ctxB = new ScenarioContext(initialCoverCropsCoveragePercent: 100.0, initialResidueRestitutionPercent: 0.0);

            var rng = new SeededRandom(0UL);
            for (int i = 0; i < 1825; i++)
            {
                ruleA.Apply(modelA, ctxA, rng);
                ruleB.Apply(modelB, ctxB, rng);
            }

            Assert.Greater(modelB.SoilCarbonStock, modelA.SoilCarbonStock,
                "Activating cover crops at 100 % should increase the carbon stock vs the no-cover run.");
            Assert.That(modelB.SoilCarbonStock - modelA.SoilCarbonStock, Is.GreaterThan(1.0),
                "After 5 years the gap between couverts 100 % and couverts 0 % should be at least ~1 tC/ha — got "
                + (modelB.SoilCarbonStock - modelA.SoilCarbonStock));
        }

        [Test]
        public void ResidueRestitutionLeverIncreasesStockOverTime()
        {
            // Same setup as the cover-crops test but toggling the residue
            // lever instead. The residue input ceiling is 0.8 tC/ha/yr
            // (vs 1.2 for cover crops), so the effect is smaller but
            // strictly positive.
            var ruleA = new SoilCarbonDynamicsRule();
            var modelA = new EcosystemModel(initialHedgerowDensity: 90.0, initialSoilCarbonStock: 50.0);
            var ctxA = new ScenarioContext(initialCoverCropsCoveragePercent: 0.0, initialResidueRestitutionPercent: 0.0);

            var ruleB = new SoilCarbonDynamicsRule();
            var modelB = new EcosystemModel(initialHedgerowDensity: 90.0, initialSoilCarbonStock: 50.0);
            var ctxB = new ScenarioContext(initialCoverCropsCoveragePercent: 0.0, initialResidueRestitutionPercent: 100.0);

            var rng = new SeededRandom(0UL);
            for (int i = 0; i < 1825; i++)
            {
                ruleA.Apply(modelA, ctxA, rng);
                ruleB.Apply(modelB, ctxB, rng);
            }

            Assert.Greater(modelB.SoilCarbonStock, modelA.SoilCarbonStock,
                "Activating residue restitution at 100 % should increase the carbon stock vs the no-residue run.");
        }
    }

    public sealed class EddyTowerSensorReaderTests
    {
        [Test]
        public void ReadingTracksStockDeltaWithNegativeSign()
        {
            // Inject controlled stock values directly: 50 → 51 means ΔC = +1
            // tC/ha/day, so the flux must report ~ −1 × 44/12 × 1000 = −3667
            // kgCO2/ha/day (sequestration). The first call establishes the
            // baseline at 50 (flux ≈ noise only).
            var reader = new EddyTowerSensorReader(new SeededRandom(1UL));
            reader.ReadAndRecord(50.0); // baseline
            double flux = reader.ReadAndRecord(51.0);

            double expected = -1.0 * EddyTowerSensorReader.CarbonToCO2MassRatio * EddyTowerSensorReader.TonnesToKilograms;
            Assert.That(flux, Is.EqualTo(expected).Within(5.0 * EddyTowerSensorReader.NoiseSigmaKgCO2PerHectarePerDay),
                "ΔC = +1 tC/ha/day → flux ≈ −3667 kgCO2/ha/day ± a few σ. Got " + flux);
            Assert.Less(flux, 0.0, "Sequestration (ΔC > 0) must report a negative net flux.");
        }

        [Test]
        public void MeanFluxConvergesToTrueSignalUnderManyDraws()
        {
            // Same step ΔC every day for 5000 days → empirical mean of the
            // measured flux should converge to the true value within the
            // Gaussian noise envelope.
            var reader = new EddyTowerSensorReader(new SeededRandom(7UL));
            reader.ReadAndRecord(50.0); // baseline, ΔC = 0
            const int n = 5000;
            double stock = 50.0;
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                stock += 0.01; // +0.01 tC/ha/day
                sum += reader.ReadAndRecord(stock);
            }
            double mean = sum / n;
            double expected = -0.01 * EddyTowerSensorReader.CarbonToCO2MassRatio * EddyTowerSensorReader.TonnesToKilograms;
            Assert.That(mean, Is.EqualTo(expected).Within(0.1),
                "Mean flux over 5000 draws should converge to −36.67 kgCO2/ha/day. Got " + mean);
        }

        [Test]
        public void HistoryFillsSlidingWindow()
        {
            var reader = new EddyTowerSensorReader(new SeededRandom(2UL));
            for (int i = 0; i < EddyTowerSensorReader.HistoryWindowDays + 50; i++)
            {
                reader.ReadAndRecord(50.0 + i * 0.01);
            }
            Assert.AreEqual(EddyTowerSensorReader.HistoryWindowDays, reader.HistoryCount);

            var snapshot = new List<double>();
            int copied = reader.CopyHistoryTo(snapshot);
            Assert.AreEqual(EddyTowerSensorReader.HistoryWindowDays, copied);
            Assert.AreEqual(EddyTowerSensorReader.HistoryWindowDays, snapshot.Count);
        }

        [Test]
        public void DeterministicForSameSeed()
        {
            var readerA = new EddyTowerSensorReader(new SeededRandom(42UL));
            var readerB = new EddyTowerSensorReader(new SeededRandom(42UL));
            double stock = 50.0;
            for (int i = 0; i < 100; i++)
            {
                stock += 0.005;
                double a = readerA.ReadAndRecord(stock);
                double b = readerB.ReadAndRecord(stock);
                Assert.AreEqual(a, b);
            }
        }

        [Test]
        public void EstimatedStockTracksTrueStockViaIntegratedFlux()
        {
            // V4 / B3: the tower integrates its measured fluxes back into a stock
            // estimate. The first read calibrates it to the known baseline; after a
            // sustained decline the integrated estimate tracks the true stock
            // closely (only the small accumulated flux noise separates them — the
            // honest, documented drift of an integrated flux sensor). The carbon-low
            // alert thresholds THIS estimate (primauté du capteur, §9).
            var reader = new EddyTowerSensorReader(new SeededRandom(11UL));
            double stock = 50.0;
            reader.ReadAndRecord(stock); // baseline → estimate calibrated to 50
            Assert.That(reader.EstimatedSoilCarbonStock, Is.EqualTo(50.0).Within(1e-9),
                "First read calibrates the estimate to the known stock.");

            for (int i = 0; i < 300; i++)
            {
                stock -= 0.02; // true stock falls to 44 over 300 days
                reader.ReadAndRecord(stock);
            }
            Assert.That(reader.EstimatedSoilCarbonStock, Is.EqualTo(stock).Within(0.1),
                "Integrated estimate should track the true stock within the small noise drift. Est="
                + reader.EstimatedSoilCarbonStock + " true=" + stock);
        }
    }
}
