"""
Extract a fixed palette of N dominant colours from a reference image.

Used once on the validated anchor image to produce palette_perche.json,
which is then consumed by postprocess.py to quantize every generated
sprite to the same palette — guaranteeing visual coherence between
sprites that come out of Nanobanana with subtly different hues.

K-means is implemented in pure numpy (no scikit-learn dependency) for a
small, portable footprint.

Usage:
    python tools/extract_palette.py Sprites/Source/01_anchor_full_scene.png \\
        --output tools/palette_perche.json --colors 16
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def load_opaque_rgb_pixels(image_path: Path, alpha_min: int = 200) -> np.ndarray:
    """Return an (N, 3) array of RGB pixels that are at least `alpha_min`
    opaque. Semi-transparent edge pixels are excluded so they do not pull
    the palette toward muddied averaged colours."""
    img = Image.open(image_path).convert("RGBA")
    arr = np.array(img, dtype=np.uint8)
    mask = arr[..., 3] >= alpha_min
    return arr[mask][:, :3]


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
    return centroids[order]


def to_hex(rgb: np.ndarray) -> str:
    r, g, b = (int(round(c)) for c in rgb)
    r = max(0, min(255, r))
    g = max(0, min(255, g))
    b = max(0, min(255, b))
    return f"#{r:02X}{g:02X}{b:02X}"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("source", type=Path, help="Reference PNG (anchor image).")
    parser.add_argument("--output", type=Path, default=Path("tools/palette_perche.json"))
    parser.add_argument("--colors", type=int, default=16, help="Number of palette entries.")
    parser.add_argument("--alpha-min", type=int, default=200,
                        help="Pixels with alpha below this are excluded from sampling.")
    parser.add_argument("--seed", type=int, default=0)
    args = parser.parse_args()

    if not args.source.is_file():
        print(f"Source not found: {args.source}", file=sys.stderr)
        return 1

    print(f"[extract_palette] Loading {args.source}")
    pixels = load_opaque_rgb_pixels(args.source, alpha_min=args.alpha_min)
    print(f"[extract_palette] {len(pixels)} opaque pixels sampled.")

    print(f"[extract_palette] Running k-means with k={args.colors} (this can take ~10-30 s)")
    centroids = kmeans(pixels, k=args.colors, seed=args.seed)
    centroids = sort_by_population(pixels, centroids)

    palette = [to_hex(rgb) for rgb in centroids]

    payload = {
        "source": str(args.source).replace("\\", "/"),
        "colors": palette,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2)

    print(f"[extract_palette] Wrote {args.output}")
    print(f"[extract_palette] Palette ({len(palette)} colours):")
    for hex_color in palette:
        print(f"  {hex_color}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
