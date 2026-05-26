using Bocage.Sensors;
using Bocage.Sensors.Events;

namespace Bocage.Decision.Recommendations
{
    /// <summary>
    /// Formats the causal chain « capteur → événement → recommandation »
    /// into a single human-readable line for the decision popup and the
    /// history list. Pure formatter, no allocation beyond the returned
    /// string, no Unity reference.
    /// <para>
    /// Sub-étape 10a friction #2 fix: a visitor opening the popup must
    /// see WHY this recommendation exists — which sensor caught what,
    /// and on which simulated day. Without that line the visitor reads
    /// the rationale in isolation and the « digital twin instrumenté »
    /// claim of the portfolio loses its anchor.
    /// </para>
    /// <para>
    /// The lookup uses <see cref="EventLog"/> as the single source of
    /// truth: the recommendation carries an event-instance id (e.g.
    /// "hedge-chalara#28") and we find the matching IEvent by
    /// matching <c>ev.Id + "#" + ev.DetectedOnDay</c>. When the lookup
    /// fails (event log not populated, or the engine produced a rec
    /// with a synthetic id) we fall back to a less precise line so the
    /// popup never shows an empty provenance.
    /// </para>
    /// </summary>
    public static class RecommendationProvenance
    {
        /// <summary>
        /// Returns a one-line provenance string suitable for a sub-title
        /// label, e.g. « Détecté jour 28 par le piège photo —
        /// Dépérissement haie compatible chalara fraxinea ». Never
        /// returns null; on failure returns a short generic line.
        /// </summary>
        public static string Format(IRecommendation rec, EventLog log)
        {
            if (rec == null) return "";

            var ev = LookupEvent(rec.TriggeredByEventId, log);
            if (ev != null)
            {
                return "Détecté jour " + ev.DetectedOnDay
                     + " par " + SensorDisplayFor(ev)
                     + " — " + ev.Summary;
            }

            // Fallback: we know the recommendation was issued on a day
            // and which event TYPE triggered it (the prefix of the
            // instance id), so we surface what we can.
            return "Détecté jour " + rec.IssuedOnDay
                 + " par " + SensorDisplayForEventTypeId(rec.TriggeredByEventId);
        }

        /// <summary>
        /// Returns the IEvent in <paramref name="log"/> whose
        /// instance id (<c>Id + "#" + DetectedOnDay</c>) matches
        /// <paramref name="triggeredByEventId"/>, or null if no match.
        /// O(N) scan; N stays small in typical runs (event log has at
        /// most a few dozen entries).
        /// </summary>
        public static IEvent LookupEvent(string triggeredByEventId, EventLog log)
        {
            if (log == null || string.IsNullOrEmpty(triggeredByEventId)) return null;
            var events = log.Events;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e == null) continue;
                // Format must match RecommendationEngine.MakeEventInstanceId.
                if (triggeredByEventId == e.Id + "#" + e.DetectedOnDay)
                {
                    return e;
                }
            }
            return null;
        }

        /// <summary>
        /// Maps an event runtime type to the human display name of the
        /// sensor that detects it. Drawn from the sensor placement
        /// definition deployed in the scene (cf. SensorPlacementDefinition).
        /// </summary>
        public static string SensorDisplayFor(IEvent ev)
        {
            switch (ev)
            {
                case HedgeChalaraEvent _:        return "le piège photo";
                case DroughtProlongedEvent _:    return "le piézomètre";
                case FaunaAcousticAnomalyEvent _:return "le capteur acoustique";
                default:                         return "un capteur";
            }
        }

        /// <summary>
        /// Same mapping but from the event type id string (parsed from
        /// the instance id prefix). Used only by the fallback path
        /// when the full IEvent could not be located.
        /// </summary>
        private static string SensorDisplayForEventTypeId(string instanceOrTypeId)
        {
            if (string.IsNullOrEmpty(instanceOrTypeId)) return "un capteur";
            // Strip "#N" suffix if present.
            int sep = instanceOrTypeId.IndexOf('#');
            string typeId = sep < 0 ? instanceOrTypeId : instanceOrTypeId.Substring(0, sep);
            switch (typeId)
            {
                case "hedge-chalara":          return "le piège photo";
                case "drought-prolonged":      return "le piézomètre";
                case "fauna-acoustic-anomaly": return "le capteur acoustique";
                default:                       return "un capteur";
            }
        }
    }
}
