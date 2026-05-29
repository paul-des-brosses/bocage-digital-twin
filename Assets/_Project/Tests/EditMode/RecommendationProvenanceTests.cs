using Bocage.Decision;
using Bocage.Decision.Recommendations;
using Bocage.Sensors;
using Bocage.Sensors.Events;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Sub-étape 10a friction #2 fix — provenance formatter that
    /// surfaces the « capteur → événement → recommandation » chain
    /// inside the decision popup and the history list.
    /// <para>
    /// The headline contract is that the instance-id format used by
    /// <see cref="RecommendationEngine.MakeEventInstanceId"/> and the
    /// lookup in <see cref="RecommendationProvenance.LookupEvent"/> are
    /// kept in lockstep — these tests pin that contract so a future
    /// change to one without the other surfaces as a red bar.
    /// </para>
    /// </summary>
    public sealed class RecommendationProvenanceTests
    {
        [Test]
        public void Lookup_finds_event_matching_recommendation_instance_id()
        {
            var log = new EventLog();
            var drought = new DroughtProlongedEvent(detectedOnDay: 28, waterTableDepthMeters: 4.0, consecutiveDryDays: 30);
            log.Append(drought);
            string instanceId = RecommendationEngine.MakeEventInstanceId(drought);

            var found = RecommendationProvenance.LookupEvent(instanceId, log);

            Assert.AreSame(drought, found);
        }

        [Test]
        public void Lookup_returns_null_when_log_has_no_matching_event()
        {
            var log = new EventLog();
            log.Append(new FaunaAcousticAnomalyEvent(detectedOnDay: 5, faunaPopulation: 0.5));

            var found = RecommendationProvenance.LookupEvent("drought-prolonged#28", log);

            Assert.IsNull(found);
        }

        [Test]
        public void Lookup_handles_null_log_and_empty_id_gracefully()
        {
            Assert.IsNull(RecommendationProvenance.LookupEvent("drought-prolonged#28", null));
            Assert.IsNull(RecommendationProvenance.LookupEvent(null, new EventLog()));
            Assert.IsNull(RecommendationProvenance.LookupEvent("", new EventLog()));
        }

        [Test]
        public void SensorDisplay_maps_each_event_type_to_a_known_sensor()
        {
            Assert.AreEqual("le piézomètre",
                RecommendationProvenance.SensorDisplayFor(
                    new DroughtProlongedEvent(detectedOnDay: 1, waterTableDepthMeters: 4, consecutiveDryDays: 30)));
            Assert.AreEqual("le capteur acoustique",
                RecommendationProvenance.SensorDisplayFor(
                    new FaunaAcousticAnomalyEvent(detectedOnDay: 1, faunaPopulation: 0.5)));
        }

        [Test]
        public void Format_with_resolved_event_yields_full_provenance_line()
        {
            var log = new EventLog();
            var drought = new DroughtProlongedEvent(detectedOnDay: 28, waterTableDepthMeters: 4.0, consecutiveDryDays: 30);
            log.Append(drought);
            string instanceId = RecommendationEngine.MakeEventInstanceId(drought);
            var rec = new IrrigationAdviceRecommendation(issuedOnDay: 28, triggeredByEventId: instanceId);

            string line = RecommendationProvenance.Format(rec, log);

            StringAssert.Contains("jour 28", line);
            StringAssert.Contains("piézomètre", line);
            StringAssert.Contains("sécheresse", line.ToLowerInvariant());
        }

        [Test]
        public void Format_falls_back_to_typed_sensor_when_event_not_in_log()
        {
            // Empty log: lookup fails, formatter falls back to typeId
            // parsing. The line should still mention the sensor type.
            var log = new EventLog();
            var rec = new IrrigationAdviceRecommendation(
                issuedOnDay: 50,
                triggeredByEventId: "drought-prolonged#50");

            string line = RecommendationProvenance.Format(rec, log);

            StringAssert.Contains("jour 50", line);
            StringAssert.Contains("piézomètre", line);
        }

        [Test]
        public void Format_of_null_recommendation_returns_empty()
        {
            Assert.AreEqual("", RecommendationProvenance.Format(null, new EventLog()));
            Assert.AreEqual("", RecommendationProvenance.Format(null, null));
        }
    }
}
