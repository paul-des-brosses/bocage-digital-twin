using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Generates a daily weather sample for the model. Centred on Perche
    /// climatology (annual mean temperature ≈ 12 °C, daily precipitation
    /// ≈ 2 mm). Climate stress raises temperature and reduces rainfall.
    /// Stochastic component added via the rule's RNG sub-stream so two
    /// runs sharing the same master seed see the same weather sequence.
    /// </summary>
    public sealed class WeatherUpdateRule : IRule
    {
        public string SubStreamId => "weather";

        private const double BaseTemperatureC = 12.0;
        private const double TemperatureStressGain = 5.0;
        private const double TemperatureNoiseStdDev = 3.0;

        private const double BasePrecipitationMm = 2.0;
        private const double PrecipitationStressFactor = 0.5;
        private const double PrecipitationNoiseStdDev = 1.5;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double climate = scenario.ClimateStress.Current;

            double meanT = BaseTemperatureC + TemperatureStressGain * climate;
            double temperature = rng.NextGaussian(meanT, TemperatureNoiseStdDev);

            double meanP = BasePrecipitationMm * (1.0 - PrecipitationStressFactor * climate);
            double precipitation = rng.NextGaussian(meanP, PrecipitationNoiseStdDev);

            model.SetWeather(new Weather(temperature, precipitation));
        }
    }
}
