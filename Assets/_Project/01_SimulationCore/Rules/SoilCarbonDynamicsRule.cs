using Bocage.SimulationCore.Model;
using Bocage.SimulationCore.Scenario;

namespace Bocage.SimulationCore.Rules
{
    /// <summary>
    /// 1-pool soil organic carbon dynamics (chantier E3 / ADR #48):
    /// <c>dC/dt = inputs − k·C</c>, integrated per simulated day on
    /// <see cref="EcosystemModel.SoilCarbonStock"/>. Calibrated against
    /// the BDAT INRAE reference for cultivated bocage soils
    /// (<c>SoilCarbonStock</c> default = 50 tC/ha) and the INRAE
    /// 4-pour-1000 narrative (<c>k</c> ≈ 1/40 yr⁻¹, half-life ~28 years).
    /// <para>
    /// Three input streams accumulate carbon into the pool, all sourced
    /// from <c>docs/CALIBRATION.md §Carbone sol</c>:
    /// <list type="bullet">
    ///   <item><b>Cover crops</b> — <c>1.2 × CoverCropsCoveragePercent / 100</c>
    ///         tC/ha/yr at full coverage (Solagro Afterres 2050).</item>
    ///   <item><b>Residue restitution</b> — <c>0.8 × ResidueRestitutionPercent / 100</c>
    ///         tC/ha/yr at full restitution (Solagro Afterres 2050).</item>
    ///   <item><b>Hedgerows (proxy)</b> — <c>0.4 × HedgerowDensity / 90</c>
    ///         tC/ha/yr at the reference density 90 m/ha (AFAC-Agroforesteries).</item>
    /// </list>
    /// Equilibrium <c>C_eq = inputs / k</c>. With couverts 50 % + résidus
    /// 80 % + haies 90 m/ha → inputs ≈ 1.64 tC/ha/yr → C_eq ≈ 66 tC/ha;
    /// the default 50 tC/ha approaches this slowly (~30 simulated years).
    /// </para>
    /// <para>
    /// Deterministic rule (no RNG). The <see cref="SubStreamId"/> is
    /// declared for IRule conformance but no sub-stream is drawn.
    /// </para>
    /// </summary>
    public sealed class SoilCarbonDynamicsRule : IRule
    {
        public string SubStreamId => "soil-carbon";

        public const double MineralisationRatePerYear = 1.0 / 40.0;
        public const double CoverCropsMaxInputTcPerHaPerYear = 1.2;
        public const double ResidueMaxInputTcPerHaPerYear = 0.8;
        public const double HedgerowMaxInputTcPerHaPerYear = 0.4;
        public const double HedgerowReferenceMetersPerHectare = 90.0;
        private const double DaysPerYear = 365.0;

        public void Apply(EcosystemModel model, ScenarioContext scenario, SeededRandom rng)
        {
            double coverCropsShare = scenario.CoverCropsCoveragePercent.Current / 100.0;
            double residueShare = scenario.ResidueRestitutionPercent.Current / 100.0;
            double hedgerowShare = model.HedgerowDensity / HedgerowReferenceMetersPerHectare;

            double annualInputs =
                CoverCropsMaxInputTcPerHaPerYear * coverCropsShare
                + ResidueMaxInputTcPerHaPerYear * residueShare
                + HedgerowMaxInputTcPerHaPerYear * hedgerowShare;

            double dailyInputs = annualInputs / DaysPerYear;
            double dailyMineralisation = (MineralisationRatePerYear / DaysPerYear) * model.SoilCarbonStock;
            double dC = dailyInputs - dailyMineralisation;

            model.SetSoilCarbonStock(model.SoilCarbonStock + dC);
        }
    }
}
