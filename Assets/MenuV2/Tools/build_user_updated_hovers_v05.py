"""Create geometry-locked Main Menu hover sprites from user's UPDATED ASSETS.

Retain exactly the normal object's interior structure; apply the warm/strong
lighting and halo from the user's chosen hover references. Never write to
Downloads, the approved normal sprites, or any other game's art.

The resulting sprites are one-for-one replacements of the approved normal
images, on the exact same canvas. The glow extends ONLY outside the normal
silhouette, with Adventure's hollow opening explicitly protected.
"""
from pathlib import Path
import os
import cv2
import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parents[1] / "Art"
UPDATED = Path.home() / "Downloads" / "MAIN MENU ASSETS" / "UPDATED ASSETS"
TARGETS = [
    ("Adventure_Arch_Base.png", "Enchanted Ivy Portal Archway.png",
     "Adventure_Arch_Hover_UserApproved_V05.png", True),
    ("Minigame_Tent_Base.png", "Glowing Purple Royal Tent.png",
     "Minigame_Tent_Hover_UserApproved_V05.png", False),
    ("Options_Workbench_Base.png", "SPELLSTRIKE_Options_Hover_PixelAligned.png",
     "Options_Workbench_Hover_UserApproved_V05.png", False),
]


def align(hov: np.ndarray, base: np.ndarray):
    maker = cv2.SIFT_create(nfeatures=6500)
    b, bd = maker.detectAndCompute(
        cv2.cvtColor(base[:, :, :3], cv2.COLOR_RGB2GRAY),
        np.uint8(base[:, :, 3] > 210))
    h, hd = maker.detectAndCompute(
        cv2.cvtColor(hov[:, :, :3], cv2.COLOR_RGB2GRAY),
        np.uint8(hov[:, :, 3] > 210))
    matches = cv2.BFMatcher().knnMatch(hd, bd, k=2)
    good = [m for m, other in matches if m.distance < .68 * other.distance]
    src = np.float32([h[m.queryIdx].pt for m in good])
    dst = np.float32([b[m.trainIdx].pt for m in good])
    mat, inliers = cv2.findHomography(src, dst, cv2.RANSAC, 3.4)
    if mat is None or int(inliers.sum()) < 110:
        raise RuntimeError("Not enough matched pixels to safely register hover")
    return cv2.warpPerspective(
        hov, mat, (base.shape[1], base.shape[0]),
        flags=cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT), mat, int(inliers.sum())


def create(base_file, hover_file, result_file, protect_portal):
    base = np.array(Image.open(ART / base_file).convert("RGBA"))
    hover = np.array(Image.open(UPDATED / hover_file).convert("RGBA"))
    registered, mat, inliers = align(hover, base)
    alpha = base[:, :, 3]

    # A connected exterior mask makes the portal opening independent of the
    # outside: Adventure's original blue inner stone lighting stays untouched.
    components, labels, stats, centers = cv2.connectedComponentsWithStats(
        np.uint8(alpha < 80), 8)
    exterior = (labels == labels[0, 0])
    hole = np.zeros_like(exterior)
    if protect_portal:
        portal = [
            c for c in range(1, components)
            if c != labels[0, 0] and stats[c, cv2.CC_STAT_AREA] > 100000
            and .27 * base.shape[1] < centers[c, 0] < .75 * base.shape[1]
        ]
        if len(portal) != 1:
            raise RuntimeError("Cannot identify protected Adventure vortex opening")
        hole = labels == portal[0]

    # Preserve the FULL halo intensity and RGB from the reference.
    # Restrict new semitransparent pixels to the EXTERIOR, never the portal void.
    halo = registered.copy()
    halo[:, :, 3][~exterior] = 0
    halo[:, :, 3][hole] = 0
    result = np.array(Image.alpha_composite(
        Image.fromarray(halo, "RGBA"), Image.fromarray(base, "RGBA")))

    # The original pixels form all visible object geometry, so silhouette,
    # object placement, ropes, banners, scrolls and trim cannot shift.
    # Transfer only the reference's positive *light difference* (not edges).
    bw = base[:, :, :3].astype(np.float32)
    rw = registered[:, :, :3].astype(np.float32)
    base_lum = cv2.cvtColor(base[:, :, :3], cv2.COLOR_RGB2GRAY).astype(np.float32)
    ref_lum = cv2.cvtColor(registered[:, :, :3], cv2.COLOR_RGB2GRAY).astype(np.float32)
    illumination = np.maximum(
        0, cv2.GaussianBlur(ref_lum - base_lum, (0, 0), 2.2))
    # For a truly matching reference (Options), preserve its light intensity.
    # Brightened gold trims & lit interiors, with source details fixed.
    illumination *= .82
    reference_mask = cv2.GaussianBlur(
        (registered[:, :, 3] > 165).astype(np.float32), (0, 0), .8)
    illumination *= reference_mask
    object_mask = alpha > 115
    # Do not alter the cool-blue inner-stone pixels on Adventure:
    # the gold brightness must stay exclusive to its exterior face.
    if protect_portal:
        opening = cv2.dilate(
            np.uint8(hole), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (61, 61)))
        illumination[opening > 0] = 0
    luminosity = np.minimum(illumination, 94.)
    warm_rgb = np.stack([
        luminosity * 1.00,
        luminosity * .68,
        luminosity * .18
    ], axis=-1)
    brightened = np.minimum(255, bw + warm_rgb)
    obj = object_mask & ~hole
    result[obj, :3] = brightened[obj].astype(np.uint8)
    result[hole] = base[hole]

    assert result.shape == base.shape
    assert np.all(result[:, :, 3][alpha > 180] == alpha[alpha > 180])
    assert np.all(result[hole] == base[hole])

    outfile = ART / result_file
    Image.fromarray(result, "RGBA").save(outfile, optimize=True)
    print(f"{result_file}: {base.shape[1]}x{base.shape[0]}, "
          f"{inliers} matching visual anchors; H=(tx={mat[0,2]:.2f},"
          f"ty={mat[1,2]:.2f},sx={mat[0,0]:.4f},sy={mat[1,1]:.4f}); "
          f"protected portal pixels={int(hole.sum())}")


if __name__ == "__main__":
    for item in TARGETS:
        create(*item)
