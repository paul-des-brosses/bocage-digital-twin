using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// Daily update of the composite fauna abundance index. A target
    /// equilibrium is computed each tick from three explicit factors —
    /// <b>habitat</b> (derived from <see cref="EcosystemModel.HedgerowDensity"/>),
    /// <b>eau</b> (derived from <see cref="EcosystemModel.WaterTableDepth"/>),
    /// <b>intrants</b> (derived from <see cref="ScenarioContext.InputIntensityFactor"/>) —
    /// plus two small modulators sourced from chantiers E2 and E3:
    /// a canicular penalty over the last 30 days of T° &gt; 30 °C, and
    /// a soil-carbon bonus when stock crosses a « sol vivant » threshold.
    /// <see cref="EcosystemModel.FaunaPopulation"/> drifts toward that
    /// target with a ~1-year time constant (EMA k = 1/365). Slow
    /// dynamics reflect the empirical observation that wildlife
    /// populations track habitat changes over multiple breeding
    /// seasons, not days.
    /// <para>
    /// The three multiplicative factors are exposed as public static
    /// helpers and published as observable <c>RC_FaunaFactor*</c>
    /// containers (chantier E5 / ADR #51). The onglet Biodiv (E6)
    /// renders one line per factor; the
    /// <see cref="Bocage.Indicators.Hero.BiodiversityCompositeIndicator"/>
    /// aggregates them as a weighted sum (40/25/35) rather than going
    /// through <see cref="EcosystemModel.FaunaPopulation"/> — that way
    /// the Hero KPI reacts immediately to scenario changes while the
    /// fauna abundance variable keeps its honest slow trajectory for
    /// the visible faune of E4.
    /// </para>
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
    ///         Sert de borne haute pour <see cref="HabitatFactorCap"/>.</item>
    ///   <item>Hallmann et al. 2017 (PLoS ONE, étude Krefeld) : −75 % de
    ///         biomasse d'insectes volants en 27 ans en zones agricoles
    ///         allemandes. MNHN 2024 : −70 à 80 % dans les paysages
    ///         agro-industriels européens. Aligne le slope
    ///         <see cref="InputsIntensityPenaltyAbove"/> et la pénalité
    ///         canicule.</item>
    ///   <item>OFB / RMT Zones humides : assèchement (nappe &gt; 5 m sous
    ///         sol) provoque la disparition des amphibiens et
    ///         hyménoptères des mares. Justifie la décroissance
    ///         <see cref="WaterFactorSlope"/> au-delà de
    ///         <see cref="WaterDepthCriticalMeters"/>.</item>
    ///   <item>INRAE BDAT : sols à stock C élevé (&gt; 80 tC/ha) sont
    ///         des « sols vivants » porteurs de macrofaune (vers de
    ///         terre, coléoptères du sol). Justifie le bonus
    ///         <see cref="SoilCarbonBonus"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note honnêteté</b> : c'est un indice composite low-fidelity, pas
    /// un vrai modèle proie-prédateur. Il agrège plusieurs effets en un
    /// scalaire pour rester dans l'esprit "digital twin pédagogique" du
    /// portfolio. Le couplage trophique fin (passereaux ↔ insectes ↔
    /// pollinisateurs) reste en backlog post-MVP.
    /// </para>
    /// </summary>
    public sealed class FaunaDynamicsRule : IRule
    {
        public string SubStreamId => "fauna";

        public const double BaselineIndex = 1.0;
        private const double TransitionRatePerDay = 1.0 / 365.0; // ~1 year TC

        // ---------------- Habitat factor (was « hedge ») ----------------
        // Linear from 0.5× at 0 m/ha to 1.0× at 90 m/ha, capped at 1.4× at
        // 180 m/ha (super-bocage). Slope 1/180 per m/ha. The cap prevents
        // unrealistic explosion if hedges grow far past historical Perche maxima.
        private const double HabitatFactorAtZero = 0.5;
        private const double HabitatFactorPerMeterPerHectare = 1.0 / 180.0;
        public const double HabitatFactorCap = 1.4;

        // ---------------- Water factor ----------------
        // Full fauna up to 3 m depth (wetland habitats remain accessible),
        // then linear decline 8 %/m, floor at 0.5×.
        public const double WaterDepthCriticalMeters = 3.0;
        public const double WaterFactorSlope = 0.08;
        private const double WaterFactorFloor = 0.5;

        // ---------------- Inputs factor (was « input ») ----------------
        // Penalty above 1.0 (conventional), bonus below 1.0 (bio /
        // extensive). Slopes are asymmetric — penalty is larger than
        // bonus because pesticides actively kill, whereas bio only
        // releases the pressure.
        public const double InputsIntensityPenaltyAbove = 0.5;  // per unit above 1.0
        public const double InputsIntensityBoostBelow = 0.2;    // per unit below 1.0
        public const double InputsFactorFloor = 0.4;

        // ---------------- Canicule (E5 / ADR #51) ----------------
        // 0.01 fauna penalty per day with T° > 30 °C in the last
        // 30 days, capped at −0.15 over the full window. Calibrated
        // from Hallmann 2017 — thermal stress is one of the lever
        // candidates explaining the Krefeld insect collapse.
        public const double CanicularPenaltyPerDay = 0.01;
        public const double CanicularPenaltyCap = 0.15;

        // ---------------- Soil-carbon bonus (E5 / ADR #51) ----------------
        // +0.02 fauna bonus when soil organic carbon stock exceeds the
        // « sol vivant » threshold (80 tC/ha, INRAE BDAT high-quality
        // bocage soils). Proxy for macrofaune density (vers de terre,
        // coléoptères du sol). Step function — could be smoothed later
        // but the step is honest given the proxy nature.
        public const double SoilCarbonLivingThresholdTonnesPerHectare = 80.0;
        public const double SoilCarbonBonus = 0.02;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double habitatF = ComputeHabitatFactor(model.HedgerowDensity);
            double waterF = ComputeWaterFactor(model.WaterTableDepth);
            double inputsF = ComputeInputsFactor(scenario.InputIntensityFactor.Current);
            double canicularPenalty = ComputeCanicularPenalty(model.RecentCanicularDayCount);
            double soilBonus = ComputeSoilCarbonBonus(model.SoilCarbonStock);

            double target = BaselineIndex * habitatF * waterF * inputsF + canicularPenalty + soilBonus;
            if (target < 0.0) target = 0.0;

            double current = model.FaunaPopulation;
            double next = current + TransitionRatePerDay * (target - current);
            model.SetFaunaPopulation(next);
        }

        /// <summary>
        /// Linear in hedgerow density, capped to avoid unrealistic
        /// explosion at hyper-bocage densities. Range [0.5, 1.4].
        /// Equals 1.0 exactly at the Perche reference 90 m/ha.
        /// </summary>
        public static double ComputeHabitatFactor(double densityMetersPerHectare)
        {
            double f = HabitatFactorAtZero + densityMetersPerHectare * HabitatFactorPerMeterPerHectare;
            if (f > HabitatFactorCap) f = HabitatFactorCap;
            return f;
        }

        /// <summary>
        /// Flat above the critical depth, linearly declining once the
        /// water table sinks below the root zone of wetland habitats.
        /// Range [0.5, 1.0].
        /// </summary>
        public static double ComputeWaterFactor(double depthMeters)
        {
            double excess = depthMeters - WaterDepthCriticalMeters;
            double f = excess > 0.0 ? 1.0 - excess * WaterFactorSlope : 1.0;
            if (f < WaterFactorFloor) f = WaterFactorFloor;
            return f;
        }

        /// <summary>
        /// Asymmetric in input intensity factor: above 1.0 a unit of
        /// intensification costs 0.5 in factor, below 1.0 a unit of
        /// de-intensification gains 0.2. Floors at 0.4 at very high
        /// intensity. Range [0.4, ~1.1] for plausible scenarios.
        /// </summary>
        public static double ComputeInputsFactor(double intensityFactor)
        {
            if (intensityFactor > 1.0)
            {
                double f = 1.0 - (intensityFactor - 1.0) * InputsIntensityPenaltyAbove;
                if (f < InputsFactorFloor) f = InputsFactorFloor;
                return f;
            }
            return 1.0 + (1.0 - intensityFactor) * InputsIntensityBoostBelow;
        }

        /// <summary>
        /// Negative or zero. <c>−0.01 × N</c> capped at <c>−0.15</c>,
        /// where N is the number of days with T° &gt; 30 °C over the
        /// last 30 days (cf. <see cref="EcosystemModel.RecentCanicularDayCount"/>).
        /// </summary>
        public static double ComputeCanicularPenalty(int recentCanicularDayCount)
        {
            if (recentCanicularDayCount <= 0) return 0.0;
            double raw = CanicularPenaltyPerDay * recentCanicularDayCount;
            if (raw > CanicularPenaltyCap) raw = CanicularPenaltyCap;
            return -raw;
        }

        /// <summary>
        /// Step function: <see cref="SoilCarbonBonus"/> when
        /// <paramref name="soilCarbonStock"/> exceeds the
        /// <see cref="SoilCarbonLivingThresholdTonnesPerHectare"/>
        /// threshold (sol vivant), zero otherwise.
        /// </summary>
        public static double ComputeSoilCarbonBonus(double soilCarbonStock)
        {
            return soilCarbonStock > SoilCarbonLivingThresholdTonnesPerHectare ? SoilCarbonBonus : 0.0;
        }
    }
}
