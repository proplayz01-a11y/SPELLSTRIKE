"""Keep the approved Minigame hover's illuminated fabric, but remove ghost art.

V05 used a homography to transfer an AI-rendered reference's external pixels.
Even though the underlying tent geometry was locked, faint alternate ropes /
stakes remained visible inside the golden halo on zoom. Here the outside is
ONLY a smooth amber-gold color glow generated from the original tent's alpha;
reference lighting INSIDE the tent still comes directly from approved V05.
Never edits V05 or the Downloads originals.
"""
from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1] / "Art"
ORIGINAL = ROOT / "Minigame_Tent_Base.png"
GLOW_REFERENCE = ROOT / "Minigame_Tent_Hover_UserApproved_V05.png"
DESTINATION = ROOT / "Minigame_Tent_Hover_CleanHalo_V06.png"

base = np.array(Image.open(ORIGINAL).convert("RGBA"))
lit = np.array(Image.open(GLOW_REFERENCE).convert("RGBA"))
assert base.shape == lit.shape
alpha = base[:, :, 3]

outside = np.uint8(alpha < 80)
count, components, stats, _ = cv2.connectedComponentsWithStats(outside, 8)
exterior = components == components[0, 0]
distance = cv2.distanceTransform(np.uint8(exterior), cv2.DIST_L2, 5)

# Soft broad (28px) amber halo plus an intense, tight 6px gold rim.
# No alternate geometry is transferred from reference pixels.
opacity = 155 * np.exp(-distance / 18.0) + 92 * np.exp(-distance / 5.2)
opacity[~exterior] = 0
opacity[distance > 56] = 0
halo = np.zeros_like(base)
halo[:, :, :3] = (255, 185, 58)
halo[:, :, 3] = np.clip(opacity, 0, 215).astype(np.uint8)

# Same source image on every lit surface. Preserves V05's reference lighting
# but restores original anti-alias alpha and discards the reference's pixels
# anywhere outside the base object.
final = np.array(Image.alpha_composite(
    Image.fromarray(halo, "RGBA"), Image.fromarray(base, "RGBA")))
visible = alpha >= 80
final[visible, :3] = lit[visible, :3]
final[visible, 3] = alpha[visible]
final[final[:, :, 3] == 0, :3] = 0  # avoid RGB bleed in simple PNG viewers

assert np.array_equal(final[visible, 3], alpha[visible])
assert final.shape == base.shape
Image.fromarray(final, "RGBA").save(DESTINATION, optimize=True)
print(f"{DESTINATION}, resolution {base.shape[1]}x{base.shape[0]}; "
      f"no reference geometry transferred outside original silhouette")
