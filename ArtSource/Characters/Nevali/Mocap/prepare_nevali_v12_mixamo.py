"""Prepare v12 Nevali GLB as a MIXAMO AUTO-RIGGER upload candidate.

No rig/weights claimed. A single T-pose mesh FBX with embedded image media,
rest transforms and Blender source, plus independent FBX reimport QA.

Blender 5.0.1, run headless with cwd at SPELLSTRIKE root.
"""
import bpy, json, hashlib, math, os, shutil
from pathlib import Path
from mathutils import Vector

ROOT=Path.cwd()
SRC=ROOT/"ArtSource/Characters/Nevali/Working/Nevali_Face_Reconstruction_v12.glb"
OUT=ROOT/"ArtSource/Characters/Nevali/Mocap/MixamoReady_v12"
OUT.mkdir(parents=True,exist_ok=True)
BLEND=OUT/"Nevali_v12_MixamoUpload_PRE_RIG.blend"
FBX=OUT/"Nevali_v12_MixamoUpload_UNRIGGED.fbx"
REPORT=OUT/"Nevali_v12_MixamoUpload_Validation.json"
assert SRC.is_file(),str(SRC)
source_hash=hashlib.sha256(SRC.read_bytes()).hexdigest()

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(SRC))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert len(meshes)>0, "Source GLB contains no mesh"
assert not any(o.type=='ARMATURE' for o in bpy.context.scene.objects)
assert not any(o.vertex_groups for o in meshes)
original_mesh_objects=len(meshes)
orig_faces=sum(len(m.data.polygons) for m in meshes)
orig_vertices=sum(len(m.data.vertices) for m in meshes)

# Preserve the source's geometry/materials; do not decimate or remesh.
# Join everything into one object, so Mixamo receives ONE deformable mesh
# rather than >40 separately parented GLB mesh primitives.
bpy.ops.object.select_all(action='DESELECT')
for ob in meshes:
    ob.select_set(True)
    bpy.context.view_layer.objects.active=ob
    if ob.data.shape_keys is not None:
        raise RuntimeError("Source has shape keys; need a shape-key-safe merge")
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.object.join()
char=bpy.context.view_layer.objects.active
char.name="NEVALI_V12_MIXAMO_AUTO_RIG_CHARACTER"
assert char.type=='MESH'
assert len(char.data.polygons)==orig_faces

# Set the pivot at the floor under the midpoint, with world origin at
# (0,0,0). Blender GLB import is Z-up, +Y back, -Y front.
def bounds_world(obj):
    corners=[obj.matrix_world@Vector(v) for v in obj.bound_box]
    return [[min(p[i] for p in corners) for i in range(3)],
            [max(p[i] for p in corners) for i in range(3)]]

lo,hi=bounds_world(char)
height=hi[2]-lo[2]
span_x=hi[0]-lo[0]
assert .5<height<3.2,("Unexpected rig source height",height,lo,hi)
assert span_x>.7*height,("Missing horizontal arms / likely no T-pose",span_x,height)
# Source is already symmetric, but ground alignment may be off.
cx=(lo[0]+hi[0])/2
front_back_center=(lo[1]+hi[1])/2
char.location.x-=cx
char.location.y-=front_back_center
char.location.z-=lo[2]
bpy.context.view_layer.update()
lo,hi=bounds_world(char)
assert abs(lo[2])<.005 and abs((lo[0]+hi[0])/2)<.005

# Mesh is NOT rigged. Avoid adding automatic weights here: Mixamo must
# fit hips, knees, elbows etc. It may still refuse complex clothing/hair.
char["AutoRiggerStatus"]="UNRIGGED_AUTORIG_INPUT"
char["Rigged"]="False"
char["SourceSHA256"]=source_hash
char["OriginalMeshObjects"]=original_mesh_objects
char["Purpose"]="Upload into Mixamo.com then place marker joints"

