"""
Bulk-quantize all detoured sprites in Sprites/Source/ through the
postprocess pipeline, placing outputs at the canonical paths inside
Assets/_Project/05_Presentation/Scene/Sprites/<subdir>/.

Per-sprite decisions (filename, category, max-size) are captured in
the SPRITES table below. This is the single source of truth for what
gets placed where and at what resolution.

Categories and target longest-side (DA decision 2026-05-12):
    Background  1024 px  - wide horizon, less detail-critical
    Midground    512 px  - hedges tile in scene, medium detail
    Foreground   768 px  - hero elements, more detail
    Fauna        256 px  - matches swallow precedent (256x121)
    Sensors      256 px  - small icon-scale objects in scene

Side-by-side previews are generated next to each quantized sprite in
Sprites/Source/_test_quantization_v1/ (gitignored) for DA inspection
before final integration in Unity.

Usage:
    python tools/bulk_quantize.py
    python tools/bulk_quantize.py --dry-run    # just print, no I/O
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

from PIL import Image


# (source_filename, subdir, output_filename, max_size)
# Already integrated and skipped here: bird_swallow_flight -> Fauna/swallow.png
SPRITES: list[tuple[str, str, str, int]] = [
    # Background
    ("hills_perche_v1_detoured.png", "Background", "hills_perche.png", 1024),

    # Midground (hedges, 9 variants)
    ("hedge_low_01_v1_detoured.png", "Midground", "hedge_low_01.png", 512),
    ("hedge_low_02_v1_detoured.png", "Midground", "hedge_low_02.png", 512),
    ("hedge_low_03_v1_detoured.png", "Midground", "hedge_low_03.png", 512),
    ("hedge_thin_sparse_01_v1_detoured.png", "Midground", "hedge_thin_sparse_01.png", 512),
    ("hedge_thin_sparse_02_v1_detoured.png", "Midground", "hedge_thin_sparse_02.png", 512),
    ("hedge_thin_sparse_03_v1_detoured.png", "Midground", "hedge_thin_sparse_03.png", 512),
    ("hedge_high_pollard_01_v1_detoured.png", "Midground", "hedge_high_pollard_01.png", 512),
    ("hedge_high_pollard_02_v1_detoured.png", "Midground", "hedge_high_pollard_02.png", 512),
    ("hedge_high_no_tree_v1_detoured.png", "Midground", "hedge_high_no_tree.png", 512),

    # Foreground
    ("pollard_ash_main_v1_detoured.png", "Foreground", "pollard_ash_main.png", 768),
    ("pond_v1_detoured.png", "Foreground", "pond.png", 768),
    ("grass_border_v1_detoured.png", "Foreground", "grass_border.png", 768),

    # Fauna (swallow already integrated, skip)
    ("bird_owl_flight_v1_detoured.png", "Fauna", "owl.png", 256),
    ("bird_harrier_flight_v1_detoured.png", "Fauna", "harrier.png", 256),
    ("heron_static_v1_detoured.png", "Fauna", "heron.png", 256),

    # Sensors. eddy_covariance_tower kept despite known rose-magenta halos
    # in its detour — DA decision 2026-05-12 (accept-and-revisit for MVP).
    ("weather_station_v1_detoured.png", "Sensors", "weather_station.png", 256),
    ("piezometer_v1_detoured.png", "Sensors", "piezometer.png", 256),
    ("acoustic_sensor_v1_detoured.png", "Sensors", "acoustic_sensor.png", 256),
    ("photo_trap_v1_detoured.png", "Sensors", "photo_trap.png", 256),
    ("eddy_covariance_tower_v1_detoured.png", "Sensors", "eddy_covariance_tower.png", 256),
]

ASSETS_ROOT = Path("Assets/_Project/05_Presentation/Scene/Sprites")
SOURCE_ROOT = Path("Sprites/Source")
PREVIEW_ROOT = Path("Sprites/Source/_test_quantization_v1")


def build_side_by_side(source: Path, quantized: Path, out: Path) -> None:
    """Write a [source-downscaled | quantized] comparison PNG matching
    the quantized output's height, so both halves are at the same
    display scale."""
    src_img = Image.open(source).convert("RGBA")
    q_img = Image.open(quantized).convert("RGBA")

    # Scale source to match quantized height for fair visual comparison.
    target_h = q_img.height
    scale = target_h / src_img.height
    target_w = max(1, int(round(src_img.width * scale)))
    src_resized = src_img.resize((target_w, target_h), Image.LANCZOS)

    gap = 16
    canvas_w = src_resized.width + gap + q_img.width
    canvas = Image.new("RGBA", (canvas_w, target_h), (40, 40, 40, 255))
    canvas.paste(src_resized, (0, 0), src_resized)
    canvas.paste(q_img, (src_resized.width + gap, 0), q_img)
    canvas.save(out, "PNG", optimize=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print what would happen without running postprocess or writing files.",
    )
    parser.add_argument(
        "--no-previews",
        action="store_true",
        help="Skip side-by-side preview generation.",
    )
    args = parser.parse_args()

    if not args.dry_run:
        PREVIEW_ROOT.mkdir(parents=True, exist_ok=True)

    failures: list[str] = []
    summary: list[tuple[str, str, str]] = []  # (source, dest, status)

    for source_name, subdir, output_name, max_size in SPRITES:
        src = SOURCE_ROOT / source_name
        dst = ASSETS_ROOT / subdir / output_name
        preview = PREVIEW_ROOT / f"{output_name.replace('.png', '')}_SIDE_BY_SIDE.png"

        if not src.is_file():
            print(f"[bulk_quantize] SKIP missing source: {src}")
            failures.append(source_name)
            summary.append((source_name, str(dst), "MISSING"))
            continue

        if args.dry_run:
            print(f"[dry-run] {src} -> {dst} (max {max_size}px)")
            summary.append((source_name, str(dst), "dry-run"))
            continue

        dst.parent.mkdir(parents=True, exist_ok=True)
        cmd = [
            sys.executable,
            "tools/postprocess.py",
            str(src),
            str(dst),
            "--max-size",
            str(max_size),
        ]
        print(f"[bulk_quantize] {source_name} -> {subdir}/{output_name} (max {max_size}px)")
        result = subprocess.run(cmd, capture_output=True, text=True)
        if result.returncode != 0:
            print(f"  FAILED:\n{result.stderr}")
            failures.append(source_name)
            summary.append((source_name, str(dst), "FAILED"))
            continue

        if not args.no_previews:
            try:
                build_side_by_side(src, dst, preview)
            except Exception as exc:  # pragma: no cover - best-effort preview
                print(f"  preview generation failed: {exc}")

        summary.append((source_name, str(dst), "OK"))

    # Final summary table
    print("\n=== Summary ===")
    for source_name, dst, status in summary:
        marker = "OK " if status == "OK" else f"{status:<8}"
        print(f"  {marker}  {source_name:<45} -> {dst}")

    if failures:
        print(f"\n[bulk_quantize] {len(failures)} failure(s).")
        return 1
    print(f"\n[bulk_quantize] {len(SPRITES)} sprites processed cleanly.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
