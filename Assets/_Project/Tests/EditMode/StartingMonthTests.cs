using Bocage.SimulationCore;
using NUnit.Framework;
using SeededRandom = Bocage.SimulationCore.SeededRandom;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// Tests du mois de démarrage (S0b) : le décalage calendaire tombe bien sur le
    /// 1ᵉʳ du mois, le moteur démarre dans le mois choisi et progresse, la météo
    /// générée suit la saison (juillet plus chaud que janvier), et le déterminisme
    /// tient (même seed + même mois → même trajectoire).
    /// </summary>
    public sealed class StartingMonthTests
    {
        private static Climatology UniformClimatology()
        {
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(11.0, 3.2, 9.0, 60.0, 0.33, 0.55, 0.22, 1.35, 0.85);
            return new Climatology(months, 0.75, 2.1);
        }

        // Climatologie franchement saisonnière : janvier froid (2 °C), juillet chaud
        // (22 °C). Paramètres de pluie identiques tous mois → seule la température
        // (donc la saison) distingue les runs.
        private static Climatology SeasonalClimatology()
        {
            double[] temps = { 2, 4, 8, 11, 15, 19, 22, 21, 17, 12, 7, 3 };
            var months = new MonthlyClimate[12];
            for (int i = 0; i < 12; i++)
                months[i] = new MonthlyClimate(temps[i], 3.0, 9.0, 60.0, 0.33, 0.55, 0.22, 1.35, 0.85);
            return new Climatology(months, 0.5, 1.5);
        }

        private static SimulationEngine MakeEngine(Climatology clim, int startingMonth, ulong seed)
        {
            var model = new EcosystemModel();
            var scenario = new ScenarioContext { StartingMonth = startingMonth };
            var weather = new WeatherGenerator(clim, new SeededRandom(seed).DeriveSubStream(WeatherGenerator.SubStreamId));
            return new SimulationEngine(model, scenario, weather);
        }

        [Test]
        public void Offset_maps_each_month_to_its_first_day()
        {
            Assert.AreEqual(0, SimulationEngine.StartDayOffsetForMonth(1));
            Assert.AreEqual(31, SimulationEngine.StartDayOffsetForMonth(2));
            Assert.AreEqual(181, SimulationEngine.StartDayOffsetForMonth(7));
            Assert.AreEqual(334, SimulationEngine.StartDayOffsetForMonth(12));
            foreach (int m in new[] { 1, 4, 7, 10, 12 })
            {
                int dayOfYear = (SimulationEngine.StartDayOffsetForMonth(m) % 365) + 1;
                Assert.AreEqual(m, SimulationEngine.MonthOfYear(dayOfYear), "le décalage tombe bien sur le 1er du mois");
            }
        }

        [Test]
        public void Engine_starts_in_the_chosen_month_and_advances()
        {
            var july = MakeEngine(UniformClimatology(), 7, 1UL);
            Assert.AreEqual(182, july.CalendarDayOfYear, "jour 0 d'un run de juillet = jour calendaire 182");
            Assert.AreEqual(7, SimulationEngine.MonthOfYear(july.CalendarDayOfYear));
            july.Run(31);
            Assert.AreEqual(8, SimulationEngine.MonthOfYear(july.CalendarDayOfYear), "après 31 jours on est passé en août");
        }

        [Test]
        public void January_start_is_offset_zero()
        {
            var jan = MakeEngine(UniformClimatology(), 1, 1UL);
            Assert.AreEqual(1, jan.CalendarDayOfYear, "à janvier le décalage est nul (rétro-compat stricte)");
        }

        [Test]
        public void July_start_is_warmer_than_january_start()
        {
            var jan = MakeEngine(SeasonalClimatology(), 1, 5UL);
            var july = MakeEngine(SeasonalClimatology(), 7, 5UL);
            jan.Run(10);
            july.Run(10);
            Assert.Greater(july.Model.CurrentWeather.TMeanCelsius, jan.Model.CurrentWeather.TMeanCelsius,
                "un run de juillet génère une météo plus chaude qu'un run de janvier");
        }

        [Test]
        public void Determinism_same_seed_and_month()
        {
            var a = MakeEngine(UniformClimatology(), 7, 9UL);
            var b = MakeEngine(UniformClimatology(), 7, 9UL);
            a.Run(400);
            b.Run(400);
            Assert.AreEqual(a.Model.CropYieldTPerHa, b.Model.CropYieldTPerHa, 1e-9);
            Assert.AreEqual(a.Model.CapitalEurosPerHa, b.Model.CapitalEurosPerHa, 1e-9);
        }
    }
}
