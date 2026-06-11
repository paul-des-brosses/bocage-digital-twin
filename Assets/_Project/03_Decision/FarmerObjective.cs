namespace Bocage.Decision
{
    /// <summary>
    /// Fonction-objectif de l'agriculteur : <b>marge actualisée ajustée du
    /// risque</b>. L'écologie est <b>déjà dans la marge</b> (PSE, MAEC, crédit
    /// carbone), donc l'utilité est purement économique + une aversion au risque
    /// sur le côté baissier — c'est ce qui rend le critère fondé (pas un poids
    /// arbitraire). Sur l'horizon court la marge n'est pas actualisée (≈ Δcapital).
    /// <code>
    ///   U = Δmarge_attendue − λ · (Δmarge_attendue − Δmarge_pire)
    /// </code>
    /// Sources : Edwards-Jones 2006, Reimer et al. 2012 (priorités agriculteur).
    /// </summary>
    public static class FarmerObjective
    {
        /// <summary>Coefficient d'aversion au risque λ.</summary>
        public const double RiskAversion = 0.5;

        public static double Utility(LeverOutcome outcome)
        {
            OutcomeDistribution margin = outcome.DeltaMarginEurosPerHa;
            double downsideRisk = margin.Expected - margin.Worst; // ≥ 0
            return margin.Expected - RiskAversion * downsideRisk;
        }
    }
}
