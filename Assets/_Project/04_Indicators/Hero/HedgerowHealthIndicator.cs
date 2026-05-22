using Bocage.Sensors;
using Bocage.Sensors.Events;
using Bocage.SimulationCore.Model;

namespace Bocage.Indicators.Hero
{
    /// <summary>
    /// Derived indicator: a [0,1] proxy for hedgerow health, intended to
    /// drive the desaturation / browning of the hedge sprites in the
    /// presentation layer (sub-étape 9β, livrable #5 of l'Étape 9
    /// applied to hedges only — fauna healthT is backlog).
    /// <para>
    /// Honest design (CLAUDE.md §9 sensor primacy): health is NOT a
    /// stand-alone state variable of <see cref="EcosystemModel"/> — it
    /// is an aggregation of what the sensors and the model already
    /// expose. Adding a "health" field to the model would force the
    /// existence of artificial update rules; deriving it here keeps the
    /// presentation channel honest and the simulation surface minimal.
    /// </para>
    /// <para>
    /// Formula (deliberately simple, tunable from data later):
    /// <code>
    /// baseline = normalized(HedgerowDensity)               // 0..1
    /// activeChalara = recent HedgeChalaraEvent within W days
    /// activeDrought = recent DroughtProlongedEvent within W days
    /// health = clamp01(baseline - 0.30 * activeChalara - 0.20 * activeDrought)
    /// </code>
    /// W = <see cref="EventInfluenceWindowDays"/> (60 days by default,
    /// matching the typical recovery time of a stressed but unbroken
    /// hedge canopy per INRAE ash-dieback monitoring).
    /// </para>
    /// <para>
    /// The chalara penalty is larger than the drought penalty because
    /// chalara is a chronic structural loss (cf. HedgeChalaraEvent
    /// calibration), whereas a prolonged drought leaves the canopy
    /// stressed but with regenerative capacity on the next wet season.
    /// </para>
    /// </summary>
    public static class HedgerowHealthIndicator
    {
        public const int EventInfluenceWindowDays = 60;
        public const double ChalaraPenalty = 0.30;
        public const double DroughtPenalty = 0.20;

        /// <summary>
        /// Computes the hedgerow health proxy in [0,1] for the current
        /// state of <paramref name="model"/>, biased downwards by any
        /// recent stress events in <paramref name="log"/>. The log may
        /// be null (e.g. during EditMode bootstrapping); in that case
        /// only the density baseline is returned.
        /// </summary>
        public static double Compute(EcosystemModel model, EventLog log)
        {
            double baseline = HedgerowDensityIndicator.Normalize(model.HedgerowDensity);

            if (log == null || log.Count == 0)
            {
                return Clamp01(baseline);
            }

            int currentDay = model.CurrentDay;
            double penalty = 0.0;

            var lastChalara = log.LatestOfType<HedgeChalaraEvent>();
            if (lastChalara != null && (currentDay - lastChalara.DetectedOnDay) < EventInfluenceWindowDays)
            {
                penalty += ChalaraPenalty;
            }

            var lastDrought = log.LatestOfType<DroughtProlongedEvent>();
            if (lastDrought != null && (currentDay - lastDrought.DetectedOnDay) < EventInfluenceWindowDays)
            {
                penalty += DroughtPenalty;
            }

            return Clamp01(baseline - penalty);
        }

        /// <summary>
        /// Identity normalize: <see cref="Compute"/> already returns a
        /// [0,1] value. Kept for symmetry with sibling indicators.
        /// </summary>
        public static double Normalize(double health01)
        {
            return Clamp01(health01);
        }

        private static double Clamp01(double v)
        {
            if (v < 0.0) return 0.0;
            if (v > 1.0) return 1.0;
            return v;
        }
    }
}
