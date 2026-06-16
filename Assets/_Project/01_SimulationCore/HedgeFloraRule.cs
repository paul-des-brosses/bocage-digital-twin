namespace Bocage.SimulationCore
{
    /// <summary>
    /// Dynamique de la densité de haie (m/ha) — proxy de la santé de la flore
    /// (aucun rôle économique, décision #1). La densité relaxe vers une capacité
    /// d'accueil pilotée par l'eau (sécheresse → flore stressée) et l'intensité
    /// des intrants (intensification → flore banalisée), plus l'effet direct du
    /// levier de gestion (plantation / réduction d'arrachage). Déterministe, sans
    /// I/O. La densité alimente l'habitat (biodiversité) et la litière (carbone).
    /// <code>
    ///   capacité = densité_réf · santé_eau(θ) · santé_intrants(N)
    ///   Δdensité = (capacité − densité)/τ + gestion/365
    /// </code>
    /// Sources : AFAC-Agroforesteries, Réseau Haies (croissance/gestion haie).
    /// </summary>
    public sealed class HedgeFloraRule
    {
        public const double ReferenceDensityMPerHa = 90.0;
        public const double WaterHealthOptimalMm = 50.0;
        public const double NitrogenReferenceKgPerHa = 60.0;
        public const double InputsHealthPenalty = 0.3;   // au-delà de N_ref, la flore régresse
        public const double HealthFloor = 0.5;
        public const double RelaxationDays = 365.0;
        private const double DaysPerYear = 365.0;

        public static double WaterHealth(double soilWaterMm)
        {
            double h = soilWaterMm / WaterHealthOptimalMm;
            if (h < HealthFloor) h = HealthFloor;
            else if (h > 1.0) h = 1.0;
            return h;
        }

        public static double InputsHealth(double mineralNitrogenKgPerHa)
        {
            double excess = (mineralNitrogenKgPerHa - NitrogenReferenceKgPerHa) / NitrogenReferenceKgPerHa;
            if (excess < 0.0) excess = 0.0;
            double h = 1.0 - InputsHealthPenalty * excess;
            if (h < HealthFloor) h = HealthFloor;
            else if (h > 1.0) h = 1.0;
            return h;
        }

        /// <summary>
        /// Vigueur <b>visible</b> de la haie ∈ [0,1] — purement pour l'affichage
        /// (teinte du shader), n'entre dans aucun flux du modèle. Même stress
        /// eau×intrants que <see cref="WaterHealth"/>/<see cref="InputsHealth"/>,
        /// mais <b>sans le plancher de résilience</b> <see cref="HealthFloor"/> : un
        /// stress sévère la mène jusqu'à 0 (haie franchement brunie), là où la santé
        /// qui pilote la <see cref="CarryingCapacity"/> garde son plancher (la haie
        /// ne disparaît pas). Découplé exprès pour rendre la santé lisible à l'écran
        /// sans modifier la dynamique de densité.
        /// </summary>
        public static double VisualVigor(double soilWaterMm, double mineralNitrogenKgPerHa)
        {
            double water = soilWaterMm / WaterHealthOptimalMm;
            if (water < 0.0) water = 0.0; else if (water > 1.0) water = 1.0;
            double excess = (mineralNitrogenKgPerHa - NitrogenReferenceKgPerHa) / NitrogenReferenceKgPerHa;
            if (excess < 0.0) excess = 0.0;
            double inputs = 1.0 - InputsHealthPenalty * excess;
            if (inputs < 0.0) inputs = 0.0; else if (inputs > 1.0) inputs = 1.0;
            return water * inputs;
        }

        /// <summary>Capacité d'accueil de la flore (m/ha) selon l'eau et les intrants.</summary>
        public static double CarryingCapacity(EcosystemModel model)
            => ReferenceDensityMPerHa * WaterHealth(model.SoilWaterMm)
               * InputsHealth(model.MineralNitrogenKgPerHa);

        public void Apply(EcosystemModel model, ScenarioContext scenario)
        {
            double target = CarryingCapacity(model);
            double density = model.HedgerowDensityMPerHa;
            double next = density + (target - density) / RelaxationDays
                + scenario.HedgeManagementMetersPerHaPerYear / DaysPerYear;
            model.SetHedgerowDensityMPerHa(next);
        }
    }
}
