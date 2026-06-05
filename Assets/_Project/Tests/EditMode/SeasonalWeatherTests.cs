using System;
using System.Collections.Generic;
using Bocage.Sensors;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Rules;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the seasonal weather model (chantier E2 / ADR #52):
    /// pure-C# components <see cref="SeasonalWeatherData"/>,
    /// <see cref="MarkovRainModel"/>, the heat-day buffer in
    /// <see cref="EcosystemModel"/>, and the Couche 02
    /// <see cref="WeatherStationReader"/>. The seasonal behaviour of
    /// <see cref="WeatherUpdateRule"/> itself is covered by
    /// <c>WeatherUpdateRuleTests</c> in <c>BiophysicalRulesTests.cs</c>;
    /// these tests target the components it composes.
    /// </summary>
    public sealed class SeasonalWeatherDataTests
    {
        [Test]
        public void MonthIndexForDay_StartingInJanuary_WalksThroughMonths()
        {
            // Calendar months, January start: day 0 = Jan day 1, day 30 = Jan
            // day 31, day 31 = Feb day 1, etc.
            Assert.AreEqual(0, SeasonalWeatherData.MonthIndexForDay(0, 1));   // Jan day 1
            Assert.AreEqual(0, SeasonalWeatherData.MonthIndexForDay(30, 1));  // Jan day 31
            Assert.AreEqual(1, SeasonalWeatherData.MonthIndexForDay(31, 1));  // Feb day 1
            Assert.AreEqual(1, SeasonalWeatherData.MonthIndexForDay(58, 1));  // Feb day 28
            Assert.AreEqual(2, SeasonalWeatherData.MonthIndexForDay(59, 1));  // Mar day 1
            Assert.AreEqual(11, SeasonalWeatherData.MonthIndexForDay(334, 1)); // Dec day 1
            Assert.AreEqual(11, SeasonalWeatherData.MonthIndexForDay(364, 1)); // Dec day 31
            Assert.AreEqual(0, SeasonalWeatherData.MonthIndexForDay(365, 1));  // wraps to Jan day 1
        }

        [Test]
        public void MonthIndexForDay_StartingInJuly_WalksThroughMonths()
        {
            // July start (1-based 7 = 0-based index 6).
            // Day 0 = July day 1. July has 31 days → day 30 = July day 31,
            // day 31 = Aug day 1.
            Assert.AreEqual(6, SeasonalWeatherData.MonthIndexForDay(0, 7));   // Jul day 1
            Assert.AreEqual(6, SeasonalWeatherData.MonthIndexForDay(30, 7));  // Jul day 31
            Assert.AreEqual(7, SeasonalWeatherData.MonthIndexForDay(31, 7));  // Aug day 1
            // From July through December = 31+31+30+31+30+31 = 184 days
            Assert.AreEqual(11, SeasonalWeatherData.MonthIndexForDay(183, 7)); // Dec day 31
            Assert.AreEqual(0, SeasonalWeatherData.MonthIndexForDay(184, 7));  // Jan day 1 (wrapping)
        }

        [Test]
        public void DefaultMortagneAuPercheCalibrationIsInternallyConsistent()
        {
            // Annual mean T° from the encoded 12 monthly values should land
            // close to the Mortagne-Parc reference (≈ 11.5 °C). Annual
            // expected precipitation from p_wet × E[log-normal] × days_in_month
            // should land close to the reference (≈ 802 mm).
            var data = SeasonalWeatherDataDefaults.MortagneAuPerche();

            double annualTempSum = 0.0;
            double annualPrecipExpected = 0.0;
            for (int m = 0; m < 12; m++)
            {
                MonthlyClimate climate = data.GetForMonth(m);
                annualTempSum += climate.TemperatureMeanCelsius;
                double expectedDailyIntensity = Math.Exp(
                    climate.LogNormalMu + 0.5 * climate.LogNormalSigma * climate.LogNormalSigma);
                annualPrecipExpected += SeasonalWeatherData.DaysIn(m)
                                        * climate.ProbabilityWetDay
                                        * expectedDailyIntensity;
            }
            double annualMeanTemp = annualTempSum / 12.0;
            Assert.That(annualMeanTemp, Is.EqualTo(11.53).Within(0.2),
                "Annual mean of encoded monthly T° should match the Mortagne-Parc Météo-France reference. Got " + annualMeanTemp);
            Assert.That(annualPrecipExpected, Is.EqualTo(802.0).Within(40.0),
                "Annual expected precipitation reconstructed from Markov params should match the 802 mm reference. Got " + annualPrecipExpected);
        }
    }

    public sealed class MarkovRainModelTests
    {
        [Test]
        public void DeterministicForSameSeed()
        {
            var month = new MonthlyClimate(10.0, 0.5, 1.25, 0.80);
            var rngA = new SeededRandom(123UL).DeriveSubStream("markov-rain");
            var rngB = new SeededRandom(123UL).DeriveSubStream("markov-rain");

            for (int i = 0; i < 50; i++)
            {
                var (wetA, mmA) = MarkovRainModel.Draw(month, rngA);
                var (wetB, mmB) = MarkovRainModel.Draw(month, rngB);
                Assert.AreEqual(wetA, wetB);
                Assert.AreEqual(mmA, mmB);
            }
        }

        [Test]
        public void EmpiricalWetDayFrequencyMatchesParameter()
        {
            // Across 10000 draws, the wet-day frequency should converge to
            // the Bernoulli probability within ~1 % (standard error ≈ √(p(1-p)/n)).
            var month = new MonthlyClimate(10.0, 0.40, 1.25, 0.80);
            var rng = new SeededRandom(7UL).DeriveSubStream("markov-rain");

            int wetCount = 0;
            const int n = 10000;
            for (int i = 0; i < n; i++)
            {
                var (wet, _) = MarkovRainModel.Draw(month, rng);
                if (wet) wetCount++;
            }
            double frequency = wetCount / (double)n;
            Assert.That(frequency, Is.EqualTo(0.40).Within(0.02),
                "Empirical wet-day frequency should match the configured p_wet within ±2 %. Got " + frequency);
        }

        [Test]
        public void DryDayReturnsZeroPrecipitation()
        {
            // p_wet = 0 → every draw is dry → 0 mm.
            var month = new MonthlyClimate(10.0, 0.0, 1.25, 0.80);
            var rng = new SeededRandom(11UL).DeriveSubStream("markov-rain");

            for (int i = 0; i < 100; i++)
            {
                var (wet, mm) = MarkovRainModel.Draw(month, rng);
                Assert.IsFalse(wet);
                Assert.AreEqual(0.0, mm);
            }
        }

        [Test]
        public void WetDayMeanIntensityMatchesLogNormalExpectation()
        {
            // p_wet = 1 forces every draw wet. Expected daily intensity
            // = exp(mu + sigma²/2). For January normals (mu = 1.25,
            // sigma = 0.80), that is exp(1.57) ≈ 4.81 mm.
            var month = new MonthlyClimate(10.0, 1.0, 1.25, 0.80);
            var rng = new SeededRandom(13UL).DeriveSubStream("markov-rain");

            double sum = 0.0;
            const int n = 10000;
            for (int i = 0; i < n; i++)
            {
                var (_, mm) = MarkovRainModel.Draw(month, rng);
                sum += mm;
            }
            double empiricalMean = sum / n;
            double expectedMean = Math.Exp(1.25 + 0.5 * 0.80 * 0.80);
            Assert.That(empiricalMean, Is.EqualTo(expectedMean).Within(0.5),
                "Empirical mean intensity over 10000 wet-day draws should match exp(mu + sigma²/2). Got "
                + empiricalMean + " vs expected " + expectedMean);
        }
    }

    public sealed class HeatDayWindowTests
    {
        [Test]
        public void RecentHeatDayCountStartsAtZero()
        {
            var model = new EcosystemModel();
            Assert.AreEqual(0, model.RecentHeatDayCount);
        }

        [Test]
        public void SingleHotDayIncrementsCount()
        {
            var model = new EcosystemModel();
            model.RecordDailyTemperatureForWindow(28.0); // > 25 °C threshold
            Assert.AreEqual(1, model.RecentHeatDayCount);

            model.RecordDailyTemperatureForWindow(15.0); // cool day
            Assert.AreEqual(1, model.RecentHeatDayCount);
        }

        [Test]
        public void OldEntriesExpireWhenWindowFills()
        {
            var model = new EcosystemModel();
            // Day 1: hot
            model.RecordDailyTemperatureForWindow(30.0);
            // Days 2..30: cool
            for (int i = 0; i < EcosystemModel.HeatDayWindowDays - 1; i++)
            {
                model.RecordDailyTemperatureForWindow(10.0);
            }
            Assert.AreEqual(1, model.RecentHeatDayCount);

            // Day 31: cool. This overwrites the old hot day at the head.
            model.RecordDailyTemperatureForWindow(10.0);
            Assert.AreEqual(0, model.RecentHeatDayCount);
        }
    }

    public sealed class WeatherStationReaderTests
    {
        [Test]
        public void ReadingMatchesTruthWithinNoiseEnvelope()
        {
            // σ_T = 0.3 °C → empirical mean over many draws should sit within
            // a small envelope around the true value.
            var reader = new WeatherStationReader(new SeededRandom(1UL));
            var truth = new Weather(18.0, 4.0);

            double tempSum = 0.0, precipSum = 0.0;
            const int n = 5000;
            for (int i = 0; i < n; i++)
            {
                Weather observed = reader.Read(truth);
                tempSum += observed.TemperatureCelsius;
                precipSum += observed.PrecipitationMillimeters;
            }
            double tempMean = tempSum / n;
            double precipMean = precipSum / n;
            Assert.That(tempMean, Is.EqualTo(18.0).Within(0.05));
            Assert.That(precipMean, Is.EqualTo(4.0).Within(0.05));
        }

        [Test]
        public void RecordHistoryFillsSlidingWindow()
        {
            var reader = new WeatherStationReader(new SeededRandom(2UL));

            // Record exactly the window's capacity + a few extra to make sure
            // the buffer wraps without losing the most recent samples.
            for (int i = 0; i < WeatherStationReader.HistoryWindowDays + 50; i++)
            {
                reader.ReadAndRecord(new Weather(10.0 + i * 0.01, 2.0));
            }
            Assert.AreEqual(WeatherStationReader.HistoryWindowDays, reader.HistoryCount);

            var snapshot = new List<Weather>();
            int copied = reader.CopyHistoryTo(snapshot);
            Assert.AreEqual(WeatherStationReader.HistoryWindowDays, copied);
            Assert.AreEqual(WeatherStationReader.HistoryWindowDays, snapshot.Count);

            // The oldest sample in the buffer must be from day i = 50 (we
            // pushed 365 + 50 samples, so the first 50 dropped). Its truth
            // temperature was 10.0 + 50*0.01 = 10.5 °C. The observed value
            // is ±~1 °C of that under σ = 0.3 (clear envelope at 3σ).
            Assert.That(snapshot[0].TemperatureCelsius, Is.EqualTo(10.5).Within(1.0),
                "Oldest sample in buffer should be the truth at day 50 ± noise. Got " + snapshot[0].TemperatureCelsius);
        }

        [Test]
        public void DeterministicForSameSeed()
        {
            var readerA = new WeatherStationReader(new SeededRandom(42UL));
            var readerB = new WeatherStationReader(new SeededRandom(42UL));
            for (int i = 0; i < 100; i++)
            {
                var truth = new Weather(10.0 + i * 0.1, i % 5);
                Weather a = readerA.Read(truth);
                Weather b = readerB.Read(truth);
                Assert.AreEqual(a.TemperatureCelsius, b.TemperatureCelsius);
                Assert.AreEqual(a.PrecipitationMillimeters, b.PrecipitationMillimeters);
            }
        }
    }
}
