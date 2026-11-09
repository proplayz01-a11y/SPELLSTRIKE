"""Transfer the GOLD from the user-approved historic arch hover onto base pixels.

This intentionally uses Adventure_Arch_Hover.png as the glow reference, rather
than a procedural approximation or AI redraw. Source stones / silhouette stay
identical to Adventure_Arch_Base.png. The inside portal hole remains transparent.
"""
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parents[1] / "Art"
BASE = ART / "Adventure_Arch_Base.png"
HISTORIC = ART / "Adventure_Arch_Hover.png"
OUTPUT = ART / "Adventure_Arch_Hover_ReferenceGold_v05.png"


def main():
    base = np.asarray(Image.open(BASE).convert("RGBA"))
    historic = np.asarray(Image.open(HISTORIC).convert("RGBA"))
    assert base.shape == historic.shape
    alpha = base[:, :, 3]

    # Exterior transparent region and separate enclosed portal void.
    n, labels, stats, centers = cv2.connectedComponentsWithStats(
        np.uint8(alpha < 72), connectivity=8)
    outside = labels == labels[0, 0]
    portal_candidates = [i for i in range(1, n)
                         if i != labels[0, 0]
                         and stats[i, cv2.CC_STAT_AREA] > 150000
                         and 0.3 * base.shape[1] < centers[i, 0] < .7 * base.shape[1]]
    assert len(portal_candidates) == 1
    inside_portal = labels == portal_candidates[0]

    # Align illumination reference to source architecture. We NEVER warp the
    # base sprite or swap the actual foreground object geometry.
    gray_b = cv2.cvtColor(base[:, :, :3], cv2.COLOR_RGB2GRAY)
    gray_h = cv2.cvtColor(historic[:, :, :3], cv2.COLOR_RGB2GRAY)
    sift = cv2.SIFT_create(nfeatures=6500)
    kb, db = sift.detectAndCompute(gray_b, np.uint8(alpha > 240))
    kh, dh = sift.detectAndCompute(gray_h, np.uint8(historic[:, :, 3] > 240))
    pairs = cv2.BFMatcher().knnMatch(dh, db, k=2)
    good = [first for first, second in pairs if first.distance < .66 * second.distance]
    if len(good) < 50:
        raise RuntimeError("Cannot confirm source/historic image registration.")
    a = np.float32([kh[m.queryIdx].pt for m in good])
    b = np.float32([kb[m.trainIdx].pt for m in good])
    matrix, inliers = cv2.findHomography(a, b, cv2.RANSAC, 3.5)
    if matrix is None or inliers.sum() < 60:
        raise RuntimeError("Reference registration confidence too low.")
    registered = cv2.warpPerspective(historic, matrix,
                                     (base.shape[1], base.shape[0]),
                                     flags=cv2.INTER_LINEAR,
                                     borderMode=cv2.BORDER_CONSTANT)

    # First composite the reference's external glow exactly where the base is
    # transparent OUTSIDE the prop. The vortex opening is excluded.
    historical_alpha = registered[:, :, 3].astype(np.float32) / 255
    external_alpha = cv2.GaussianBlur(
        historical_alpha * outside.astype(np.float32), (0, 0), 1.45)
    # Restrict to outside only, which removes any inward glow at the portal rim.
    external_alpha *= outside.astype(np.float32)
    ext = np.zeros_like(base)
    ext[:, :, :3] = registered[:, :, :3]
    ext[:, :, 3] = (np.clip(external_alpha, 0, .98) * 255).astype(np.uint8)
    ext[inside_portal] = (0, 0, 0, 0)
    backing = np.asarray(Image.alpha_composite(
        Image.fromarray(ext, "RGBA"), Image.fromarray(base, "RGBA")))

    # Preserve the intense reference lighting/edge sheen rather than making
    # yet another pale gold filter. Match original surface pixels using SIFT
    # homography, then apply original reference *lighting* only to the exact
    # current base silhouette. The alpha/silhouette remains the BASE's.
    src_rgb = base[:, :, :3].astype(np.float32)
    ref_rgb = registered[:, :, :3].astype(np.float32)
    reference_confidence = cv2.GaussianBlur(
        np.float32(registered[:, :, 3] > 210), (0, 0), 1.0)
    blend = .90 * reference_confidence * (alpha > 90)
    blend[inside_portal] = 0
    result = backing.copy()
    mapped_rgb = (
        src_rgb * (1 - blend[:, :, None]) +
        ref_rgb * blend[:, :, None]
    )
    result[:, :, :3] = np.clip(mapped_rgb, 0, 255).astype(np.uint8)
    result[inside_portal] = base[inside_portal]
    Image.fromarray(result, "RGBA").save(OUTPUT, optimize=True)
    print("Saved:", OUTPUT,
          "registration matches", len(good), "inliers", int(inliers.sum()),
          "portal hole pixels preserved", int(inside_portal.sum()))


if __name__ == "__main__":
    main()
