using System;
using System.Collections.Generic;

namespace Bocage.SimulationCore.Model
{
    /// <summary>
    /// Per-month climatological parameters consumed by the seasonal weather
    /// model (chantier E2 / ADR #52). Twelve entries indexed 0 = January …
    /// 11 = December, plus the mapping from a simulated day-counter to the
    /// month it falls in for a given starting month.
    /// <para>
    /// The class is pure C# and lives in Couche 01 (no UnityEngine
    /// reference). Authoring assets in Couche 05 build instances of this
    /// class via <c>SeasonalWeatherDataAsset.ToSeasonalWeatherData()</c>
    /// and pass them to <see cref="Bocage.SimulationCore.Rules.WeatherUpdateRule"/>.
    /// </para>
    /// </summary>
    public sealed class SeasonalWeatherData
    {
        public const int MonthsPerYear = 12;
        public const int DaysPerYear = 365;

        // Calendar months, no leap year (sum = 365). The fixed calendar
        // keeps the day-to-month mapping trivially reversible and ensures
        // the monthly normals (T° mean, p_wet, mu, sigma) line up with the
        // days they were averaged over.
        private static readonly int[] DaysInMonthArray =
            { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        private readonly MonthlyClimate[] _months;

        /// <summary>Free-text source label persisted from the authoring asset.</summary>
        public string SourceLabel { get; }

        public SeasonalWeatherData(IReadOnlyList<MonthlyClimate> months, string sourceLabel)
        {
            if (months == null) throw new ArgumentNullException(nameof(months));
            if (months.Count != MonthsPerYear)
                throw new ArgumentException(
                    $"SeasonalWeatherData expects exactly {MonthsPerYear} monthly entries, got {months.Count}.",
                    nameof(months));

            _months = new MonthlyClimate[MonthsPerYear];
            for (int i = 0; i < MonthsPerYear; i++) _months[i] = months[i];
            SourceLabel = sourceLabel ?? "(unspecified)";
        }

        /// <summary>Returns the monthly normals for a 0-based month index (0 = January).</summary>
        public MonthlyClimate GetForMonth(int monthIndexZeroBased)
        {
            if (monthIndexZeroBased < 0 || monthIndexZeroBased >= MonthsPerYear)
                throw new ArgumentOutOfRangeException(nameof(monthIndexZeroBased));
            return _months[monthIndexZeroBased];
        }

        /// <summary>Number of days in the given 0-based month (no leap year).</summary>
        public static int DaysIn(int monthIndexZeroBased)
        {
            if (monthIndexZeroBased < 0 || monthIndexZeroBased >= MonthsPerYear)
                throw new ArgumentOutOfRangeException(nameof(monthIndexZeroBased));
            return DaysInMonthArray[monthIndexZeroBased];
        }

        /// <summary>
        /// Maps a simulated day-counter to its 0-based month index, given the
        /// 1-based starting month at day 0. Day 0 always lands on the first
        /// day of the starting month. Wraps every 365 days.
        /// </summary>
        public static int MonthIndexForDay(int currentDay, int startingMonthOneBased)
        {
            if (startingMonthOneBased < 1 || startingMonthOneBased > MonthsPerYear)
                throw new ArgumentOutOfRangeException(nameof(startingMonthOneBased));

            int dayInYear = ((currentDay % DaysPerYear) + DaysPerYear) % DaysPerYear;
            int month = startingMonthOneBased - 1;
            int accumulated = 0;
            while (true)
            {
                int daysThisMonth = DaysInMonthArray[month];
                if (dayInYear < accumulated + daysThisMonth) return month;
                accumulated += daysThisMonth;
                month = (month + 1) % MonthsPerYear;
            }
        }
    }

    /// <summary>
    /// Climatological normals for one month: mean temperature plus the three
    /// Markov-rain parameters (probability of a wet day and log-normal
    /// parameters of daily intensity in mm).
    /// </summary>
    public readonly struct MonthlyClimate
    {
        public double TemperatureMeanCelsius { get; }
        public double ProbabilityWetDay { get; }
        public double LogNormalMu { get; }
        public double LogNormalSigma { get; }

        public MonthlyClimate(
            double temperatureMeanCelsius,
            double probabilityWetDay,
            double logNormalMu,
            double logNormalSigma)
        {
            TemperatureMeanCelsius = temperatureMeanCelsius;
            ProbabilityWetDay = probabilityWetDay < 0.0 ? 0.0 : (probabilityWetDay > 1.0 ? 1.0 : probabilityWetDay);
            LogNormalMu = logNormalMu;
            LogNormalSigma = logNormalSigma < 0.0 ? 0.0 : logNormalSigma;
        }
    }
}
