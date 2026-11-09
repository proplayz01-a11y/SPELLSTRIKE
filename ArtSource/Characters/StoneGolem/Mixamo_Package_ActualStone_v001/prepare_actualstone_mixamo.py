import bpy
import hashlib
import json
import math
import pathlib
import shutil
import zipfile
from mathutils import Matrix, Vector

ROOT = pathlib.Path(r"C:/Users/David/PROTOTYPE/SPELLSTRIKE")
SOURCE = ROOT / "ArtSource/Characters/StoneGolem/VolumetricStone_v001/StoneGolem_ActualStone_v002.blend"
OUT = ROOT / "ArtSource/Characters/StoneGolem/Mixamo_Package_ActualStone_v001"
VERIFY = OUT / "Verification"

FBX = OUT / "StoneGolem_ActualStone_Mixamo.fbx"
BLEND = OUT / "StoneGolem_ActualStone_Mixamo_Copy.blend"
PREVIEW = OUT / "StoneGolem_ActualStone_Mixamo_Front.png"
QA = OUT / "Export_Verification.json"
README = OUT / "README.md"
ZIP = OUT / "StoneGolem_ActualStone_Mixamo_Package.zip"

OUT.mkdir(parents=True, exist_ok=True)
VERIFY.mkdir(parents=True, exist_ok=True)

source_hash_before = hashlib.sha256(SOURCE.read_bytes()).hexdigest()

scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene.frame_start = 1
scene.frame_end = 1
scene.frame_set(1)

def is_character_piece(obj):
    if obj.type != 'MESH':
        return False
    return (
        obj.name == 'Golem_Dark_Joint_Core'
        or obj.name == 'Golem_Carved_Head'
        or obj.name.startswith('Golem_Rock_')
    )

pieces = [o for o in scene.objects if is_character_piece(o)]
rock_pieces = [o for o in pieces if o.name.startswith('Golem_Rock_')]

assert len(rock_pieces) == 135, f"Expected 135 rock pieces, found {len(rock_pieces)}"
assert any(o.name == 'Golem_Dark_Joint_Core' for o in pieces)
assert any(o.name == 'Golem_Carved_Head' for o in pieces)

# Preserve exact authored world-space geometry while consolidating to one upload mesh.
for obj in pieces:
    obj.hide_set(False)
    obj.hide_viewport = False
    obj.hide_render = False
    obj.select_set(False)

bpy.ops.object.select_all(action='DESELECT')
for obj in pieces:
    obj.select_set(True)

active = next(o for o in pieces if o.name == 'Golem_Dark_Joint_Core')
bpy.context.view_layer.objects.active = active
bpy.ops.object.join()
golem = bpy.context.object
golem.name = 'StoneGolem_ActualStone_Mixamo'
golem.data.name = 'StoneGolem_ActualStone_Mixamo_Mesh'

# Collapse duplicate/unused material slots without altering the authored look.
try:
    bpy.ops.object.material_slot_remove_unused()
except Exception:
    pass

# Normalize to 2.00 m tall, centered on X/Y, feet on Z=0. Keep the authored front direction.
world_points = [golem.matrix_world @ v.co for v in golem.data.vertices]
lo = Vector((min(p.x for p in world_points), min(p.y for p in world_points), min(p.z for p in world_points)))
hi = Vector((max(p.x for p in world_points), max(p.y for p in world_points), max(p.z for p in world_points)))
height_before = hi.z - lo.z
assert height_before > 0.01

uniform_scale = 2.0 / height_before
offset = Vector(((lo.x + hi.x) * 0.5, (lo.y + hi.y) * 0.5, lo.z))
golem.matrix_world = Matrix.Identity(4)
for v, p in zip(golem.data.vertices, world_points):
    v.co = (p - offset) * uniform_scale
golem.data.update()

# Clean scene: no source references, floor, cameras, lights, or authoring helpers.
for obj in list(scene.objects):
    if obj != golem:
        bpy.data.objects.remove(obj, do_unlink=True)

golem.location = (0.0, 0.0, 0.0)
golem.rotation_euler = (0.0, 0.0, 0.0)
golem.scale = (1.0, 1.0, 1.0)

golem.data.calc_loop_triangles()
triangle_count = len(golem.data.loop_triangles)
vertex_count = len(golem.data.vertices)
material_names = [m.name for m in golem.data.materials if m]

# Save the clean editable upload copy before adding temporary preview helpers.
bpy.ops.object.select_all(action='DESELECT')
golem.select_set(True)
bpy.context.view_layer.objects.active = golem
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))

# Export one static character mesh only. Mixamo will create the armature/skin.
bpy.ops.object.select_all(action='DESELECT')
golem.select_set(True)
bpy.context.view_layer.objects.active = golem
bpy.ops.export_scene.fbx(
    filepath=str(FBX),
    use_selection=True,
    object_types={'MESH'},
    bake_anim=False,
    use_mesh_modifiers=True,
    axis_forward='-Z',
    axis_up='Y',
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_NONE',
    path_mode='COPY',
    embed_textures=True,
)

# Front material preview. The current ActualStone look is material-driven rather than texture-baked.
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 1000
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.display.shading.light = 'STUDIO'
scene.display.shading.color_type = 'MATERIAL'
scene.display.shading.show_cavity = True

