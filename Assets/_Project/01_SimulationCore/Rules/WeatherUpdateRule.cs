using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Generates a daily weather value for the model. Centred on Perche
    /// climatology (annual mean temperature ≈ 12 °C, daily precipitation
    /// ≈ 2 mm). The scenario applies a temperature anomaly (°C) and a
    /// precipitation anomaly (%) directly to the daily means, which keeps
    /// the relationship between user inputs and weather behaviour
    /// transparent (a +3 °C anomaly shifts the mean by exactly 3 °C).
    /// <para>
    /// <b>No stochastic noise in v1.</b> Gaussian noise (σ = 3 °C,
    /// σ = 1.5 mm) was removed before publication (MVP_polish #1):
    /// without a seasonal cycle the noise produced unstructured
    /// day-to-day variation with no agronomic meaning, making event
    /// triggers harder to read without adding scientific value.
    /// The <c>rng</c> parameter is kept in the signature (contract of
    /// <see cref="IRule"/>) and will be consumed again when seasonal
    /// modulation is added (backlog #12).
    /// </para>
    /// </summary>
    public sealed class WeatherUpdateRule : IRule
    {
        public string SubStreamId => "weather";

        private const double BaseTemperatureC = 12.0;
        private const double BasePrecipitationMm = 2.0;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double tempAnomalyC = scenario.TemperatureAnomalyC.Current;
            double precipAnomalyPct = scenario.PrecipitationAnomalyPercent.Current;

            double temperature = BaseTemperatureC + tempAnomalyC;

            double precipitation = BasePrecipitationMm * (1.0 + precipAnomalyPct / 100.0);
            if (precipitation < 0.0) precipitation = 0.0;

            model.SetWeather(new Weather(temperature, precipitation));
        }
    }
}
