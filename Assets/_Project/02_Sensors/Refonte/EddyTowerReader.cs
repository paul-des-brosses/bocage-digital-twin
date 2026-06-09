using System;
using Bocage.SimulationCore;

namespace Bocage.Sensors.Refonte
{
    /// <summary>
    /// Tour à covariance de turbulences (Couche 02, refonte) : mesure bruitée du
    /// flux net de CO2 du jour (NEE, kgCO2/ha/j, convention positif = émission)
    /// dérivé de la perte nette de carbone du sol (respiration − apports), et
    /// maintient une <b>estimation intégrée du stock de carbone</b> qui dérive
    /// lentement de la vérité (c'est elle que seuille l'alerte carbone, pas la
    /// vérité du modèle — primauté du capteur). Déterministe ; aucune I/O.
    /// </summary>
    public sealed class EddyTowerReader
    {
        public const string SubStreamId = "eddy-tower";
        public const double FluxNoiseSigmaKgCo2 = 1.5;

        private const double CarbonToCo2MassRatio = 44.0 / 12.0;
        private const double TonnesToKilograms = 1000.0;

        private readonly SeededRandom _rng;
        private double _estimatedCarbonStockTPerHa;

        /// <summary>Estimation intégrée du stock de carbone du sol (tC/ha).</summary>
        public double EstimatedCarbonStockTPerHa => _estimatedCarbonStockTPerHa;

        public EddyTowerReader(SeededRandom rng, double initialCarbonStockTPerHa)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _estimatedCarbonStockTPerHa = initialCarbonStockTPerHa;
        }

        /// <summary>
        /// Mesure le flux net CO2 du jour à partir de la perte nette de carbone
        /// <paramref name="netCarbonLossTPerHaPerDay"/> (= respiration − apports,
        /// positif = le sol perd du carbone), bruité, et met à jour l'estimation
        /// intégrée du stock. Renvoie le flux mesuré (kgCO2/ha/j).
        /// </summary>
        public double ReadFluxKgCo2(double netCarbonLossTPerHaPerDay)
        {
            double trueFlux = netCarbonLossTPerHaPerDay * CarbonToCo2MassRatio * TonnesToKilograms;
            double measured = trueFlux + _rng.NextGaussian(0.0, FluxNoiseSigmaKgCo2);
            // Le stock estimé diminue de la perte mesurée (reconvertie en carbone).
            _estimatedCarbonStockTPerHa -= measured / (CarbonToCo2MassRatio * TonnesToKilograms);
            return measured;
        }
    }
}
