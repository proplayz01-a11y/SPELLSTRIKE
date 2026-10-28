"""Generate isolated, mobile-sized comic assets from historical storyboard files.

Run from the Unity project root: python Assets/Cinematics/ComicMode/ComicModeBuildAssets.py
Use --check to verify provenance, exact source SHA-256 and all 42 output byte files.
Source PNGs are read only. Outputs are version-protected (no silent replacement).
"""
import argparse
import hashlib
import io
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps

ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = ROOT / "Assets/Story/Work"
OUT_ROOT = ROOT / "Assets/Resources/ComicMode"
DOC_ROOT = ROOT / "Assets/Cinematics/ComicMode"

# The user's comic mode specifically uses rejected art. "rejected" entries have
# an explicit rejection in docs/STORY_PANEL_ASSET_MANIFEST.md. Others are the
# earliest/superseded archived Work versions, separately and honestly marked.
SOURCES = {
    1: ("Prologue_Panel_01_YOUNG_VAN_STYLE_CANDIDATE_V1.png", "rejected", "Explicitly rejected: Younger Van looked too young"),
    2: ("Prologue_Panel_02_V2_PRE_YOUNG_VAN_REVISION_ARCHIVE.png", "original_fallback", "Original archived before approved Younger Van revision"),
    3: ("Prologue_Panel_03_PRE_YOUNG_VAN_REVISION_ARCHIVE.png", "original_fallback", "Original archived before approved Younger Van revision"),
    4: ("Prologue_Panel_04_YOUNG_VAN_STYLE_CANDIDATE_V1.png", "rejected", "Explicitly rejected: staff looked too short"),
    5: ("Prologue_Panel_05_CANDIDATE_V2.png", "rejected", "Explicitly rejected: wrong age and art style"),
    6: ("Prologue_Panel_06_CANDIDATE_V2.png", "rejected", "Explicitly rejected: inconsistent painterly/cel style"),
    7: ("Prologue_Panel_07_YOUNG_VAN_STYLE_CANDIDATE_V1.png", "rejected", "Explicitly rejected: narrow wall bay by gateway"),
    8: ("Prologue_Panel_08_CANDIDATE_V2.png", "rejected", "Explicitly rejected: incorrect shelf perspective"),
    9: ("Prologue_Panel_09_CANDIDATE_V2.png", "rejected", "Explicitly rejected: Young Van and wrong environment"),
   10: ("Prologue_Panel_10_CANDIDATE_V1.png", "original_fallback", "Early Work candidate predating approved V2"),
   11: ("Prologue_Panel_11_SOURCE_APPROVED.png", "approved_exception", "No historical Work image for Panel 11; sole approved-source exception"),
   12: ("Prologue_Panel_12_CANDIDATE_V3.png", "rejected", "Explicitly rejected: background continuity broken"),
   13: ("Prologue_Panel_13_CANDIDATE_V1.png", "original_fallback", "Early Work candidate predating approved V2"),
   14: ("Prologue_Panel_14_CANDIDATE_V1.png", "original_fallback", "Early Work candidate predating approved V2"),
   15: ("Prologue_Panel_15_CANDIDATE_V2.png", "rejected", "Explicitly rejected: missing staff"),
   16: ("Prologue_Panel_16_CANDIDATE_V2.png", "rejected", "Explicitly rejected: gateway geometry drift"),
   17: ("Prologue_Panel_17_CANDIDATE_V1.png", "rejected", "Explicitly rejected: invented second chamber"),
   18: ("Prologue_Panel_18_CANDIDATE_V1.png", "original_fallback", "Only Work candidate, later source approved"),
   19: ("Prologue_Panel_19_CANDIDATE_V1.png", "original_fallback", "Only Work candidate, later source approved"),
   20: ("Prologue_Panel_20_CANDIDATE_V1.png", "original_fallback", "Early Work candidate predating approved V2"),
   21: ("Prologue_Panel_21_CANDIDATE_V1.png", "original_fallback", "Only Work candidate, later source approved"),
   22: ("Prologue_Panel_22_CANDIDATE_V1.png", "rejected", "Explicitly rejected: foreground book and missing staff"),
   23: ("Prologue_Panel_23_CANDIDATE_V1.png", "rejected", "Explicitly rejected: incorrect opposing-seat composition"),
   24: ("Prologue_Panel_24_CANDIDATE_V2.png", "original_fallback", "Superseded Work variant; V1 was ultimately restored as approved"),
   25: ("Prologue_Panel_25_Early_Glow_CANDIDATE_V1.png", "original_fallback", "Alternate early glow candidate instead of promoted V1"),
   26: ("Prologue_Panel_26_CANDIDATE_V1.png", "rejected", "Explicitly rejected: blank pages, absent Nevali message"),
   27: ("Prologue_Panel_27_CANDIDATE_V1.png", "original_fallback", "Only Work candidate, later source approved"),
   28: ("Prologue_Panel_28_CANDIDATE_V1.png", "rejected", "Explicitly rejected: book text and orientation"),
   29: ("Prologue_Panel_29_CANDIDATE_V4.png", "rejected", "Explicitly rejected: incorrect library background"),
   30: ("Prologue_Panel_30_CANDIDATE_V1.png", "original_fallback", "Only Work candidate, later source approved"),
   31: ("Prologue_Panel_31_Angry_Decision_CANDIDATE_V3.png", "original_fallback", "Alternate Work iteration; V1 approved as controlled resolve"),
   32: ("Prologue_Panel_32_CANDIDATE_V1_REJECTED.png", "rejected", "Explicitly rejected: book oriented away from Van"),
   33: ("Prologue_Panel_33_CANDIDATE_V1.png", "original_fallback", "Early Work iteration; wide V4 was selected"),
   34: ("Prologue_Panel_34_CANDIDATE_V1.png", "original_fallback", "Only original version, later source approved"),
   35: ("Prologue_Panel_35_CANDIDATE_V1.png", "rejected", "Explicitly rejected: Vander staff oversized"),
   36: ("Prologue_Panel_36_CANDIDATE_V2.png", "rejected", "Explicitly rejected: Van staff crystal shape incorrect"),
   37: ("Prologue_Panel_37_CANDIDATE_V1.png", "rejected", "Explicitly rejected: understated expression"),
   38: ("Prologue_Panel_38_CANDIDATE_V1.png", "rejected", "Explicitly rejected: staff far too short"),
   39: ("Prologue_Panel_39_CANDIDATE_V1.png", "original_fallback", "Early Work image: retired route handoff, not approved lying POV"),
   40: ("Prologue_Panel_40_CANDIDATE_V1.png", "original_fallback", "Only Work candidate, later source approved"),
   41: ("Prologue_Panel_41_CANDIDATE_V1.png", "original_fallback", "Earlier Work candidate predating approved V2"),
   42: ("Prologue_Panel_42_CANDIDATE_V1.png", "original_fallback", "Early Work candidate; restored rear-route approved artwork not used"),
}

