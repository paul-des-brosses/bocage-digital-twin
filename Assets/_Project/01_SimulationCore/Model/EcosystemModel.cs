using System;

namespace Bocage.SimulationCore.Model
{
    /// <summary>
    /// Minimal state container for the bocage ecosystem. Holds the variables
    /// the simulation core can read and rules can mutate, plus a simulated
    /// day counter. State is constrained at the setters so invariants
    /// (non-negative depth, non-negative density, ...) cannot be broken.
    /// Defaults are centred on Perche bocage realistic ranges (annual
    /// average temperature ≈ 12 °C, hedgerow density 60-110 m/ha,
    /// mixed cereal/oilseed yield ≈ 5.5 t/ha Eure-et-Loir/Orne,
    /// input cost ≈ 1200 €/ha/yr CIVAM grandes cultures).
    /// </summary>
    public sealed class EcosystemModel
    {
        public int CurrentDay { get; private set; }

        public Weather CurrentWeather { get; private set; }

        /// <summary>Distance from surface to top of the water table, in metres. 0 = at surface.</summary>
        public double WaterTableDepth { get; private set; }

        /// <summary>Hedgerow density expressed in metres of hedgerow per hectare.</summary>
        public double HedgerowDensity { get; private set; }

        /// <summary>
        /// Current estimate of the crop yield at harvest, in tonnes per
        /// hectare. Evolves daily under the influence of the windbreak
        /// effect of hedgerows, the water table depth, climate stress
        /// and agricultural pressure (cf. CropYieldDynamicsRule).
        /// Calibration source: weighted average of Eure-et-Loir wheat
        /// (7 t/ha) and rapeseed (3 t/ha) yields per Agreste 2015-2024.
        /// </summary>
        public double CropYield { get; private set; }

        /// <summary>
        /// Annualised cost of inputs (fertilisers, pesticides, fuel,
        /// seeds), in € per hectare per year. Calibration source:
        /// CIVAM and AFPF range 1100-2000 €/ha/yr for grandes cultures
        /// annuelles. Default 1200 = median conventional Perche mix.
        /// </summary>
        public double InputCost { get; private set; }

        /// <summary>
        /// Annualised cost of maintaining the bocage features (hedge
        /// trimming, replanting, pond clearance), in € per hectare per
        /// year. Linear in HedgerowDensity at 1.0 €/m/yr (cf.
        /// MaintenanceCostDynamicsRule sources).
        /// </summary>
        public double MaintenanceCost { get; private set; }

        /// <summary>
        /// Composite fauna abundance index, dimensionless. 1.0 represents
        /// the Perche bocage reference state (90 m/ha hedges, water table
        /// at 2 m, conventional input intensity). Values above 1 indicate
        /// richer fauna than reference (denser hedges, wetter, less
        /// intensive), values below 1 indicate impoverished fauna
        /// (intensification, drought, hedge removal).
        /// <para>
        /// Calibration sources: INRAE / OFB Vigie-Nature (−30 % oiseaux
        /// des milieux agricoles depuis 1989), Réseau Haies / Solagro
        /// (doublement des passereaux bocage vs zone ouverte — Constant
        /// et al. 1976), Hallmann et al. 2017 / MNHN 2024 (−75 % biomasse
        /// insectes en 27 ans sous pression agricole). Dynamics with a ~1-year
        /// time constant: fauna populations track habitat changes slowly.
        /// </para>
        /// </summary>
        public double FaunaPopulation { get; private set; }

        /// <summary>
        /// Number of days with daily mean temperature above
        /// <see cref="HeatDayThresholdCelsius"/> over the last
        /// <see cref="HeatDayWindowDays"/> simulated days. Updated by
        /// <see cref="Bocage.SimulationCore.Rules.WeatherUpdateRule"/>
        /// after each daily draw via
        /// <see cref="RecordDailyTemperatureForWindow"/>; read by
        /// <see cref="Bocage.SimulationCore.Rules.CropYieldDynamicsRule"/>
        /// and
        /// <see cref="Bocage.SimulationCore.Rules.InputCostDynamicsRule"/>
        /// to apply an additive heat-stress term on top of the scenario
        /// anomaly term (chantier E2 / ADR #52).
        /// <para>
        /// During the first 30 simulated days the buffer is still warming
        /// up, so the count reflects only the days observed so far. After
        /// day 30 the window is fully populated and behaves as a rolling
        /// 30-day count.
        /// </para>
        /// </summary>
        public int RecentHeatDayCount { get; private set; }

        public const int HeatDayWindowDays = 30;
        public const double HeatDayThresholdCelsius = 25.0;
        private readonly int[] _heatDayBuffer = new int[HeatDayWindowDays];
        private int _heatDayBufferIndex;