bpy.ops.object.camera_add(location=(0.0, -5.5, 1.0))
camera = bpy.context.object
camera.name = 'TEMP_ReviewCamera'
camera.rotation_euler = (Vector((0.0, 0.0, 1.0)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 2.35
scene.camera = camera

bpy.ops.object.light_add(type='AREA', location=(-3.0, -4.0, 4.0))
key = bpy.context.object
key.data.energy = 900
key.data.shape = 'DISK'
key.data.size = 4.0
key.rotation_euler = (Vector((0.0, 0.0, 1.0)) - key.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='AREA', location=(3.0, -2.0, 2.5))
fill = bpy.context.object
fill.data.energy = 450
fill.data.size = 3.0
fill.rotation_euler = (Vector((0.0, 0.0, 1.0)) - fill.location).to_track_quat('-Z', 'Y').to_euler()

scene.render.filepath = str(PREVIEW)
bpy.ops.render.render(write_still=True)

# Isolated reimport verification proves the FBX is one clean static mesh at the expected scale.
verify_fbx = VERIFY / FBX.name
shutil.copy2(FBX, verify_fbx)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(verify_fbx), use_image_search=False)

verify_scene = bpy.context.scene
verify_meshes = [o for o in verify_scene.objects if o.type == 'MESH']
verify_armatures = [o for o in verify_scene.objects if o.type == 'ARMATURE']
verify_points = []
verify_triangles = 0
verify_vertices = 0
for obj in verify_meshes:
    obj.data.calc_loop_triangles()
    verify_triangles += len(obj.data.loop_triangles)
    verify_vertices += len(obj.data.vertices)
    verify_points.extend(obj.matrix_world @ v.co for v in obj.data.vertices)

assert len(verify_meshes) == 1, f"Expected one mesh after FBX reimport, found {len(verify_meshes)}"
assert len(verify_armatures) == 0, f"Unexpected armature in pre-rig upload FBX: {len(verify_armatures)}"
triangle_delta = verify_triangles - triangle_count
triangle_delta_ratio = abs(triangle_delta) / max(1, triangle_count)
assert triangle_delta_ratio < 0.005, (verify_triangles, triangle_count, triangle_delta_ratio)
assert verify_points

vmin = Vector((min(p.x for p in verify_points), min(p.y for p in verify_points), min(p.z for p in verify_points)))
vmax = Vector((max(p.x for p in verify_points), max(p.y for p in verify_points), max(p.z for p in verify_points)))
height_after = vmax.z - vmin.z

source_hash_after = hashlib.sha256(SOURCE.read_bytes()).hexdigest()

qa = {
    'source_file': str(SOURCE),
    'source_sha256': source_hash_before,
    'source_unchanged': source_hash_before == source_hash_after,
    'blender': bpy.app.version_string,
    'authored_piece_count': 137,
    'rock_piece_count': 135,
    'upload_mesh_count': len(verify_meshes),
    'upload_armature_count': len(verify_armatures),
    'vertices': verify_vertices,
    'triangles': verify_triangles,
    'pre_export_triangles': triangle_count,
    'fbx_triangle_delta': triangle_delta,
    'fbx_triangle_delta_ratio': triangle_delta_ratio,
    'height_m': height_after,
    'feet_z': vmin.z,
    'center_x': (vmin.x + vmax.x) * 0.5,
    'center_y': (vmin.y + vmax.y) * 0.5,
    'uniform_scale_from_actualstone_v002': uniform_scale,
    'materials': material_names,
    'front_direction_blender': '-Y',
    'fbx_axis_forward': '-Z',
    'fbx_axis_up': 'Y',
    'mixamo_upload_tested': False,
    'texture_bake_complete': False,
}

assert qa['source_unchanged']
assert abs(qa['height_m'] - 2.0) < 1e-4, qa['height_m']
assert abs(qa['feet_z']) < 1e-4, qa['feet_z']
assert abs(qa['center_x']) < 1e-4, qa['center_x']
assert abs(qa['center_y']) < 1e-4, qa['center_y']

QA.write_text(json.dumps(qa, indent=2), encoding='utf-8')

README.write_text(
    "# Stone Golem ActualStone - Mixamo upload copy\n\n"
    "Prepared from `StoneGolem_ActualStone_v002.blend`. The source file is not modified.\n\n"
    "## Upload\n"
    "Upload `StoneGolem_ActualStone_Mixamo.fbx` to Mixamo. It contains one static character mesh, "
    "2.00 m tall, feet at ground, centered, no armature, no animation, no camera/light/review helpers.\n\n"
    "## Mixamo markers\n"
    "Place chin, wrists, elbows, knees and groin markers on the visible anatomical joints. The model has bulky stone anatomy, "
    "so inspect shoulders, elbows, hips and knees in a test animation after auto-rigging.\n\n"
    "## Materials\n"
    "The current ActualStone appearance is primarily Blender material-driven. This package intentionally does not claim final texture baking. "
    "Rigging can proceed first; final texture/material polish can be baked after the skeleton workflow is confirmed.\n\n"
    "See `Export_Verification.json` for fresh isolated FBX reimport checks.\n",
    encoding='utf-8',
)

with zipfile.ZipFile(ZIP, 'w', zipfile.ZIP_DEFLATED) as z:
    for path in [FBX, BLEND, PREVIEW, QA, README]:
        z.write(path, path.name)

print('ACTUALSTONE_MIXAMO_READY=' + json.dumps(qa))
print('ACTUALSTONE_PACKAGE=' + str(ZIP))
