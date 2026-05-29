namespace Bocage.SimulationCore.Model
{
    /// <summary>
    /// Hard-coded calibration of the Perche site used as the default
    /// payload for <see cref="SeasonalWeatherData"/>. Values are encoded
    /// here in pure C# so EditMode tests can build the fixture without
    /// loading a Unity asset, and so the authoring asset
    /// (<c>SeasonalWeatherDataAsset</c>, Couche 05) can default to them.
    /// <para>
    /// Source: modelled monthly normals for Mortagne-au-Perche (Orne, 61)
    /// retrieved from planificateur.a-contresens.net (NEMS 30 km
    /// reanalysis, fetched 2026-05-29). Annual mean 10.77 °C, annual
    /// precipitation 720.4 mm — matches the figures quoted by
    /// Météo-France for the area. The official Météo-France 1991-2020
    /// normals page (station 61293001) was not reachable at fetch time
    /// (HTTP 404); the modelled values are used as a fit-for-portfolio
    /// proxy. See docs/CALIBRATION.md §Saisonnalité for the verification
    /// TODO.
    /// </para>
    /// <para>
    /// Markov-rain parameters were derived as
    /// <c>p_wet = rainy_days / days_in_month</c> and
    /// <c>mu = ln(monthly_precip / rainy_days) − sigma² / 2</c> with
    /// <c>sigma = 0.80</c> held constant across months (typical for daily
    /// rain intensity log-normal models). This ensures the expected
    /// monthly cumul, <c>days_in_month × p_wet × exp(mu + sigma²/2)</c>,
    /// matches the observed normal by construction.
    /// </para>
    /// </summary>
    public static class SeasonalWeatherDataDefaults
    {
        public const string DefaultSourceLabel =
            "Mortagne-au-Perche modelled normals — planificateur.a-contresens.net (NEMS 30km reanalysis), fetched 2026-05-29";

        public static SeasonalWeatherData MortagneAuPerche()
        {
            var months = new[]
            {
                // T° (°C),  p_wet,   mu,    sigma
                new MonthlyClimate( 4.1, 0.484, 1.25, 0.80), // Jan
                new MonthlyClimate( 4.5, 0.429, 1.18, 0.80), // Fév
                new MonthlyClimate( 7.1, 0.452, 1.11, 0.80), // Mar
                new MonthlyClimate( 9.4, 0.400, 1.04, 0.80), // Avr
                new MonthlyClimate(13.0, 0.419, 1.30, 0.80), // Mai
                new MonthlyClimate(16.2, 0.367, 1.19, 0.80), // Juin
                new MonthlyClimate(18.3, 0.355, 1.21, 0.80), // Juil
                new MonthlyClimate(18.2, 0.323, 1.13, 0.80), // Août
                new MonthlyClimate(15.1, 0.367, 1.26, 0.80), // Sept
                new MonthlyClimate(11.4, 0.452, 1.36, 0.80), // Oct
                new MonthlyClimate( 7.1, 0.467, 1.24, 0.80), // Nov
                new MonthlyClimate( 4.8, 0.484, 1.42, 0.80), // Déc
            };
            return new SeasonalWeatherData(months, DefaultSourceLabel);
        }
    }
}
