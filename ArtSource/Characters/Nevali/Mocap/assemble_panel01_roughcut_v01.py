"""Family Home P01 rough-cut assembly, using actual v13 Nevali motion.

Creates separate preview .blend + 16:9 frames. Does not touch the existing
environment source, Stand-in GLB, v13 source, Unity scenes or Build Settings.
Toy contact is *hand-tuned blocking*, not solved IK/contact mocap.
"""
from pathlib import Path
import bpy
import json
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[4]
BASE = Path(__file__).resolve().parent
V13 = BASE / "RiggedReturn_v13" / "Nevali_v13_MediaPipe_BlockPlay_Rough_v04.blend"
HOME = ROOT / "ArtSource/LibraryProps/DefenseSets/Prologue_Home_01_Peaceful_v01.blend"
YOUNG = ROOT / "Assets/Cinematics/Storyboard3D/Characters/DefensePrototypes/YoungVanAert_Prototype_v01.glb"
OUT = BASE / "Panel01_RoughCut_v01"
BLEND = OUT / "SPELLSTRIKE_P01_FamilyHome_NevaliV13_RoughCut_v01.blend"
REPORT = OUT / "P01_AssemblyReport_v01.json"


def main():
    for p in (V13, HOME, YOUNG):
        if not p.is_file():
            raise FileNotFoundError(p)
    if OUT.exists() and any(OUT.iterdir()):
        raise FileExistsError("P01 rough-cut v01 folder is not empty")
    OUT.mkdir(exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(HOME))
    scene = bpy.context.scene
    if not scene.camera:
        raise RuntimeError("Original Family Home has no camera")

    # Append ONLY Nevali's skinned mesh and armature from her own v04 blend.
    with bpy.data.libraries.load(str(V13), link=False) as (src, dst):
        # The FBX mesh uses a version-suffixed name across Blender imports.
        # Only two objects exist in the animation source: rig + skinned mesh.
        dst.objects = list(src.objects)
    imported = [obj for obj in dst.objects if obj]
    rig = next((o for o in imported if o.type == "ARMATURE"), None)
    skin = next((o for o in imported if o.type == "MESH"), None)
    if not rig or not skin:
        # Different mesh name across Blender versions, allow inspecting all
        # appended objects without hiding failure.
        raise RuntimeError("Missing Nevali rig or mesh among " + str([(o.name, o.type) for o in imported]))
    for obj in imported:
        if obj.name not in scene.objects:
            scene.collection.objects.link(obj)
    anchor = bpy.data.objects.get("ANCHOR_NEVALI_CHILD")
    if not anchor:
        raise RuntimeError("Canonical Nevali marker missing in home")
    rig.location += anchor.location
    rig.name = "P01_NEVALI_V13_VIDEO_MOTION_ROUGH"
    skin.name = "P01_NEVALI_V13_SKIN"

    # Existing Young Van prototype. Deliberately NOT the old Van gameplay model
    # and deliberately not claimed to be the approved newer Young Van character.
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(YOUNG))
    added = [o for o in bpy.data.objects if o not in before]
    van_rigs = [o for o in added if o.type == "ARMATURE"]
    van_renderers = [o for o in added if o.type == "MESH"]
    van_root = next((o for o in added if o.parent is None and o.type in ("ARMATURE", "EMPTY")), None)
    if not van_rigs or not van_renderers or van_root is None:
        raise RuntimeError("Young Van GLB did not contain expected skinned actor geometry")
    van = bpy.data.objects.get("ANCHOR_VAN_YOUNG")
    if not van:
        raise RuntimeError("Canonical Young Van marker missing in home")
    van_root.location += van.location
    van_root.rotation_euler.z = 0.12  # profile silhouette at doorway
    van_root.name = "P01_YOUNG_VAN_TEMPORARY_STANDIN"

    # Adjust a few toy props in THIS DERIVED scene only. Keep existing 16
    # block-grid objects; animate an independent colored block reaching the
    # two-block local stack on the nearest edge of the play mat.
    toy = bpy.data.objects.get("TOY_BUILDING_BLOCK_03")
    if not toy:
        raise RuntimeError("Existing P01 wooden toy block not found")
    # Existing closest block forms the base. Create physical 3D duplicates,
    # with slightly different tint to make the placement readable.
    base_world = toy.location.copy()
    def clone_block(name, pos):
        block = toy.copy()
        block.data = toy.data.copy()
        scene.collection.objects.link(block)
        block.name = name
        block.location = pos
        return block
    second = clone_block("P01_TOY_STACK_SECOND", base_world + Vector((0, 0, 0.19)))
    second.rotation_euler.z = 0.05
    moving = clone_block("P01_TOY_BLOCK_HAND_BLOCKED", base_world + Vector((0.20, -0.035, 0.40)))
    second.name = "P01_TOY_STACK_SECOND_STATIC"
    poses = {
        1: base_world + Vector((0.24, -0.04, 0.4)),
        48: base_world + Vector((0.24, -0.04, 0.4)),
        72: base_world + Vector((0.16, -0.02, 0.53)),
        95: base_world + Vector((0.09, 0.01, 0.55)),
        121: base_world + Vector((0.055, 0.02, 0.5)),
        140: base_world + Vector((0.0, 0.0, 0.38)),
        160: base_world + Vector((0.0, 0.0, 0.38)),
        240: base_world + Vector((0.0, 0.0, 0.38)),
    }
    for f, p in poses.items():
        moving.location = p
        moving.keyframe_insert(data_path="location", frame=f, group="P01_PropPlacement")

    scene.render.fps = 30
    scene.frame_start = 1
    scene.frame_end = 240
    scene.render.resolution_x = 960
    scene.render.resolution_y = 540
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.engine = "BLENDER_EEVEE"
    scene.camera.data.lens = 36
    # Existing composition is canonical; render its scene camera rather
    # than inventing a new framing before inspecting real previews.
    scene.frame_set(1)
    scene["roughcut_notice"] = "Nevali v13 MediaPipe video driven 8s; Young Van temporary GLB; manual toy contact"
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
    report = {
        "source_home": str(HOME), "source_nevali": str(V13),
        "source_young_van_temp_standin": str(YOUNG),
        "separate_blend": str(BLEND),
        "scene": scene.name, "camera": scene.camera.name,
        "fps": scene.render.fps, "range": [scene.frame_start, scene.frame_end],
        "nevali_rig": rig.name, "nevali_mesh": skin.name,
        "nevali_bones": len(rig.pose.bones), "nevali_action": (
            rig.animation_data.action.name if rig.animation_data and rig.animation_data.action else None),
        "young_van_rig_count": len(van_rigs),
        "young_van_mesh_count": len(van_renderers),
        "new_toy_blocks": [second.name, moving.name],
        "manual_contact_frames": list(poses.keys()),
        "remaining": ["Camera shot QA", "toy hand contact visual check", "actual Unity Timeline and Play Mode"],
    }
    REPORT.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print("P01_ROUGHCUT_BLEND=" + str(BLEND))
    print("P01_ASSEMBLY_QA=" + str(REPORT))
    print("P01_COMPONENTS=" + json.dumps({
        "nevali_bones": len(rig.pose.bones), "van_rigs": len(van_rigs),
        "mesh_count": len([o for o in scene.objects if o.type == "MESH"])}))


if __name__ == "__main__":
    main()
