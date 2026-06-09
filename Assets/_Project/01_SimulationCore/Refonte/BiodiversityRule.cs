namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Dynamique de l'indice de biodiversité D ∈ [0,1]. D relaxe lentement
    /// (~1 an de latence) vers une cible composite — c'est l'état que la faune
    /// « vit » réellement. La <see cref="Target"/> instantanée (la « pression »)
    /// est exposée pour affichage à côté de l'état laggé (doc 10 §B.4).
    /// <code>
    ///   cible = (w_h·habitat(densité) + w_w·eau(θ) + w_i·intrants(N,IFT)) · climat(canicule)
    /// </code>
    /// La sécheresse frappe par l'eau ET l'habitat (densité↓) ; l'intensification
    /// (N, IFT) frappe par les intrants ; les canicules par le climat. Couplée au
    /// climat, contrairement à l'ancien composite. Déterministe, sans I/O.
    /// Sources : Hallmann 2017, Vigie-Nature/INRAE-OFB, Réseau Haies.
    /// </summary>
    public sealed class BiodiversityRule
    {
        public const double HabitatWeight = 0.40;
        public const double WaterWeight = 0.25;
        public const double InputsWeight = 0.35;
        public const double HabitatReferenceMPerHa = 130.0;
        public const double WaterBiodivOptimalMm = 80.0;
        public const double InputsNitrogenPenalty = 0.3;    // par 100 kgN
        public const double InputsPesticidePenalty = 0.2;   // par unité d'IFT
        public const double CanicularPenaltyPerDay = 0.01;
        public const double CanicularPenaltyCap = 0.2;
        public const double RelaxationDays = 365.0;
        public const double GrasslandHabitatBonus = 0.35;   // habitat prairie permanente (Vigie-Nature)

        private static double Clamp01(double v) => v < 0.0 ? 0.0 : (v > 1.0 ? 1.0 : v);

        public static double HabitatFactor(double densityMPerHa) => HabitatFactor(densityMPerHa, 0.0);

        /// <summary>Habitat = densité de haie + bonus prairie permanente (part g).</summary>
        public static double HabitatFactor(double densityMPerHa, double grasslandFraction)
            => Clamp01(densityMPerHa / HabitatReferenceMPerHa + GrasslandHabitatBonus * grasslandFraction);

        public static double WaterFactor(double soilWaterMm) => Clamp01(soilWaterMm / WaterBiodivOptimalMm);

        public static double InputsFactor(double mineralNitrogenKgPerHa, double pesticideIntensity)
            => InputsFactor(mineralNitrogenKgPerHa, pesticideIntensity, 0.0);

        /// <summary>
        /// Facteur intrants : la pression chimique (N, IFT) ne s'exerce que sur la
        /// part cultivée (1−g) — plus de prairie dilue la pression à l'échelle ferme.
        /// </summary>
        public static double InputsFactor(double mineralNitrogenKgPerHa, double pesticideIntensity, double grasslandFraction)
        {
            double cropShare = 1.0 - grasslandFraction;
            if (cropShare < 0.0) cropShare = 0.0; else if (cropShare > 1.0) cropShare = 1.0;
            return Clamp01(1.0 - cropShare * (InputsNitrogenPenalty * (mineralNitrogenKgPerHa / 100.0)
                                              + InputsPesticidePenalty * pesticideIntensity));
        }

        public static double ClimateFactor(int recentCanicularDayCount)
        {
            double penalty = CanicularPenaltyPerDay * recentCanicularDayCount;
            if (penalty > CanicularPenaltyCap) penalty = CanicularPenaltyCap;
            return 1.0 - penalty;
        }

        /// <summary>Pression instantanée de biodiversité (la cible vers laquelle D relaxe).</summary>
        public static double Target(EcosystemModel model, ScenarioContext scenario)
        {
            double g = scenario.GrasslandFraction;
            double composite = HabitatWeight * HabitatFactor(model.HedgerowDensityMPerHa, g)
                + WaterWeight * WaterFactor(model.SoilWaterMm)
                + InputsWeight * InputsFactor(model.MineralNitrogenKgPerHa, scenario.PesticideIntensity, g);
            return Clamp01(composite * ClimateFactor(model.RecentCanicularDayCount));
        }

        public void Apply(EcosystemModel model, ScenarioContext scenario)
        {
            double target = Target(model, scenario);
            double d = model.Biodiversity;
            model.SetBiodiversity(d + (target - d) / RelaxationDays);
        }
    }
}
