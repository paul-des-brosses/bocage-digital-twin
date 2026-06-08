using System;
using Bocage.SimulationCore;
using Bocage.SimulationCore.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du générateur météo de la refonte (Couche 01). Vérifient le
    /// déterminisme (B7) et que les statistiques tirées convergent vers la
    /// climatologie de calibration : stationnaire de la chaîne de Markov,
    /// moyenne de température, persistance des épisodes pluvieux.
    /// </summary>
    public sealed class WeatherGeneratorRefonteTests
    {
        private const double TempMean = 11.0;
        private const double ProbWet = 0.35;
        private const double PWetAfterWet = 0.55;
        private const double PWetAfterDry = 0.22;
        private const double Phi = 0.75;
        private const double ResidStd = 2.1;

        // Climatologie synthétique UNIFORME (12 mois identiques) → valeurs
        // attendues analytiques simples, indépendantes du mois passé à Next().
        private static Climatology UniformClimatology()
        {
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
            {
                months[i] = new MonthlyClimate(
                    tempMeanCelsius: TempMean, tempStdCelsius: 3.2, diurnalRangeCelsius: 8.0,
                    precipTotalMm: 60.0, probWetDay: ProbWet,
                    pWetAfterWet: PWetAfterWet, pWetAfterDry: PWetAfterDry,
                    lognormalMu: 1.3, lognormalSigma: 0.85);
            }
            return new Climatology(months, Phi, ResidStd);
        }

        private static WeatherGenerator MakeGenerator(ulong seed)
            => new WeatherGenerator(
                UniformClimatology(),
                new SeededRandom(seed).DeriveSubStream(WeatherGenerator.SubStreamId));

        [Test]
        public void Same_seed_yields_identical_sequence()
        {
            var a = MakeGenerator(12345UL);
            var b = MakeGenerator(12345UL);
            for (int day = 0; day < 400; day++)
            {
                DailyWeather wa = a.Next(6);
                DailyWeather wb = b.Next(6);
                Assert.AreEqual(wa.TMeanCelsius, wb.TMeanCelsius, 1e-12, $"T° diverge au jour {day}");
                Assert.AreEqual(wa.PrecipMm, wb.PrecipMm, 1e-12, $"pluie diverge au jour {day}");
            }
        }

        [Test]
        public void Different_seeds_diverge()
        {
            var a = MakeGenerator(1UL);
            var b = MakeGenerator(2UL);
            bool diverged = false;
            for (int day = 0; day < 400 && !diverged; day++)
            {
                if (Math.Abs(a.Next(6).TMeanCelsius - b.Next(6).TMeanCelsius) > 1e-9)
                    diverged = true;
            }
            Assert.IsTrue(diverged, "Deux seeds différents devraient produire des séries différentes.");
        }

        [Test]
        public void Wet_day_fraction_converges_to_markov_stationary()
        {
            // Stationnaire d'une chaîne de Markov 2 états : π = P01 / (1 − P11 + P01).
            double expected = PWetAfterDry / (1.0 - PWetAfterWet + PWetAfterDry);
            var gen = MakeGenerator(99UL);
            int wet = 0, n = 40000;
            for (int day = 0; day < n; day++)
                if (gen.Next(6).PrecipMm > 0.0) wet++;
            double frac = (double)wet / n;
            Assert.AreEqual(expected, frac, 0.02,
                $"fraction pluvieuse {frac:F3} vs stationnaire attendu {expected:F3}");
        }

        [Test]
        public void Mean_temperature_matches_climatology()
        {
            var gen = MakeGenerator(7UL);
            double sum = 0.0; int n = 40000;
            for (int day = 0; day < n; day++) sum += gen.Next(6).TMeanCelsius;
            Assert.AreEqual(TempMean, sum / n, 0.2);
        }

        [Test]
        public void Rain_persists_more_after_wet_days()
        {
            var gen = MakeGenerator(3UL);
            int wwNum = 0, wwDen = 0, dwNum = 0, dwDen = 0;
            bool prevWet = false, first = true;
            int n = 60000;
            for (int day = 0; day < n; day++)
            {
                bool wet = gen.Next(6).PrecipMm > 0.0;
                if (!first)
                {
                    if (prevWet) { wwDen++; if (wet) wwNum++; }
                    else { dwDen++; if (wet) dwNum++; }
                }
                prevWet = wet; first = false;
            }
            double pWW = (double)wwNum / wwDen;
            double pDW = (double)dwNum / dwDen;
            Assert.Greater(pWW, pDW, "la pluie doit persister : P(pluie|pluie) > P(pluie|sec)");
            Assert.AreEqual(PWetAfterWet, pWW, 0.03, "P(pluie|pluie) doit retrouver le paramètre");
            Assert.AreEqual(PWetAfterDry, pDW, 0.03, "P(pluie|sec) doit retrouver le paramètre");
        }
    }
}
