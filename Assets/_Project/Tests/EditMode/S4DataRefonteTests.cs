using Bocage.SimulationCore.Refonte;
using Bocage.Decision.Refonte;
using NUnit.Framework;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Données exposées pour les onglets Niveau B (S4) : la décomposition de la
    /// marge (sa somme EST la marge totale, pas de recomputation parallèle) et la
    /// fenêtre glissante météo + dernier flux de la session (onglet Climat).
    /// </summary>
    public sealed class S4DataRefonteTests
    {
        private static Climatology UniformClimatology()
        {
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(11.0, 3.2, 9.0, 60.0, 0.33, 0.55, 0.22, 1.35, 0.85);
            return new Climatology(months, 0.75, 2.1);
        }

        [Test]
        public void Margin_breakdown_sums_to_the_total_margin()
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext { NitrogenDoseKgPerHaPerYear = 120.0, PesticideIntensity = 1.0, TillageIntensity = 1.0 };
            MarginBreakdown b = EconomyRule.Breakdown(model, scenario);
            Assert.AreEqual(EconomyRule.AnnualMarginEurosPerHa(model, scenario), b.TotalEurosPerHa, 1e-9,
                "la somme des postes EST la marge totale");
            Assert.AreEqual(356.0, b.TotalEurosPerHa, 1e-6, "marge de référence Perche");
        }

        [Test]
        public void Margin_breakdown_exposes_the_service_payments()
        {
            var model = new EcosystemModel(initialHedgerowDensityMPerHa: 90.0);
            var scenario = new ScenarioContext { PesticideIntensity = 0.5 }; // IFT bas → MAEC
            MarginBreakdown b = EconomyRule.Breakdown(model, scenario);
            Assert.AreEqual(220.0, b.PacEurosPerHa, 1e-9, "PAC forfait");
            Assert.AreEqual(45.0, b.PseEurosPerHa, 1e-9, "PSE = 90 m/ha × 0.5");
            Assert.AreEqual(90.0, b.MaecEurosPerHa, 1e-9, "MAEC car IFT ≤ 0.7");
        }

        [Test]
        public void Session_tracks_recent_weather_and_flux()
        {
            var session = new SimulationSession(new EcosystemModel(), new ScenarioContext(), UniformClimatology(), 100UL);
            session.Run(120);
            Assert.That(session.MeanRecentTemperatureC, Is.InRange(7.0, 15.0), "T° moyenne récente cohérente avec la climato (~11 °C)");
            Assert.Greater(session.RecentPrecipitationCumulMm, 0.0, "du cumul de pluie sur la fenêtre");
            Assert.AreNotEqual(0.0, session.LastFluxKgCo2, "le flux CO2 du dernier jour est exposé");
        }
    }
}
