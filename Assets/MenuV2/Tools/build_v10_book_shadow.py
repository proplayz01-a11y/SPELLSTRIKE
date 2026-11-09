"""Soft, transparent perspective shadow for actual 3D menu Spellbook.

Uses only local procedural alpha; no AI art, no source asset modification.
"""
from pathlib import Path
from PIL import Image, ImageFilter
import numpy as np

out = Path(__file__).resolve().parents[1] / "Art" / "Book_GroundShadow_V10.png"
if out.exists():
    raise SystemExit("V10 shadow art exists already; refusing overwrite")
w, h = 512, 160
y, x = np.mgrid[0:h, 0:w]
soft = 0.30 * np.exp(-((x - 256) / 170) ** 2 - ((y - 81) / 32) ** 2)
contact = 0.38 * np.exp(-((x - 256) / 105) ** 2 - ((y - 81) / 17) ** 2)
alpha = (np.clip(soft + contact, 0, .68) * 255).astype(np.uint8)
canvas = Image.new("RGBA", (w,h), (22,20,26,0))
canvas.putalpha(Image.fromarray(alpha, "L").filter(ImageFilter.GaussianBlur(2)))
canvas.save(out, optimize=True)
print(out, canvas.size, "alpha_peak", np.asarray(canvas)[:,:,3].max())
