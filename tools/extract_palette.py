"""
Extract a fixed palette of N dominant colours from one or more reference
images via k-means in RGB.

Supports two modes:

  Single source (used for early v0.1-provisional from the anchor image):
      python tools/extract_palette.py Sprites/Source/01_anchor_full_scene.png \\
          --output tools/palette_perche.json --colors 16

  Multi-source corpus (used to rebuild v1.0 from the complete sprite set
  once all detoured PNGs are available — see ASSETS_LIST.md §6):
      python tools/extract_palette.py \\
          Sprites/Source/bird_swallow_flight_v1_detoured.png \\
          Sprites/Source/hedge_low_01_v1_detoured.png \\
          ... \\
          --output tools/palette_perche.json --colors 32

  Or with a glob pattern (more practical for the full corpus):
      python tools/extract_palette.py --sources-glob \\
          "Sprites/Source/*_detoured.png" --colors 32 \\
          --exclude Sprites/Source/01_anchor_full_scene.png

To keep the k-means tractable when the corpus is 20+ sprites totalling
tens of millions of opaque pixels, the loader subsamples each source to
at most `--per-source-cap` opaque pixels (default 25 000). Equal
per-sprite sampling is intentional: it prevents the largest sprites
(2-3 MP hedges, hills_perche) from drowning out the chromatically rare
but functionally critical signatures of small sprites (piezometer
metallic ring, photo_trap lens charcoal, etc.).

K-means is implemented in pure numpy (no scikit-learn dependency).
"""

from __future__ import annotations

import argparse
import glob
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def load_opaque_rgb_pixels(
    image_path: Path,
    alpha_min: int = 200,
    per_source_cap: int | None = None,
    rng: np.random.Generator | None = None,
) -> np.ndarray:
    """Return an (N, 3) array of RGB pixels that are at least `alpha_min`
    opaque. Semi-transparent edge pixels are excluded so they do not pull
    the palette toward muddied averaged colours.

    If `per_source_cap` is set and the source has more opaque pixels than
    the cap, a uniform random subsample of size `per_source_cap` is
    returned. This is the mechanism that gives every source equal weight
    in the multi-source palette.
    """
    img = Image.open(image_path).convert("RGBA")
    arr = np.array(img, dtype=np.uint8)
    mask = arr[..., 3] >= alpha_min
    pixels = arr[mask][:, :3]
    if per_source_cap is not None and len(pixels) > per_source_cap:
        if rng is None:
            rng = np.random.default_rng(0)
        idx = rng.choice(len(pixels), size=per_source_cap, replace=False)
        pixels = pixels[idx]
    return pixels


def kmeans(pixels: np.ndarray, k: int, max_iter: int = 50, seed: int = 0) -> np.ndarray:
    """Plain k-means on pixel colours. Returns (k, 3) centroids."""
    rng = np.random.default_rng(seed)
    indices = rng.choice(len(pixels), size=k, replace=False)
    centroids = pixels[indices].astype(np.float64)

    for _ in range(max_iter):
        # Assign each pixel to its nearest centroid.
        diffs = pixels[:, None, :].astype(np.float64) - centroids[None, :, :]
        dists = np.einsum("ijk,ijk->ij", diffs, diffs)
        labels = np.argmin(dists, axis=1)

        new_centroids = np.empty_like(centroids)
        for i in range(k):
            members = pixels[labels == i]
            if len(members) == 0:
                new_centroids[i] = centroids[i]
            else:
                new_centroids[i] = members.mean(axis=0)

        if np.allclose(new_centroids, centroids, atol=0.5):
            centroids = new_centroids
            break
        centroids = new_centroids

    return centroids


def sort_by_population(pixels: np.ndarray, centroids: np.ndarray) -> np.ndarray:
    diffs = pixels[:, None, :].astype(np.float64) - centroids[None, :, :]
    dists = np.einsum("ijk,ijk->ij", diffs, diffs)
    labels = np.argmin(dists, axis=1)
    counts = np.bincount(labels, minlength=len(centroids))
    order = np.argsort(-counts)
    return centroids[order], counts[order]


def to_hex(rgb: np.ndarray) -> str:
    r, g, b = (int(round(c)) for c in rgb)
    r = max(0, min(255, r))
    g = max(0, min(255, g))
    b = max(0, min(255, b))
    return f"#{r:02X}{g:02X}{b:02X}"


