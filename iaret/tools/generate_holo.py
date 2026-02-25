"""Generate holographic wireframe overlays from Iaret avatar images.

Takes portrait and full-body PNGs and produces cyan-tinted edge-detected
wireframe overlays on transparent backgrounds for the Thinking state effect.
"""

from pathlib import Path
from PIL import Image, ImageFilter, ImageOps, ImageEnhance
import numpy as np

# Resolve asset directory relative to this script's location
SCRIPT_DIR = Path(__file__).resolve().parent
ASSET_DIR = SCRIPT_DIR.parent / "assets" / "avatar"
HOLO_DIR = SCRIPT_DIR.parent / "assets" / "holo"

# Source -> Holographic output mapping
MAPPINGS = {
    "idle.png": "holo_portrait.png",         # Portrait bust wireframe
    "fullbody_front.png": "holo_front.png",
    "fullbody_threequarter.png": "holo_threequarter.png",
    "fullbody_side.png": "holo_side.png",
    "fullbody_back.png": "holo_back.png",
    "fullbody_sideleft.png": "holo_sideleft.png",
}

# Holographic cyan color
HOLO_CYAN = (0, 255, 220)
HOLO_VIOLET = (147, 51, 234)


def create_wireframe(src_path: Path, dst_path: Path):
    """Create a holographic wireframe overlay from an avatar image."""
    img = Image.open(src_path).convert("RGBA")

    # Convert to grayscale for edge detection
    gray = img.convert("L")

    # Multi-scale edge detection for rich wireframe
    edges_fine = gray.filter(ImageFilter.Kernel(
        size=(3, 3),
        kernel=[-1, -1, -1, -1, 8, -1, -1, -1, -1],
        scale=1, offset=0
    ))
    edges_medium = gray.filter(ImageFilter.FIND_EDGES)
    edges_coarse = gray.filter(ImageFilter.Kernel(
        size=(5, 5),
        kernel=[
            -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1,
            -1, -1, 24, -1, -1,
            -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1,
        ],
        scale=1, offset=0
    ))

    # Combine edge layers
    arr_fine = np.array(edges_fine, dtype=np.float32)
    arr_medium = np.array(edges_medium, dtype=np.float32)
    arr_coarse = np.array(edges_coarse, dtype=np.float32)

    combined = np.clip(arr_fine * 0.5 + arr_medium * 0.3 + arr_coarse * 0.2, 0, 255)

    # Boost contrast on edges
    combined = np.clip(combined * 2.5, 0, 255).astype(np.uint8)

    # Threshold to clean up noise
    combined[combined < 30] = 0

    # Create RGBA output with cyan wireframe
    w, h = img.size
    result = np.zeros((h, w, 4), dtype=np.uint8)

    # Main wireframe in cyan
    edge_mask = combined > 0
    result[edge_mask, 0] = HOLO_CYAN[0]  # R
    result[edge_mask, 1] = HOLO_CYAN[1]  # G
    result[edge_mask, 2] = HOLO_CYAN[2]  # B
    result[edge_mask, 3] = np.clip(combined[edge_mask] * 0.9, 0, 255).astype(np.uint8)  # A

    # Add subtle violet secondary edges (offset slightly for depth)
    edges_offset = gray.filter(ImageFilter.CONTOUR)
    arr_offset = np.array(edges_offset, dtype=np.float32)
    arr_offset = np.clip(arr_offset * 1.5, 0, 255).astype(np.uint8)
    offset_mask = (arr_offset > 40) & ~edge_mask  # Only where cyan isn't
    result[offset_mask, 0] = HOLO_VIOLET[0]
    result[offset_mask, 1] = HOLO_VIOLET[1]
    result[offset_mask, 2] = HOLO_VIOLET[2]
    result[offset_mask, 3] = np.clip(arr_offset[offset_mask] * 0.4, 0, 180).astype(np.uint8)

    # Add very faint fill from original image (ghostly presence)
    orig_arr = np.array(img)
    # Use luminance of original as very faint cyan fill
    orig_gray = np.array(gray, dtype=np.float32)
    faint_mask = (orig_gray > 30) & ~edge_mask & ~offset_mask
    result[faint_mask, 0] = 0
    result[faint_mask, 1] = np.clip(orig_gray[faint_mask] * 0.25, 0, 80).astype(np.uint8)
    result[faint_mask, 2] = np.clip(orig_gray[faint_mask] * 0.22, 0, 70).astype(np.uint8)
    result[faint_mask, 3] = np.clip(orig_gray[faint_mask] * 0.12, 0, 40).astype(np.uint8)

    out_img = Image.fromarray(result, "RGBA")

    # Slight glow effect: composite a blurred version underneath
    glow = out_img.filter(ImageFilter.GaussianBlur(radius=2))
    glow_arr = np.array(glow)
    glow_arr[:, :, 3] = np.clip(glow_arr[:, :, 3].astype(np.float32) * 0.5, 0, 128).astype(np.uint8)
    glow_img = Image.fromarray(glow_arr, "RGBA")

    final = Image.alpha_composite(glow_img, out_img)
    final.save(dst_path, "PNG")
    print(f"  ✓ {dst_path.name} ({final.size[0]}x{final.size[1]})")


def main():
    # Ensure output directory exists
    HOLO_DIR.mkdir(parents=True, exist_ok=True)

    print(f"Source: {ASSET_DIR}")
    print(f"Output: {HOLO_DIR}")
    print("Generating holographic wireframe overlays...")

    for src_name, dst_name in MAPPINGS.items():
        src = ASSET_DIR / src_name
        dst = HOLO_DIR / dst_name
        if not src.exists():
            print(f"  ✗ Source not found: {src_name}")
            continue
        create_wireframe(src, dst)

    print("\nDone! Holographic wireframe overlays generated.")


if __name__ == "__main__":
    main()
