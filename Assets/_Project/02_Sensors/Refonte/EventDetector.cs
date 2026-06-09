namespace Bocage.Sensors.Refonte
{
    /// <summary>
    /// Détecteur d'événements (Couche 02, refonte). Appelé une fois par jour, il
    /// seuille les <b>MESURES</b> fournies par les capteurs (pas la vérité du
    /// modèle — primauté du capteur, §9) et appending les alertes au
    /// <see cref="EventLog"/>. Stress hydrique avec comptage de jours consécutifs ;
    /// les autres au franchissement, avec un cooldown par catégorie. Aucune I/O.
    /// <para>
    /// Le contrat de primauté du capteur est tenu par l'appelant : il passe les
    /// sorties bruitées des capteurs (humidité station, estimation tour Eddy,
    /// indice faune), pas les variables du modèle. L'azote et la rentabilité sont
    /// des proxies dérivés du modèle (pas de capteur dédié au MVP).
    /// </para>
    /// </summary>
    public sealed class EventDetector
    {
        public const double HydricStressHumidityThreshold = 0.20;  // < 20 % RU
        public const int HydricStressConsecutiveDays = 30;
        public const double SoilCarbonLowThreshold = 45.0;          // tC/ha
        public const double FaunaAnomalyThreshold = 0.45;
        public const double NitrogenDeficiencyThreshold = 25.0;     // kgN/ha
        public const double NitrogenExcessThreshold = 100.0;        // kgN/ha
        public const double LowProfitabilityThreshold = 50.0;       // €/ha
        public const int CooldownDays = 30;

        private int _consecutiveStressDays;

        /// <summary>
        /// Passe de détection du jour. Renvoie le nombre d'événements ajoutés.
        /// Les valeurs passées sont des MESURES (capteurs) / proxies, pas la vérité.
        /// </summary>
        public int Detect(int day,
            double measuredHumidityFraction, double estimatedCarbonTPerHa, double measuredFauna,
            double mineralNitrogenKgPerHa, double marginEurosPerHa, EventLog log)
        {
            int appended = 0;

            // Stress hydrique : humidité mesurée sous le seuil, jours consécutifs.
            if (measuredHumidityFraction < HydricStressHumidityThreshold) _consecutiveStressDays++;
            else _consecutiveStressDays = 0;
            if (_consecutiveStressDays >= HydricStressConsecutiveDays
                && !InCooldown(EventKind.HydricStress, day, log))
            {
                log.Append(new DetectedEvent(EventKind.HydricStress, day, measuredHumidityFraction));
                appended++;
            }

            appended += Fire(estimatedCarbonTPerHa < SoilCarbonLowThreshold,
                EventKind.SoilCarbonLow, day, estimatedCarbonTPerHa, log);
            appended += Fire(measuredFauna < FaunaAnomalyThreshold,
                EventKind.FaunaAnomaly, day, measuredFauna, log);
            appended += Fire(mineralNitrogenKgPerHa < NitrogenDeficiencyThreshold,
                EventKind.NitrogenDeficiency, day, mineralNitrogenKgPerHa, log);
            appended += Fire(mineralNitrogenKgPerHa > NitrogenExcessThreshold,
                EventKind.NitrogenExcess, day, mineralNitrogenKgPerHa, log);
            appended += Fire(marginEurosPerHa < LowProfitabilityThreshold,
                EventKind.LowProfitability, day, marginEurosPerHa, log);

            return appended;
        }

        /// <summary>Réinitialise le compteur interne (réutilisation entre scénarios de test).</summary>
        public void Reset() => _consecutiveStressDays = 0;

        private static int Fire(bool condition, EventKind kind, int day, double value, EventLog log)
        {
            if (!condition || InCooldown(kind, day, log)) return 0;
            log.Append(new DetectedEvent(kind, day, value));
            return 1;
        }

        private static bool InCooldown(EventKind kind, int day, EventLog log)
        {
            DetectedEvent? last = log.LatestOfKind(kind);
            if (last == null) return false;
            return day - last.Value.Day < CooldownDays;
        }
    }
}
