"""Create five exact-color, original SPELLSTRIKE gameplay-inspired WORDS faces.

Preserves the official cream/ivory tile color, gold frame and corner dot.
Changes only center glyph to a dark Times New Roman Bold serif.
Independent aligned RGBA textures for front/back in Blender, not a combined
atlas. Original gameplay tile source files are READ ONLY.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np

ROOT = Path(__file__).resolve().parents[2]  # Assets/
SOURCE = ROOT / "Sprites" / "Tile Design" / "LetterTiles"
PROJECT_OUTPUT = ROOT / "MenuV2" / "Art" / "WordsFacesV11"
USER_OUTPUT = Path.home() / "Downloads" / "MAIN MENU ASSETS" / "WORDS TILE FACES"
FONT = Path("C:/Windows/Fonts/timesbd.ttf")
LETTERS = "WORDS"
SCALE = 4
assert FONT.is_file(), f"Missing local system font: {FONT}"
assert SOURCE.is_dir(), f"Missing official source tiles: {SOURCE}"

def build():
    tiles = {
        ch: np.array(Image.open(SOURCE / f"tile-{ch}.png").convert("RGBA"))
        for ch in "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    }
    shapes = {a.shape for a in tiles.values()}
    assert len(shapes) == 1, shapes
    shape = next(iter(shapes))
    assert shape[:2] == (224, 224), shape
    # The original face behind the letters can be reconstructed using a high
    # percentile of the unchanged identical backgrounds from all 26 letters.
    # At each point, only a few source letters cover the cream tile with ink.
    stack = np.stack([tiles[ch][:, :, :3] for ch in tiles], axis=0)
    # The 99th percentile removes ghost imprints at pixels where several
    # original letters share a vertical stroke/shadow. That keeps the
    # *original* ivory base texture rather than painting it blue/navy.
    clean = np.percentile(stack, 99, axis=0).astype(np.uint8)
    # Only replace an interior rectangle. Outside remains source-perfect,
    # including beveled gold edging, corners, and colored scoring dot.
    x0, y0, x1, y1 = 42, 37, 177, 164
    mask = Image.new("L", (224, 224), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((x0, y0, x1, y1), radius=15, fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(5))
    for directory in (PROJECT_OUTPUT, USER_OUTPUT):
        directory.mkdir(parents=True, exist_ok=True)

    outputs = []
    for ch in LETTERS:
        source = Image.fromarray(tiles[ch], "RGBA")
        painted = source.copy()
        patched = source.copy()
        patched_arr = np.array(patched)
        patched_arr[:, :, :3] = clean
        patched = Image.fromarray(patched_arr, "RGBA")
        painted.paste(patched, (0, 0), mask)
        large = painted.resize((224*SCALE, 224*SCALE), Image.Resampling.LANCZOS)
        image = Image.new("RGBA", large.size, (0,0,0,0))
        image.alpha_composite(large)
        font_size = 122 * SCALE
        font = ImageFont.truetype(str(FONT), font_size)
        d = ImageDraw.Draw(image)
        bbox = d.textbbox((0, 0), ch, font=font)
        glyph_width = bbox[2] - bbox[0]
        glyph_height = bbox[3] - bbox[1]
        # Center letter in the ivory face above original bottom-right dot.
        cx, cy = 111*SCALE, 107*SCALE
        ox = int(cx - glyph_width/2 - bbox[0])
        oy = int(cy - glyph_height/2 - bbox[1])
        d.text((ox + 1*SCALE, oy + 2*SCALE), ch,
               font=font, fill=(74, 57, 41, 85))
        d.text((ox, oy), ch, font=font, fill=(27, 28, 34, 255))
        name = f"Spellstrike_WordTile_{ch}_Face_V11.png"
        for directory in (PROJECT_OUTPUT, USER_OUTPUT):
            path = directory / name
            # This script owns these five V11 generated assets; rerunning
            # refreshes ONLY its own derived copies, never gameplay sources.
            image.save(path, optimize=True)
        outputs.append(image)
        print(ch, name, "size", image.size)

    swatch = Image.new("RGBA", (5*896, 896), (0,0,0,0))
    for i, image in enumerate(outputs):
        swatch.alpha_composite(image, (i*896, 0))
    qa_path = ROOT.parent / "Captures" / "MenuV2QA" / "WORDS_V11_Review_Only_Combined.png"
    qa_path.parent.mkdir(parents=True, exist_ok=True)
    swatch.save(qa_path, optimize=True)
    print("Individual textures:", USER_OUTPUT)
    print("Preview-only montage:", qa_path)

if __name__ == "__main__":
    build()
