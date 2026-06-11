using System;
using Bocage.SimulationCore;

namespace Bocage.Sensors
{
    /// <summary>
    /// Capteurs faune (Couche 02) : deux canaux indépendants (recorder
    /// acoustique + piège photo) mesurent l'indice de biodiversité bruité, puis
    /// la moyenne réduit le bruit. C'est cette mesure que seuille l'alerte
    /// d'anomalie faune (primauté du capteur). Déterministe ; aucune I/O.
    /// </summary>
    public sealed class FaunaSensorReader
    {
        public const string SubStreamId = "fauna-sensors";
        public const double ChannelNoiseSigma = 0.05;

        private readonly SeededRandom _acoustic;
        private readonly SeededRandom _camera;

        public FaunaSensorReader(SeededRandom rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            _acoustic = rng.DeriveSubStream("acoustic");
            _camera = rng.DeriveSubStream("camera");
        }

        /// <summary>Mesure de l'indice de biodiversité ∈ [0,1] (moyenne des deux canaux bruités).</summary>
        public double ReadBiodiversity(double biodiversityTruth)
        {
            double a = biodiversityTruth + _acoustic.NextGaussian(0.0, ChannelNoiseSigma);
            double c = biodiversityTruth + _camera.NextGaussian(0.0, ChannelNoiseSigma);
            double measured = (a + c) / 2.0;
            return measured < 0.0 ? 0.0 : (measured > 1.0 ? 1.0 : measured);
        }
    }
}
