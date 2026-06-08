using System;

namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// Normale climatique d'un mois calendaire, calibrée hors-ligne depuis un
    /// relevé Météo-France (cf <c>tools/extract_weather_normals.py</c> → JSON).
    /// Consommée par le <see cref="WeatherGenerator"/>. Pas d'I/O ici (Couche
    /// 01) : la structure est chargée et injectée par une couche externe.
    /// </summary>
    public sealed class MonthlyClimate
    {
        /// <summary>Température moyenne journalière du mois (°C).</summary>
        public double TempMeanCelsius { get; }

        /// <summary>Écart-type journalier de la température (°C).</summary>
        public double TempStdCelsius { get; }

        /// <summary>Amplitude diurne moyenne T_max − T_min (°C), requise par Hargreaves.</summary>
        public double DiurnalRangeCelsius { get; }

        /// <summary>Cumul mensuel moyen de précipitations (mm).</summary>
        public double PrecipTotalMm { get; }

        /// <summary>Fraction de jours pluvieux (RR ≥ 1 mm).</summary>
        public double ProbWetDay { get; }

        /// <summary>P(pluie aujourd'hui | pluie hier) — chaîne de Markov 2 états.</summary>
        public double PWetAfterWet { get; }

        /// <summary>P(pluie aujourd'hui | sec hier) — chaîne de Markov 2 états.</summary>
        public double PWetAfterDry { get; }

        /// <summary>Paramètre μ de la log-normale de l'intensité des jours pluvieux.</summary>
        public double LognormalMu { get; }

        /// <summary>Paramètre σ de la log-normale de l'intensité des jours pluvieux.</summary>
        public double LognormalSigma { get; }

        public MonthlyClimate(
            double tempMeanCelsius, double tempStdCelsius, double diurnalRangeCelsius,
            double precipTotalMm, double probWetDay,
            double pWetAfterWet, double pWetAfterDry,
            double lognormalMu, double lognormalSigma)
        {
            TempMeanCelsius = tempMeanCelsius;
            TempStdCelsius = tempStdCelsius;
            DiurnalRangeCelsius = diurnalRangeCelsius;
            PrecipTotalMm = precipTotalMm;
            ProbWetDay = probWetDay;
            PWetAfterWet = pWetAfterWet;
            PWetAfterDry = pWetAfterDry;
            LognormalMu = lognormalMu;
            LognormalSigma = lognormalSigma;
        }
    }

    /// <summary>
    /// Climatologie complète d'une station : 12 normales mensuelles + les
    /// paramètres globaux de l'AR(1) de température (persistance des anomalies,
    /// d'où des vagues de chaleur/froid réalistes). Entrée immuable du
    /// <see cref="WeatherGenerator"/>.
    /// </summary>
    public sealed class Climatology
    {
        public const int MonthsPerYear = 12;

        private readonly MonthlyClimate[] _months;

        /// <summary>Autocorrélation lag-1 de l'anomalie de température (coef. AR(1)).</summary>
        public double TempAr1Phi { get; }

        /// <summary>Écart-type du résidu de l'AR(1) de température (°C).</summary>
        public double TempAr1ResidStd { get; }

        public Climatology(MonthlyClimate[] months, double tempAr1Phi, double tempAr1ResidStd)
        {
            if (months == null) throw new ArgumentNullException(nameof(months));
            if (months.Length != MonthsPerYear)
                throw new ArgumentException(
                    $"Climatologie : {MonthsPerYear} mois attendus, {months.Length} reçus.",
                    nameof(months));
            _months = (MonthlyClimate[])months.Clone();
            TempAr1Phi = tempAr1Phi;
            TempAr1ResidStd = tempAr1ResidStd;
        }

        /// <summary>Normale du mois (1 = janvier … 12 = décembre).</summary>
        public MonthlyClimate Month(int month1To12)
        {
            if (month1To12 < 1 || month1To12 > MonthsPerYear)
                throw new ArgumentOutOfRangeException(nameof(month1To12),
                    "Le mois doit être dans [1, 12].");
            return _months[month1To12 - 1];
        }
    }
}
