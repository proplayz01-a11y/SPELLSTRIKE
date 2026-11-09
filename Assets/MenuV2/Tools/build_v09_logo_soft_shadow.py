"""Generate ONLY a soft UI shadow silhouette for the approved SPELLSTRIKE logo.

This is not a replacement logo. The source PNG is read-only and the resulting
dark, blurred silhouette is rendered behind it as a separate Unity Image.
"""
from pathlib import Path
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1] / "Art"
SOURCE = ROOT / "Spellstrike_Logo.png"
DEST = ROOT / "SpellstrikeLogo_SoftBlurShadow_V09.png"

if DEST.exists():
    raise SystemExit("Refusing to overwrite an existing shadow asset")

source = Image.open(SOURCE).convert("RGBA")
pad = 105
shadow = Image.new("L", (source.width + 2 * pad, source.height + 2 * pad))
shadow.paste(source.getchannel("A"), (pad + 12, pad + 18))
shadow = shadow.filter(ImageFilter.GaussianBlur(radius=31))
# Subdued dark cocoa-gray cast shadow, softened enough for the sky background.
shadow = shadow.point(lambda a: round(a * .60))
out = Image.new("RGBA", shadow.size, (30, 23, 31, 0))
out.putalpha(shadow)
out.save(DEST, optimize=True)
print("Shadow texture:", DEST, "dimensions:", out.size)
print("Source dimensions:", source.size, "pad:", pad, "blur radius: 31")
