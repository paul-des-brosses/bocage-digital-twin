using System;

namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Conteneur d'état du nouveau modèle (refonte). Porte les stocks que les
    /// flux lisent et mutent, le compteur de jour, et la météo générée du jour.
    /// Les invariants (positivité, bornes [0,1]) sont garantis aux setters.
    /// Aucune I/O, aucun UnityEngine (Couche 01).
    /// <para>
    /// Stocks (cf <c>docs/refonte/08_MODELE.md §4</c>) : eau du sol racinaire θ,
    /// nappe h, carbone 2 pools (jeune/vieux, ICBM), azote minéral, rendement,
    /// densité de haie (proxy de la santé de la flore), biodiversité, pression
    /// d'adventices, capital. Plus deux quantités transitoires journalières
    /// (drainage, ETP réelle) produites par le bilan hydrique et lues le même
    /// tick par la nappe et le lessivage.
    /// </para>
    /// </summary>
    public sealed class EcosystemModel
    {
        public int CurrentDay { get; private set; }
        public DailyWeather CurrentWeather { get; private set; }

        /// <summary>θ — eau disponible du sol racinaire (mm), bornée [0, RU_max].</summary>
        public double SoilWaterMm { get; private set; }

        /// <summary>h — profondeur de nappe (m), 0 = surface.</summary>
        public double WaterTableDepthM { get; private set; }

        /// <summary>C_y — pool carbone « jeune » (tC/ha), modèle ICBM.</summary>
        public double CarbonYoungTPerHa { get; private set; }

        /// <summary>C_o — pool carbone « vieux » (tC/ha), modèle ICBM.</summary>
        public double CarbonOldTPerHa { get; private set; }

        /// <summary>Carbone organique total du sol (tC/ha) = jeune + vieux.</summary>
        public double SoilCarbonTotalTPerHa => CarbonYoungTPerHa + CarbonOldTPerHa;

        /// <summary>N — azote minéral disponible (kgN/ha).</summary>
        public double MineralNitrogenKgPerHa { get; private set; }

        /// <summary>Y — rendement courant projeté à la récolte (t/ha).</summary>
        public double CropYieldTPerHa { get; private set; }

        /// <summary>Densité de haie (m/ha) — proxy de la santé de la flore (pas de rôle économique).</summary>
        public double HedgerowDensityMPerHa { get; private set; }

        /// <summary>D — indice de biodiversité, borné [0, 1].</summary>
        public double Biodiversity { get; private set; }

        /// <summary>W — pression d'adventices, bornée [0, 1].</summary>
        public double WeedPressure { get; private set; }

        /// <summary>Capital / marge cumulée (€/ha), non borné (peut être négatif).</summary>
        public double CapitalEurosPerHa { get; private set; }

        /// <summary>Drainage du jour sous la zone racinaire (mm) — transitoire, lu par la nappe et le lessivage.</summary>
        public double LastDrainageMm { get; private set; }

        /// <summary>Évapotranspiration réelle du jour (mm) — transitoire, pour diagnostics/conservation.</summary>
        public double LastEvapotranspirationMm { get; private set; }

        /// <summary>Apports carbone du jour (tC/ha) — transitoire (tour Eddy / conservation).</summary>
        public double LastCarbonInputTPerHa { get; private set; }

        /// <summary>Respiration CO₂ du jour (tC/ha) — transitoire ; flux net NEE = respiration − apports.</summary>
        public double LastCarbonRespirationTPerHa { get; private set; }

        /// <summary>Prélèvement azoté du jour (kgN/ha) — transitoire (limitation rendement Kn).</summary>
        public double LastNitrogenUptakeKgPerHa { get; private set; }

        /// <summary>Lessivage azoté du jour (kgN/ha) — transitoire (pénalité aquatique / événement lessivage).</summary>
        public double LastNitrogenLeachingKgPerHa { get; private set; }

        /// <summary>Marge annualisée du jour (€/ha) — transitoire (Hero KPI rentabilité). Peut être négative.</summary>
        public double LastAnnualMarginEurosPerHa { get; private set; }

        // --- Fenêtres glissantes de chaleur (même mécanique que le modèle actuel) ---
        public int RecentHeatDayCount { get; private set; }
        public int RecentCanicularDayCount { get; private set; }
        public const int HeatDayWindowDays = 30;
        public const double HeatDayThresholdCelsius = 25.0;
        public const double CanicularDayThresholdCelsius = 30.0;
        private readonly int[] _heatDayBuffer = new int[HeatDayWindowDays];
        private int _heatDayBufferIndex;
        private readonly int[] _canicularDayBuffer = new int[HeatDayWindowDays];
        private int _canicularDayBufferIndex;

        public EcosystemModel(
            int initialDay = 0,
            DailyWeather initialWeather = default,
            double initialSoilWaterMm = 90.0,
            double initialWaterTableDepthM = 2.0,
            double initialCarbonYoungTPerHa = 3.0,
            double initialCarbonOldTPerHa = 47.0,
            double initialMineralNitrogenKgPerHa = 40.0,
            double initialCropYieldTPerHa = 5.5,
            double initialHedgerowDensityMPerHa = 90.0,
            double initialBiodiversity = 0.6,
            double initialWeedPressure = 0.2,
            double initialCapitalEurosPerHa = 0.0)
        {
            CurrentDay = initialDay;
            CurrentWeather = initialWeather;
            SoilWaterMm = ClampNonNegative(initialSoilWaterMm);
            WaterTableDepthM = ClampNonNegative(initialWaterTableDepthM);
            CarbonYoungTPerHa = ClampNonNegative(initialCarbonYoungTPerHa);
            CarbonOldTPerHa = ClampNonNegative(initialCarbonOldTPerHa);
            MineralNitrogenKgPerHa = ClampNonNegative(initialMineralNitrogenKgPerHa);
            CropYieldTPerHa = ClampNonNegative(initialCropYieldTPerHa);
            HedgerowDensityMPerHa = ClampNonNegative(initialHedgerowDensityMPerHa);
            Biodiversity = Clamp01(initialBiodiversity);
            WeedPressure = Clamp01(initialWeedPressure);
            CapitalEurosPerHa = initialCapitalEurosPerHa;
        }

        /// <summary>Copie profonde de l'état (pour la projection forward, Couche 03).</summary>
        public EcosystemModel(EcosystemModel other)
        {
            CurrentDay = other.CurrentDay;
            CurrentWeather = other.CurrentWeather;
            SoilWaterMm = other.SoilWaterMm;
            WaterTableDepthM = other.WaterTableDepthM;
            CarbonYoungTPerHa = other.CarbonYoungTPerHa;
            CarbonOldTPerHa = other.CarbonOldTPerHa;
            MineralNitrogenKgPerHa = other.MineralNitrogenKgPerHa;
            CropYieldTPerHa = other.CropYieldTPerHa;
            HedgerowDensityMPerHa = other.HedgerowDensityMPerHa;
            Biodiversity = other.Biodiversity;
            WeedPressure = other.WeedPressure;
            CapitalEurosPerHa = other.CapitalEurosPerHa;
            LastDrainageMm = other.LastDrainageMm;
            LastEvapotranspirationMm = other.LastEvapotranspirationMm;
            LastCarbonInputTPerHa = other.LastCarbonInputTPerHa;
            LastCarbonRespirationTPerHa = other.LastCarbonRespirationTPerHa;
            LastNitrogenUptakeKgPerHa = other.LastNitrogenUptakeKgPerHa;
            LastNitrogenLeachingKgPerHa = other.LastNitrogenLeachingKgPerHa;
            LastAnnualMarginEurosPerHa = other.LastAnnualMarginEurosPerHa;
            RecentHeatDayCount = other.RecentHeatDayCount;
            RecentCanicularDayCount = other.RecentCanicularDayCount;
            System.Array.Copy(other._heatDayBuffer, _heatDayBuffer, HeatDayWindowDays);
            _heatDayBufferIndex = other._heatDayBufferIndex;
            System.Array.Copy(other._canicularDayBuffer, _canicularDayBuffer, HeatDayWindowDays);
            _canicularDayBufferIndex = other._canicularDayBufferIndex;
        }

        public void AdvanceDay() => CurrentDay++;
        public void SetWeather(DailyWeather weather) => CurrentWeather = weather;
        public void SetSoilWaterMm(double mm) => SoilWaterMm = ClampNonNegative(mm);
        public void SetWaterTableDepthM(double meters) => WaterTableDepthM = ClampNonNegative(meters);

        public void SetCarbonPools(double youngTPerHa, double oldTPerHa)
        {
            CarbonYoungTPerHa = ClampNonNegative(youngTPerHa);
            CarbonOldTPerHa = ClampNonNegative(oldTPerHa);
        }

        public void SetMineralNitrogenKgPerHa(double kg) => MineralNitrogenKgPerHa = ClampNonNegative(kg);
        public void SetCropYieldTPerHa(double tonnes) => CropYieldTPerHa = ClampNonNegative(tonnes);
        public void SetHedgerowDensityMPerHa(double meters) => HedgerowDensityMPerHa = ClampNonNegative(meters);
        public void SetBiodiversity(double value) => Biodiversity = Clamp01(value);
        public void SetWeedPressure(double value) => WeedPressure = Clamp01(value);
        public void SetCapitalEurosPerHa(double value) => CapitalEurosPerHa = value;
        public void AddCapitalEurosPerHa(double delta) => CapitalEurosPerHa += delta;
        public void SetLastDrainageMm(double mm) => LastDrainageMm = ClampNonNegative(mm);
        public void SetLastEvapotranspirationMm(double mm) => LastEvapotranspirationMm = ClampNonNegative(mm);
        public void SetLastCarbonInputTPerHa(double tPerHa) => LastCarbonInputTPerHa = ClampNonNegative(tPerHa);
        public void SetLastCarbonRespirationTPerHa(double tPerHa) => LastCarbonRespirationTPerHa = ClampNonNegative(tPerHa);
        public void SetLastNitrogenUptakeKgPerHa(double kg) => LastNitrogenUptakeKgPerHa = ClampNonNegative(kg);
        public void SetLastNitrogenLeachingKgPerHa(double kg) => LastNitrogenLeachingKgPerHa = ClampNonNegative(kg);
        public void SetLastAnnualMarginEurosPerHa(double euros) => LastAnnualMarginEurosPerHa = euros;

        /// <summary>
        /// Fenêtre glissante O(1) : enregistre la T° moyenne du jour et maintient
        /// <see cref="RecentHeatDayCount"/> (&gt; 25 °C) et
        /// <see cref="RecentCanicularDayCount"/> (&gt; 30 °C) sur 30 jours.
        /// </summary>
        public void RecordDailyTemperatureForWindow(double temperatureCelsius)
        {
            int newSample = temperatureCelsius > HeatDayThresholdCelsius ? 1 : 0;
            int oldSample = _heatDayBuffer[_heatDayBufferIndex];
            _heatDayBuffer[_heatDayBufferIndex] = newSample;
            _heatDayBufferIndex = (_heatDayBufferIndex + 1) % HeatDayWindowDays;
            RecentHeatDayCount = RecentHeatDayCount - oldSample + newSample;

            int newCanicular = temperatureCelsius > CanicularDayThresholdCelsius ? 1 : 0;
            int oldCanicular = _canicularDayBuffer[_canicularDayBufferIndex];
            _canicularDayBuffer[_canicularDayBufferIndex] = newCanicular;
            _canicularDayBufferIndex = (_canicularDayBufferIndex + 1) % HeatDayWindowDays;
            RecentCanicularDayCount = RecentCanicularDayCount - oldCanicular + newCanicular;
        }

        private static double ClampNonNegative(double value) => value < 0.0 ? 0.0 : value;
        private static double Clamp01(double value) => value < 0.0 ? 0.0 : (value > 1.0 ? 1.0 : value);
    }
}
