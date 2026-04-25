namespace Bocage.SimulationCore.Scenario
{
    /// <summary>
    /// Scenario inputs the user controls (climate, agricultural pressure,
    /// regulatory constraints, time horizon). Continuous parameters use
    /// TransitioningParameter so user changes spread over 7-14 simulated
    /// days. Tick() must be called once per simulated day to advance the
    /// transitions.
    /// </summary>
    public sealed class ScenarioContext
    {
        public TransitioningParameter<double> ClimateStress { get; }
        public TransitioningParameter<double> AgriculturalPressure { get; }
        public TransitioningParameter<double> RegulatoryConstraints { get; }

        public int HorizonInDays { get; set; }

        public ScenarioContext(
            double initialClimateStress = 0.0,
            double initialAgriculturalPressure = 0.0,
            double initialRegulatoryConstraints = 0.0,
            int horizonInDays = 365)
        {
            ClimateStress = TransitioningParameter.ForDouble(initialClimateStress);
            AgriculturalPressure = TransitioningParameter.ForDouble(initialAgriculturalPressure);
            RegulatoryConstraints = TransitioningParameter.ForDouble(initialRegulatoryConstraints);
            HorizonInDays = horizonInDays;
        }

        public void Tick()
        {
            ClimateStress.Tick();
            AgriculturalPressure.Tick();
            RegulatoryConstraints.Tick();
        }
    }
}
