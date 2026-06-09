using System;
using Bocage.SimulationCore;

namespace Bocage.Sensors.Refonte
{
    /// <summary>
    /// Piézomètre (Couche 02, refonte) : mesure bruitée de la profondeur de la
    /// nappe (m). Capteur secondaire — le stress agronomique « utile » passe par
    /// l'humidité du sol (station météo) ; le piézomètre sert la lecture nappe.
    /// Déterministe ; aucune I/O.
    /// </summary>
    public sealed class PiezometerReader
    {
        public const string SubStreamId = "piezometer";
        public const double DepthNoiseSigmaM = 0.05;

        private readonly SeededRandom _rng;

        public PiezometerReader(SeededRandom rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        public double ReadDepthMeters(double truthDepthM)
        {
            double measured = truthDepthM + _rng.NextGaussian(0.0, DepthNoiseSigmaM);
            return measured < 0.0 ? 0.0 : measured;
        }
    }
}
