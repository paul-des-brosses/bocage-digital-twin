"""
Generate the Unity ScriptableObject assets for the E4 fauna pool, plus
the matching .meta files for both the new C# scripts (so Unity uses our
deterministic GUIDs instead of generating its own on first import) and
the SO assets themselves.

Why programmatic: 4 SO assets × many fields each (trajectories with
Vector2 endpoints, sub-sprite references via internalID + guid, etc.)
is too verbose to author by hand in the Inspector reliably. This
script writes everything deterministically; the user can still tweak
values in Inspector afterwards (the .asset YAML round-trips cleanly).

Idempotent: deterministic GUIDs from hashing, so re-runs don't break
references downstream.

Run from project root:
    python tools/build_fauna_so_assets.py
"""

from __future__ import annotations

import sys
from pathlib import Path

# Reuse hashing helpers from the sprite-sheet builder so GUIDs and
# sub-sprite internalIDs are consistent across the toolchain.
sys.path.insert(0, str(Path(__file__).resolve().parent))
from build_animation_sheet import (
    _hash_hex32,
    family_guid as sheet_family_guid,
    sub_sprite_internal_id,
)


# ---------------------------------------------------------------------------
# Deterministic GUIDs for new artefacts
# ---------------------------------------------------------------------------

SCRIPT_GUIDS = {
    "TrajectoryDefinition": _hash_hex32("bocage_fauna_script_TrajectoryDefinition_v1"),
    "FaunaSpeciesDefinition": _hash_hex32("bocage_fauna_script_FaunaSpeciesDefinition_v1"),
    "FaunaPlacementDefinition": _hash_hex32("bocage_fauna_script_FaunaPlacementDefinition_v1"),
}

ASSET_GUIDS = {
    "FaunaSpecies_Swallow": _hash_hex32("bocage_fauna_asset_Species_Swallow_v1"),
    "FaunaSpecies_Owl": _hash_hex32("bocage_fauna_asset_Species_Owl_v1"),
    "FaunaSpecies_Buzzard": _hash_hex32("bocage_fauna_asset_Species_Buzzard_v1"),
    "FaunaPlacement": _hash_hex32("bocage_fauna_asset_Placement_v1"),
}


# ---------------------------------------------------------------------------
# Species defaults (calibrated for ~1.55 birds-on-screen avg at biodiv = 1,
# with λ_max already scaled ×0.6 vs the indicative values discussed in chat)
# ---------------------------------------------------------------------------

SPECIES = [
    {
        "asset_name": "FaunaSpecies_Swallow",
        "id": "swallow",
        "sheet_family": "swallow",
        "frame_count": 3,
        "fps": 8.0,
        "threshold": 0.30,
        "lambda_max": 0.108,  # 0.18 × 0.6 per-trajectory
        "default_faces_right": True,  # wave-2 prompt: "vue profil pur orientée droite"
        "sorting_layer": "Fauna",
        "sorting_order": 5,
        "trajectories": [
            # Path 1: high + fast L↔R
            {"left": (-7.0, 3.5), "right": (7.0, 3.5),
             "duration": 4.0, "bob_amp": 0.15, "bob_freq": 0.50},
            # Path 2: low + fast L↔R
            {"left": (-7.0, 1.8), "right": (7.0, 1.8),
             "duration": 4.5, "bob_amp": 0.20, "bob_freq": 0.40},
        ],
    },
    {
        "asset_name": "FaunaSpecies_Owl",
        "id": "owl",
        "sheet_family": "owl",
        "frame_count": 3,
        "fps": 6.0,
        "threshold": 0.40,
        "lambda_max": 0.042,  # 0.07 × 0.6
        "default_faces_right": True,  # 3/4 face-on view; user-confirmed visually OK 2026-05-30
        "sorting_layer": "Fauna",
        "sorting_order": 5,
        "trajectories": [
            {"left": (-7.0, 2.5), "right": (7.0, 2.5),
             "duration": 6.0, "bob_amp": 0.18, "bob_freq": 0.35},
        ],
    },
    {
        "asset_name": "FaunaSpecies_Buzzard",
        "id": "buzzard",
        "sheet_family": "buzzard",
        "frame_count": 3,
        "fps": 2.0,
        "threshold": 0.50,
        "lambda_max": 0.036,  # 0.06 × 0.6 (rare planar species)
        "default_faces_right": False,  # wave-2 buzzard source draws the bird facing left (user-confirmed 2026-05-30 "vole en marche arrière" → flag flipped)
        "sorting_layer": "Fauna",
        "sorting_order": 5,
        "trajectories": [
            {"left": (-8.0, 4.5), "right": (8.0, 4.5),
             "duration": 9.0, "bob_amp": 0.10, "bob_freq": 0.25},
        ],
    },
]


# ---------------------------------------------------------------------------
# .meta writers
# ---------------------------------------------------------------------------

