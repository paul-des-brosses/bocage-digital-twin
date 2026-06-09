using System;

namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Moteur de simulation de la refonte : orchestre un tick = un jour. Génère
    /// la météo (perturbée par le scénario climat), puis applique les flux dans
    /// l'ordre causal du tick (cf <c>docs/refonte/10 §A.2</c>) :
    /// <code>
    ///   météo → fenêtres chaleur → eau (θ) → nappe → adventices → rendement
    ///         → azote → carbone → flore/densité → biodiversité → économie → jour+1
    /// </code>
    /// Les seules boucles circulaires (carbone↔azote↔rendement) sont résolues par
    /// un décalage d'un jour sur les variables lentes. Déterministe (même seed +
    /// même climatologie + mêmes décisions → même état). Aucune I/O.
    /// </summary>
    public sealed class SimulationEngine
    {
        private static readonly int[] MonthEndDay = { 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };

        private readonly WeatherGenerator _weather;
        private readonly int _startDayOffset;
        private readonly WaterBalanceRule _water = new WaterBalanceRule();
        private readonly NappeRule _nappe = new NappeRule();
        private readonly WeedPressureRule _weed = new WeedPressureRule();
        private readonly YieldRule _yield = new YieldRule();
        private readonly NitrogenDynamicsRule _nitrogen = new NitrogenDynamicsRule();
        private readonly CarbonDynamicsRule _carbon = new CarbonDynamicsRule();
        private readonly HedgeFloraRule _flora = new HedgeFloraRule();
        private readonly BiodiversityRule _biodiversity = new BiodiversityRule();
        private readonly EconomyRule _economy = new EconomyRule();

        public EcosystemModel Model { get; }
        public ScenarioContext Scenario { get; }

        public SimulationEngine(EcosystemModel model, ScenarioContext scenario, WeatherGenerator weather)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            _weather = weather ?? throw new ArgumentNullException(nameof(weather));
            // Snapshot du mois de démarrage : un changement mid-run ne décale pas
            // le run courant (cohérent avec la sémantique de Rebuild).
            _startDayOffset = StartDayOffsetForMonth(scenario.StartingMonth);
        }

        /// <summary>Mois calendaire (1-12) du jour de l'année (1-365).</summary>
        public static int MonthOfYear(int dayOfYear)
        {
            if (dayOfYear < 1) dayOfYear = 1;
            else if (dayOfYear > 365) dayOfYear = 365;
            for (int m = 0; m < 12; m++)
                if (dayOfYear <= MonthEndDay[m]) return m + 1;
            return 12;
        }

        /// <summary>
        /// Décalage en jours pour démarrer au mois <paramref name="startingMonth"/>
        /// (1-12) : nombre de jours calendaires avant le 1ᵉʳ de ce mois.
        /// </summary>
        public static int StartDayOffsetForMonth(int startingMonth)
        {
            if (startingMonth < 1) startingMonth = 1;
            else if (startingMonth > 12) startingMonth = 12;
            return startingMonth == 1 ? 0 : MonthEndDay[startingMonth - 2];
        }

        /// <summary>Jour calendaire courant (1-365), décalé par le mois de démarrage.</summary>
        public int CalendarDayOfYear => ((Model.CurrentDay + _startDayOffset) % 365) + 1;

        public void Tick()
        {
            int dayOfYear = CalendarDayOfYear;
            int month = MonthOfYear(dayOfYear);

            // Météo générée, perturbée par le scénario climat (ΔT additif, ×pluie).
            DailyWeather raw = _weather.Next(month);
            double dt = Scenario.TemperatureAnomalyC;
            double pf = Scenario.PrecipitationFactor;
            DailyWeather w = new DailyWeather(
                raw.TMinCelsius + dt, raw.TMaxCelsius + dt, raw.TMeanCelsius + dt, raw.PrecipMm * pf);
            Model.SetWeather(w);
            Model.RecordDailyTemperatureForWindow(w.TMeanCelsius);

            _water.Apply(Model, dayOfYear);
            _nappe.Apply(Model);
            _weed.Apply(Model, Scenario);
            _yield.Apply(Model, dayOfYear);
            _nitrogen.Apply(Model, Scenario, dayOfYear);
            _carbon.Apply(Model, Scenario);
            _flora.Apply(Model, Scenario);
            _biodiversity.Apply(Model, Scenario);
            _economy.Apply(Model, Scenario);

            Model.AdvanceDay();
        }

        /// <summary>Avance la simulation de <paramref name="days"/> jours.</summary>
        public void Run(int days)
        {
            for (int d = 0; d < days; d++) Tick();
        }
    }
}