        /// <summary>
        /// Number of days with daily mean temperature above
        /// <see cref="CanicularDayThresholdCelsius"/> over the last
        /// <see cref="HeatDayWindowDays"/> simulated days. Tracked in
        /// parallel with <see cref="RecentHeatDayCount"/> but at a higher
        /// threshold (heatwave, not just hot day). Used by
        /// <see cref="Bocage.SimulationCore.Rules.FaunaDynamicsRule"/>
        /// to apply a small penalty on fauna when heatwaves accumulate
        /// (chantier E5 / ADR #51 — Hallmann 2017 insect collapse
        /// under thermal stress).
        /// </summary>
        public int RecentCanicularDayCount { get; private set; }

        public const double CanicularDayThresholdCelsius = 30.0;
        private readonly int[] _canicularDayBuffer = new int[HeatDayWindowDays];
        private int _canicularDayBufferIndex;

        /// <summary>
        /// Soil organic carbon stock in tonnes of carbon per hectare,
        /// tracked by the 1-pool model in
        /// <see cref="Bocage.SimulationCore.Rules.SoilCarbonDynamicsRule"/>
        /// (chantier E3 / ADR #48). Default 50 tC/ha reflects BDAT INRAE
        /// reference for cultivated bocage soils in the Perche.
        /// <para>
        /// Read by the EddyTower sensor (Couche 02) to derive the daily
        /// net CO2/CH4 flux from ΔSoilCarbonStock, and by
        /// <see cref="Bocage.Indicators.Hero.SoilCarbonIndicator"/>
        /// (Couche 04) for the Climat &amp; Ressources panel.
        /// </para>
        /// </summary>
        public double SoilCarbonStock { get; private set; }

        public EcosystemModel(
            int initialDay = 0,
            Weather initialWeather = default,
            double initialWaterTableDepth = 2.0,
            double initialHedgerowDensity = 90.0,
            double initialCropYield = 5.5,
            double initialInputCost = 1200.0,
            double initialMaintenanceCost = 90.0,
            double initialFaunaPopulation = 1.0,
            double initialSoilCarbonStock = 50.0)
        {
            CurrentDay = initialDay;
            CurrentWeather = initialWeather;
            WaterTableDepth = ClampNonNegative(initialWaterTableDepth);
            HedgerowDensity = ClampNonNegative(initialHedgerowDensity);
            CropYield = ClampNonNegative(initialCropYield);
            InputCost = ClampNonNegative(initialInputCost);
            MaintenanceCost = ClampNonNegative(initialMaintenanceCost);
            FaunaPopulation = ClampNonNegative(initialFaunaPopulation);
            SoilCarbonStock = ClampNonNegative(initialSoilCarbonStock);
        }

        public void AdvanceDay()
        {
            CurrentDay++;
        }

        public void SetWeather(Weather weather)
        {
            CurrentWeather = weather;
        }

        public void SetWaterTableDepth(double depthInMeters)
        {
            WaterTableDepth = ClampNonNegative(depthInMeters);
        }

        public void SetHedgerowDensity(double metersPerHectare)
        {
            HedgerowDensity = ClampNonNegative(metersPerHectare);
        }

        public void SetCropYield(double tonnesPerHectare)
        {
            CropYield = ClampNonNegative(tonnesPerHectare);
        }

        public void SetInputCost(double eurosPerHectarePerYear)
        {
            InputCost = ClampNonNegative(eurosPerHectarePerYear);
        }

        public void SetMaintenanceCost(double eurosPerHectarePerYear)
        {
            MaintenanceCost = ClampNonNegative(eurosPerHectarePerYear);
        }

        public void SetFaunaPopulation(double index)
        {
            FaunaPopulation = ClampNonNegative(index);
        }

        public void SetSoilCarbonStock(double tonnesCarbonPerHectare)
        {
            SoilCarbonStock = ClampNonNegative(tonnesCarbonPerHectare);
        }

        /// <summary>
        /// Rolling-window update: registers today's daily mean temperature,
        /// overwriting the entry that was recorded
        /// <see cref="HeatDayWindowDays"/> days ago. Maintains
        /// <see cref="RecentHeatDayCount"/> (T° > 25 °C) and
        /// <see cref="RecentCanicularDayCount"/> (T° > 30 °C) in O(1).
        /// The two counters share the same window length but distinct
        /// thresholds and buffers — a 32 °C day increments both counts,
        /// a 27 °C day increments only the heat-day count.
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

        private static double ClampNonNegative(double value)
        {
            return value < 0.0 ? 0.0 : value;
        }
    }
}
