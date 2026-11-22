"""Read-only source art inspection and optional contact-sheet output.

Outputs only beneath Assets/Cinematics/ComicMode; never modifies originals.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import argparse

ROOT = Path(__file__).resolve().parents[3]
COMIC = ROOT / "Assets/Cinematics/ComicMode"


def contact(paths, target, columns=4, width=480, height=270):
    rows = (len(paths) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * width, rows * (height + 28)), "#121520")
    pen = ImageDraw.Draw(sheet)
    for index, path in enumerate(paths):
        with Image.open(path) as source:
            image = source.convert("RGB")
            image.thumbnail((width - 8, height - 8))
        x = (index % columns) * width + (width - image.width) // 2
        y = (index // columns) * (height + 28) + (height - image.height) // 2
        sheet.paste(image, (x, y))
        pen.text(((index % columns) * width + 10, (index // columns) *
                  (height + 28) + height + 3), path.stem, fill="#fff0bb")
    sheet.save(target, optimize=True)
    print(f"Wrote {target.relative_to(ROOT)} with {len(paths)} sources")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", choices=("pages", "stills"), default="pages")
    args = parser.parse_args()
    if args.source == "pages":
        paths = [ROOT / f"Assets/Story/Pages/Prologue_Page_{i:02}.png"
                 for i in range(1, 29)]
        contact(paths, COMIC / "Historical28Page_Audit_20261029.png", 4, 320, 180)
    else:
        paths = sorted((ROOT / "Assets/Cinematics/Storyboard3D/Timelines/"
            "PrologueMaster_v07_SpellbookWalkPolish/Validation/Screenshots").glob("P3*_Gait*.png"))
        contact(paths, COMIC / "P34_P35_Gait_Audit_20261029.png", 3, 360, 203)
