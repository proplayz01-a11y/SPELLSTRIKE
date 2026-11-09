"""Derived Main Menu V3 presentation sprites. Never changes approved source files.

Usage: python Assets/MenuV2/Tools/build_qa_art.py
All output paths are in Assets/MenuV2/Art and intentionally have _QA suffixes.
"""
from pathlib import Path
import math

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont


ART = Path(__file__).resolve().parents[1] / "Art"
FONTS = Path(__file__).resolve().parents[2] / "Fonts" / "SPELLSTRIKE"
FONT = FONTS / "Cinzel-Variable.ttf"


def make_registered_hover(source: str, destination: str) -> None:
    original = Image.open(ART / source).convert("RGBA")
    alpha = original.getchannel("A")
    # The glowing silhouette is extracted from the SAME approved base pixels.
    # Consequently the normal/hover pair cannot disagree in props, geometry,
    # framing, pixel dimensions or anchors.
    outer = alpha.filter(ImageFilter.MaxFilter(27))
    rim = ImageChops.subtract(outer, alpha).filter(ImageFilter.GaussianBlur(2))
    haze = outer.filter(ImageFilter.GaussianBlur(16))
    rim = rim.point(lambda n: round(n * 0.58))
    haze = haze.point(lambda n: round(n * 0.15))
    glow = Image.new("RGBA", original.size, (245, 193, 88, 0))
    glow.putalpha(ImageChops.lighter(rim, haze))
    result = Image.alpha_composite(glow, original)
    result.save(ART / destination, optimize=True)
    print(f"{source} -> {destination}: {original.size}")


def draw_oval_shadow() -> None:
    import numpy as np
    width, height = 512, 144
    xx, yy = np.meshgrid(np.linspace(-1, 1, width), np.linspace(-1, 1, height))
    opacity = np.exp(-((xx / .70) ** 2 + (yy / .53) ** 2) * 2.2) * 89
    img = np.zeros((height, width, 4), dtype=np.uint8)
    img[:, :, :3] = (26, 19, 19)
    img[:, :, 3] = np.clip(opacity, 0, 255).astype("uint8")
    Image.fromarray(img, "RGBA").save(ART / "ContactShadow_QA.png", optimize=True)


def draw_portal_spill() -> None:
    import numpy as np
    width, height = 512, 512
    xx, yy = np.meshgrid(np.linspace(-1, 1, width), np.linspace(-1, 1, height))
    radius = np.sqrt(xx * xx + yy * yy)
    # Cyan radial light, faint at the center and softer at the outer edges.
    opacity = 68 * np.exp(-((radius - .68) / .27) ** 2)
    img = np.zeros((height, width, 4), dtype=np.uint8)
    img[:, :, :3] = (63, 226, 255)
    img[:, :, 3] = np.clip(opacity, 0, 255).astype("uint8")
    Image.fromarray(img, "RGBA").save(ART / "Portal_CyanLightSpill_QA.png", optimize=True)


def draw_glint() -> None:
    import numpy as np
    width = height = 128
    xx, yy = np.meshgrid(np.linspace(-1, 1, width), np.linspace(-1, 1, height))
    cross = np.exp(-(xx / .035) ** 2 - (yy / .63) ** 2) + np.exp(
        -(yy / .045) ** 2 - (xx / .63) ** 2
    )
    center = .75 * np.exp(-(xx * xx + yy * yy) / .032)
    opacity = np.minimum(255, 192 * (cross + center))
    img = np.zeros((height, width, 4), dtype=np.uint8)
    img[:, :, :3] = (232, 251, 255)
    img[:, :, 3] = np.clip(opacity, 0, 255).astype("uint8")
    Image.fromarray(img, "RGBA").save(ART / "LogoCrystalGlint_QA.png", optimize=True)


def make_label(name: str) -> None:
    # Pre-rendered fantasy HUD plaques: art sprites, no in-game TMP captions.
    w, h = 520, 126
    im = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((14, 18, w - 14, h - 15), radius=20,
                        fill=(10, 28, 58, 239),
                        outline=(172, 116, 48, 255), width=5)
    d.rounded_rectangle((21, 25, w - 21, h - 22), radius=15,
                        outline=(233, 192, 110, 220), width=2)
    d.line(((75, 37), (w - 75, 37)), fill=(136, 103, 53, 185), width=2)
    d.line(((75, h - 34), (w - 75, h - 34)), fill=(136, 103, 53, 185), width=2)
    for x in (29, w - 29):
        d.polygon(((x, 47), (x + 11, 61), (x, 77), (x - 11, 61)),
                  fill=(58, 194, 227, 244), outline=(243, 209, 127, 255), width=2)
    try:
        font = ImageFont.truetype(str(FONT), 48 if len(name) < 9 else 43)
    except OSError:
        font = ImageFont.truetype(r"C:\Windows\Fonts\georgiab.ttf", 44)
    bounds = d.textbbox((0, 0), name, font=font, stroke_width=0)
    d.text(((w - (bounds[2] - bounds[0])) / 2,
            (h - (bounds[3] - bounds[1])) / 2 - bounds[1] - 2),
           name, font=font, fill=(246, 228, 178, 255),
           stroke_width=1, stroke_fill=(86, 54, 19, 255))
    im.save(ART / f"HoverPlaque_{name.title()}_QA.png", optimize=True)


if __name__ == "__main__":
    for normal, hover in [
        ("Adventure_Arch_Base.png", "Adventure_Arch_Hover_Aligned_QA.png"),
        ("Minigame_Tent_Base.png", "Minigame_Tent_Hover_Aligned_QA.png"),
        ("Options_Workbench_Base.png", "Options_Workbench_Hover_Aligned_QA.png"),
        ("Quit_Sign_Base.png", "Quit_Sign_Hover_Aligned_QA.png"),
    ]:
        make_registered_hover(normal, hover)
    draw_oval_shadow()
    draw_portal_spill()
    draw_glint()
    for label in ("ADVENTURE", "MINIGAME", "SPELLBOOK", "OPTIONS", "QUIT"):
        make_label(label)
