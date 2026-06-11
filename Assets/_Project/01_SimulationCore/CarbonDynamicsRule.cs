using System;

namespace Bocage.SimulationCore
{
    /// <summary>
    /// Dynamique du carbone organique du sol — modèle ICBM 2 pools (Andrén &amp;
    /// Kätterer 1997), avec décomposition sensible au climat (Q10 pour la
    /// température, fonction d'humidité) :
    /// <code>
    ///   r_e   = f_T(T) · f_θ(θ),   f_T = Q10^((T−10)/10),  f_θ ∈ [floor, 1]
    ///   ΔC_y  = i − k_y·r_e·C_y
    ///   ΔC_o  = h·k_y·r_e·C_y − k_o·r_e·C_o
    /// </code>
    /// Les apports <c>i</c> dépendent du rendement (résidus), des couverts et de
    /// la densité de haie (litière) → le rendement chute sous sécheresse →
    /// apports↓ → carbone↓ (ferme le cul-de-sac de l'ancien modèle). La
    /// minéralisation s'accélère au chaud (Q10) → carbone↓ sous réchauffement.
    /// Déterministe, sans I/O. Sources : ICBM ; AMG/RothC (modificateurs
    /// climat) ; Davidson &amp; Janssens 2006 (Q10≈2) ; Solagro, AFAC (apports).
    /// </summary>
    public sealed class CarbonDynamicsRule
    {
        public const double DecayYoungPerYear = 0.8;       // k_y
        public const double DecayOldPerYear = 0.007;       // k_o
        public const double HumificationFraction = 0.13;   // h
        public const double Q10 = 2.0;
        public const double TempReferenceCelsius = 10.0;
        public const double MoistureOptimalMm = 78.0;      // 0,6 · RU_base : au-delà, f_θ = 1
        public const double MoistureFloor = 0.2;           // minéralisation résiduelle en sol sec

        // Apports carbone (tC/ha/an) — calibrés pour i ≈ 2,5 au référentiel
        // (Y=5,5 ; densité=90 ; sans couverts) → équilibre ICBM C* ≈ 50 (BDAT).
        public const double BaselineInputTPerHaPerYear = 0.4;   // rhizodéposition / divers
        public const double ResidueInputCoeff = 1.8;            // résidus, au rendement de référence
        public const double FloraInputCoeff = 0.3;              // litière flore, à la densité de référence
        public const double CoverCropInputCoeff = 1.2;          // à 100 % de couverts (Solagro)
        public const double YieldReferenceTPerHa = 5.5;
        public const double HedgeDensityReferenceMPerHa = 90.0;
        public const double GrasslandCarbonInputTPerHaPerYear = 2.5;  // litière racinaire prairie permanente (Soussana/INRAE)

        private const double DaysPerYear = 365.0;

        /// <summary>Facteur climat de décomposition r_e = f_T(T)·f_θ(θ).</summary>
        public static double ClimateFactor(double tMeanCelsius, double soilWaterMm)
        {
            double fT = Math.Pow(Q10, (tMeanCelsius - TempReferenceCelsius) / 10.0);
            double fTheta = soilWaterMm / MoistureOptimalMm;
            if (fTheta < MoistureFloor) fTheta = MoistureFloor;
            else if (fTheta > 1.0) fTheta = 1.0;
            return fT * fTheta;
        }

        /// <summary>Apports carbone annuels i (tC/ha/an) : baseline + résidus(Y) + flore(densité) + couverts.</summary>
        public static double CarbonInputsTPerHaPerYear(EcosystemModel model, ScenarioContext scenario)
        {
            // Résidus de culture et couverts ne concernent que la part cultivée (1−g) ;
            // la prairie permanente apporte sa propre litière racinaire (forte, g).
            double g = scenario.GrasslandFraction;
            if (g < 0.0) g = 0.0; else if (g > 1.0) g = 1.0;
            double cropShare = 1.0 - g;
            return BaselineInputTPerHaPerYear
                + cropShare * ResidueInputCoeff * (model.CropYieldTPerHa / YieldReferenceTPerHa)
                + FloraInputCoeff * (model.HedgerowDensityMPerHa / HedgeDensityReferenceMPerHa)
                + cropShare * CoverCropInputCoeff * (scenario.CoverCropsCoveragePercent / 100.0)
                + g * GrasslandCarbonInputTPerHaPerYear;
        }

        public void Apply(EcosystemModel model, ScenarioContext scenario)
        {
            double re = ClimateFactor(model.CurrentWeather.TMeanCelsius, model.SoilWaterMm);
            double iDaily = CarbonInputsTPerHaPerYear(model, scenario) / DaysPerYear;
            double ky = DecayYoungPerYear / DaysPerYear;
            double ko = DecayOldPerYear / DaysPerYear;

            double cy = model.CarbonYoungTPerHa;
            double co = model.CarbonOldTPerHa;

            double youngDecay = ky * re * cy;
            double oldDecay = ko * re * co;

            double dCy = iDaily - youngDecay;
            double dCo = HumificationFraction * youngDecay - oldDecay;

            // CO₂ respiré (le reste de la décomposition jeune part vers le pool
            // vieux par humification) : ΔC_total = apports − respiration.
            double respiration = (1.0 - HumificationFraction) * youngDecay + oldDecay;

            model.SetCarbonPools(cy + dCy, co + dCo);
            model.SetLastCarbonInputTPerHa(iDaily);
            model.SetLastCarbonRespirationTPerHa(respiration);
        }
    }
}
