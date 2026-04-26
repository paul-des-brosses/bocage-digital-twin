"""
Post-process a single sprite for use in the Bocage Digital Twin Unity
project. The user delivers a pre-detoured PNG (transparent background
already removed, even if imperfect) and this script:

1. Cleans up the alpha channel: snaps near-transparent and near-opaque
   pixels to 0 / 255, preserves anti-aliased gradients in between.
2. Quantizes every visible pixel's RGB to the nearest colour in the
   project palette (palette_perche.json) so all sprites share the same
   chromatic vocabulary regardless of which Nanobanana run produced them.
3. Crops to the bounding box of non-transparent content (drops empty
   margins).
4. Resizes so the longest side fits within --max-size, preserving
   aspect ratio. Defaults to 512 px.
5. Saves to the destination path.

Usage:
    python tools/postprocess.py Sprites/Source/bird_swallow_v1_detoured.png \\
        Assets/_Project/05_Presentation/Scene/Sprites/Fauna/swallow.png

    # Override max size for a small fauna sprite:
    python tools/postprocess.py input.png output.png --max-size 256
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def load_palette(path: Path) -> np.ndarray:
    with path.open("r", encoding="utf-8") as fh:
        payload = json.load(fh)
    hex_colors = payload["colors"]
    rgb = np.array(
        [(int(c[1:3], 16), int(c[3:5], 16), int(c[5:7], 16)) for c in hex_colors],
        dtype=np.float64,
    )
    return rgb


def alpha_cleanup(arr: np.ndarray, lower: int, upper: int) -> np.ndarray:
    """Snap alpha to 0 below `lower`, to 255 above `upper`. Keep the
    intermediate band as-is so anti-aliased edges stay smooth."""
    out = arr.copy()
    a = out[..., 3]
    a = np.where(a < lower, 0, a)
    a = np.where(a > upper, 255, a)
    out[..., 3] = a
    return out


def quantize_to_palette(arr: np.ndarray, palette: np.ndarray) -> np.ndarray:
    """Replace every pixel's RGB with the nearest palette colour. Pixels
    with alpha 0 are left untouched (irrelevant; only RGB matters where
    alpha > 0). Computed on the whole image — fast on 1k×1k images."""
    h, w = arr.shape[:2]
    pixels = arr[..., :3].reshape(-1, 3).astype(np.float64)

    diffs = pixels[:, None, :] - palette[None, :, :]
    dists = np.einsum("ijk,ijk->ij", diffs, diffs)
    labels = np.argmin(dists, axis=1)
    quantized = palette[labels].astype(np.uint8)

    out = arr.copy()
    out[..., :3] = quantized.reshape(h, w, 3)
    return out


def crop_to_alpha_bbox(img: Image.Image) -> Image.Image:
    bbox = img.split()[-1].getbbox()  # alpha channel bbox
    if bbox is None:
        return img
    return img.crop(bbox)


def fit_within(img: Image.Image, max_side: int) -> Image.Image:
    w, h = img.size
    longest = max(w, h)
    if longest <= max_side:
        return img
    scale = max_side / longest
    new_size = (max(1, int(round(w * scale))), max(1, int(round(h * scale))))
    return img.resize(new_size, Image.LANCZOS)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("source", type=Path, help="Pre-detoured PNG with alpha.")
    parser.add_argument("destination", type=Path, help="Output PNG path.")
    parser.add_argument("--palette", type=Path, default=Path("tools/palette_perche.json"))
    parser.add_argument("--max-size", type=int, default=512,
                        help="Longest side after resize, in pixels (default 512).")
    parser.add_argument("--alpha-lower", type=int, default=30,
                        help="Alpha below this snaps to 0 (default 30).")
    parser.add_argument("--alpha-upper", type=int, default=230,
                        help="Alpha above this snaps to 255 (default 230).")
    parser.add_argument("--no-quantize", action="store_true",
                        help="Skip palette quantization (debug).")
    args = parser.parse_args()

    if not args.source.is_file():
        print(f"Source not found: {args.source}", file=sys.stderr)
        return 1
    if not args.palette.is_file() and not args.no_quantize:
        print(f"Palette not found: {args.palette} (run extract_palette.py first)", file=sys.stderr)
        return 1

    print(f"[postprocess] Reading {args.source}")
    img = Image.open(args.source).convert("RGBA")
    arr = np.array(img, dtype=np.uint8)
    h0, w0 = arr.shape[:2]
    print(f"[postprocess] Source size {w0}x{h0}, mode RGBA")

    print(f"[postprocess] Alpha cleanup (snap < {args.alpha_lower} -> 0, > {args.alpha_upper} -> 255)")
    arr = alpha_cleanup(arr, args.alpha_lower, args.alpha_upper)

    if not args.no_quantize:
        palette = load_palette(args.palette)
        print(f"[postprocess] Quantizing to {len(palette)}-colour palette")
        arr = quantize_to_palette(arr, palette)

    img = Image.fromarray(arr, mode="RGBA")

    print("[postprocess] Cropping to alpha bbox")
    img = crop_to_alpha_bbox(img)
    print(f"[postprocess] After crop: {img.size[0]}x{img.size[1]}")

    print(f"[postprocess] Fitting within max side {args.max_size} px")
    img = fit_within(img, args.max_size)
    print(f"[postprocess] After resize: {img.size[0]}x{img.size[1]}")

    args.destination.parent.mkdir(parents=True, exist_ok=True)
    img.save(args.destination, "PNG", optimize=True)
    print(f"[postprocess] Wrote {args.destination}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
