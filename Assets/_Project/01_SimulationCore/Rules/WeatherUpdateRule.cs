using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Generates a daily weather sample for the model. Centred on Perche
    /// climatology (annual mean temperature ≈ 12 °C, daily precipitation
    /// ≈ 2 mm). The scenario applies a temperature anomaly (°C) and a
    /// precipitation anomaly (%) directly to the daily means, which keeps
    /// the relationship between user inputs and weather behaviour
    /// transparent (a +3°C anomaly shifts the mean by exactly 3°C).
    /// Stochastic component added via the rule's RNG sub-stream so two
    /// runs sharing the same master seed see the same weather sequence.
    /// </summary>
    public sealed class WeatherUpdateRule : IRule
    {
        public string SubStreamId => "weather";

        private const double BaseTemperatureC = 12.0;
        private const double TemperatureNoiseStdDev = 3.0;

        private const double BasePrecipitationMm = 2.0;
        private const double PrecipitationNoiseStdDev = 1.5;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double tempAnomalyC = scenario.TemperatureAnomalyC.Current;
            double precipAnomalyPct = scenario.PrecipitationAnomalyPercent.Current;

            double meanT = BaseTemperatureC + tempAnomalyC;
            double temperature = rng.NextGaussian(meanT, TemperatureNoiseStdDev);

            double meanP = BasePrecipitationMm * (1.0 + precipAnomalyPct / 100.0);
            if (meanP < 0.0) meanP = 0.0;
            double precipitation = rng.NextGaussian(meanP, PrecipitationNoiseStdDev);
            if (precipitation < 0.0) precipitation = 0.0;

            model.SetWeather(new Weather(temperature, precipitation));
        }
    }
}
