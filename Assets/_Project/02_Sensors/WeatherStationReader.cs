using System;
using System.Collections.Generic;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;

namespace Bocage.Sensors
{
    /// <summary>
    /// Couche 2 reader for the on-site WeatherStation sprite. Takes the
    /// model's daily Weather and returns a noisy observation that matches
    /// what a real meteorological station would record on the same day —
    /// a Gaussian on T° (σ = 0.3 °C, typical thermistor accuracy) and a
    /// Gaussian on precipitation (σ proportional to amount at 5 %,
    /// typical tipping-bucket rain gauge error). No event detection, no
    /// recommendation — this is the pure measurement chain that ADR #52
    /// requires to make WeatherStation a first-class sensor instead of a
    /// decorative sprite.
    /// <para>
    /// Each call to <see cref="ReadAndRecord"/> also stores the noisy
    /// observation in an internal 365-day circular buffer so the future
    /// inspection panel (chantier E6 / ADR #53) can plot T° and
    /// precipitation history vs. the monthly normals. The buffer is
    /// pre-allocated at construction (no runtime allocation) and the
    /// oldest entry is overwritten in O(1) as the window slides.
    /// </para>
    /// <para>
    /// The reader owns a derived sub-stream (<c>"weather-station"</c>) so
    /// its noise sequence is reproducible from the master seed and
    /// independent of the rule that generates the true weather and every
    /// other sub-system.
    /// </para>
    /// </summary>
    public sealed class WeatherStationReader
    {
        public const int HistoryWindowDays = 365;
        public const double TemperatureNoiseSigmaC = 0.3;
        public const double PrecipitationRelativeNoiseSigma = 0.05;

        private readonly SeededRandom _rng;
        private readonly Weather[] _history = new Weather[HistoryWindowDays];
        private int _historyHead; // next slot to write
        private int _historyCount; // number of valid samples (≤ HistoryWindowDays)

        public WeatherStationReader(SeededRandom masterRng)
        {
            if (masterRng == null) throw new ArgumentNullException(nameof(masterRng));
            _rng = masterRng.DeriveSubStream("weather-station");
        }

        /// <summary>Total number of samples currently stored (caps at 365).</summary>
        public int HistoryCount => _historyCount;

        /// <summary>Returns a noisy reading without touching the history buffer.</summary>
        public Weather Read(in Weather trueWeather)
        {
            double tempNoise = _rng.NextGaussian(0.0, TemperatureNoiseSigmaC);
            double sigmaPrecip = PrecipitationRelativeNoiseSigma * trueWeather.PrecipitationMillimeters;
            double precipNoise = sigmaPrecip > 0.0 ? _rng.NextGaussian(0.0, sigmaPrecip) : 0.0;

            double noisyTemperature = trueWeather.TemperatureCelsius + tempNoise;
            double noisyPrecipitation = trueWeather.PrecipitationMillimeters + precipNoise;
            if (noisyPrecipitation < 0.0) noisyPrecipitation = 0.0;

            return new Weather(noisyTemperature, noisyPrecipitation);
        }

        /// <summary>Reads and appends the result to the rolling 365-day buffer.</summary>
        public Weather ReadAndRecord(in Weather trueWeather)
        {
            Weather observed = Read(trueWeather);
            _history[_historyHead] = observed;
            _historyHead = (_historyHead + 1) % HistoryWindowDays;
            if (_historyCount < HistoryWindowDays) _historyCount++;
            return observed;
        }

        /// <summary>
        /// Copies the recorded samples into <paramref name="destination"/>
        /// in chronological order (oldest first). Returns the number of
        /// samples actually written.
        /// </summary>
        public int CopyHistoryTo(IList<Weather> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            int oldestIndex = _historyCount < HistoryWindowDays
                ? 0
                : _historyHead;
            for (int i = 0; i < _historyCount; i++)
            {
                int slot = (oldestIndex + i) % HistoryWindowDays;
                destination.Add(_history[slot]);
            }
            return _historyCount;
        }
    }
}
