using System;
using Bocage.SimulationCore.Model;
using UnityEngine;

namespace Bocage.Presentation.Weather
{
    /// <summary>
    /// Authoring asset for the seasonal weather model (chantier E2 /
    /// ADR #52). Stores 12 monthly normals + Markov-rain parameters in a
    /// Unity-serialised form and exposes a
    /// <see cref="ToSeasonalWeatherData"/> conversion that yields the
    /// immutable, pure-C# <see cref="SeasonalWeatherData"/> consumed by
    /// <see cref="Bocage.SimulationCore.Rules.WeatherUpdateRule"/>.
    /// <para>
    /// On first creation, the asset is populated with the
    /// Mortagne-au-Perche calibration encoded in
    /// <see cref="SeasonalWeatherDataDefaults"/>. The Inspector then
    /// allows manual tweaking per month if the user wants to plug
    /// alternative normals (e.g. an RCP4.5 climatology).
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Bocage/Weather/Seasonal Weather Data",
        fileName = "SeasonalWeather_Mortagne")]
    public sealed class SeasonalWeatherDataAsset : ScriptableObject
    {
        [Serializable]
        private struct MonthlyAuthoring
        {
            [Tooltip("Read-only label for inspector clarity.")]
            public string label;

            [Tooltip("Monthly mean daily temperature in °C.")]
            public float temperatureMeanCelsius;

            [Range(0f, 1f), Tooltip("Probability that a given day is a wet day (Bernoulli parameter).")]
            public float probabilityWetDay;

            [Tooltip("Log-normal mean parameter for daily rain intensity in mm.")]
            public float logNormalMu;

            [Tooltip("Log-normal standard deviation for daily rain intensity in mm.")]
            public float logNormalSigma;
        }

        [Header("Identity")]
        [SerializeField, TextArea(2, 3), Tooltip("Source attribution persisted into the runtime SeasonalWeatherData.")]
        private string sourceLabel = SeasonalWeatherDataDefaults.DefaultSourceLabel;

        [Header("Monthly normals (index 0 = January, 11 = December)")]
        [SerializeField] private MonthlyAuthoring[] months = new MonthlyAuthoring[12];

        /// <summary>Build the immutable pure-C# view consumed by Couche 01.</summary>
        public SeasonalWeatherData ToSeasonalWeatherData()
        {
            if (months == null || months.Length != SeasonalWeatherData.MonthsPerYear)
            {
                FillWithMortagneAuPerche();
            }
            var pure = new MonthlyClimate[SeasonalWeatherData.MonthsPerYear];
            for (int i = 0; i < SeasonalWeatherData.MonthsPerYear; i++)
            {
                MonthlyAuthoring src = months[i];
                pure[i] = new MonthlyClimate(
                    src.temperatureMeanCelsius,
                    src.probabilityWetDay,
                    src.logNormalMu,
                    src.logNormalSigma);
            }
            return new SeasonalWeatherData(pure, string.IsNullOrEmpty(sourceLabel)
                ? SeasonalWeatherDataDefaults.DefaultSourceLabel
                : sourceLabel);
        }

        // Reset fires when the asset is first created (Right-click → Create →
        // Bocage → Weather → Seasonal Weather Data) and when the user clicks
        // the gear → Reset menu in the Inspector. We use it to seed the
        // 12 months with Mortagne-au-Perche normals so the asset is usable
        // out of the box.
        private void Reset()
        {
            FillWithMortagneAuPerche();
        }

        private void OnValidate()
        {
            if (months == null || months.Length != SeasonalWeatherData.MonthsPerYear)
            {
                FillWithMortagneAuPerche();
            }
        }

        private void FillWithMortagneAuPerche()
        {
            SeasonalWeatherData fixture = SeasonalWeatherDataDefaults.MortagneAuPerche();
            months = new MonthlyAuthoring[SeasonalWeatherData.MonthsPerYear];
            for (int i = 0; i < SeasonalWeatherData.MonthsPerYear; i++)
            {
                MonthlyClimate src = fixture.GetForMonth(i);
                months[i] = new MonthlyAuthoring
                {
                    label = MonthLabels[i],
                    temperatureMeanCelsius = (float)src.TemperatureMeanCelsius,
                    probabilityWetDay = (float)src.ProbabilityWetDay,
                    logNormalMu = (float)src.LogNormalMu,
                    logNormalSigma = (float)src.LogNormalSigma,
                };
            }
            sourceLabel = SeasonalWeatherDataDefaults.DefaultSourceLabel;
        }

        private static readonly string[] MonthLabels =
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };
    }
}