# Direct transcription from docs/STORY_CANVA_DIALOGUE_SCRIPT.md. "proposed"
# remains visibly identified as draft in the viewer. Empty means truly silent.
TEXTS = {
  1: ("Narration", "Once upon a time, a wizard named Van Aert lived peacefully with his daughter, Nevali...", "locked"),
  2: ("Narration", "...until Broken Language spread across the kingdoms, twisting Words into weapons of ruin.", "locked"),
  3: ("Narration", "Van fought back with every Word he knew - but even his strongest magic could not stop the war.", "locked"),
  4: ("Narration", "When the war finally ended, Van returned home - only to find his wife gone and his daughter, Nevali, nowhere to be found.", "locked"),
  5: ("Narration", "With nowhere else left to turn, Van sought refuge in the library of his former master, Vander Poel.", "locked"),
  6: ("Van Aert", "The war took my wife.", "locked"),
  7: ("Van Aert", "And my daughter, Nevali... I searched everywhere, but I could not find her.", "locked"),
  8: ("", "", "silent"),
  9: ("Time card", "Decades passed...", "proposed"),
 10: ("Van Aert", "I dreamed of Nevali again. She was calling to me... as though she were somewhere close.", "proposed"),
 11: ("Nevali", "Father... can you hear me?", "proposed"),
 12: ("", "", "silent"),
 13: ("", "", "silent"),
 14: ("Vander Poel", "That was no ordinary dream. Something within this library is reaching out to you.", "proposed"),
 15: ("Vander Poel", "Come with me. There is something you must see.", "proposed"),
 16: ("", "", "silent"),
 17: ("Narration", "From the deepest shelf, Vander retrieved a volume untouched for generations.", "proposed"),
 18: ("Narration", "From the deepest shelf, Vander retrieved a volume untouched for generations.", "proposed"),
 19: ("Vander Poel", "This book records the Great War... and the force that corrupted the power of Words.", "proposed"),
 20: ("Vander Poel", "It consumed every kingdom it touched.", "proposed"),
 21: ("Vander Poel", "In the end, the black-robed entity was defeated and sealed away.", "proposed"),
 22: ("", "", "silent"),
 23: ("Van Aert", "You speak as though you knew this entity. How do you know so much about it?", "proposed"),
 24: ("Vander Poel", "Our paths crossed during the war. I witnessed its fall. Nothing more.", "proposed"),
 25: ("", "", "silent"),
 26: ("Nevali's written message", "Please help me.\n\nI've been trapped in this book for so long.\n\nMy name is Nevali.", "locked"),
 27: ("", "", "silent"),
 28: ("Van Aert", "Why is my daughter's name inside this book?", "proposed"),
 29: ("Vander Poel", "Van... this is no ordinary volume.", "locked"),
 30: ("Vander Poel (off-panel)", "It is a living prison - an entire world sealed within its pages. If Nevali wrote this message... then she is trapped inside.", "locked"),
 31: ("Van Aert", "Then tell me how to get inside.", "locked"),
 32: ("Vander Poel", "There is a way. But it requires a forbidden technique - one no wizard is permitted to use.", "locked"),
 33: ("Van Aert", "Forbidden or not, I will not leave my daughter in there.", "locked"),
 34: ("Vander Poel", "Wait here. I must retrieve my staff.", "locked"),
 35: ("", "", "silent"),
 36: ("Vander Poel", "Prepare yourself. Once the spell begins, I cannot stop it.", "locked"),
 37: ("", "", "silent"),
 38: ("Van Aert", "Where... am I?", "proposed"),
 39: ("", "", "silent"),
 40: ("", "", "silent"),
 41: ("", "", "silent"),
 42: ("Narration", "With no path back, Van followed the road ahead.", "proposed"),
}

