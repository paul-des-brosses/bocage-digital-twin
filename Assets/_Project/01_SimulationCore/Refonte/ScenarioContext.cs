namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Entrées de pilotage du modèle (refonte) : les 6 leviers agriculteur MVP
    /// + le forçage climatique. POCO simple et mutable ; les transitions douces
    /// (7-14 j) seront ajoutées au moment du câblage décisionnel (couche 03).
    /// Aucune I/O, aucun UnityEngine.
    /// <para>
    /// Conformément à la règle « reco ⊆ leviers », ces valeurs sont aussi bien
    /// pilotables directement (sliders) que via une popup de recommandation.
    /// </para>
    /// </summary>
    public sealed class ScenarioContext
    {
        // --- Leviers agriculteur (MVP) ---

        /// <summary>Dose d'azote minéral apportée (kgN/ha/an).</summary>
        public double NitrogenDoseKgPerHaPerYear { get; set; } = 120.0;

        /// <summary>Couverture en couverts d'interculture (%), 0-100.</summary>
        public double CoverCropsCoveragePercent { get; set; } = 0.0;

        /// <summary>Intensité de traitement phytosanitaire (IFT relatif) ; 1 = référence.</summary>
        public double PesticideIntensity { get; set; } = 1.0;

        /// <summary>Intensité de travail du sol ; 1 = labour, 0 = semis direct.</summary>
        public double TillageIntensity { get; set; } = 1.0;

        /// <summary>Gestion de la flore/haies : taux net (m/ha/an), + plantation, − arrachage.</summary>
        public double HedgeManagementMetersPerHaPerYear { get; set; } = 0.0;

        /// <summary>Part de prairie permanente dans l'assolement [0, 1].</summary>
        public double GrasslandFraction { get; set; } = 0.0;

        // --- Forçage climatique (perturbe les paramètres du générateur météo) ---

        /// <summary>Anomalie de température additive (°C).</summary>
        public double TemperatureAnomalyC { get; set; } = 0.0;

        /// <summary>Facteur multiplicatif sur la pluie ; 1 = baseline, 0.5 = −50 %.</summary>
        public double PrecipitationFactor { get; set; } = 1.0;

        /// <summary>Constructeur par défaut (les initialiseurs de propriété fixent les valeurs).</summary>
        public ScenarioContext() { }

        /// <summary>Copie (pour la projection forward, Couche 03).</summary>
        public ScenarioContext(ScenarioContext other)
        {
            NitrogenDoseKgPerHaPerYear = other.NitrogenDoseKgPerHaPerYear;
            CoverCropsCoveragePercent = other.CoverCropsCoveragePercent;
            PesticideIntensity = other.PesticideIntensity;
            TillageIntensity = other.TillageIntensity;
            HedgeManagementMetersPerHaPerYear = other.HedgeManagementMetersPerHaPerYear;
            GrasslandFraction = other.GrasslandFraction;
            TemperatureAnomalyC = other.TemperatureAnomalyC;
            PrecipitationFactor = other.PrecipitationFactor;
        }
    }
}
