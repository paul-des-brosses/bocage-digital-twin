namespace Bocage.SimulationCore
{
    /// <summary>
    /// Dynamique de la pression d'adventices W ∈ [0,1]. W relaxe vers une cible
    /// déterminée par le travail du sol et les pesticides : le labour détruit
    /// mécaniquement les adventices, l'IFT les supprime chimiquement. Perdre les
    /// DEUX (semis direct + zéro phyto) fait exploser le salissement — c'est le
    /// downside du non-labour (cf docs/refonte/10 D.1). Déterministe, sans I/O.
    /// <code>
    ///   W_target = clamp01(1 − contrôle_travail·Tillage − contrôle_phyto·IFT)
    /// </code>
    /// </summary>
    public sealed class WeedPressureRule
    {
        public const double MaxWeedTarget = 1.0;
        public const double TillageControl = 0.5;     // le labour à pleine intensité retire 0,5
        public const double PesticideControl = 0.5;   // l'IFT de référence retire 0,5
        public const double RelaxationDays = 60.0;     // les adventices répondent en ~2 mois

        /// <summary>Cible de pression d'adventices selon les leviers travail du sol / phyto.</summary>
        public static double Target(ScenarioContext scenario)
        {
            double target = MaxWeedTarget
                - TillageControl * scenario.TillageIntensity
                - PesticideControl * scenario.PesticideIntensity;
            if (target < 0.0) target = 0.0;
            else if (target > 1.0) target = 1.0;
            return target;
        }

        public void Apply(EcosystemModel model, ScenarioContext scenario)
        {
            double target = Target(scenario);
            double w = model.WeedPressure;
            model.SetWeedPressure(w + (target - w) / RelaxationDays);
        }
    }
}
