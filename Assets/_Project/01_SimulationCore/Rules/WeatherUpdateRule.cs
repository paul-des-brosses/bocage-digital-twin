using System;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Seasonal weather generator for the modelled Perche site
    /// (chantier E2 / ADR #52). Each tick:
    /// <list type="number">
    ///   <item>Look up the month for <c>model.CurrentDay</c> given
    ///         <c>scenario.StartingMonth</c>.</item>
    ///   <item>Draw daily precipitation via
    ///         <see cref="MarkovRainModel"/> (Bernoulli on <c>p_wet</c>
    ///         then LogNormal on intensity) on the
    ///         <c>"markov-rain"</c> sub-stream.</item>
    ///   <item>Draw daily temperature as
    ///         <c>T_month + N(0, σ = 2 °C)</c> on the
    ///         <c>"weather-noise"</c> sub-stream.</item>
    ///   <item>Apply scenario anomalies: additive on °C, multiplicative
    ///         on mm.</item>
    /// </list>
    /// <para>
    /// The two child sub-streams are derived from the rule's own RNG
    /// (passed in by <see cref="SimulationEngine"/>) on first apply so
    /// the rain and noise sequences stay independent of one another and
    /// of every other rule.
    /// </para>
    /// <para>
    /// The previous implementation (BaseTemperatureC = 12 °C, BasePrecipitationMm
    /// = 2 mm constant) was retired here: it gave the same distribution to
    /// every day of the year, which was the most visible scientific gap of
    /// the digital twin. Annual averages of the new seasonal output
    /// (≈ 10.8 °C, ≈ 720 mm) stay aligned with the prior constants so
    /// downstream economic and biophysical calibration windows remain valid.
    /// </para>
    /// </summary>
    public sealed class WeatherUpdateRule : IRule
    {
        public string SubStreamId => "weather";

        public const double TemperatureNoiseSigmaC = 2.0;

        private readonly SeasonalWeatherData _data;
        private readonly int _startingMonth;
        private SeededRandom _markovRng;
        private SeededRandom _noiseRng;
        private bool _streamsInitialized;

        public WeatherUpdateRule(SeasonalWeatherData data, int startingMonth = 1)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            // Snapshot the starting month at construction so the seasonal
            // cycle stays continuous across the run even if the user
            // changes ScenarioContext.StartingMonth mid-simulation — the
            // change only takes effect on the next engine rebuild
            // (SimulationRunner.Rebuild), which is the contract documented
            // in ROADMAP §E2: "Reset only at CurrentDay == 0".
            _startingMonth = startingMonth < 1 ? 1 : (startingMonth > 12 ? 12 : startingMonth);
        }

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            if (!_streamsInitialized)
            {
                // Sub-streams derived on first tick: the rule receives one
                // RNG from the engine (its own per-rule stream) and splits
                // it into two named children so reordering Markov draws and
                // gaussian noise draws can never shift one another's
                // sequence.
                _markovRng = rng.DeriveSubStream("markov-rain");
                _noiseRng = rng.DeriveSubStream("weather-noise");
                _streamsInitialized = true;
            }

            int monthIndex = SeasonalWeatherData.MonthIndexForDay(model.CurrentDay, _startingMonth);
            MonthlyClimate climate = _data.GetForMonth(monthIndex);

            double tempAnomalyC = scenario.TemperatureAnomalyC.Current;
            double precipAnomalyPct = scenario.PrecipitationAnomalyPercent.Current;

            double temperature = climate.TemperatureMeanCelsius
                                 + _noiseRng.NextGaussian(0.0, TemperatureNoiseSigmaC)
                                 + tempAnomalyC;

            (_, double precipitationBase) = MarkovRainModel.Draw(climate, _markovRng);
            double precipitation = precipitationBase * (1.0 + precipAnomalyPct / 100.0);
            if (precipitation < 0.0) precipitation = 0.0;

            model.SetWeather(new Weather(temperature, precipitation));
            model.RecordDailyTemperatureForWindow(temperature);
        }
    }
}
