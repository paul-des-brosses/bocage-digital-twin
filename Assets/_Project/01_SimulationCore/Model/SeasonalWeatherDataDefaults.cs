namespace Bocage.SimulationCore.Model
{
    /// <summary>
    /// Hard-coded calibration of the Perche site used as the default
    /// payload for <see cref="SeasonalWeatherData"/>. Values are encoded
    /// here in pure C# so EditMode tests can build the fixture without
    /// loading a Unity asset, and so the authoring asset
    /// (<c>SeasonalWeatherDataAsset</c>, Couche 05) can default to them.
    /// <para>
    /// Source: official Météo-France 1991-2020 normals for the
    /// <b>Mortagne-Parc</b> station (Mortagne-au-Perche, Orne, indicatif
    /// MF61293003), retrieved via infoclimat. Annual mean 11.53 °C, annual
    /// precipitation 802.0 mm. The monthly mean temperatures and the monthly
    /// precipitation totals are the real station normals.
    /// </para>
    /// <para>
    /// Markov-rain parameters: <c>mu = ln(monthly_precip / rainy_days) −
    /// sigma² / 2</c> with <c>sigma = 0.80</c> held constant across months,
    /// so the expected monthly cumul (<c>days × p_wet × exp(mu + sigma²/2)</c>)
    /// matches the published total by construction. The wet-day frequency
    /// <c>p_wet</c> is provisional (carried over from the previous calibration)
    /// because it requires the daily series; it will be recomputed from the
    /// Météo-France daily open data by <c>tools/extract_weather_normals.py</c>.
    /// See docs/CALIBRATION.md §Saisonnalité.
    /// </para>
    /// </summary>
    public static class SeasonalWeatherDataDefaults
    {
        public const string DefaultSourceLabel =
            "Mortagne-Parc (MF61293003) — normales Météo-France 1991-2020 via infoclimat (11,53 °C / 802 mm)";

        public static SeasonalWeatherData MortagneAuPerche()
        {
            var months = new[]
            {
                // T° (°C),  p_wet,   mu,    sigma   (Mortagne-Parc 1991-2020)
                new MonthlyClimate( 4.6, 0.484, 1.344, 0.80), // Jan  79.2 mm
                new MonthlyClimate( 5.6, 0.429, 1.348, 0.80), // Fév  63.7 mm
                new MonthlyClimate( 8.0, 0.452, 1.156, 0.80), // Mar  61.3 mm
                new MonthlyClimate(10.4, 0.400, 1.165, 0.80), // Avr  53.0 mm
                new MonthlyClimate(14.1, 0.419, 1.318, 0.80), // Mai  66.8 mm
                new MonthlyClimate(17.1, 0.367, 1.312, 0.80), // Juin 56.3 mm
                new MonthlyClimate(19.1, 0.355, 1.325, 0.80), // Juil 57.0 mm
                new MonthlyClimate(19.1, 0.323, 1.341, 0.80), // Août 52.7 mm
                new MonthlyClimate(15.7, 0.367, 1.312, 0.80), // Sept 56.3 mm
                new MonthlyClimate(12.3, 0.452, 1.398, 0.80), // Oct  78.1 mm
                new MonthlyClimate( 7.7, 0.467, 1.440, 0.80), // Nov  81.4 mm
                new MonthlyClimate( 4.7, 0.484, 1.538, 0.80), // Déc  96.2 mm
            };
            return new SeasonalWeatherData(months, DefaultSourceLabel);
        }
    }
}