# GLB import has its diffuse, roughness and normal images ALREADY PACKED
# but with empty image.filepath. FBX export needs a resolvable file path.
# Extract the exact packed bytes to our staging directory; these are copies,
# never changes to the user's original GLB or its embedded media.
texdir=OUT/"Textures"
texdir.mkdir(parents=True,exist_ok=True)
imgs=[im for im in bpy.data.images if im.packed_file is not None]
texfiles=[]
for idx,im in enumerate(imgs):
    packed=bytes(im.packed_file.data)
    ext='.png' if packed[:8]==b'\x89PNG\r\n\x1a\n' else '.jpg' if packed[:2]==b'\xff\xd8' else None
    if ext is None: raise RuntimeError("Unknown embedded source image type: "+im.name)
    slug=''.join(c if c.isalnum() else '_' for c in im.name)
    texpath=texdir/(f"{idx:02d}_{slug}"+ext)
    texpath.write_bytes(packed)
    im.filepath=str(texpath)
    texfiles.append(texpath)
    print("IMAGE_STAGED",im.name,texpath,len(packed))

bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND),compress=True)

bpy.ops.object.select_all(action='DESELECT')
char.select_set(True)
bpy.context.view_layer.objects.active=char
bpy.ops.export_scene.fbx(
    filepath=str(FBX),
    use_selection=True,
    object_types={'MESH'},
    use_mesh_modifiers=True,
    bake_anim=False,
    add_leaf_bones=False,
    use_armature_deform_only=True,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',
    axis_forward='-Z',
    axis_up='Y',
    path_mode='COPY',
    embed_textures=True,
)
assert FBX.exists() and FBX.stat().st_size>50000
with FBX.open("rb") as f:
    assert f.read(20).startswith(b"Kaydara FBX Binary"),"FBX not binary"
rawfbx=FBX.read_bytes()
embedded_image_signatures=rawfbx.count(b'\x89PNG\r\n\x1a\n')+rawfbx.count(b'\xff\xd8\xff')
assert embedded_image_signatures>0,("FBX has no embedded image bytes",len(texfiles))

# Rigging-compatibility verification via a *real* Blender FBX roundtrip.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(FBX), use_anim=False)
rt_mesh=[o for o in bpy.context.scene.objects if o.type=='MESH']
rt_arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
assert len(rt_mesh)==1,("Expected 1 upload mesh",len(rt_mesh))
assert len(rt_arm)==0,("Rigged in FBX unexpectedly",len(rt_arm))
re=rt_mesh[0]
assert len(re.data.polygons)>=orig_faces*.99
assert len(re.data.materials)>0
reimported_textures=[n.image for m in re.data.materials if m and m.use_nodes
                     for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
assert reimported_textures, "FBX roundtrip lost all diffuse texture nodes"
rlo,rhi=bounds_world(re)
assert 0.5 < rhi[2]-rlo[2] <3.2
assert abs(rlo[2])<.01,("FBX floor offset",rlo)

report={
    "status":"AUTORIGGER_UPLOAD_CANDIDATE_ROUNDTRIP_PASS",
    "what_it_is":"Unrigged, neutral T-pose, single-MESH FBX with embedded textures",
    "what_it_is_not":"Not yet skinned/auto-rigged in Mixamo and not a guaranteed Mixamo website acceptance",
    "source":str(SRC),"source_sha256":source_hash,
    "blend":str(BLEND),"fbx":str(FBX),"fbx_size":FBX.stat().st_size,
    "source_mesh_objects":original_mesh_objects,
    "source_mesh_vertices":orig_vertices,
    "source_mesh_polygons":orig_faces,
    "upload_mesh_objects":1,
    "upload_polygons":len(re.data.polygons),
    "reimport_material_count":len(re.data.materials),
    "packed_source_images":len(imgs),
    "texture_files_staged":[str(t) for t in texfiles],
    "embedded_fbx_image_signatures":embedded_image_signatures,
    "reimported_textures":[im.name for im in reimported_textures],
    "source_z_up_height_m":round(height,4),
    "reimport_z_up_height_m":round(rhi[2]-rlo[2],4),
    "fbx_ground_z_m":round(rlo[2],5),
    "reimport_rigged":len(rt_arm)>0,
    "notes":[
        "Do not upload .glb to Mixamo: use the .fbx upload candidate.",
        "Website auto-rigger needs user login and wrist/elbow/knee/groin markers.",
        "Inspect mesh deformation and skirt/hair weighting after auto-rigging.",
        "If Mixamo auto-rig fails, use a simplified hair/skirt-proxy variant."
    ],
}
REPORT.write_text(json.dumps(report,indent=2),encoding="utf8")
print("NEVALI_MIXAMO_PREP_QA",json.dumps(report))
