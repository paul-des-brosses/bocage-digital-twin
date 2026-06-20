using System;
using Bocage.SimulationCore;
using SeededRandom = Bocage.SimulationCore.SeededRandom;

namespace Bocage.Decision
{
    /// <summary>
    /// Projette l'effet d'un levier en <b>simulant réellement le futur</b> (pas de
    /// coefficient figé) : copie l'état, applique le levier, fait tourner le
    /// <see cref="SimulationEngine"/> en avant sur plusieurs réalisations météo,
    /// et compare au scénario « sans rien faire » sous la <b>même</b> météo. La
    /// bande worst→best vient de la variabilité inter-annuelle. Aucune I/O.
    /// </summary>
    public sealed class ModelOutcomeProjector
    {
        public const int HorizonDays = 1095;          // 3 ans (horizon de décision agriculteur)
        // 9 réalisations météo : assez d'échantillons pour stabiliser l'espérance ET le downside
        // (le min sur 3 tirages était un estimateur trop bruité du pire cas → recos instables).
        public const int WeatherRealisations = 9;
        private const ulong RealisationSeedStride = 1000003UL;

        private readonly Climatology _climatology;

        public ModelOutcomeProjector(Climatology climatology)
        {
            _climatology = climatology ?? throw new ArgumentNullException(nameof(climatology));
        }

        /// <summary>
        /// Projette le levier décrit par <paramref name="applyLever"/> (qui mute une
        /// copie du scénario) vs le scénario inchangé, depuis l'état courant.
        /// </summary>
        public LeverOutcome Project(EcosystemModel model, ScenarioContext scenario,
            ulong masterSeed, Action<ScenarioContext> applyLever)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            if (applyLever == null) throw new ArgumentNullException(nameof(applyLever));

            var deltaMargin = new double[WeatherRealisations];
            var deltaBiodiversity = new double[WeatherRealisations];
            var deltaCarbon = new double[WeatherRealisations];

            for (int r = 0; r < WeatherRealisations; r++)
            {
                ulong seed = masterSeed + (ulong)(r + 1) * RealisationSeedStride;

                SimulationEngine baseline = MakeEngine(new EcosystemModel(model), new ScenarioContext(scenario), seed);
                var leverScenario = new ScenarioContext(scenario);
                applyLever(leverScenario);
                SimulationEngine lever = MakeEngine(new EcosystemModel(model), leverScenario, seed);

                // Capital remis à zéro → Δcapital sur l'horizon = la différence de marge cumulée.
                baseline.Model.SetCapitalEurosPerHa(0.0);
                lever.Model.SetCapitalEurosPerHa(0.0);
                baseline.Run(HorizonDays);
                lever.Run(HorizonDays);

                deltaMargin[r] = lever.Model.CapitalEurosPerHa - baseline.Model.CapitalEurosPerHa;
                deltaBiodiversity[r] = lever.Model.Biodiversity - baseline.Model.Biodiversity;
                deltaCarbon[r] = lever.Model.SoilCarbonTotalTPerHa - baseline.Model.SoilCarbonTotalTPerHa;
            }

            return new LeverOutcome(
                OutcomeDistribution.FromSamples(deltaMargin),
                OutcomeDistribution.FromSamples(deltaBiodiversity),
                OutcomeDistribution.FromSamples(deltaCarbon));
        }

        private SimulationEngine MakeEngine(EcosystemModel model, ScenarioContext scenario, ulong seed)
        {
            var weather = new WeatherGenerator(_climatology,
                new SeededRandom(seed).DeriveSubStream(WeatherGenerator.SubStreamId));
            return new SimulationEngine(model, scenario, weather);
        }
    }
}
