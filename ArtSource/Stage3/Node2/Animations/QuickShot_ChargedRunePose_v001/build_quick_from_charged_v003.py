import bpy
import pathlib

ROOT = pathlib.Path(r"C:/Users/David/PROTOTYPE/SPELLSTRIKE")
SOURCE = ROOT / "ArtSource/Stage3/Node2/Animations/ChargedRuneShot_v003/S3N2_DesertArcher_ChargedRuneShot_NLA_v003.blend"
OUT_DIR = ROOT / "ArtSource/Stage3/Node2/Animations/QuickShot_ChargedRunePose_v001"
OUT_BLEND = OUT_DIR / "S3N2_DesertArcher_QuickShot_ChargedRunePose_NLA_v001.blend"
OUT_FBX = OUT_DIR / "S3N2_DesertArcher_QuickShot_ChargedRunePose_v001.fbx"

OUT_DIR.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))

scene = bpy.context.scene
rig = bpy.data.objects["Armature"]
ad = rig.animation_data
ad.action = None

for track in list(ad.nla_tracks):
    ad.nla_tracks.remove(track)

draw = bpy.data.actions["SOURCE_Standing_Draw_Arrow_FULL_1_32"]
overdraw = bpy.data.actions["SOURCE_Standing_Aim_Overdraw_FULL_1_114"]
hold = bpy.data.actions["SOURCE_Full_Overdraw_TensionHold_1_18"]
recoil = bpy.data.actions["SOURCE_Standing_Aim_Recoil_FULL_1_22"]

def add_strip(track_name, strip_name, action, start, end):
    track = ad.nla_tracks.new()
    track.name = track_name
    strip = track.strips.new(strip_name, start, action)
    if action.slots:
        strip.action_slot = action.slots[0]
    strip.action_frame_start = action.frame_range[0]
    strip.action_frame_end = action.frame_range[1]
    strip.frame_end = end
    strip.blend_type = 'REPLACE'
    strip.extrapolation = 'NOTHING'
    strip.use_auto_blend = False
    strip.blend_in = 0.0
    strip.blend_out = 0.0
    return strip

# 54 authored frames (1..54) at 30 FPS = 1.7667 sec in Unity.
# The release/recoil handoff is frame 33, matching Quick Shot's 1.0667 sec event.
add_strip("01 QUICK Draw", "01 Standing Draw Arrow", draw, 1, 13)
add_strip("02 QUICK Overdraw", "02 Standing Aim Overdraw", overdraw, 13, 29)
add_strip("03 QUICK Hold", "03 Full Overdraw Tension Hold", hold, 29, 33)
add_strip("04 QUICK Recoil", "04 Standing Aim Recoil", recoil, 33, 54)

scene.frame_start = 1
scene.frame_end = 54
scene.render.fps = 30
scene.name = "S3N2_DesertArcher_QuickShot_ChargedRunePose"

for marker in list(scene.timeline_markers):
    scene.timeline_markers.remove(marker)
for name, frame in [
    ("DRAW START", 1),
    ("CHARGED-RUNE TWIST", 29),
    ("ARROW RELEASE / RECOIL", 33),
    ("QUICK END", 54),
]:
    scene.timeline_markers.new(name, frame=frame)

# Bake the evaluated multi-strip NLA result into one action. FBX otherwise exports
# every NLA strip as a separate take, which makes Unity see four short clips instead
# of one 54-frame Quick Shot.
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='POSE')
bpy.ops.nla.bake(
    frame_start=1,
    frame_end=54,
    step=1,
    only_selected=False,
    visual_keying=True,
    clear_constraints=False,
    clear_parents=False,
    use_current_action=False,
    clean_curves=False,
    bake_types={'POSE'},
)
bpy.ops.object.mode_set(mode='OBJECT')

baked = ad.action
if baked is None:
    raise RuntimeError('NLA bake did not create a single action')
baked.name = 'S3N2_QuickShot_ChargedRunePose_BAKED_1_54'
baked.use_fake_user = True

for track in list(ad.nla_tracks):
    ad.nla_tracks.remove(track)
ad.action = baked

bpy.ops.wm.save_as_mainfile(filepath=str(OUT_BLEND))

bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.data.objects:
    if obj.type in {'ARMATURE', 'MESH'}:
        obj.select_set(True)
bpy.context.view_layer.objects.active = rig

bpy.ops.export_scene.fbx(
    filepath=str(OUT_FBX),
    use_selection=True,
    object_types={'ARMATURE', 'MESH'},
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',
    use_space_transform=True,
    bake_space_transform=False,
    axis_forward='-Z',
    axis_up='Y',
    add_leaf_bones=False,
    use_armature_deform_only=False,
    bake_anim=True,
    bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0,
    path_mode='AUTO',
    embed_textures=False,
)

print(f"QUICK_CHARGEDPOSE_READY blend={OUT_BLEND} fbx={OUT_FBX}")
