"""Produce pixel-registered warm-gold hover variants from approved source PNGs.

No ImageGen rerender and no geometry changes: transparent external glow plus
a restrained gold rim sheen are composited over *the exact original base*.
Does not modify any existing user artwork.
"""
from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parents[1] / "Art"
ITEMS = [
    ("Minigame_Tent_Base.png", "Minigame_Tent_Hover_Registered_v05.png", .82),
    ("Options_Workbench_Base.png", "Options_Workbench_Hover_Registered_v05.png", .65),
]


def render(input_name: str, output_name: str, strength: float):
    raw = np.array(Image.open(ART / input_name).convert("RGBA"))
    alpha = raw[:, :, 3]
    transparent = (alpha < 75).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(transparent, 8)
    exterior = (labels == labels[0, 0]).astype(np.uint8)

    dist = cv2.distanceTransform(exterior, cv2.DIST_L2, 5)
    inside = cv2.distanceTransform(1 - exterior, cv2.DIST_L2, 5)

    # Purely outward glow: no new decorations, shadows, or misregistered edges.
    exterior_glow = np.where(
        exterior > 0,
        strength * (184 * np.exp(-dist / 10) +
                    76 * np.exp(-dist / 27)),
        0,
    )
    glow = np.zeros_like(raw)
    glow[:, :, :3] = (255, 188, 66)
    glow[:, :, 3] = np.clip(exterior_glow, 0, 220).astype(np.uint8)

    baked = np.array(Image.alpha_composite(
        Image.fromarray(glow, "RGBA"),
        Image.fromarray(raw, "RGBA"),
    ))

    # Small highlight ON the object at the outside silhouette. Unlike a
    # generated hover replacement, the fabric, chest, stakes, ropes, etc.
    # are from the exact same normal image.
    gold_edge = np.clip(
        strength * .46 * np.exp(-inside / 5.8), 0, .5
    ).astype(np.float32)
    gold_edge[exterior > 0] = 0
    source = baked[:, :, :3].astype(np.float32)
    gold = np.array([255, 205, 107], dtype=np.float32)
    baked[:, :, :3] = np.clip(
        source * (1 - gold_edge[:, :, None])
        + gold[None, None, :] * gold_edge[:, :, None],
        0, 255,
    ).astype(np.uint8)

    assert baked.shape == raw.shape
    assert not np.any(baked[:, :, 3] < raw[:, :, 3])
    out = ART / output_name
    Image.fromarray(baked, "RGBA").save(out, optimize=True)
    print(input_name, "=>", output_name, "shape", raw.shape)


if __name__ == "__main__":
    for item in ITEMS:
        render(*item)
