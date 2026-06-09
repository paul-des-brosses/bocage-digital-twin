namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Dynamique de la nappe h (m) — réservoir type GARDÉNIA, alimenté par le
    /// drainage du bilan hydrique (couplé à θ, plus directement à la pluie) :
    /// <code>
    ///   Δh = − (drainage/1000)/S + r·(h_eq − h)
    /// </code>
    /// Le drainage remonte la nappe (depth↓), la récession la rappelle vers son
    /// équilibre profond. Stock secondaire : le stress agronomique « utile »
    /// passe par θ (la nappe est structurellement bornée près de 3 m). Sert la
    /// chaîne capteur piézomètre (Couche 02). Déterministe, sans I/O.
    /// Sources : BRGM/GARDÉNIA, SIGES Seine-Normandie.
    /// </summary>
    public sealed class NappeRule
    {
        public const double StorageCoefficient = 0.075;
        public const double RecessionRatePerDay = 0.012;
        public const double DeepEquilibriumDepthMeters = 3.0;

        public void Apply(EcosystemModel model)
        {
            double h = model.WaterTableDepthM;
            double recharge = (model.LastDrainageMm / 1000.0) / StorageCoefficient; // m : le drainage remonte la nappe
            double recession = RecessionRatePerDay * (DeepEquilibriumDepthMeters - h);
            model.SetWaterTableDepthM(h - recharge + recession);
        }
    }
}
