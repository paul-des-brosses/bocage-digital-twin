namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Météo générée d'un jour. Porte explicitement T_min/T_max (requis par
    /// l'ETP de Hargreaves) en plus de la moyenne et de la pluie.
    /// </summary>
    public readonly struct DailyWeather
    {
        /// <summary>Température minimale du jour (°C).</summary>
        public double TMinCelsius { get; }

        /// <summary>Température maximale du jour (°C).</summary>
        public double TMaxCelsius { get; }

        /// <summary>Température moyenne du jour (°C).</summary>
        public double TMeanCelsius { get; }

        /// <summary>Précipitation du jour (mm), bornée ≥ 0.</summary>
        public double PrecipMm { get; }

        public DailyWeather(double tMinCelsius, double tMaxCelsius, double tMeanCelsius, double precipMm)
        {
            TMinCelsius = tMinCelsius;
            TMaxCelsius = tMaxCelsius;
            TMeanCelsius = tMeanCelsius;
            PrecipMm = precipMm < 0.0 ? 0.0 : precipMm;
        }
    }
}
