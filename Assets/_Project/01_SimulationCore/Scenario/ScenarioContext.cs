namespace Bocage.SimulationCore.Scenario
{
    /// <summary>
    /// Scenario inputs the user controls. Replaces the previous abstract
    /// [0,1] stress sliders with six physical parameters carrying real
    /// units, so that the digital twin's controls map to documented
    /// climate, agricultural and policy levers rather than dimensionless
    /// dials. Continuous parameters use <see cref="TransitioningParameter{T}"/>
    /// so user changes spread over 7-14 simulated days
    /// (CLAUDE.md §15). <see cref="Tick"/> must be called once per
    /// simulated day to advance the transitions.
    /// <para>
    /// <b>Climate</b>:
    /// <list type="bullet">
    ///   <item><see cref="TemperatureAnomalyC"/> — annual mean
    ///         temperature anomaly relative to the Perche reference,
    ///         in °C. Typical IPCC range −2 to +6 (RCP scenarios from
    ///         2050 to 2100).</item>
    ///   <item><see cref="PrecipitationAnomalyPercent"/> — annual
    ///         precipitation anomaly, in % relative to reference.
    ///         Typical range −60 to +20.</item>
    /// </list>
    /// <b>Agriculture</b>:
    /// <list type="bullet">
    ///   <item><see cref="HedgeRemovalRate"/> — sustained hedge
    ///         grubbing pressure in m of hedgerow per ha per year.
    ///         Range 0 to 10 m/ha/yr (0 = no removal, ~5 = aggressive
    ///         consolidation as observed in 1970-90 Perche).</item>
    ///   <item><see cref="InputIntensityFactor"/> — multiplier on
    ///         the reference fertiliser/pesticide/fuel input level.
    ///         Range 0.5 (organic extensive) to 2.0 (intensive).</item>
    /// </list>
    /// <b>Policy</b>:
    /// <list type="bullet">
    ///   <item><see cref="MaecCoveragePercent"/> — share of the farm
    ///         under MAEC environmental contracts, in %.</item>
    ///   <item><see cref="PseSubsidyRate"/> — per-metre subsidy paid
    ///         for maintained hedges, in €/m/yr (MAEC linéaire +
    ///         local PNR rate). Range 0 to ~1.0.</item>
    /// </list>
    /// <b>Soil carbon levers</b> (chantier E3 / ADR #48):
    /// <list type="bullet">
    ///   <item><see cref="CoverCropsCoveragePercent"/> — share of the
    ///         cropland sown with cover crops between cash crops, in %.
    ///         Drives the cover-crop input term of
    ///         <see cref="Bocage.SimulationCore.Rules.SoilCarbonDynamicsRule"/>.
    ///         Range 0 to 100.</item>
    ///   <item><see cref="ResidueRestitutionPercent"/> — share of
    ///         crop residues left on the field after harvest (vs.
    ///         exported as straw/silage), in %. Drives the residue
    ///         input term of the same rule. Range 0 to 100.</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class ScenarioContext
    {
        // ---------------- Climate ----------------
        public TransitioningParameter<double> TemperatureAnomalyC { get; }
        public TransitioningParameter<double> PrecipitationAnomalyPercent { get; }

        // ---------------- Agriculture ----------------
        public TransitioningParameter<double> HedgeRemovalRate { get; }
        public TransitioningParameter<double> InputIntensityFactor { get; }

        // ---------------- Policy ----------------
        public TransitioningParameter<double> MaecCoveragePercent { get; }
        public TransitioningParameter<double> PseSubsidyRate { get; }

        // ---------------- Soil carbon (chantier E3 / ADR #48) ----------------
        public TransitioningParameter<double> CoverCropsCoveragePercent { get; }
        public TransitioningParameter<double> ResidueRestitutionPercent { get; }

        // ---------------- Initial conditions ----------------
        /// <summary>
        /// Starting month at day 0 of the run, 1 = January … 12 = December.
        /// Determines the phase of the seasonal weather model
        /// (chantier E2 / ADR #52): day 0 always lands on the first day of
        /// this month. The value is consumed by
        /// <see cref="Bocage.SimulationCore.Rules.WeatherUpdateRule"/> via
        /// <see cref="Bocage.SimulationCore.Model.SeasonalWeatherData.MonthIndexForDay"/>
        /// and is NOT a transitioning parameter — it is an initial
        /// condition, not a setpoint. The presenter binds it to a combo
        /// box and only reads it at <c>CurrentDay == 0</c>.
        /// </summary>
        public int StartingMonth { get; set; }

        // ---------------- Horizon ----------------
        public int HorizonInDays { get; set; }

        public ScenarioContext(
            double initialTemperatureAnomalyC = 0.0,
            double initialPrecipitationAnomalyPercent = 0.0,
            double initialHedgeRemovalRate = 0.0,
            double initialInputIntensityFactor = 1.0,
            double initialMaecCoveragePercent = 0.0,
            double initialPseSubsidyRate = 0.0,
            double initialCoverCropsCoveragePercent = 0.0,
            double initialResidueRestitutionPercent = 0.0,
            int startingMonth = 1,
            int horizonInDays = 365)
        {
            TemperatureAnomalyC = TransitioningParameter.ForDouble(initialTemperatureAnomalyC);
            PrecipitationAnomalyPercent = TransitioningParameter.ForDouble(initialPrecipitationAnomalyPercent);
            HedgeRemovalRate = TransitioningParameter.ForDouble(initialHedgeRemovalRate);
            InputIntensityFactor = TransitioningParameter.ForDouble(initialInputIntensityFactor);
            MaecCoveragePercent = TransitioningParameter.ForDouble(initialMaecCoveragePercent);
            PseSubsidyRate = TransitioningParameter.ForDouble(initialPseSubsidyRate);
            CoverCropsCoveragePercent = TransitioningParameter.ForDouble(initialCoverCropsCoveragePercent);
            ResidueRestitutionPercent = TransitioningParameter.ForDouble(initialResidueRestitutionPercent);
            StartingMonth = startingMonth < 1 ? 1 : (startingMonth > 12 ? 12 : startingMonth);
            HorizonInDays = horizonInDays;
        }

        public void Tick()
        {
            TemperatureAnomalyC.Tick();
            PrecipitationAnomalyPercent.Tick();
            HedgeRemovalRate.Tick();
            InputIntensityFactor.Tick();
            MaecCoveragePercent.Tick();
            PseSubsidyRate.Tick();
            CoverCropsCoveragePercent.Tick();
            ResidueRestitutionPercent.Tick();
        }
    }
}
