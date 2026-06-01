"""
Build a sliced sprite sheet for an animated fauna family.

Takes N source frame PNGs (pre-detoured, RGBA), aligns them via cross-frame
alpha-bbox centering (so the subject stays the same size and position across
frames), runs chroma cleanup + alpha cleanup + palette quantization, concats
horizontally into a single sprite sheet, resizes to a Unity-friendly size,
and writes the sheet PNG plus a matching .meta with Sprite Mode Multiple,
grid slicing, Bilinear filter, and deterministic GUIDs (stable across reruns).

Motivation: postprocess.py is single-image-in/out and crops each image to its
own bbox independently. For animation frames that produces inconsistent canvas
sizes and subject positions — the animation "jumps" between frames. This
script fixes it by computing a common bbox-derived target canvas for the whole
family.

Usage:
    python tools/build_animation_sheet.py swallow \\
        Assets/_Project/05_Presentation/Scene/Sprites/Fauna/swallow_sheet.png \\
        Sprites/Source/bird_swallow_flight_frame_02_detoured.png \\
        Sprites/Source/bird_swallow_flight_frame_03_detoured.png \\
        Sprites/Source/bird_swallow_flight_frame_04_detoured.png
"""

from __future__ import annotations

import argparse
import hashlib
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from postprocess import (
    load_palette,
    chroma_key_removal,
    alpha_cleanup,
    quantize_to_palette,
)


# ---------------------------------------------------------------------------
# Cross-frame alignment
# ---------------------------------------------------------------------------

def cleanup_and_bbox(arr: np.ndarray, alpha_lower: int, alpha_upper: int):
    arr, _ = chroma_key_removal(arr)
    arr = alpha_cleanup(arr, alpha_lower, alpha_upper)
    bbox = Image.fromarray(arr, mode="RGBA").split()[-1].getbbox()
    return arr, bbox


