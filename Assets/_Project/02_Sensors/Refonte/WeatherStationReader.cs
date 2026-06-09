using System;
using Bocage.SimulationCore;

namespace Bocage.Sensors.Refonte
{
    /// <summary>
    /// Station météo (Couche 02, refonte) : mesure bruitée de la température, de
    /// la pluie, et — décision MVP, pas de capteur dédié — de l'<b>humidité</b>
    /// (fraction d'eau du sol θ/RU_max), qui arme l'alerte de stress hydrique.
    /// Transforme la vérité du modèle en mesure imparfaite (primauté du capteur).
    /// Déterministe via son sous-flux RNG ; aucune I/O.
    /// </summary>
    public sealed class WeatherStationReader
    {
        public const string SubStreamId = "weather-station";
        public const double TemperatureNoiseSigmaC = 0.3;
        public const double PrecipitationRelativeNoiseSigma = 0.05;
        public const double HumidityNoiseSigma = 0.02;

        private readonly SeededRandom _rng;

        public WeatherStationReader(SeededRandom rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public double ReadTemperatureCelsius(double truthCelsius)
            => truthCelsius + _rng.NextGaussian(0.0, TemperatureNoiseSigmaC);

        public double ReadPrecipitationMm(double truthMm)
        {
            double measured = truthMm + _rng.NextGaussian(0.0, PrecipitationRelativeNoiseSigma * truthMm);
            return measured < 0.0 ? 0.0 : measured;
        }

        /// <summary>
        /// Mesure d'humidité = fraction d'eau du sol θ/RU_max ∈ [0,1] (proxy du
        /// stress hydrique). C'est elle que seuille l'alerte sécheresse.
        /// </summary>
        public double ReadHumidityFraction(double soilWaterFraction)
        {
            double measured = soilWaterFraction + _rng.NextGaussian(0.0, HumidityNoiseSigma);
            return measured < 0.0 ? 0.0 : (measured > 1.0 ? 1.0 : measured);
        }
    }
}
