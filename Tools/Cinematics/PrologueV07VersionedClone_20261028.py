"""Create an independent prologue v07 scene + Timeline/animation assets.

Offline, conservative, fail-closed cloning. NEVER edits versioned sources,
build registration, routing, or git. Unity Editor import and playback QA remain
mandatory before activating v07. Re-running refuses existing v07 destinations.
"""
from __future__ import annotations
import hashlib
import json
import pathlib
import re
import uuid

ROOT = pathlib.Path(__file__).resolve().parents[2]
SCENES = ROOT / "Assets/Scenes/Cinematics"
SRC = SCENES / "Prologue_Master_Sequence_P01_42_v06_DialogueContinuity.unity"
DST = SCENES / "Prologue_Master_Sequence_P01_42_v07_SpellbookWalkPolish.unity"
TIMELINES = ROOT / "Assets/Cinematics/Storyboard3D/Timelines"
TIMELINE_DIR = TIMELINES / "PrologueMaster_v07_SpellbookWalkPolish"
MANIFEST = ROOT / "Library/PrologueV07_CloneManifest_20261028.json"
BACKUP = ROOT / "RecoveryBackups/ProloguePolishV07_20261028_Immutable_v01/Prologue_Master_v06_BEFORE_POLISH.unity"

def sha(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def meta_guid(path: pathlib.Path) -> str:
    result = re.search(r"(?m)^guid: ([a-f0-9]{32})$", pathlib.Path(str(path) + ".meta").read_text())
    if not result:
        raise ValueError(f"No GUID in {path}.meta")
    return result.group(1)

def new_meta(raw: str) -> tuple[str, str]:
    old = re.search(r"(?m)^guid: ([a-f0-9]{32})$", raw)
    if not old:
        raise ValueError("Missing GUID from source .meta")
    guid = uuid.uuid4().hex
    return raw.replace(f"guid: {old.group(1)}", f"guid: {guid}", 1), guid

def copy_asset(src: pathlib.Path, dst: pathlib.Path, change_name: bool = False) -> dict:
    if not src.is_file() or not pathlib.Path(str(src) + ".meta").is_file():
        raise FileNotFoundError(src)
    if dst.exists() or pathlib.Path(str(dst) + ".meta").exists():
        raise FileExistsError(f"Never overwrite v07: {dst}")
    text = src.read_text(encoding="utf-8")
    if change_name:
        text = text.replace(src.stem, dst.stem)
    src_meta = pathlib.Path(str(src) + ".meta").read_text(encoding="utf-8")
    updated_meta, guid = new_meta(src_meta)
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_text(text, encoding="utf-8", newline="")
    pathlib.Path(str(dst) + ".meta").write_text(updated_meta, encoding="utf-8", newline="")
    return {"old_guid": meta_guid(src), "new_guid": guid,
            "source": str(src.relative_to(ROOT)), "destination": str(dst.relative_to(ROOT)),
            "source_sha256": sha(src), "destination_sha256": sha(dst)}

def build_director_map(text: str) -> dict:
    gameobjects, transforms, directors, own_transform = {}, {}, [], {}
    for block in re.split(r"(?=^--- !u!)", text, flags=re.M):
        h = re.match(r"--- !u!(\d+) &([^\n]+)", block)
        if not h:
            continue
        typ, fid = int(h.group(1)), h.group(2).split()[0]
        if typ == 1:
            name = re.search(r"(?m)^  m_Name: (.*)$", block)
            if name: gameobjects[fid] = name.group(1)
        if typ in (4, 224):
            go = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
            parent = re.search(r"m_Father: \{fileID: (\d+)\}", block)
            if go:
                transforms[fid] = (go.group(1), parent.group(1) if parent else "0")
                own_transform[go.group(1)] = fid
        if typ == 320:
            go = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
            asset = re.search(r"m_PlayableAsset:.*?guid: ([a-f0-9]{32})", block)
            if go and asset:
                directors.append((own_transform.get(go.group(1)), asset.group(1)))
    # Names may appear after the director component in source file.
    directors.clear()
    for block in re.split(r"(?=^--- !u!)", text, flags=re.M):
        if not block.startswith("--- !u!320 "):
            continue
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", block)
        asset = re.search(r"m_PlayableAsset:.*?guid: ([a-f0-9]{32})", block)
        if go and asset:
            directors.append((own_transform.get(go.group(1)), asset.group(1)))
    result = {}
    for t, guid in directors:
        while t in transforms:
            go, t = transforms[t]
            name = gameobjects.get(go, "")
            match = re.fullmatch(r"P(\d{2})(?:_P18)?_SHOT_ROOT", name)
            if match:
                panel = int(match.group(1))
                result[15 if panel == 15 else panel] = guid
                break
    return result

def main() -> None:
    if not SRC.is_file() or not BACKUP.is_file() or sha(SRC) != sha(BACKUP):
        raise RuntimeError("v06 original/immutable backup absent or different")
    if MANIFEST.exists() or DST.exists() or pathlib.Path(str(DST)+".meta").exists() or TIMELINE_DIR.exists():
        raise FileExistsError("v07 outputs already present; no overwrite or repeat permitted")

    src_scene = SRC.read_text(encoding="utf-8")
    per_panel = build_director_map(src_scene)
    needed_panels = [15, 19, 20, 21, 25, 26, 29, 30, 34, 35]
    missing = [p for p in needed_panels if p not in per_panel]
    if missing:
        raise RuntimeError(f"Missing targeted nested directors {missing}. Found {per_panel}")

    # Index all Timeline GUID -> existing versioned asset path.
    timeline_index = {meta_guid(p): p for p in TIMELINES.rglob("*.playable") if pathlib.Path(str(p)+".meta").exists()}
    master_src = TIMELINES / "PrologueMaster_v05/Prologue_Master_P01_42_Sequence_v05.playable"
    master_guid = meta_guid(master_src)
    # The master Timeline GUID appears once for the PlayableDirector and also
    # for its scene-serialized exposed track bindings. Rebind ALL occurrences.
    if src_scene.count(master_guid) < 1:
        raise RuntimeError("Master Timeline GUID does not appear in v06 scene")
    for panel in needed_panels:
        guid = per_panel[panel]
        if guid not in timeline_index or src_scene.count(guid) < 1:
            raise RuntimeError(f"Panel {panel} Timeline missing {guid}")

    manifest = {"source_scene": str(SRC.relative_to(ROOT)), "source_sha256": sha(SRC),
                "immutable_backup": str(BACKUP.relative_to(ROOT)),
                "versioned_scene": str(DST.relative_to(ROOT)),
                "source_master_timeline": str(master_src.relative_to(ROOT)),
                "source_director_guids": {str(k):v for k,v in per_panel.items()},
                "copies":[]}

    # Verify all destinations BEFORE creating anything.
    assets = [(master_src, TIMELINE_DIR / "Prologue_Master_P01_42_Sequence_v07.playable")]
    for p in needed_panels:
        original = timeline_index[per_panel[p]]
        assets.append((original, TIMELINE_DIR / f"P{p:02d}_{original.stem}_v07.playable"))
    motion_assets = [
        TIMELINES / "Panels15_18/Block_Vander_Path_v01.anim",
        TIMELINES / "Panels15_18/Block_Van_Path_v01.anim",
        TIMELINES / "PrologueP31_42_v01/P34_Vander_CorridorMotion_v01.anim",
        TIMELINES / "PrologueP31_42_v01/P35_Vander_CorridorMotion_v01.anim",
    ]
    for original in motion_assets:
        assets.append((original, TIMELINE_DIR / f"{original.stem}_v07.anim"))
    assert len({str(d) for _,d in assets}) == len(assets), "Destination collision"
    if any(d.exists() or pathlib.Path(str(d)+".meta").exists() for _,d in assets):
        raise FileExistsError("Some v07 Timeline/animation destination already exists")

    # Separate source file assets, new independent GUIDs (no source file overwrite).
    for src, dst in assets:
        manifest["copies"].append(copy_asset(src, dst, change_name=True))

    by_src = {row["old_guid"]:row for row in manifest["copies"]}
    scene_v07 = src_scene
    for old_guid,row in by_src.items():
        scene_v07 = scene_v07.replace(old_guid, row["new_guid"])
    # Four copied movement clips must also be rebound in duplicated Timeline YAML.
    for row in manifest["copies"]:
        if not row["destination"].endswith(".playable"):
            continue
        target = ROOT / row["destination"]
        data = target.read_text(encoding="utf-8")
        for key, value in by_src.items():
            if value["destination"].endswith(".anim"):
                data = data.replace(key, value["new_guid"])
        target.write_text(data, encoding="utf-8", newline="")
        row["destination_sha256"] = sha(target)

    updated_meta, new_scene_guid = new_meta(pathlib.Path(str(SRC)+".meta").read_text(encoding="utf-8"))
    DST.write_text(scene_v07, encoding="utf-8", newline="")
    pathlib.Path(str(DST)+".meta").write_text(updated_meta, encoding="utf-8", newline="")
    manifest["new_scene_guid"] = new_scene_guid
    manifest["versioned_scene_sha256"] = sha(DST)
    manifest["source_scene_sha256_after"] = sha(SRC)
    if sha(SRC) != manifest["source_sha256"]:
        raise RuntimeError("Source changed during versioned clone")
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print(f"Created v07: {DST.relative_to(ROOT)} ({len(scene_v07)} chars), scene GUID={new_scene_guid}")
    print(f"Copied {len(assets)} independent Timelines/motion assets; source v06 SHA unchanged {sha(SRC)}")
    print(f"Manifest: {MANIFEST.relative_to(ROOT)}; panel directors: {needed_panels}")

if __name__ == "__main__":
    main()