def resolve_sources(args: argparse.Namespace) -> list[Path]:
    """Build the final ordered list of source images from positional
    arguments, --sources-glob, and --exclude. Deduplicated, sorted."""
    candidates: list[Path] = []
    for s in args.sources:
        candidates.append(Path(s))
    if args.sources_glob:
        for pattern in args.sources_glob:
            for match in glob.glob(pattern):
                candidates.append(Path(match))

    excluded = {Path(p).resolve() for p in (args.exclude or [])}
    final = []
    seen = set()
    for p in candidates:
        resolved = p.resolve()
        if resolved in excluded:
            continue
        if resolved in seen:
            continue
        seen.add(resolved)
        final.append(p)
    final.sort(key=lambda p: str(p))
    return final


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument(
        "sources",
        nargs="*",
        type=str,
        help="One or more reference PNGs. For multi-source mode, list "
        "all detoured sprites here or use --sources-glob.",
    )
    parser.add_argument(
        "--sources-glob",
        action="append",
        help="Glob pattern matching source PNGs. Can be repeated. "
        "Combined with any positional `sources` arguments.",
    )
    parser.add_argument(
        "--exclude",
        action="append",
        help="Path to exclude even if matched by --sources-glob. Useful "
        "to drop 01_anchor_full_scene.png (which carries UI colours).",
    )
    parser.add_argument("--output", type=Path, default=Path("tools/palette_perche.json"))
    parser.add_argument("--colors", type=int, default=32, help="Number of palette entries.")
    parser.add_argument(
        "--alpha-min",
        type=int,
        default=200,
        help="Pixels with alpha below this are excluded from sampling.",
    )
    parser.add_argument(
        "--per-source-cap",
        type=int,
        default=25_000,
        help="Cap on opaque pixels sampled per source. Equal cap across "
        "sources gives every sprite equal weight in the palette. "
        "Set to 0 to disable capping (uses all opaque pixels).",
    )
    parser.add_argument("--seed", type=int, default=0)
    parser.add_argument(
        "--version-tag",
        type=str,
        default="v1.0",
        help="Palette version label written into the output JSON.",
    )
    args = parser.parse_args()

    sources = resolve_sources(args)
    if not sources:
        print(
            "No source images. Pass positional paths or --sources-glob.",
            file=sys.stderr,
        )
        return 1
    for s in sources:
        if not s.is_file():
            print(f"Source not found: {s}", file=sys.stderr)
            return 1

    print(f"[extract_palette] {len(sources)} source image(s)")
    for s in sources:
        print(f"  - {s}")

    rng = np.random.default_rng(args.seed)
    cap = args.per_source_cap if args.per_source_cap > 0 else None

    all_pixels = []
    per_source_counts = []
    for s in sources:
        pixels = load_opaque_rgb_pixels(
            s, alpha_min=args.alpha_min, per_source_cap=cap, rng=rng
        )
        per_source_counts.append((s.name, len(pixels)))
        all_pixels.append(pixels)
    pixels = np.concatenate(all_pixels, axis=0)
    print(f"[extract_palette] Total sampled pixels: {len(pixels):,}")
    print(f"[extract_palette] Per-source contributions:")
    for name, n in per_source_counts:
        print(f"  - {name:<50} {n:>8,} px")

    print(
        f"[extract_palette] Running k-means with k={args.colors} "
        "(this can take 10-60 s depending on corpus size)"
    )
    centroids = kmeans(pixels, k=args.colors, seed=args.seed)
    centroids, counts = sort_by_population(pixels, centroids)

    palette = [to_hex(rgb) for rgb in centroids]
    populations = [int(c) for c in counts]

    payload = {
        "version": args.version_tag,
        "method": "k-means (RGB) on multi-source corpus"
        if len(sources) > 1
        else "k-means (RGB) on single source",
        "n_sources": len(sources),
        "sources": [str(s).replace("\\", "/") for s in sources],
        "n_sampled_pixels": int(len(pixels)),
        "per_source_cap": cap if cap is not None else 0,
        "k": args.colors,
        "seed": args.seed,
        "colors": palette,
        "populations": populations,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2)

    print(f"[extract_palette] Wrote {args.output}")
    print(f"[extract_palette] Palette ({len(palette)} colours, sorted by population):")
    for hex_color, pop in zip(palette, populations):
        print(f"  {hex_color}  ({pop:,} px)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
