namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Bilan hydrique du sol racinaire — réservoir « bucket » FAO-56. Met à jour
    /// θ (mm) chaque jour :
    /// <code>
    ///   ETP_r   = ETP0 · Kc · Ks,   Ks = clamp(θ / (p·RU_max), 0, 1)
    ///   θ'      = θ + pluie − ETP_r        (ETP_r plafonnée à l'eau disponible)
    ///   drainage = max(0, θ' − RU_max)     (excès évacué vers la nappe)
    ///   θ_suiv  = θ' − drainage            ∈ [0, RU_max]
    /// </code>
    /// La capacité RU_max dépend du carbone (sol vivant → meilleure rétention) :
    /// c'est la rétroaction carbone → réserve en eau. Déterministe, sans I/O.
    /// Sources : Allen et al. 1998 (FAO-56) ; Hargreaves &amp; Samani 1985 ;
    /// Hudson 1994 (carbone → réserve utile).
    /// </summary>
    public sealed class WaterBalanceRule
    {
        public const double RuBaseMm = 150.0;                  // réserve utile de base (limon profond Perche)
        public const double CarbonReferenceTPerHa = 50.0;      // C de référence (BDAT)
        public const double CapacityCarbonSensitivity = 0.5;   // β : ±20 % C → ±10 % RU_max
        public const double ReadilyAvailableFraction = 0.5;    // p (FAO-56)
        public const double CropCoefficient = 0.95;            // Kc (constant — courbe saisonnière = raffinement futur)
        public const double LatitudeDegrees = 48.5;            // Tourouvre-au-Perche

        /// <summary>
        /// Capacité du réservoir RU_max (mm) :
        /// <c>RU_base · clamp(1 + β·(C − C_ref)/C_ref, 0.5, 1.8)</c>.
        /// </summary>
        public static double SoilWaterCapacityMm(double soilCarbonTotalTPerHa)
        {
            double factor = 1.0 + CapacityCarbonSensitivity
                * (soilCarbonTotalTPerHa - CarbonReferenceTPerHa) / CarbonReferenceTPerHa;
            if (factor < 0.5) factor = 0.5;
            else if (factor > 1.8) factor = 1.8;
            return RuBaseMm * factor;
        }

        /// <summary>
        /// Applique le bilan du jour. <paramref name="dayOfYear"/> (1-365) sert au
        /// calcul de Ra (Hargreaves). Lit <see cref="EcosystemModel.CurrentWeather"/>
        /// et le carbone total ; écrit θ, le drainage et l'ETP réelle du jour.
        /// </summary>
        public void Apply(EcosystemModel model, int dayOfYear)
        {
            DailyWeather w = model.CurrentWeather;
            double ruMax = SoilWaterCapacityMm(model.SoilCarbonTotalTPerHa);

            double et0 = Hargreaves.ReferenceEt0(dayOfYear, LatitudeDegrees,
                w.TMinCelsius, w.TMaxCelsius, w.TMeanCelsius);

            double theta = model.SoilWaterMm;
            double ks = theta / (ReadilyAvailableFraction * ruMax);
            if (ks < 0.0) ks = 0.0;
            else if (ks > 1.0) ks = 1.0;

            double available = theta + w.PrecipMm;
            double etDemand = et0 * CropCoefficient * ks;
            double etActual = etDemand < available ? etDemand : available; // ne peut évaporer plus que présent

            double afterEt = available - etActual;                          // ≥ 0
            double drainage = afterEt > ruMax ? afterEt - ruMax : 0.0;
            double nextTheta = afterEt - drainage;                          // ∈ [0, RU_max]

            model.SetSoilWaterMm(nextTheta);
            model.SetLastEvapotranspirationMm(etActual);
            model.SetLastDrainageMm(drainage);
        }
    }
}
