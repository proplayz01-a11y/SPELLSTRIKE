"""Create a masked, project-local facial reference decal from approved art.

This is a texture transfer for the *existing* 3D head, not a 2D replacement
for the character. No changes to the source photo or source Rodin GLB.
"""
from pathlib import Path
from PIL import Image
import math

ROOT = Path.cwd()
SOURCE = ROOT / "ArtSource/Characters/Nevali/References/Nevali_Front_Approved.png"
DEST = ROOT / "ArtSource/Characters/Nevali/Working/Nevali_FacialReference_v07_RGBA.png"
im = Image.open(SOURCE).convert("RGBA")
assert im.size == (1086, 1448), im.size

# Coordinate anchors calibrated against the user's approved front ref:
# pupil centers ~ 486,574 px / 222px; nose ~530/254; mouth ~530/284.
left, top, right, bottom = 391, 117, 669, 355
face = im.crop((left, top, right, bottom))
px = face.load()
for j in range(face.height):
    for i in range(face.width):
        x, y = left + i, top + j
        # Feather to the existing face contour; opaque over the old giant
        # pupils, nose and lip. Topmost hair/fringe remains original.
        r = math.sqrt(((x - 530.5)/112.0)**2 +
                      ((y - 237.0)/101.5)**2)
        t = min(1., max(0., (1.025-r) / .19))
        a = (t*t*(3.0-2.0*t))
        # Softly avoid alpha over original hair at top of forehead.
        if y < 165:
            a *= min(1., max(0., (y-135)/30.0))
        p = px[i,j]
        px[i,j] = (p[0],p[1],p[2],int(round(p[3]*a)))

face = face.resize((1112,952),Image.Resampling.LANCZOS)
face.save(DEST)
print("FACE_DECAL_TEXTURE",DEST,DEST.stat().st_size)