def center_in_canvas(arr: np.ndarray, bbox, target_w: int, target_h: int) -> Image.Image:
    img = Image.fromarray(arr, mode="RGBA")
    cropped = img.crop(bbox)
    canvas = Image.new("RGBA", (target_w, target_h), (0, 0, 0, 0))
    cw, ch = cropped.size
    canvas.paste(cropped, ((target_w - cw) // 2, (target_h - ch) // 2))
    return canvas


# ---------------------------------------------------------------------------
# Deterministic IDs (stable across re-runs so Unity references survive)
# ---------------------------------------------------------------------------

def _hash_hex32(*parts: str) -> str:
    return hashlib.sha256("|".join(parts).encode()).hexdigest()[:32]


def _hash_int64_signed(*parts: str) -> int:
    digest = hashlib.sha256("|".join(parts).encode()).digest()
    return int.from_bytes(digest[:8], byteorder="little", signed=True)


def family_guid(family: str) -> str:
    return _hash_hex32("fauna_sheet", family, "v1")


def sub_sprite_internal_id(family: str, index: int) -> int:
    return _hash_int64_signed("fauna_sheet", family, str(index), "internal_v1")


def sub_sprite_id_hex(family: str, index: int) -> str:
    return _hash_hex32("fauna_sheet", family, str(index), "spriteid_v1")


# ---------------------------------------------------------------------------
# .meta authoring (Unity TextureImporter YAML)
# ---------------------------------------------------------------------------

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
{id_to_name_entries}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{sprite_entries}    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
{name_file_id_entries}  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


SPRITE_ENTRY_TEMPLATE = """    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {w}
        height: {h}
      alignment: 0
      pivot: {{x: 0, y: 0}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id_hex}
      internalID: {internal_id}
      vertices: []
      indices:
      edges: []
      weights: []
"""


def build_meta(family: str, n_frames: int, sub_w: int, sub_h: int) -> str:
    guid = family_guid(family)
    id_to_name = []
    sprite_entries = []
    name_file_id = []
    for i in range(n_frames):
        name = f"{family}_{i}"
        internal_id = sub_sprite_internal_id(family, i)
        sprite_id = sub_sprite_id_hex(family, i)
        id_to_name.append(
            f"  - first:\n      213: {internal_id}\n    second: {name}"
        )
        sprite_entries.append(
            SPRITE_ENTRY_TEMPLATE.format(
                name=name,
                x=i * sub_w,
                y=0,
                w=sub_w,
                h=sub_h,
                sprite_id_hex=sprite_id,
                internal_id=internal_id,
            )
        )
        name_file_id.append(f"      {name}: {internal_id}")

    return META_TEMPLATE.format(
        guid=guid,
        id_to_name_entries="\n".join(id_to_name),
        sprite_entries="".join(sprite_entries),
        name_file_id_entries="\n".join(name_file_id) + "\n",
    )


# ---------------------------------------------------------------------------
# Main pipeline
# ---------------------------------------------------------------------------

def build_family_sheet(
    family: str,
    frames_paths,
    output_sheet: Path,
    palette_path: Path,
    max_subsprite_width: int,
    alpha_lower: int,
    alpha_upper: int,
    no_quantize: bool,
) -> dict:
    print(f"[build_animation_sheet] Family: {family}, {len(frames_paths)} frames")

    cleaned, bboxes = [], []
    for path in frames_paths:
        print(f"[build_animation_sheet]   Loading {path.name}")
        if not path.is_file():
            raise FileNotFoundError(f"Missing frame: {path}")
        arr = np.array(Image.open(path).convert("RGBA"), dtype=np.uint8)
        print(f"[build_animation_sheet]     Source {arr.shape[1]}x{arr.shape[0]}")
        arr, bbox = cleanup_and_bbox(arr, alpha_lower, alpha_upper)
        if bbox is None:
            raise ValueError(f"Frame {path.name} has no opaque content after cleanup")
        bw, bh = bbox[2] - bbox[0], bbox[3] - bbox[1]
        print(f"[build_animation_sheet]     Alpha bbox {bw}x{bh} at ({bbox[0]},{bbox[1]})")
        cleaned.append(arr)
        bboxes.append(bbox)

    max_bw = max(b[2] - b[0] for b in bboxes)
    max_bh = max(b[3] - b[1] for b in bboxes)
    margin = max(max_bw, max_bh) // 20
    target_w = max_bw + 2 * margin
    target_h = max_bh + 2 * margin
    print(f"[build_animation_sheet] Common canvas {target_w}x{target_h} (margin {margin})")

    aligned = [center_in_canvas(a, b, target_w, target_h) for a, b in zip(cleaned, bboxes)]

    if not no_quantize:
        palette = load_palette(palette_path)
        print(f"[build_animation_sheet] Quantizing to {len(palette)}-colour palette")
        quantized = []
        for canvas in aligned:
            arr = quantize_to_palette(np.array(canvas, dtype=np.uint8), palette)
            quantized.append(Image.fromarray(arr, mode="RGBA"))
    else:
        quantized = aligned

    sub_w = max_subsprite_width
    sub_h = max(1, int(round(target_h * (sub_w / target_w))))
    print(f"[build_animation_sheet] Sub-sprite {sub_w}x{sub_h}")

    resized = [img.resize((sub_w, sub_h), Image.LANCZOS) for img in quantized]

    n = len(resized)
    sheet_w = sub_w * n
    sheet_h = sub_h
    sheet = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))
    for i, frame in enumerate(resized):
        sheet.paste(frame, (i * sub_w, 0))

    output_sheet.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_sheet, "PNG", optimize=True)
    print(f"[build_animation_sheet] Wrote sheet: {output_sheet} ({sheet_w}x{sheet_h})")

    meta_path = output_sheet.with_suffix(output_sheet.suffix + ".meta")
    meta_path.write_bytes(build_meta(family, n, sub_w, sub_h).encode("utf-8"))
    print(f"[build_animation_sheet] Wrote meta:  {meta_path}")

    return {
        "family": family,
        "n_frames": n,
        "sheet_size": (sheet_w, sheet_h),
        "sub_size": (sub_w, sub_h),
        "sheet_path": str(output_sheet),
        "meta_path": str(meta_path),
        "guid": family_guid(family),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.strip().splitlines()[0])
    parser.add_argument("family", type=str, help="Family name (e.g. swallow, owl, buzzard).")
    parser.add_argument("output_sheet", type=Path, help="Output sprite sheet PNG path.")
    parser.add_argument("frames", nargs="+", type=Path,
                        help="Source frame PNG paths in display order (frame_01 first).")
    parser.add_argument("--palette", type=Path, default=Path("tools/palette_perche.json"))
    parser.add_argument("--max-subsprite-width", type=int, default=256)
    parser.add_argument("--alpha-lower", type=int, default=30)
    parser.add_argument("--alpha-upper", type=int, default=230)
    parser.add_argument("--no-quantize", action="store_true")
    args = parser.parse_args()

    if not args.palette.is_file() and not args.no_quantize:
        print(f"Palette not found: {args.palette}", file=sys.stderr)
        return 1

    try:
        result = build_family_sheet(
            family=args.family,
            frames_paths=list(args.frames),
            output_sheet=args.output_sheet,
            palette_path=args.palette,
            max_subsprite_width=args.max_subsprite_width,
            alpha_lower=args.alpha_lower,
            alpha_upper=args.alpha_upper,
            no_quantize=args.no_quantize,
        )
    except (FileNotFoundError, ValueError) as e:
        print(f"[build_animation_sheet] ERROR: {e}", file=sys.stderr)
        return 1

    print(
        f"[build_animation_sheet] OK — {result['family']}: {result['n_frames']} frames, "
        f"sheet {result['sheet_size'][0]}x{result['sheet_size'][1]}, "
        f"sub {result['sub_size'][0]}x{result['sub_size'][1]}, "
        f"guid {result['guid']}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
