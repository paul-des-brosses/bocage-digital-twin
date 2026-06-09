using Bocage.SimulationCore;
using Bocage.Sensors.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests des capteurs de la refonte (Couche 02) : bruit présent mais non
    /// biaisé (moyenne ≈ vérité), bornes respectées, déterminisme, et
    /// l'estimation intégrée de la tour Eddy qui suit la perte de carbone.
    /// </summary>
    public sealed class SensorsRefonteTests
    {
        private static SeededRandom Sub(ulong seed, string id) => new SeededRandom(seed).DeriveSubStream(id);

        [Test]
        public void Weather_station_temperature_is_noisy_but_unbiased()
        {
            var station = new WeatherStationReader(Sub(1UL, WeatherStationReader.SubStreamId));
            double sum = 0.0; int n = 5000; bool anyDifferent = false;
            for (int i = 0; i < n; i++)
            {
                double m = station.ReadTemperatureCelsius(12.0);
                sum += m;
                if (m != 12.0) anyDifferent = true;
            }
            Assert.IsTrue(anyDifferent, "la mesure doit être bruitée");
            Assert.AreEqual(12.0, sum / n, 0.05, "le bruit doit être non biaisé (moyenne ~ vérité)");
        }

        [Test]
        public void Humidity_is_bounded_and_tracks_truth()
        {
            var station = new WeatherStationReader(Sub(2UL, WeatherStationReader.SubStreamId));
            double sum = 0.0; int n = 5000;
            for (int i = 0; i < n; i++)
            {
                double m = station.ReadHumidityFraction(0.18);
                Assert.That(m, Is.InRange(0.0, 1.0));
                sum += m;
            }
            Assert.AreEqual(0.18, sum / n, 0.01);
        }

        [Test]
        public void Determinism_same_seed_same_measurements()
        {
            var a = new WeatherStationReader(Sub(7UL, WeatherStationReader.SubStreamId));
            var b = new WeatherStationReader(Sub(7UL, WeatherStationReader.SubStreamId));
            for (int i = 0; i < 200; i++)
                Assert.AreEqual(a.ReadTemperatureCelsius(10.0), b.ReadTemperatureCelsius(10.0), 1e-12);
        }

        [Test]
        public void Fauna_measurement_is_bounded_and_unbiased()
        {
            var fauna = new FaunaSensorReader(Sub(3UL, FaunaSensorReader.SubStreamId));
            double sum = 0.0; int n = 5000;
            for (int i = 0; i < n; i++)
            {
                double m = fauna.ReadBiodiversity(0.6);
                Assert.That(m, Is.InRange(0.0, 1.0));
                sum += m;
            }
            Assert.AreEqual(0.6, sum / n, 0.01);
        }

        [Test]
        public void Piezometer_is_non_negative_and_unbiased()
        {
            var piezo = new PiezometerReader(Sub(4UL, PiezometerReader.SubStreamId));
            double sum = 0.0; int n = 5000;
            for (int i = 0; i < n; i++)
            {
                double m = piezo.ReadDepthMeters(2.5);
                Assert.GreaterOrEqual(m, 0.0);
                sum += m;
            }
            Assert.AreEqual(2.5, sum / n, 0.01);
        }

        [Test]
        public void Eddy_tower_estimate_tracks_carbon_loss()
        {
            var tower = new EddyTowerReader(Sub(5UL, EddyTowerReader.SubStreamId), 50.0);
            // Perte nette constante de 0,001 tC/ha/j pendant un an ~ -0,365 tC.
            for (int day = 0; day < 365; day++) tower.ReadFluxKgCo2(0.001);
            Assert.Less(tower.EstimatedCarbonStockTPerHa, 50.0, "le stock estimé doit baisser quand le sol perd du carbone");
            Assert.That(tower.EstimatedCarbonStockTPerHa, Is.InRange(49.4, 49.9),
                "l'estimation intégrée suit la perte (~-0,365 tC), au bruit près");
        }

        [Test]
        public void Eddy_tower_estimate_rises_when_sequestering()
        {
            var tower = new EddyTowerReader(Sub(6UL, EddyTowerReader.SubStreamId), 50.0);
            // Sequestration (perte nette negative) → le stock estime monte.
            for (int day = 0; day < 365; day++) tower.ReadFluxKgCo2(-0.001);
            Assert.Greater(tower.EstimatedCarbonStockTPerHa, 50.0);
        }
    }
}
