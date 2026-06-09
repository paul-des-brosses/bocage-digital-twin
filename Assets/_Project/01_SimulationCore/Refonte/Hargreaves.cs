using System;

namespace Bocage.SimulationCore.Refonte
{
    /// <summary>
    /// ETP de référence (mm/j) par la formule de Hargreaves &amp; Samani (1985),
    /// qui ne demande que les températures (T_min, T_max, T_moy) — adapté à un
    /// relevé sans rayonnement mesuré. Le rayonnement extraterrestre Ra est
    /// calculé depuis la latitude et le jour de l'année (FAO-56, Allen et al.
    /// 1998). Sensible à la température : sous réchauffement, ETP↑ → le sol
    /// s'assèche plus vite — le mécanisme qui fait « mordre » la sécheresse.
    /// </summary>
    public static class Hargreaves
    {
        private const double SolarConstant = 0.0820;  // Gsc, MJ/m²/min
        private const double MjToMmEvap = 0.408;       // 1 MJ/m²/j ≡ 0,408 mm/j (FAO-56)

        /// <summary>
        /// Rayonnement extraterrestre Ra (mm/j équivalent évaporation),
        /// FAO-56 eq. 21-24, depuis le jour de l'année (1-365) et la latitude.
        /// </summary>
        public static double ExtraterrestrialRadiationMm(int dayOfYear, double latitudeDegrees)
        {
            double j = dayOfYear;
            double latRad = latitudeDegrees * Math.PI / 180.0;
            double dr = 1.0 + 0.033 * Math.Cos(2.0 * Math.PI * j / 365.0);
            double decl = 0.409 * Math.Sin(2.0 * Math.PI * j / 365.0 - 1.39);
            double sunsetArg = -Math.Tan(latRad) * Math.Tan(decl);
            if (sunsetArg < -1.0) sunsetArg = -1.0;
            else if (sunsetArg > 1.0) sunsetArg = 1.0;
            double ws = Math.Acos(sunsetArg);
            double raMj = (24.0 * 60.0 / Math.PI) * SolarConstant * dr
                * (ws * Math.Sin(latRad) * Math.Sin(decl)
                   + Math.Cos(latRad) * Math.Cos(decl) * Math.Sin(ws));
            double raMm = raMj * MjToMmEvap;
            return raMm < 0.0 ? 0.0 : raMm;
        }

        /// <summary>
        /// ETP de référence ET0 (mm/j) :
        /// <c>0.0023 · Ra · (T_moy + 17.8) · sqrt(T_max − T_min)</c>.
        /// </summary>
        public static double ReferenceEt0(int dayOfYear, double latitudeDegrees,
            double tMinCelsius, double tMaxCelsius, double tMeanCelsius)
        {
            double raMm = ExtraterrestrialRadiationMm(dayOfYear, latitudeDegrees);
            double range = tMaxCelsius - tMinCelsius;
            if (range < 0.0) range = 0.0;
            double et0 = 0.0023 * raMm * (tMeanCelsius + 17.8) * Math.Sqrt(range);
            return et0 < 0.0 ? 0.0 : et0;
        }
    }
}
