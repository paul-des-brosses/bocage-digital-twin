using Bocage.SimulationCore;
using NUnit.Framework;
using SeededRandom = Bocage.SimulationCore.SeededRandom;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Régression de la calibration de la réponse azotée du rendement, verrouillée
    /// après la passe sourcée Arvalis / COMIFER / INRAE (cf docs/refonte/08 §5.5).
    /// Sur la vraie climatologie Tourouvre et à l'état stationnaire, la courbe
    /// dose→rendement doit : (a) viser la cible rotation ~5,5 t/ha à la dose
    /// Référence (Agreste blé/colza Orne–Eure-et-Loir), (b) garder un plancher
    /// N=0 agronomique (~50 % du plateau, pas l'effondrement — minéralisation Mh,
    /// INRAE), (c) plafonner vers l'optimum (Mitscherlich saturant).
    /// </summary>
    public sealed class NitrogenResponseCalibrationTests
    {
        // VRAIE climatologie Tourouvre (Perche, Orne) — copiée de
        // SimulationRunner.TourouvreClimatology() pour que les rendements
        // absolus correspondent au jeu (pas un climat synthétique).
        private static Climatology TourouvreClimatology()
        {
            double[] tmean = { 3.97, 4.91, 7.23, 9.76, 13.05, 16.69, 18.61, 18.25, 15.53, 11.81, 7.66, 4.87 };
            double[] tstd = { 3.74, 3.72, 3.04, 3.19, 3.11, 3.14, 2.96, 3.01, 3.09, 3.19, 3.12, 3.69 };
            double[] diurn = { 5.25, 6.8, 8.52, 10.79, 10.82, 11.26, 12.38, 11.82, 10.92, 8.18, 6.13, 5.42 };
            double[] precip = { 72.6, 54.8, 58.9, 49.6, 64.7, 62.9, 51.6, 51.1, 54.1, 73.0, 74.4, 83.9 };
            double[] pwet = { 0.417, 0.385, 0.368, 0.286, 0.313, 0.316, 0.25, 0.269, 0.29, 0.372, 0.4, 0.441 };
            double[] p11 = { 0.616, 0.611, 0.648, 0.562, 0.494, 0.509, 0.366, 0.407, 0.531, 0.585, 0.581, 0.614 };
            double[] p01 = { 0.28, 0.243, 0.202, 0.178, 0.227, 0.228, 0.211, 0.22, 0.193, 0.245, 0.279, 0.301 };
            double[] mu = { 1.344, 1.273, 1.306, 1.366, 1.436, 1.439, 1.364, 1.345, 1.36, 1.403, 1.402, 1.43 };
            double[] sig = { 0.834, 0.793, 0.775, 0.829, 0.902, 0.902, 0.982, 0.881, 0.884, 0.858, 0.864, 0.838 };
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(tmean[i], tstd[i], diurn[i], precip[i], pwet[i], p11[i], p01[i], mu[i], sig[i]);
            return new Climatology(months, 0.75, 2.157);
        }

        // Rendement stationnaire (t/ha) pour une dose d'azote, leviers Référence.
        private static double SteadyYield(double nitrogenDose)
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext
            {
                NitrogenDoseKgPerHaPerYear = nitrogenDose,
                PesticideIntensity = 1.0,
                TillageIntensity = 1.0,
                GrasslandFraction = 0.0,
                CoverCropsCoveragePercent = 0.0
            };
            var weather = new WeatherGenerator(
                TourouvreClimatology(),
                new SeededRandom(12345UL).DeriveSubStream(WeatherGenerator.SubStreamId));
            new SimulationEngine(model, scenario, weather).Run(25 * 365); // → stationnaire
            return model.CropYieldTPerHa;
        }

        [Test]
        public void Reference_dose_hits_rotation_target()
        {
            Assert.That(SteadyYield(120.0), Is.InRange(5.3, 5.7),
                "dose Référence N120 → ~5,5 t/ha (cible rotation blé/colza, Agreste)");
        }

        [Test]
        public void Zero_nitrogen_floor_stays_agronomic()
        {
            double plateau = SteadyYield(200.0);
            double floor = SteadyYield(0.0);
            Assert.That(floor / plateau, Is.InRange(0.45, 0.60),
                "sans azote, le sol minéralise ~la moitié du potentiel (pas d'effondrement) — INRAE Mh");
        }

        [Test]
        public void Response_saturates_near_optimum()
        {
            double atOptimum = SteadyYield(160.0);
            double overdosed = SteadyYield(240.0);
            Assert.That(atOptimum, Is.InRange(0.98 * overdosed, 1.02 * overdosed),
                "le rendement plafonne : au-delà de ~N160, doubler l'azote ne gagne ~rien (Mitscherlich)");
        }

        [Test]
        public void Interannual_yield_variation_is_realistic()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext
            {
                NitrogenDoseKgPerHaPerYear = 120.0,
                PesticideIntensity = 1.0,
                TillageIntensity = 1.0,
                GrasslandFraction = 0.0,
                CoverCropsCoveragePercent = 0.0
            };
            var weather = new WeatherGenerator(
                TourouvreClimatology(),
                new SeededRandom(987654321UL).DeriveSubStream(WeatherGenerator.SubStreamId));
            var engine = new SimulationEngine(model, scenario, weather);
            engine.Run(8 * 365); // warm-up → stationnaire

            const int years = 25;
            double sum = 0.0, sumSq = 0.0, min = double.MaxValue, max = double.MinValue;
            for (int k = 0; k < years; k++)
            {
                engine.Run(365);
                double y = model.CropYieldTPerHa; // récolte de l'année (figée au jour 210)
                sum += y; sumSq += y * y;
                if (y < min) min = y;
                if (y > max) max = y;
            }
            double mean = sum / years;
            double cv = System.Math.Sqrt(sumSq / years - mean * mean) / mean;

            // La météo stochastique doit produire une variabilité interannuelle
            // réaliste (blé FR ~10-18 % de CV) : ni figée (incohérent), ni chaotique.
            Assert.That(cv, Is.InRange(0.06, 0.22),
                "le rendement varie d'une année sur l'autre comme un vrai blé, piloté par la météo");
            Assert.That(max - min, Is.GreaterThan(1.0),
                "l'écart bonne année / mauvaise année est franc (> 1 t/ha)");
        }
    }
}
