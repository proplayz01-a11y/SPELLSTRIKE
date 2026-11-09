"""Recreates the approved stronger gold glow on the OUTSIDE of the Adventure arch.

The historic glow art is used as a visual reference, not as pixel geometry:
the hover frame is derived directly from the approved base sprite so it
cannot jump or replace any stones, banners, crystals, or steps.
The large portal opening is identified as an enclosed transparent component
and receives no gold illumination from this image.
"""

from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parents[1] / "Art"
SOURCE = ART / "Adventure_Arch_Base.png"
DESTINATION = ART / "Adventure_Arch_Hover_OuterGold_V04.png"


def main() -> None:
    base = np.array(Image.open(SOURCE).convert("RGBA"))
    alpha = base[:, :, 3]
    h, w = alpha.shape

    # Separate the transparent *outside* from the hollow portal opening.
    # These are different connected components, even though both are alpha=0.
    transparent = (alpha < 70).astype(np.uint8)
    count, components, stats, centroids = cv2.connectedComponentsWithStats(
        transparent, connectivity=8
    )
    outer_component = int(components[0, 0])
    outside = (components == outer_component).astype(np.uint8)

    # Confirm the intended hole is a substantial central transparent component.
    portals = [
        idx for idx in range(1, count)
        if idx != outer_component
        and stats[idx, cv2.CC_STAT_AREA] > 150000
        and 0.3 * w < centroids[idx, 0] < 0.7 * w
    ]
    if len(portals) != 1:
        raise RuntimeError(
            f"Expected one enclosed central portal opening, found {len(portals)}"
        )
    portal = (components == portals[0])

    # The outside distance measures only the outer silhouette, not the inner
    # portal contour. No golden halo can appear inside the vortex opening.
    outside_distance = cv2.distanceTransform(outside, cv2.DIST_L2, 5)
    inside_distance = cv2.distanceTransform(1 - outside, cv2.DIST_L2, 5)
    glow = np.zeros_like(base)
    glow[:, :, 0:3] = [255, 184, 42]

    exterior_alpha = np.where(
        outside == 1,
        173 * np.exp(-outside_distance / 11.5)
        + 75 * np.exp(-outside_distance / 32.0),
        0,
    )
    glow[:, :, 3] = np.clip(exterior_alpha, 0, 255).astype(np.uint8)
    # Underlying glow first; then retain the original sprite pixel-for-pixel.
    underlying = Image.alpha_composite(
        Image.fromarray(glow, "RGBA"), Image.fromarray(base, "RGBA")
    )
    out = np.array(underlying)

    # Bright gold edge light only 0-10 pixels inward from the EXTERIOR.
    # No highlight is added to the arch's inward-facing rim.
    edge_sheen = np.where(
        outside == 0,
        0.58 * np.exp(-inside_distance / 5.0),
        0,
    ).astype(np.float32)
    original_rgb = out[:, :, :3].astype(np.float32)
    gold = np.array([255, 206, 85], dtype=np.float32)
    out[:, :, :3] = np.clip(
        original_rgb * (1 - edge_sheen[:, :, None])
        + gold[None, None, :] * edge_sheen[:, :, None],
        0, 255,
    ).astype(np.uint8)

    # For a hover-only overlay, preserve source geometry outside the halo.
    # Enforce no gold added to central portal pixels whatsoever.
    out[portal] = base[portal]
    Image.fromarray(out, "RGBA").save(DESTINATION, optimize=True)

    print(f"Created {DESTINATION}; {w}x{h}; portal pixels preserved={np.count_nonzero(portal)}")


if __name__ == "__main__":
    main()
