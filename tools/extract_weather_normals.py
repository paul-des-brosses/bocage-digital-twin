"""
Extrait, depuis un CSV climatologique quotidien Météo-France, TOUS les
paramètres dont le **générateur météo stochastique** du digital twin a besoin
(Couche 01) — calibrés sur une station réelle, sur une période de référence.

Le générateur (approche Richardson/WGEN) ne *rejoue pas* le CSV : il le résume
en une climatologie mensuelle, puis tire une météo synthétique déterministe
(seedée). Pour chaque mois calendaire le script dérive :

  - temp_mean_celsius     : température moyenne journalière (TM, ou (TN+TX)/2)
  - temp_std_celsius      : écart-type journalier de TM (amplitude saisonnière)
  - diurnal_range_celsius : amplitude diurne moyenne TX − TN  (requise par l'ETP
                            de Hargreaves)
  - precip_total_mm       : cumul mensuel moyen (somme RR / nb d'années)
  - prob_wet_day          : fraction de jours pluvieux (RR ≥ 1 mm, conv. OMM)
  - p_wet_after_wet (P11) : P(pluie aujourd'hui | pluie hier)   ┐ chaîne de
  - p_wet_after_dry (P01) : P(pluie aujourd'hui | sec hier)     ┘ Markov 2 états
  - lognormal_mu / sigma  : ajustement log-normal de l'intensité des jours pluvieux

Et, au niveau global (persistance des anomalies de température, → vagues de
chaleur/froid réalistes) :

  - temp_ar1_phi          : autocorrélation lag-1 de l'anomalie de T° (modèle AR(1))
  - temp_ar1_resid_std    : écart-type du résidu AR(1)

Ainsi la chaîne « open data → climatologie → générateur » est entièrement
traçable, ce qui justifie de versionner le CSV + ce script dans le repo plutôt
que de coder des nombres magiques.

Source : Météo-France « Données climatologiques de base - quotidiennes »
(meteo.data.gouv.fr, Licence Ouverte / Etalab 2.0). Station du projet :
Tourouvre-au-Perche (« TOUROUVRE_SAPC », Orne, Perche).

Formats acceptés (détection insensible à la casse) :
  - colonne station : NUM_POSTE *ou* NOM_USUEL (filtre optionnel via --station)
  - colonne date    : AAAAMMJJ (YYYYMMDD) *ou* DATE
  - pluie           : RR (mm)
  - température      : TM *ou*, à défaut, TN et TX (TM dérivé = (TN+TX)/2)
Séparateur ';' ou ',' auto-détecté. Bibliothèque standard uniquement.

Usage :
    python tools/extract_weather_normals.py Meteo_Tourouvre.csv \\
        --output data/normales_tourouvre.json
    # --station, --start-year, --end-year sont optionnels (défaut : tout le fichier)
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import statistics
import sys
from collections import defaultdict
from datetime import date
from pathlib import Path

MONTH_NAMES_FR = [
    "Jan", "Fév", "Mar", "Avr", "Mai", "Juin",
    "Juil", "Août", "Sep", "Oct", "Nov", "Déc",
]
# Un jour avec au moins cette pluie compte comme « jour pluvieux » (conv. OMM).
WET_DAY_THRESHOLD_MM = 1.0


def parse_float(raw: str | None) -> float | None:
    """Parse une cellule numérique MF. Accepte ',' ou '.' ; vide/invalide → None."""
    if raw is None:
        return None
    s = raw.strip().replace(",", ".")
    if s == "":
        return None
    try:
        return float(s)
    except ValueError:
        return None


def parse_date(raw: str | None) -> date | None:
    """AAAAMMJJ (YYYYMMDD) → datetime.date, ou None si invalide."""
    s = (raw or "").strip()
    if len(s) < 8 or not s[:8].isdigit():
        return None
    try:
        return date(int(s[:4]), int(s[4:6]), int(s[6:8]))
    except ValueError:
        return None


def read_station_daily(csv_path: Path, station, start_year, end_year):
    """
    Yield (d: date, rr, tm, tn, tx) pour la station, dans [start_year, end_year]
    (bornes None = pas de filtre). TM est dérivé de (TN+TX)/2 si absent.
    """
    with csv_path.open("r", encoding="utf-8", newline="") as fh:
        sample = fh.read(8192)
        fh.seek(0)
        delimiter = ";" if sample.count(";") >= sample.count(",") else ","
        reader = csv.DictReader(fh, delimiter=delimiter)
        fields = {name.upper(): name for name in (reader.fieldnames or [])}
        col_station = fields.get("NUM_POSTE") or fields.get("NOM_USUEL")
        col_date = fields.get("AAAAMMJJ") or fields.get("DATE")
        col_rr = fields.get("RR")
        col_tm = fields.get("TM")
        col_tn = fields.get("TN")
        col_tx = fields.get("TX")
        if not (col_date and col_rr):
            raise SystemExit(
                f"CSV : colonne date (AAAAMMJJ/DATE) ou RR manquante. "
                f"Trouvé : {reader.fieldnames}")
        if col_tm is None and not (col_tn and col_tx):
            raise SystemExit("CSV : ni TM ni (TN, TX) — température inexploitable.")
        target = str(station).strip() if station else None
        for row in reader:
            if target and col_station and str(row[col_station]).strip() != target:
                continue
            d = parse_date(row[col_date])
            if d is None:
                continue
            if start_year and d.year < start_year:
                continue
            if end_year and d.year > end_year:
                continue
            rr = parse_float(row[col_rr])
            tn = parse_float(row[col_tn]) if col_tn else None
            tx = parse_float(row[col_tx]) if col_tx else None
            tm = parse_float(row[col_tm]) if col_tm else None
            if tm is None and tn is not None and tx is not None:
                tm = (tn + tx) / 2.0
            yield (d, rr, tm, tn, tx)


def compute(rows):
    """Agrège les jours en 12 normales mensuelles + paramètres du générateur."""
    rows = sorted(rows, key=lambda r: r[0])

    tm_by_month: dict[int, list[float]] = defaultdict(list)
    diurnal_by_month: dict[int, list[float]] = defaultdict(list)
    rr_by_year_month: dict[tuple[int, int], float] = defaultdict(float)
    years_seen: dict[int, set[int]] = defaultdict(set)
    wet_intensities: dict[int, list[float]] = defaultdict(list)
    wet_count: dict[int, int] = defaultdict(int)
    day_count: dict[int, int] = defaultdict(int)

    for d, rr, tm, tn, tx in rows:
        m = d.month
        if tm is not None:
            tm_by_month[m].append(tm)
        if tn is not None and tx is not None:
            diurnal_by_month[m].append(tx - tn)
        if rr is not None:
            rr_by_year_month[(d.year, m)] += rr
            years_seen[m].add(d.year)
            day_count[m] += 1
            if rr >= WET_DAY_THRESHOLD_MM:
                wet_count[m] += 1
                wet_intensities[m].append(rr)

    temp_mean = {m: statistics.fmean(tm_by_month[m])
                 for m in range(1, 13) if tm_by_month[m]}

    # --- Chaîne de Markov d'occurrence + AR(1) température, sur jours consécutifs ---
    ww_num = defaultdict(int); ww_den = defaultdict(int)   # pluie | pluie hier
    dw_num = defaultdict(int); dw_den = defaultdict(int)   # pluie | sec hier
    ar_pairs: list[tuple[float, float]] = []               # (anomalie hier, anomalie auj.)
    prev = None
    for d, rr, tm, tn, tx in rows:
        if prev is not None:
            pd_, prr, ptm = prev[0], prev[1], prev[2]
            if (d - pd_).days == 1:                        # jours calendaires consécutifs
                m = d.month
                if prr is not None and rr is not None:
                    prev_wet = prr >= WET_DAY_THRESHOLD_MM
                    cur_wet = rr >= WET_DAY_THRESHOLD_MM
                    if prev_wet:
                        ww_den[m] += 1
                        if cur_wet:
                            ww_num[m] += 1
                    else:
                        dw_den[m] += 1
                        if cur_wet:
                            dw_num[m] += 1
                if (tm is not None and ptm is not None
                        and pd_.month in temp_mean and m in temp_mean):
                    ar_pairs.append((ptm - temp_mean[pd_.month], tm - temp_mean[m]))
        prev = (d, rr, tm, tn, tx)

    sxx = sum(a * a for a, _ in ar_pairs)
    sxy = sum(a * b for a, b in ar_pairs)
    phi = (sxy / sxx) if sxx > 0 else 0.0
    resid_std = (math.sqrt(statistics.fmean([(b - phi * a) ** 2 for a, b in ar_pairs]))
                 if ar_pairs else None)

    normals: list[dict] = []
    for m in range(1, 13):
        tms = tm_by_month[m]
        intensities = wet_intensities[m]
        if len(intensities) >= 2:
            logs = [math.log(x) for x in intensities]
            mu = statistics.fmean(logs)
            sigma = statistics.pstdev(logs)
        else:
            mu = sigma = None
        n_years = len(years_seen[m])
        monthly_sums = [v for (y, mm), v in rr_by_year_month.items() if mm == m]
        precip_total = (sum(monthly_sums) / n_years) if n_years else None
        days = day_count[m]
        prob_wet = (wet_count[m] / days) if days else None

        normals.append({
            "month": m,
            "name": MONTH_NAMES_FR[m - 1],
            "n_days": days,
            "n_years": n_years,
            "temp_mean_celsius": _round(statistics.fmean(tms), 2) if tms else None,
            "temp_std_celsius": _round(statistics.pstdev(tms), 2) if len(tms) >= 2 else None,
            "diurnal_range_celsius": _round(statistics.fmean(diurnal_by_month[m]), 2)
                                     if diurnal_by_month[m] else None,
            "precip_total_mm": _round(precip_total, 1),
            "prob_wet_day": _round(prob_wet, 3),
            "p_wet_after_wet": _round(ww_num[m] / ww_den[m], 3) if ww_den[m] else None,
            "p_wet_after_dry": _round(dw_num[m] / dw_den[m], 3) if dw_den[m] else None,
            "lognormal_mu": _round(mu, 3),
            "lognormal_sigma": _round(sigma, 3),
        })

    return normals, _round(phi, 3), _round(resid_std, 3)


def _round(value, digits):
    return None if value is None else round(value, digits)


def _fmt(value) -> str:
    return "—" if value is None else f"{value}"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("csv", type=Path, help="CSV quotidien Météo-France (base quotidienne).")
    parser.add_argument("--station", default=None,
                        help="NUM_POSTE ou NOM_USUEL à filtrer (défaut : tout le fichier).")
    parser.add_argument("--start-year", type=int, default=None)
    parser.add_argument("--end-year", type=int, default=None)
    parser.add_argument("--output", type=Path, default=Path("data/normales_tourouvre.json"))
    args = parser.parse_args()

    if not args.csv.is_file():
        print(f"CSV introuvable : {args.csv}", file=sys.stderr)
        return 1

    rows = list(read_station_daily(args.csv, args.station, args.start_year, args.end_year))
    if not rows:
        print("Aucune ligne ne correspond (vérifier --station et le CSV).", file=sys.stderr)
        return 1
    y0 = min(r[0].year for r in rows)
    y1 = max(r[0].year for r in rows)
    print(f"[weather] station={args.station or 'TOUTES'} periode={y0}-{y1} "
          f"({len(rows):,} jours)")

    normals, phi, resid_std = compute(rows)

    temps = [m["temp_mean_celsius"] for m in normals if m["temp_mean_celsius"] is not None]
    annual_temp = round(statistics.fmean(temps), 2) if temps else None
    annual_precip = round(sum(m["precip_total_mm"] for m in normals
                             if m["precip_total_mm"] is not None), 1)

    payload = {
        "source": "Météo-France — base quotidienne (meteo.data.gouv.fr, Licence Ouverte / Etalab 2.0)",
        "station": args.station or (normals and "TOUROUVRE_SAPC"),
        "period": f"{y0}-{y1}",
        "wet_day_threshold_mm": WET_DAY_THRESHOLD_MM,
        "annual_temp_mean_celsius": annual_temp,
        "annual_precip_total_mm": annual_precip,
        "temp_ar1_phi": phi,
        "temp_ar1_resid_std": resid_std,
        "monthly": normals,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2, ensure_ascii=False)
    print(f"[weather] ecrit {args.output}")

    print(f"[weather] annuel : {annual_temp} C / {annual_precip} mm | "
          f"AR(1) T : phi={phi}  resid_std={resid_std} C")
    print(f"{'Mois':<5}{'Tmoy':>6}{'Tstd':>6}{'diurn':>7}{'precip':>8}{'p_wet':>7}"
          f"{'P11':>6}{'P01':>6}{'mu':>6}{'sig':>6}")
    for m in normals:
        print(f"{m['name']:<5}{_fmt(m['temp_mean_celsius']):>6}{_fmt(m['temp_std_celsius']):>6}"
              f"{_fmt(m['diurnal_range_celsius']):>7}{_fmt(m['precip_total_mm']):>8}"
              f"{_fmt(m['prob_wet_day']):>7}{_fmt(m['p_wet_after_wet']):>6}"
              f"{_fmt(m['p_wet_after_dry']):>6}{_fmt(m['lognormal_mu']):>6}"
              f"{_fmt(m['lognormal_sigma']):>6}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