def write_script_meta(script_path: Path, guid: str) -> None:
    """Minimal Unity .cs.meta — fileFormatVersion + guid only."""
    content = f"fileFormatVersion: 2\nguid: {guid}\n"
    meta_path = Path(str(script_path) + ".meta")
    meta_path.write_bytes(content.encode("utf-8"))
    print(f"[build_fauna_so] wrote {meta_path.name}")


def write_asset_meta(asset_path: Path, guid: str) -> None:
    """NativeFormatImporter .meta for a ScriptableObject .asset."""
    content = (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )
    meta_path = Path(str(asset_path) + ".meta")
    meta_path.write_bytes(content.encode("utf-8"))
    print(f"[build_fauna_so] wrote {meta_path.name}")


# ---------------------------------------------------------------------------
# Asset YAML writers
# ---------------------------------------------------------------------------

ASSET_HEADER = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\n"


def asset_preamble(asset_name: str, script_guid: str, full_class: str) -> str:
    return (
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}\n"
        f"  m_Name: {asset_name}\n"
        f"  m_EditorClassIdentifier: Bocage.Presentation::{full_class}\n"
    )


def write_species_asset(species: dict, output_dir: Path) -> Path:
    """Write FaunaSpecies_<Name>.asset for one species."""
    asset_name = species["asset_name"]
    sheet_guid = sheet_family_guid(species["sheet_family"])

    lines = [
        ASSET_HEADER,
        asset_preamble(
            asset_name,
            SCRIPT_GUIDS["FaunaSpeciesDefinition"],
            "Bocage.Presentation.Scene.Fauna.FaunaSpeciesDefinition",
        ),
        f"  id: {species['id']}\n",
        "  frames:\n",
    ]
    for i in range(species["frame_count"]):
        sub_id = sub_sprite_internal_id(species["sheet_family"], i)
        lines.append(f"  - {{fileID: {sub_id}, guid: {sheet_guid}, type: 3}}\n")

    lines.extend([
        f"  framesPerSecond: {species['fps']}\n",
        f"  appearanceThreshold: {species['threshold']}\n",
        f"  spawnRateAtMaxBiodiv: {species['lambda_max']}\n",
        f"  defaultFacesRight: {1 if species['default_faces_right'] else 0}\n",
        f"  sortingLayerName: {species['sorting_layer']}\n",
        f"  sortingOrderInLayer: {species['sorting_order']}\n",
        "  trajectories:\n",
    ])
    for t in species["trajectories"]:
        lines.extend([
            f"  - leftPoint: {{x: {t['left'][0]}, y: {t['left'][1]}}}\n",
            f"    rightPoint: {{x: {t['right'][0]}, y: {t['right'][1]}}}\n",
            f"    durationSec: {t['duration']}\n",
            f"    verticalBobAmplitude: {t['bob_amp']}\n",
            f"    verticalBobFrequencyHz: {t['bob_freq']}\n",
        ])

    asset_path = output_dir / f"{asset_name}.asset"
    asset_path.write_bytes("".join(lines).encode("utf-8"))
    print(f"[build_fauna_so] wrote {asset_path.name}")
    return asset_path


def write_placement_asset(output_dir: Path) -> Path:
    """Write FaunaPlacement.asset — root SO listing the 3 species."""
    lines = [
        ASSET_HEADER,
        asset_preamble(
            "FaunaPlacement",
            SCRIPT_GUIDS["FaunaPlacementDefinition"],
            "Bocage.Presentation.Scene.Fauna.FaunaPlacementDefinition",
        ),
        "  species:\n",
    ]
    for sp in SPECIES:
        guid = ASSET_GUIDS[sp["asset_name"]]
        lines.append(f"  - {{fileID: 11400000, guid: {guid}, type: 2}}\n")

    asset_path = output_dir / "FaunaPlacement.asset"
    asset_path.write_bytes("".join(lines).encode("utf-8"))
    print(f"[build_fauna_so] wrote {asset_path.name}")
    return asset_path


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    project_root = Path(__file__).resolve().parent.parent

    # 1) Write .meta files for the 3 new .cs scripts so Unity uses our GUIDs.
    scripts_dir = project_root / "Assets" / "_Project" / "05_Presentation" / "Scene" / "Fauna"
    for cs_name, guid in SCRIPT_GUIDS.items():
        script_path = scripts_dir / f"{cs_name}.cs"
        if not script_path.is_file():
            print(f"[build_fauna_so] WARNING: script not found: {script_path}", file=sys.stderr)
        write_script_meta(script_path, guid)

    # 2) Write the 4 SO .asset files + matching .meta files.
    so_dir = project_root / "Assets" / "_Project" / "Data" / "Fauna"
    so_dir.mkdir(parents=True, exist_ok=True)

    for sp in SPECIES:
        asset_path = write_species_asset(sp, so_dir)
        write_asset_meta(asset_path, ASSET_GUIDS[sp["asset_name"]])

    placement_path = write_placement_asset(so_dir)
    write_asset_meta(placement_path, ASSET_GUIDS["FaunaPlacement"])

    print("[build_fauna_so] OK — 3 script metas + 4 asset+meta pairs.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
