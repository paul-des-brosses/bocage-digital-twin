"""
Extract the 12 monthly weather normals + Markov-rain parameters for one
Météo-France station from its daily climatology CSV, over a reference period
(default 1991-2020).

For each calendar month the script derives EVERY parameter the simulation's
seasonal weather model consumes (Couche 01 `SeasonalWeatherDataDefaults` →
`MonthlyClimate` → `MarkovRainModel` / `WeatherUpdateRule`):

  - temp_mean_celsius   : mean daily temperature (TM)
  - precip_total_mm     : mean monthly precipitation total (sum of RR / years)
  - prob_wet_day        : fraction of days with RR >= 1 mm (WMO rain day)
  - lognormal_mu/sigma  : log-normal fit of wet-day rainfall intensity

so the chain « raw open data → monthly normals → simulation » is fully
traceable, which is the whole point of versioning the CSV + this script in
the repo rather than hard-coding magic numbers.

Data source: Météo-France « Données climatologiques de base - quotidiennes »
(https://meteo.data.gouv.fr, Licence Ouverte / Etalab 2.0). Reference station:
Mortagne-au-Perche « Mortagne-Parc », NUM_POSTE 61293003 (Orne, Perche).

Usage:
    python tools/extract_weather_normals.py \\
        data/Q_61_previous-1950-2023_RR-T-Vent.csv \\
        --station 61293003 --start-year 1991 --end-year 2020 \\
        --output data/normales_mortagne_1991-2020.json

The CSV is the semicolon-separated MF base-quotidienne export for a département
(it bundles every station, so `--station` selects ours). Required columns:
NUM_POSTE, AAAAMMJJ (date YYYYMMDD), RR (daily precip mm), TM (daily mean
temperature °C). Rows with a missing RR or TM are skipped for that variable
only. Pure standard library — no third-party dependency.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import statistics
import sys
from collections import defaultdict
from pathlib import Path

MONTH_NAMES_FR = [
    "Jan", "Fév", "Mar", "Avr", "Mai", "Juin",
    "Juil", "Août", "Sep", "Oct", "Nov", "Déc",
]
# A day with at least this much rain counts as a "wet day" (WMO convention).
WET_DAY_THRESHOLD_MM = 1.0


def parse_float(raw: str | None) -> float | None:
    """Parse an MF numeric cell. Accepts ',' or '.' decimals; empty/invalid → None."""
    if raw is None:
        return None
    s = raw.strip().replace(",", ".")
    if s == "":
        return None
    try:
        return float(s)
    except ValueError:
        return None


def read_station_daily(csv_path: Path, station: str, start_year: int, end_year: int):
    """
    Yield (year, month, rr_mm_or_None, tm_c_or_None) for the station's rows
    within [start_year, end_year]. Auto-detects the delimiter (MF uses ';')
    and looks columns up case-insensitively.
    """
    with csv_path.open("r", encoding="utf-8", newline="") as fh:
        sample = fh.read(8192)
        fh.seek(0)
        delimiter = ";" if sample.count(";") >= sample.count(",") else ","
        reader = csv.DictReader(fh, delimiter=delimiter)
        fields = {name.upper(): name for name in (reader.fieldnames or [])}
        col_poste = fields.get("NUM_POSTE")
        col_date = fields.get("AAAAMMJJ") or fields.get("DATE")
        col_rr = fields.get("RR")
        col_tm = fields.get("TM")
        if not (col_poste and col_date and col_rr and col_tm):
            raise SystemExit(
                "CSV is missing one of the required columns NUM_POSTE, AAAAMMJJ, "
                f"RR, TM. Found: {reader.fieldnames}"
            )
        target = str(station).strip()
        for row in reader:
            if str(row[col_poste]).strip() != target:
                continue
            date = str(row[col_date]).strip()
            if len(date) < 6 or not date[:6].isdigit():
                continue
            year = int(date[:4])
            if year < start_year or year > end_year:
                continue
            month = int(date[4:6])
            if month < 1 or month > 12:
                continue
            yield year, month, parse_float(row[col_rr]), parse_float(row[col_tm])


def compute_normals(rows) -> list[dict]:
    """Aggregate daily rows into the 12 monthly normals (see module docstring)."""
    tm_by_month: dict[int, list[float]] = defaultdict(list)
    rr_by_year_month: dict[tuple[int, int], float] = defaultdict(float)
    years_seen: dict[int, set[int]] = defaultdict(set)
    wet_intensities: dict[int, list[float]] = defaultdict(list)
    wet_count: dict[int, int] = defaultdict(int)
    day_count: dict[int, int] = defaultdict(int)

    for year, month, rr, tm in rows:
        if tm is not None:
            tm_by_month[month].append(tm)
        if rr is not None:
            rr_by_year_month[(year, month)] += rr
            years_seen[month].add(year)
            day_count[month] += 1
            if rr >= WET_DAY_THRESHOLD_MM:
                wet_count[month] += 1
                wet_intensities[month].append(rr)

    normals: list[dict] = []
    for month in range(1, 13):
        tms = tm_by_month[month]
        temp_mean = statistics.fmean(tms) if tms else None

        n_years = len(years_seen[month])
        monthly_sums = [v for (y, m), v in rr_by_year_month.items() if m == month]
        precip_total = (sum(monthly_sums) / n_years) if n_years else None

        days = day_count[month]
        prob_wet = (wet_count[month] / days) if days else None

        intensities = wet_intensities[month]
        if len(intensities) >= 2:
            logs = [math.log(x) for x in intensities]
            mu = statistics.fmean(logs)
            sigma = statistics.pstdev(logs)
        else:
            mu = sigma = None

        normals.append({
            "month": month,
            "name": MONTH_NAMES_FR[month - 1],
            "n_days": days,
            "n_years": n_years,
            "temp_mean_celsius": _round(temp_mean, 2),
            "precip_total_mm": _round(precip_total, 1),
            "prob_wet_day": _round(prob_wet, 3),
            "lognormal_mu": _round(mu, 3),
            "lognormal_sigma": _round(sigma, 3),
        })
    return normals


def _round(value: float | None, digits: int):
    return None if value is None else round(value, digits)


def _fmt(value) -> str:
    return "—" if value is None else f"{value}"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("csv", type=Path, help="Météo-France daily climatology CSV (base quotidienne).")
    parser.add_argument("--station", default="61293003",
                        help="NUM_POSTE of the station (default Mortagne-Parc 61293003).")
    parser.add_argument("--start-year", type=int, default=1991)
    parser.add_argument("--end-year", type=int, default=2020)
    parser.add_argument("--output", type=Path,
                        default=Path("data/normales_mortagne_1991-2020.json"))
    args = parser.parse_args()

    if not args.csv.is_file():
        print(f"CSV not found: {args.csv}", file=sys.stderr)
        return 1

    print(f"[weather-normals] station={args.station} period={args.start_year}-{args.end_year}")
    rows = list(read_station_daily(args.csv, args.station, args.start_year, args.end_year))
    if not rows:
        print("No rows matched the station / period. Check --station and the CSV.", file=sys.stderr)
        return 1
    print(f"[weather-normals] {len(rows):,} daily records")

    normals = compute_normals(rows)

    temps = [m["temp_mean_celsius"] for m in normals if m["temp_mean_celsius"] is not None]
    annual_temp = round(statistics.fmean(temps), 2) if temps else None
    annual_precip = round(sum(m["precip_total_mm"] for m in normals
                             if m["precip_total_mm"] is not None), 1)

    payload = {
        "source": "Météo-France — Données climatologiques de base quotidiennes "
                  "(meteo.data.gouv.fr, Licence Ouverte / Etalab 2.0)",
        "station": args.station,
        "period": f"{args.start_year}-{args.end_year}",
        "wet_day_threshold_mm": WET_DAY_THRESHOLD_MM,
        "annual_temp_mean_celsius": annual_temp,
        "annual_precip_total_mm": annual_precip,
        "monthly": normals,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2, ensure_ascii=False)
    print(f"[weather-normals] wrote {args.output}")

    # Human-readable table the user can copy into SeasonalWeatherDataDefaults.
    print(f"[weather-normals] annual mean: {annual_temp} °C / {annual_precip} mm")
    print(f"{'Mois':<5} {'T°':>6} {'Précip':>7} {'p_wet':>6} {'mu':>6} {'sigma':>6}")
    for m in normals:
        print(f"{m['name']:<5} {_fmt(m['temp_mean_celsius']):>6} {_fmt(m['precip_total_mm']):>7} "
              f"{_fmt(m['prob_wet_day']):>6} {_fmt(m['lognormal_mu']):>6} {_fmt(m['lognormal_sigma']):>6}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
