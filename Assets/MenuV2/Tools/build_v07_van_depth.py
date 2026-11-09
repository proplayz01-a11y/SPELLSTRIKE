"""Non-destructive painterly integration polish for the approved Van artwork.

Preserves every shape, face, robe motif, staff and original alpha registration.
No cartoon stroke, no AI redraw, no silhouette expansion. The subtle changes
are alpha-fringe cleanup, *internal* edge shading, directional cloth bounce and
robe microcontrast. A separate two-boot/staff contact shadow grounds Van.
"""

from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ART = Path(__file__).resolve().parents[1] / "Art"
SOURCE = ART / "VanAert_Approved.png"
OUTPUT = ART / "VanAert_PainterlyDepth_V07.png"
SHADOW = ART / "VanGroundContact_V07.png"


def build_character():
    src = np.array(Image.open(SOURCE).convert("RGBA"))
    h, w, _ = src.shape
    rgb = src[:, :, :3].astype(np.float32)
    alpha = src[:, :, 3]
    surface = alpha >= 85
    solid = alpha >= 215
    ys, xs = np.indices((h, w))

    # Replace only near-transparent contaminated fringe RGB using an
    # *interior* nearest neighbor. Preserve the original alpha/shape exactly.
    _, labels = cv2.distanceTransformWithLabels(
        np.uint8(~solid), cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL
    )
    inside_y, inside_x = np.where(solid)
    nearest_pixels = np.zeros((int(labels.max()) + 1, 2), dtype=np.int32)
    nearest_pixels[labels[inside_y, inside_x]] = np.stack(
        [inside_y, inside_x], axis=-1
    )
    fringe = (alpha > 0) & (alpha < 130)
    closest = nearest_pixels[labels[fringe]]
    rgb[fringe] = rgb[closest[:, 0], closest[:, 1]]

    # Ink stays on the INSIDE of the painting, with no external stroke.
    # Hair needs a touch more definition against the sky, cloak against grass.
    internal_distance = cv2.distanceTransform(
        np.uint8(surface), cv2.DIST_L2, 5
    )
    boundary = np.clip(np.exp(-internal_distance / 4.3), 0, 1)
    light_hair = (ys < 340) & (rgb.mean(axis=2) > 130)
    blue_cloth = (ys > 250) & (rgb[:, :, 2] > rgb[:, :, 0] * 1.32)
    weight = np.where(light_hair, .205, np.where(blue_cloth, .16, .095))
    ink = weight * boundary * surface
    # Painterly charcoal/navy shade; not an opaque black comic stroke.
    ink_rgb = np.array([20., 27., 39.], dtype=np.float32)
    rgb = rgb * (1.0 - ink[..., None]) + ink_rgb * ink[..., None]

    # A barely visible sky bounce from upper-left on navy cloth, feathered
    # inward over 13 pixels. Does not recolor skin, hair, gold or staff.
    bounce_region = blue_cloth & (xs < 610) & (ys < 1140)
    bounce = (np.exp(-internal_distance / 16.0)
              * np.clip((1050.0 - ys) / 750, 0, 1)
              * bounce_region * surface)
    rgb += bounce[..., None] * np.array([2.5, 4.0, 7.5], dtype=np.float32)

    # Environmental light wrap: cool blue/cyan from the portal on camera-left,
    # warm meadow sunlight on camera-right. This is NOT an outer glow or a
    # cartoon line. It lives 10-20 source pixels INSIDE the original robe art.
    edge_wrap = np.exp(-internal_distance / 17.0) * surface
    portal_side = np.clip((670.0 - xs) / 360.0, 0, 1)
    sun_side = np.clip((xs - 540.0) / 340.0, 0, 1)
    cloth_wrap = blue_cloth & (ys > 290) & (ys < 1180)
    rgb += (
        edge_wrap * cloth_wrap * portal_side
    )[..., None] * np.array([5.0, 10.0, 15.0], dtype=np.float32)
    rgb += (
        edge_wrap * cloth_wrap * sun_side
    )[..., None] * np.array([12.0, 8.0, 3.0], dtype=np.float32)

    # Low-intensity earth/grass reflected light on the bottom of the robe
    # pulls its palette into the meadow's terrain without painting new folds.
    low_robe = cloth_wrap & (ys > 780)
    low_wrap = edge_wrap * low_robe * np.clip((ys - 780) / 460., 0, 1)
    rgb += low_wrap[..., None] * np.array([9.0, 9.0, 3.0], dtype=np.float32)

    # Enhance *existing* fold texture subtly, only on the dark navy outer
    # garment. Avoid over-sharpening the face, beard and gold embroidery.
    luma = rgb[:, :, 0] * .2126 + rgb[:, :, 1] * .7152 + rgb[:, :, 2] * .0722
    low = cv2.GaussianBlur(luma, (0, 0), 4.0)
    detail = np.clip(luma - low, -12, 12)
    fold_mask = (
        blue_cloth & (ys >= 260) & (ys <= 1210) & solid
    ).astype(np.float32)
    fold_mask = cv2.GaussianBlur(fold_mask, (0, 0), 1.0)
    rgb += (detail * fold_mask * .23)[..., None]

    out = src.copy()
    out[:, :, :3] = np.clip(rgb, 0, 255).astype(np.uint8)
    # Exact same geometry and alpha. No new outline, no new mask.
    assert np.array_equal(out[:, :, 3], src[:, :, 3])
    Image.fromarray(out, "RGBA").save(OUTPUT, optimize=True)
    print(f"Van V07: {w}x{h}, exact original alpha preserved; {OUTPUT}")


def build_contact_shadow():
    w, h = 512, 160
    yy, xx = np.mgrid[0:h, 0:w]

    def ellipse(x, y, rx, ry, strength):
        return strength * np.exp(
            -((xx - x) / rx) ** 2 * 1.75
            -((yy - y) / ry) ** 2 * 1.75
        )

    # Wide soft ambient shadow and distinct boot-ground/staff-point contact.
    shadow = (
        ellipse(260, 78, 225, 43, .23)
        + ellipse(182, 77, 60, 23, .37)   # Van's left boot
        + ellipse(350, 78, 60, 25, .38)   # Van's right boot
        + ellipse(88, 80, 20, 15, .24)    # wooden staff tip
    )
    shadow = np.clip(cv2.GaussianBlur(shadow, (0, 0), 1.7), 0, .54)
    image = np.zeros((h, w, 4), dtype=np.uint8)
    image[:, :, 0] = 20
    image[:, :, 1] = 25
    image[:, :, 2] = 25
    image[:, :, 3] = (shadow * 255).astype(np.uint8)
    Image.fromarray(image, "RGBA").save(SHADOW, optimize=True)
    print(f"Van V07 contact shadow: {w}x{h}; {SHADOW}")


if __name__ == "__main__":
    build_character()
    build_contact_shadow()
