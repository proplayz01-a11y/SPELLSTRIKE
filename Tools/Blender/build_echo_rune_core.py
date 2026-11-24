"""Author-owned SPELLSTRIKE Echo Rune Core; run with Blender 5 in background.

Creates editable .blend and a lightweight glTF scene asset for Unity.
No downloaded meshes, textures, or external asset packs are used.
"""
import bpy
import math
import os
import random
from mathutils import Vector

random.seed(3107)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND = os.path.join(ROOT, "ArtSource", "Characters", "SpiritRemnant", "EchoRuneCore.blend")
GLB = os.path.join(ROOT, "Assets", "Characters", "SpiritRemnant", "EchoRuneCore", "EchoRuneCore.glb")
os.makedirs(os.path.dirname(BLEND), exist_ok=True)
os.makedirs(os.path.dirname(GLB), exist_ok=True)


def material(name, color, metallic=0.0, roughness=0.5, emission=None, strength=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    principled = m.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (*color, 1.0)
    principled.inputs["Metallic"].default_value = metallic
    principled.inputs["Roughness"].default_value = roughness
    if emission is not None:
        principled.inputs["Emission Color"].default_value = (*emission, 1.0)
        principled.inputs["Emission Strength"].default_value = strength
    return m


stone = material("Fractured ancient indigo stone", (0.052, 0.083, 0.15), 0.22, 0.58)
facets = material("Rune shell bevels", (0.15, 0.24, 0.36), 0.65, 0.31)
seams = material("Cyan luminous cracks", (0.02, 0.23, 0.47), 0.0, 0.4,
                 (0.03, 0.72, 1.0), 3.4)
core = material("Ancient blue white essence", (0.13, 0.65, 0.95), 0.0, 0.25,
                (0.12, 0.78, 1.0), 5.0)
runes = material("Glowing runic inscription", (0.05, 0.55, 0.84), 0.0, 0.25,
                 (0.12, 0.91, 1.0), 4.5)

bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0)
template = bpy.context.object
face_data = [tuple(template.data.vertices[i].co.copy() for i in f.vertices)
             for f in template.data.polygons]
bpy.data.objects.remove(template, do_unlink=True)

verts, faces, face_materials = [], [], []
surface_centers = []
for n, tri in enumerate(face_data):
    center = sum(tri, Vector()) / 3.0
    direction = center.normalized()
    surface_centers.append((center, direction))
    # Shrink each face independently: exposed gaps reveal the glowing heart.
    shrink = random.uniform(0.895, 0.944)
    outer = [((p - center) * shrink + center).normalized() *
             random.uniform(0.995, 1.02) for p in tri]
    inner = [p * 0.85 for p in outer]
    off = len(verts)
    verts.extend(outer + inner)
    faces.append((off, off+1, off+2))
    face_materials.append(0)
    faces.append((off+5, off+4, off+3))
    face_materials.append(1)
    for edge in range(3):
        a = edge
        b = (edge+1) % 3
        faces.append((off+a, off+b, off+3+b, off+3+a))
        face_materials.append(1 if n % 4 else 2)

mesh = bpy.data.meshes.new("Fractured wedges with natural rune gaps")
mesh.from_pydata(verts, [], faces)
mesh.materials.append(stone)
mesh.materials.append(facets)
mesh.materials.append(seams)
mesh.update()
shell = bpy.data.objects.new("EchoRuneCore_FracturedStoneShell", mesh)
bpy.context.collection.objects.link(shell)
for polygon, mat_index in zip(mesh.polygons, face_materials):
    polygon.material_index = mat_index

bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=0.795)
heart = bpy.context.object
heart.name = "EchoRuneCore_LuminousEssence"
heart.data.materials.append(core)
for polygon in heart.data.polygons:
    polygon.use_smooth = True

# Tiny rune strokes are actual 3D geometry, visible on the stone surface.
# This keeps the entire asset editable in Blender and independent of textures.
glyph_segments = [
    [((-0.40, -0.45), (0.00, 0.46)), ((0.00, 0.46), (0.42, -0.45))],
    [((-0.40, 0.45), (-0.40, -0.45)), ((-0.40, 0.0), (0.38, 0.0)),
     ((0.38, 0.0), (0.38, 0.45))],
    [((-0.35, 0.43), (0.33, 0.43)), ((0.0, 0.43), (0.0, -0.42)),
     ((-0.32, -0.42), (0.32, -0.42))],
    [((-0.38, -0.37), (0.38, 0.40)), ((-0.38, 0.40), (0.38, -0.37))]
]

for j, face_index in enumerate(range(1, 80, 5)):
    center, normal = surface_centers[face_index]
    # Tangent space on shell. All rune points stay just above the visible face.
    u = normal.cross(Vector((0.0, 0.0, 1.0)))
    if u.length < 0.1:
        u = normal.cross(Vector((0.0, 1.0, 0.0)))
    u.normalize()
    v = normal.cross(u).normalized()
    anchor = normal * 1.026
    for a, b in glyph_segments[j % len(glyph_segments)]:
        pa = anchor + u * (a[0] * 0.24) + v * (a[1] * 0.24)
        pb = anchor + u * (b[0] * 0.24) + v * (b[1] * 0.24)
        direction = pb - pa
        middle = (pa + pb) * 0.5
        bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.013,
                                            depth=direction.length, location=middle)
        bar = bpy.context.object
        bar.name = "Rune_%02d" % j
        bar.rotation_mode = "QUATERNION"
        bar.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
        bar.data.materials.append(runes)

# Circular arcane hoops are spare, thin, and clearly distinct from Nevali's fragments.
for angle in (0.0, 1.2):
    bpy.ops.mesh.primitive_torus_add(major_radius=1.085, minor_radius=0.012,
                                    major_segments=80, minor_segments=5)
    hoop = bpy.context.object
    hoop.name = "FloatingRunicOrbit"
    hoop.rotation_euler = (0.36 + angle, 0.18, angle)
    hoop.data.materials.append(seams)

# Merge all rune strokes into a single mesh for fewer Unity draw calls.
# They remain individual connected glyphs in Edit Mode.
glyph_objects = [obj for obj in bpy.context.scene.objects if obj.name.startswith("Rune_")]
bpy.ops.object.select_all(action="DESELECT")
for obj in glyph_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = glyph_objects[0]
bpy.ops.object.join()
glyph_objects[0].name = "EchoRuneCore_AncientRuneGlyphs"

# Collect all visible objects under a root so they import as one clean model.
root = bpy.data.objects.new("EchoRuneCore", None)
bpy.context.collection.objects.link(root)
for obj in bpy.context.scene.objects:
    if obj != root and obj.type == "MESH":
        obj.parent = root

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
bpy.ops.export_scene.gltf(filepath=GLB, export_format="GLB",
                          export_apply=True, export_cameras=False, export_lights=False)
print("EXPORTED_ECHO_RUNE_CORE=" + GLB)
