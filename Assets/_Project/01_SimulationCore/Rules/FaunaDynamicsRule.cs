using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily update of the composite fauna abundance index. A target
    /// equilibrium is computed each tick from the current habitat state
    /// (hedge density, water table depth) and the agricultural pressure
    /// (input intensity factor), and <see cref="EcosystemModel.FaunaPopulation"/>
    /// drifts toward that target with a ~1-year time constant
    /// (EMA k = 1/365). Slow dynamics reflect the empirical observation
    /// that wildlife populations track habitat changes over multiple
    /// breeding seasons, not days.
    /// <para>
    /// <b>Calibration sources</b>
    /// <list type="bullet">
    ///   <item>INRAE / OFB Vigie-Nature : oiseaux des milieux agricoles
    ///         −30 % entre 1989 et 2017 dans les zones intensifiées.
    ///         Sert d'ordre de grandeur pour la pénalité d'intrants
    ///         (intensity 2.0 → fauna ~0.5).</item>
    ///   <item>Constant, Eybert et Maheo (1976), cité par Réseau Haies :
    ///         bocage dense vs zone agricole ouverte ≈ doublement des oiseaux
    ///         nicheurs (99 vs 35 individus/10 ha). Seuil 100 m/ha = proxy
    ///         non sourcé précisément, interprété comme bocage fonctionnel.
    ///         Sert de borne haute pour <see cref="HedgeFactorCap"/>.</item>
    ///   <item>Hallmann et al. 2017 (PLoS ONE, étude Krefeld) : −75 % de
    ///         biomasse d'insectes volants en 27 ans en zones agricoles
    ///         allemandes. MNHN 2024 : −70 à 80 % dans les paysages
    ///         agro-industriels européens. Aligne le slope
    ///         <see cref="InputIntensityPenaltyAbove"/>.</item>
    ///   <item>OFB / RMT Zones humides : assèchement (nappe > 5 m sous
    ///         sol) provoque la disparition des amphibiens et
    ///         hyménoptères des mares. Justifie la décroissance
    ///         <see cref="WaterFactorSlope"/> au-delà de
    ///         <see cref="WaterDepthCriticalMeters"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note honnêteté</b> : c'est un indice composite low-fidelity, pas
    /// un vrai modèle proie-prédateur. Il agrège plusieurs effets en un
    /// scalaire pour rester dans l'esprit "digital twin pédagogique" du
    /// portfolio. Le couplage trophique fin (passereaux ↔ insectes ↔
    /// pollinisateurs) serait un livrable Étape 9+ si scope autorise.
    /// </para>
    /// </summary>
    public sealed class FaunaDynamicsRule : IRule
    {
        public string SubStreamId => "fauna";

        public const double BaselineIndex = 1.0;
        private const double TransitionRatePerDay = 1.0 / 365.0; // ~1 year TC

        // Hedge corridor effect: linear from 0.5× at 0 m/ha to 1.0× at 90 m/ha,
        // capped at 1.4× at 180 m/ha (super-bocage). Slope 1/180 per m/ha.
        // The cap prevents unrealistic explosion if hedges grow far past
        // historical Perche maxima.
        private const double HedgeFactorAtZero = 0.5;
        private const double HedgeFactorPerMeterPerHectare = 1.0 / 180.0;
        public const double HedgeFactorCap = 1.4;

        // Water table effect: full fauna up to 3 m depth (wetland habitats
        // remain accessible), then linear decline 8 %/m, floor at 0.5×.
        // 8 m depth → factor = 0.6 (typical drought-stressed bocage state).
        private const double WaterDepthCriticalMeters = 3.0;
        private const double WaterFactorSlope = 0.08;
        private const double WaterFactorFloor = 0.5;

        // Input intensity effect: penalty above 1.0 (conventional), bonus
        // below 1.0 (bio / extensive). Slopes are asymmetric — penalty is
        // larger than bonus because pesticides actively kill, whereas bio
        // only releases the pressure.
        private const double InputIntensityPenaltyAbove = 0.5;  // per unit above 1.0
        private const double InputIntensityBoostBelow = 0.2;    // per unit below 1.0
        private const double InputFactorFloor = 0.4;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double hedgeF = ComputeHedgeFactor(model.HedgerowDensity);
            double waterF = ComputeWaterFactor(model.WaterTableDepth);
            double inputF = ComputeInputFactor(scenario.InputIntensityFactor.Current);

            double target = BaselineIndex * hedgeF * waterF * inputF;
            if (target < 0.0) target = 0.0;

            double current = model.FaunaPopulation;
            double next = current + TransitionRatePerDay * (target - current);
            model.SetFaunaPopulation(next);
        }

        /// <summary>
        /// Linear in hedgerow density, capped to avoid unrealistic
        /// explosion at hyper-bocage densities.
        /// </summary>
        public static double ComputeHedgeFactor(double densityMetersPerHectare)
        {
            double f = HedgeFactorAtZero + densityMetersPerHectare * HedgeFactorPerMeterPerHectare;
            if (f > HedgeFactorCap) f = HedgeFactorCap;
            return f;
        }

        /// <summary>
        /// Flat above the critical depth, linearly declining once the
        /// water table sinks below the root zone of wetland habitats.
        /// </summary>
        public static double ComputeWaterFactor(double depthMeters)
        {
            double excess = depthMeters - WaterDepthCriticalMeters;
            double f = excess > 0.0 ? 1.0 - excess * WaterFactorSlope : 1.0;
            if (f < WaterFactorFloor) f = WaterFactorFloor;
            return f;
        }

        /// <summary>
        /// Asymmetric: above 1.0 a unit of intensity costs 0.5 in factor,
        /// below 1.0 a unit of de-intensification gains 0.2. Floors at 0.4
        /// at very high intensity.
        /// </summary>
        public static double ComputeInputFactor(double intensityFactor)
        {
            if (intensityFactor > 1.0)
            {
                double f = 1.0 - (intensityFactor - 1.0) * InputIntensityPenaltyAbove;
                if (f < InputFactorFloor) f = InputFactorFloor;
                return f;
            }
            return 1.0 + (1.0 - intensityFactor) * InputIntensityBoostBelow;
        }
    }
}
