using System;

namespace Bocage.SimulationCore
{
    /// <summary>
    /// Générateur météo stochastique seedé (type Richardson/WGEN), calibré sur
    /// une <see cref="Climatology"/>. Il NE rejoue PAS le relevé : il en tire une
    /// trajectoire synthétique. Composantes :
    /// <list type="bullet">
    ///   <item>occurrence de pluie par chaîne de Markov 2 états (persistance des
    ///   épisodes secs/humides — corrige le « Bernoulli sans mémoire ») ;</item>
    ///   <item>intensité des jours pluvieux par loi log-normale ;</item>
    ///   <item>température = moyenne saisonnière + anomalie AR(1) (les vagues de
    ///   chaleur émergent au lieu d'être tirées indépendamment).</item>
    /// </list>
    /// Entièrement déterministe : même seed → même série (le run réel et le run
    /// fantôme partagent le sous-flux météo). Aucune I/O, aucun UnityEngine.
    /// </summary>
    public sealed class WeatherGenerator
    {
        /// <summary>Identifiant du sous-flux RNG dédié à la météo.</summary>
        public const string SubStreamId = "weather";

        private readonly Climatology _climatology;
        private readonly SeededRandom _rng;
        private readonly double _stationaryTempStd;

        private bool _initialized;
        private bool _previousDayWet;
        private double _previousTempAnomaly;

        /// <param name="climatology">Normales mensuelles + paramètres AR(1).</param>
        /// <param name="rng">
        /// Sous-flux dédié, idéalement <c>master.DeriveSubStream(SubStreamId)</c>.
        /// </param>
        public WeatherGenerator(Climatology climatology, SeededRandom rng)
        {
            _climatology = climatology ?? throw new ArgumentNullException(nameof(climatology));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            // Écart-type stationnaire de l'AR(1) : σ_stat = σ_resid / sqrt(1 − φ²).
            // Sert à tirer l'anomalie du PREMIER jour dans son régime permanent,
            // pour éviter un transitoire de démarrage non physique.
            double phi = _climatology.TempAr1Phi;
            double denom = 1.0 - phi * phi;
            _stationaryTempStd = denom > 1e-6
                ? _climatology.TempAr1ResidStd / Math.Sqrt(denom)
                : _climatology.TempAr1ResidStd;
        }

        /// <summary>
        /// Tire la météo du jour pour le mois donné (1 = janvier … 12 = décembre).
        /// Les appels sont séquentiels : l'état (jour précédent humide/sec, anomalie
        /// de température précédente) est porté d'un appel au suivant.
        /// </summary>
        public DailyWeather Next(int month1To12)
        {
            MonthlyClimate mc = _climatology.Month(month1To12);

            // --- Occurrence de pluie : chaîne de Markov 2 états ---
            double pWet = _initialized
                ? (_previousDayWet ? mc.PWetAfterWet : mc.PWetAfterDry)
                : mc.ProbWetDay;
            bool wet = _rng.NextDouble() < pWet;

            // --- Intensité du jour pluvieux : loi log-normale ---
            double precip = 0.0;
            if (wet)
            {
                double z = _rng.NextGaussian(0.0, 1.0);
                precip = Math.Exp(mc.LognormalMu + mc.LognormalSigma * z);
            }

            // --- Température : moyenne saisonnière + anomalie AR(1) ---
            double anomaly = _initialized
                ? _climatology.TempAr1Phi * _previousTempAnomaly
                    + _rng.NextGaussian(0.0, _climatology.TempAr1ResidStd)
                : _rng.NextGaussian(0.0, _stationaryTempStd);

            double tMean = mc.TempMeanCelsius + anomaly;
            double half = mc.DiurnalRangeCelsius * 0.5;

            _previousDayWet = wet;
            _previousTempAnomaly = anomaly;
            _initialized = true;

            return new DailyWeather(tMean - half, tMean + half, tMean, precip);
        }
    }
}
