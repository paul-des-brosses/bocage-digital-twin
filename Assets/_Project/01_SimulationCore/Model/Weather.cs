namespace Bocage.SimulationCore.Model
{
    /// <summary>
    /// Daily aggregated weather for the modelled Perche site.
    /// </summary>
    public readonly struct Weather
    {
        /// <summary>Average daily temperature in degrees Celsius.</summary>
        public double TemperatureCelsius { get; }

        /// <summary>Daily precipitation in millimetres of water (mm/day).</summary>
        public double PrecipitationMillimeters { get; }

        public Weather(double temperatureCelsius, double precipitationMillimeters)
        {
            TemperatureCelsius = temperatureCelsius;
            PrecipitationMillimeters = precipitationMillimeters < 0.0 ? 0.0 : precipitationMillimeters;
        }
    }
}
