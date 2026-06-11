using Bocage.Sensors;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du détecteur d'événements (Couche 02) : état sain → rien ;
    /// chaque alerte se déclenche au franchissement de sa mesure ; comptage de
    /// jours consécutifs pour le stress hydrique ; cooldown ; et l'événement
    /// enregistre la valeur mesurée.
    /// </summary>
    public sealed class EventDetectorTests
    {
        private static int Detect(EventDetector detector, EventLog log, int day,
            double humidity = 0.6, double carbon = 50.0, double fauna = 0.65,
            double nitrogen = 60.0, double margin = 300.0)
            => detector.Detect(day, humidity, carbon, fauna, nitrogen, margin, log);

        [Test]
        public void Healthy_state_emits_no_event()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            Detect(detector, log, 1);
            Assert.AreEqual(0, log.Count);
        }

        [Test]
        public void Hydric_stress_fires_after_consecutive_low_humidity()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            for (int day = 1; day <= 29; day++) Detect(detector, log, day, humidity: 0.15);
            Assert.AreEqual(0, log.CountOfKind(EventKind.HydricStress), "29 jours ne suffisent pas");
            Detect(detector, log, 30, humidity: 0.15);
            Assert.AreEqual(1, log.CountOfKind(EventKind.HydricStress), "le 30e jour déclenche");
        }

        [Test]
        public void Hydric_stress_counter_resets_on_recovery()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            for (int day = 1; day <= 20; day++) Detect(detector, log, day, humidity: 0.15);
            Detect(detector, log, 21, humidity: 0.50);                 // recharge → reset
            for (int day = 22; day <= 50; day++) Detect(detector, log, day, humidity: 0.15); // 29 j
            Assert.AreEqual(0, log.CountOfKind(EventKind.HydricStress), "le compteur doit repartir de zéro");
        }

        [Test]
        public void Carbon_low_fires_below_threshold()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            Detect(detector, log, 1, carbon: 40.0);
            Assert.AreEqual(1, log.CountOfKind(EventKind.SoilCarbonLow));
        }

        [Test]
        public void Fauna_anomaly_fires_below_threshold()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            Detect(detector, log, 1, fauna: 0.40);
            Assert.AreEqual(1, log.CountOfKind(EventKind.FaunaAnomaly));
        }

        [Test]
        public void Nitrogen_deficiency_fires_when_low()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            Detect(detector, log, 1, nitrogen: 20.0);
            Assert.AreEqual(1, log.CountOfKind(EventKind.NitrogenDeficiency));
            Assert.AreEqual(0, log.CountOfKind(EventKind.NitrogenExcess));
        }

        [Test]
        public void Nitrogen_excess_fires_when_high()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            Detect(detector, log, 1, nitrogen: 130.0);
            Assert.AreEqual(1, log.CountOfKind(EventKind.NitrogenExcess));
            Assert.AreEqual(0, log.CountOfKind(EventKind.NitrogenDeficiency));
        }

        [Test]
        public void Low_profitability_fires_below_threshold()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            Detect(detector, log, 1, margin: 20.0);
            Assert.AreEqual(1, log.CountOfKind(EventKind.LowProfitability));
        }

        [Test]
        public void Cooldown_prevents_refiring_then_allows_after()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            Detect(detector, log, 1, carbon: 40.0);    // fire
            Detect(detector, log, 10, carbon: 40.0);   // dans le cooldown → pas de refire
            Assert.AreEqual(1, log.CountOfKind(EventKind.SoilCarbonLow));
            Detect(detector, log, 40, carbon: 40.0);   // au-delà du cooldown → refire
            Assert.AreEqual(2, log.CountOfKind(EventKind.SoilCarbonLow));
        }

        [Test]
        public void Event_records_the_measured_value()
        {
            var detector = new EventDetector();
            var log = new EventLog();
            Detect(detector, log, 1, fauna: 0.40);
            DetectedEvent? e = log.LatestOfKind(EventKind.FaunaAnomaly);
            Assert.IsNotNull(e);
            Assert.AreEqual(0.40, e.Value.MeasuredValue, 1e-9, "l'événement enregistre la MESURE");
        }
    }
}
