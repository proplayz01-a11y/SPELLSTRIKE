"""Stronger painterly Van integration for isolated SPELLSTRIKE Main Menu V08.

Regrades the already-approved V07 2D Van without geometry, face, or costume
changes. Keeps EXACT alpha and 1122x1402 canvas. Avoids a cartoon silhouette
stroke. Generates a physically anchored, deliberately visible ground shadow
from the STAFF TIP and TWO BOOT positions measured on the actual sprite.

No writes to original VanAert_Approved.png or earlier v07 assets.
"""

from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parents[1] / "Art"
SRC = ART / "VanAert_PainterlyDepth_V07.png"
OUT = ART / "VanAert_IntegratedDepth_V08.png"
GROUND = ART / "VanGroundContact_Strong_V08.png"


def make_van():
    original = np.array(Image.open(SRC).convert("RGBA"))
    y, x = np.indices(original.shape[:2])
    h, w = original.shape[:2]
    rgb = original[:, :, :3].astype(np.float32)
    alpha = original[:, :, 3]
    opaque = alpha >= 185

    # This palette gate includes the dark navy fabric but not skin, trim,
    # white hair, cyan crystal, or natural dark brown wooden staff.
    navy = (rgb[:, :, 2] > rgb[:, :, 0] * 1.18) & (
        rgb[:, :, 2] > rgb[:, :, 1] * 1.02
    ) & (rgb[:, :, 0] < 100) & opaque & (y >= 210)

    # Diffuse AO: darkest under overlapping cloak and around hips/robe hem.
    # This is low-frequency shading, not a sharp line or cutout silhouette.
    fold_zone = (np.exp(-((y - 730) / 360.0) ** 2) * .35 +
                 np.exp(-((y - 1150) / 175.0) ** 2) * .60)
    valley = np.exp(-((x - 560) / 190.0) ** 2) * .36
    darken = (5.5 + 11.0 * fold_zone + 5.0 * valley) * navy
    rgb -= darken[:, :, None] * np.array([.58, .76, 1.04], np.float32)

    # Stronger light/shadow on already-painted folds. A restrained bandpass
    # exposes form already present in the source rather than inventing lines.
    luminance = (rgb[:, :, 0] * .2126 + rgb[:, :, 1] * .7152
                 + rgb[:, :, 2] * .0722)
    smooth = cv2.GaussianBlur(luminance, (0, 0), 8.0)
    folds = np.clip(luminance - smooth, -15, 14)
    fold_strength = 0.64 * navy
    rgb += (fold_strength * folds)[:, :, None]

    # Cool portal-side skylight and warm tent-side reflected daylight. Applied
    # to *interior* robe margins only; absolutely NO black exterior stroke.
    inner_dist = cv2.distanceTransform(np.uint8(alpha > 90), cv2.DIST_L2, 5)
    wrap = np.exp(-inner_dist / 27.0) * navy
    left = np.clip((685.0 - x) / 300.0, 0, 1)
    right = np.clip((x - 565.0) / 330.0, 0, 1)
    height = np.clip((1130 - y) / 750., 0.12, 1.0)
    rgb += (wrap * left * height)[:, :, None] * np.array([5., 13., 20.])
    rgb += (wrap * right * height)[:, :, None] * np.array([13., 10., 4.])

    # Fine dark contact shadow under each sleeve where the arm overlaps the
    # cloak. Soft oval fields are deliberately localized, not uniform edges.
    sleeves = (np.exp(-((x - 344) / 75.) ** 2 - ((y - 570) / 150.) ** 2)
               + np.exp(-((x - 740) / 93.) ** 2 - ((y - 710) / 138.) ** 2))
    rgb -= (sleeves * navy * 8.0)[:, :, None]

    # Deepen near hem between boots where the hanging cloth occludes light;
    # keep brown boots and gold hem material exactly as drawn.
    hem = (navy & (y > 1050)) * np.exp(-((x - 565) / 225.) ** 2)
    rgb -= (hem * 8.0)[:, :, None]

    result = original.copy()
    result[:, :, :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    assert result.shape == original.shape
    assert np.array_equal(result[:, :, 3], original[:, :, 3])
    Image.fromarray(result, "RGBA").save(OUT, optimize=True)
    occupied = alpha >= 150
    diff = np.abs(result[occupied, :3].astype(np.int16) -
                  original[occupied, :3].astype(np.int16))
    print("V08 character", w, h, "mean RGB shift", round(float(diff.mean()), 2),
          "max", int(diff.max()), "alpha unchanged", True)


def make_ground_shadow():
    # Canvas location (2400x1080): center (1190, 63), size (500,140).
    # Actual contact coordinates measured in V07's 2D character sprite:
    # staff (1068,46), left boot (1127,45), right boot (1251,31) in
    # screen pixels, Y measured from bottom. Cast shadow sits on the path.
    W, H = 1024, 288
    yy, xx = np.indices((H, W), dtype=np.float32)
    screen_x = 940.0 + xx * (500.0 / W)
    screen_y = 63.0 + (H * .5 - yy) * (140.0 / H)

    def oval(cx, cy, rx, ry, strength, lean=0.0):
        delta_x = screen_x - cx - lean * (screen_y - cy)
        return strength * np.exp(-.5 * ((delta_x / rx) ** 2 +
                                         ((screen_y - cy) / ry) ** 2))

    # Broad floor shadow, diffused in perspective, then visible dark
    # elliptical contact patches at two distinct boot soles & staff tip.
    ambient = oval(1190, 58, 169, 23, .22)
    cast = oval(1145, 69, 127, 20, .20, lean=.65)
    # Two contact bands touch the soles and rise slightly *behind* the shoes
    # in image perspective, then wider falloff on the sunlit path.
    boot_l = oval(1127, 48, 44, 7.0, .78)
    boot_l_soft = oval(1124, 62, 64, 16, .33)
    boot_r = oval(1251, 36, 45, 8.0, .84)
    boot_r_soft = oval(1251, 51, 66, 17, .35)
    staff = oval(1068, 49, 13, 6.5, .73)
    under_robe = oval(1193, 53, 104, 15.0, .16)

    opacity = 1.0 - (1.0 - np.clip(ambient, 0, .9)) * (
        1.0 - np.clip(cast, 0, .9)) * (1.0 - np.clip(boot_l, 0, .9)) * (
        1.0 - np.clip(boot_l_soft, 0, .9)) * (1.0 - np.clip(boot_r, 0, .9)) * (
        1.0 - np.clip(boot_r_soft, 0, .9)) * (1.0 - np.clip(staff, 0, .9)) * (
        1.0 - np.clip(under_robe, 0, .9))
    opacity = cv2.GaussianBlur(np.clip(opacity, 0, .82), (0, 0), 2.0)
    out = np.zeros((H, W, 4), dtype=np.uint8)
    out[:, :, :3] = np.array([29, 27, 22], dtype=np.uint8)
    out[:, :, 3] = np.rint(opacity * 255).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(GROUND, optimize=True)
    print("V08 ground", W, H, "peak opacity",
          round(float(opacity.max()), 3),
          "contact points", (1068, 49), (1127, 48), (1251, 36))


if __name__ == "__main__":
    make_van()
    make_ground_shadow()
