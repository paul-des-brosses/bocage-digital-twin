using System.Collections.Generic;

namespace Bocage.Sensors
{
    /// <summary>Catégories d'événements détectés.</summary>
    public enum EventKind
    {
        HydricStress,
        SoilCarbonLow,
        FaunaAnomaly,
        NitrogenDeficiency,
        NitrogenExcess,
        LowProfitability
    }

    /// <summary>Un événement détecté : sa catégorie, le jour, et la valeur mesurée qui l'a déclenché.</summary>
    public readonly struct DetectedEvent
    {
        public EventKind Kind { get; }
        public int Day { get; }
        public double MeasuredValue { get; }

        public DetectedEvent(EventKind kind, int day, double measuredValue)
        {
            Kind = kind;
            Day = day;
            MeasuredValue = measuredValue;
        }
    }

    /// <summary>Journal append-only des événements détectés.</summary>
    public sealed class EventLog
    {
        private readonly List<DetectedEvent> _events = new List<DetectedEvent>();

        public int Count => _events.Count;
        public IReadOnlyList<DetectedEvent> Events => _events;

        public void Append(DetectedEvent detectedEvent) => _events.Add(detectedEvent);

        /// <summary>Dernier événement de la catégorie, ou null.</summary>
        public DetectedEvent? LatestOfKind(EventKind kind)
        {
            for (int i = _events.Count - 1; i >= 0; i--)
                if (_events[i].Kind == kind) return _events[i];
            return null;
        }

        public int CountOfKind(EventKind kind)
        {
            int count = 0;
            for (int i = 0; i < _events.Count; i++)
                if (_events[i].Kind == kind) count++;
            return count;
        }
    }
}