TITLES = [
 "Peace Before the Great War", "Broken Language", "Van's Last Stand",
 "Destroyed Home", "Library Refuge", "Confession", "Nevali Missing",
 "Vander's Silence", "Decades Later", "A Familiar Dream", "Nevali's Voice",
 "The Vision Fades", "Vander Thinks", "An Unusual Dream", "Follow Me",
 "The Gateway", "An Ancient Volume", "At the Table", "The Great War",
 "War in the Pages", "The Black-Robed Entity", "Van's Suspicion",
 "The Question", "Vander's Answer", "History Dissolves", "A Message",
 "The Name", "Confrontation", "No Ordinary Volume", "A Living Prison",
 "Van's Decision", "Forbidden Technique", "Van Accepts", "Vander Departs",
 "The Staff", "The Spell", "Into the Book", "Awakening", "Sunlight",
 "Dust and Grass", "Staff Retrieved", "The Road Ahead",
]

def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()

def save_protected(path: Path, data: bytes, check: bool) -> None:
    if path.is_file():
        if path.read_bytes() != data:
            raise RuntimeError(f"Refusing to overwrite mismatched existing file: {path}")
        return
    if check:
        raise RuntimeError(f"Missing generated file: {path}")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)

def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--review-sheet", action="store_true",
                        help="Build 42-frame historical variant contact sheet for review")
    args = parser.parse_args()
    assert len(SOURCES) == len(TEXTS) == len(TITLES) == 42
    mapping = []
    for i in range(1, 43):
        filename, kind, reason = SOURCES[i]
        source = (
            ROOT / "Assets/Story/Approved" / filename if kind == "approved_exception"
            else SOURCE_ROOT / f"Panel_{i:02}" / filename
        )
        if not source.is_file():
            raise RuntimeError(f"Missing source Panel {i:02}: {source}")
        source_bytes = source.read_bytes()
        with Image.open(io.BytesIO(source_bytes)) as img:
            img = ImageOps.exif_transpose(img).convert("RGB")
            img.thumbnail((1600, 900), Image.Resampling.LANCZOS)
            buffer = io.BytesIO()
            img.save(buffer, format="JPEG", quality=84, optimize=True, subsampling=0)
            output_bytes = buffer.getvalue()
            width, height = img.size
        output_path = OUT_ROOT / f"Panel_{i:02}.bytes"
        save_protected(output_path, output_bytes, args.check)
        speaker, caption, caption_status = TEXTS[i]
        mapping.append({
            "id": i, "title": TITLES[i - 1], "imageResource": f"ComicMode/Panel_{i:02}",
            "speaker": speaker, "caption": caption, "textStatus": caption_status,
            "artStatus": kind, "reason": reason,
            "sourcePath": source.relative_to(ROOT).as_posix(),
            "sourceSha256": sha(source_bytes),
            "imageSha256": sha(output_bytes),
            "imageWidth": width, "imageHeight": height,
        })

    data = {"version": 1, "panels": mapping}
    manifest = (json.dumps(data, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    # Resources .json can load directly as TextAsset via Resources.Load.
    save_protected(OUT_ROOT / "manifest.json", manifest, args.check)
    save_protected(DOC_ROOT / "ComicModePanelSources.json", manifest, args.check)
    lines = [
        "# Comic Mode: 42-panel historical artwork mapping",
        "",
        "Comic displays archival artwork selected by the user for an intentionally alternative presentation.",
        "The historical originals in Assets/Story are never edited. Images are rendered as separate",
        "1600 × 900 maximum JPEG bytes for on-demand Android playback.",
        "",
        "Classification: **rejected** = explicitly recorded rejected iteration in the story manifest;",
        "**original_fallback** = earliest/superseded Work iteration with no explicit rejection",
        "(some were later approved as originals); **approved_exception** = no Work candidate exists.",
        "",
        "| Panel | Classification | Historical source | Why |",
        "| --- | --- | --- | --- |",
    ]
    for x in mapping:
        lines.append(f"| {x['id']:02} | {x['artStatus']} | {x['sourcePath']} | {x['reason']} |")
    counts = {kind: sum(x["artStatus"] == kind for x in mapping) for kind in ("rejected","original_fallback","approved_exception")}
    lines += [
        "",
        f"Count: {counts['rejected']} confirmed rejected, {counts['original_fallback']} Work fallbacks, {counts['approved_exception']} approved exception.",
        "",
        "Captions are transcribed from docs/STORY_CANVA_DIALOGUE_SCRIPT.md. Proposed",
        "text is labeled DRAFT in the viewer; silent frames remain silent.",
        "Panel 17-18 share one script narration; Panel 20-21 split the shared",
        "two-panel dialogue for navigation without changing the original words.",
        "Panel 39's earliest historical Work version depicts a retired route view",
        "instead of the eventual lying POV. This intentional fallback is flagged.",
        "",
        "### Build/check",
        "python Assets/Cinematics/ComicMode/ComicModeBuildAssets.py --check",
        "",
        "The generator refuses to overwrite modified images or mapping records.",
        "To revise a panel intentionally, version a new asset set and update this manifest.",
        "",
    ]
    save_protected(DOC_ROOT / "ComicModeMapping.md", ("\n".join(lines)).encode("utf-8"), args.check)
    if args.review_sheet:
        thumb_w, thumb_h, text_h = 260, 147, 31
        columns, rows = 7, 6
        sheet = Image.new("RGB", (columns * thumb_w, rows * (thumb_h + text_h)), "#101522")
        draw = ImageDraw.Draw(sheet)
        for index, item in enumerate(mapping):
            col, row = index % columns, index // columns
            with Image.open(OUT_ROOT / f"Panel_{item['id']:02}.bytes") as image:
                thumb = image.copy()
                thumb.thumbnail((thumb_w - 8, thumb_h - 8), Image.Resampling.LANCZOS)
                px = col * thumb_w + (thumb_w - thumb.width) // 2
                py = row * (thumb_h + text_h) + (thumb_h - thumb.height) // 2
                sheet.paste(thumb, (px, py))
            label = f"P{item['id']:02} {item['artStatus']}"
            draw.text((col * thumb_w + 7, row * (thumb_h + text_h) + thumb_h + 5),
                      label, fill="#f9e7b1")
        out = io.BytesIO()
        sheet.save(out, format="PNG", optimize=True)
        save_protected(DOC_ROOT / "ComicMode_42_Panel_ContactSheet_v01.png",
                       out.getvalue(), args.check)
    print("ComicMode mapping verified" if args.check else "ComicMode assets written", counts,
          "bytes=", sum((OUT_ROOT / f"Panel_{i:02}.bytes").stat().st_size for i in SOURCES))

if __name__ == "__main__":
    main()
