"""Create a NONDESTRUCTIVE face re-projection on the actual Nevali GLB mesh.

Approved user reference art gives the eyes, nose and mouth; the original mesh,
costume, hair and original materials remain present, and are never overwritten.
Output is a defense-ready face prototype, not a retopologized facial rig.
"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector

ROOT=Path.cwd()
WORK=ROOT/"ArtSource/Characters/Nevali/Working"
QA=ROOT/"Library/NevaliModelRepair_20261023/After_v08"
QA.mkdir(parents=True,exist_ok=True)
source=WORK/"Nevali_FaceHair_Repair_v06.blend"
texture=WORK/"Nevali_FacialReference_v08_RGBA.png"
assert source.is_file() and texture.is_file()
bpy.ops.wm.open_mainfile(filepath=str(source))
body=next(o for o in bpy.data.objects if o.name.startswith("NEVALI__RODIN_ORIGINAL_BODY"))

# Old procedural eyebrows can be inspected/toggled in Blender, but the new
# approved facial image already contains correctly styled brows.
for ob in bpy.data.objects:
    if ob.name.startswith("NEVALI_EYEBROW_"):
        ob.hide_render=True
        ob.hide_set(True)
        ob["previous_prototype_brows"]="v06, hidden by concept reprojection"

image=bpy.data.images.load(str(texture),check_existing=True)
image.pack()
face_material=bpy.data.materials.new("NEVALI_FACE_FROM_APPROVED_CONCEPT_v08")
face_material.use_nodes=True
nodes=face_material.node_tree.nodes
links=face_material.node_tree.links
bsdf=nodes.get("Principled BSDF")
bsdf.inputs["Roughness"].default_value=.85
bsdf.inputs["Metallic"].default_value=0
tex=nodes.new("ShaderNodeTexImage")
tex.image=image;tex.interpolation='Linear'
links.new(tex.outputs["Color"],bsdf.inputs["Base Color"])
links.new(tex.outputs["Alpha"],bsdf.inputs["Alpha"])
face_material.surface_render_method='DITHERED'
face_material.diffuse_color=(1,1,1,1)
face_material.use_nodes=True

# Fitted center head coordinates relative to the original geometry (meters).
# The nonlinear placement loosely follows the actual face's frontmost surface,
# but remains smooth enough for expressive anime facial shading.
x_steps=53
z_steps=51
minx,maxx=-.185,.185
# Mesh ends outside the fade-to-transparent zone to avoid a forehead or
# chin edge where the transparent reference overlay terminates.
minz,maxz=1.447,1.755
vertices=[]
uvs=[]
hit_count=0
for j in range(z_steps):
    z=minz+(maxz-minz)*j/(z_steps-1)
    for i in range(x_steps):
        x=minx+(maxx-minx)*i/(x_steps-1)
        # Convex child face, center nose and cheeks.
        y=-.257+.084*min(1,(abs(x)/.17)**2)
        y-=.018*math.exp(-((x/.029)**2+((z-1.565)/.028)**2))
        hit,p,n,faceid=body.ray_cast(Vector((x,-.85,z)),
                                     Vector((0,1,0)),distance=1.5)
        if hit and -.33<p.y<-.16:
            y=min(y,p.y-.006)
            hit_count+=1
        vertices.append((x,y-.003,z))
        # Projection calibrated using approved front view anatomical points:
        # eye x ~ -/+ .074 at z 1.62; mouth y ~ 1.535.
        original_px=530.5 + x/0.00156
        original_py=(1.912-z)/.00132
        u=(original_px-391.)/(669.-391.)
        v=1.-(original_py-117.)/(355.-117.)
        uvs.append((u,v))

faces=[]
for j in range(z_steps-1):
    for i in range(x_steps-1):
        idx=j*x_steps+i
        faces.append((idx,idx+1,idx+x_steps+1,idx+x_steps))
facemesh=bpy.data.meshes.new("Nevali_Face_Surface_Reproject_Mesh_v08")
facemesh.from_pydata(vertices,[],faces)
facemesh.update()
layer=facemesh.uv_layers.new(name="USER_APPROVED_FACE_UV")
for face in facemesh.polygons:
    face.use_smooth=True
for face in facemesh.polygons:
    for li in face.loop_indices:
        vi=facemesh.loops[li].vertex_index
        layer.data[li].uv=uvs[vi]
decal=bpy.data.objects.new("NEVALI_FACE_EYES_NOSE_MOUTH__REFERENCE_REPROJECT_v08",facemesh)
bpy.context.collection.objects.link(decal)
facemesh.materials.append(face_material)
decal["status"]="Prototype UV facial texture projection, not facial rig"
decal["reference"]="References/Nevali_Front_Approved.png"
decal["editable"]="Texture and independent quad mesh"

# Pack all 4 approved concept files as toggleable reference empties, outside
# model's bounding box. Never exported as part of the model and never rendered.
reference_collection=bpy.data.collections.new("USER_APPROVED_REFERENCE_IMAGES__VIEWPORT_ONLY")
bpy.context.scene.collection.children.link(reference_collection)
ref_names=["Front","Side","Back","Top"]
for name in ref_names:
    p=ROOT/("ArtSource/Characters/Nevali/References/Nevali_%s_Approved.png"%name)
    if not p.exists():continue
    img=bpy.data.images.load(str(p),check_existing=True)
    img.pack()
    ob=bpy.data.objects.new("REFERENCE_%s__TOGGLE_VISIBILITY"%name,None)
    reference_collection.objects.link(ob)
    ob.empty_display_type='IMAGE'
    ob.data=img
    ob.empty_display_size=2.0
    ob.location=(3.0+ref_names.index(name)*2.0,-.7,.98)
    ob.hide_render=True
    ob.hide_set(True)

# Use the already authored render lights and camera; preserve 3-view
# comparability with v06 and avoid touching existing gameplay/Blender windows.
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1100
scene.render.resolution_y=950
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
cam=next(o for o in bpy.data.objects if o.type=='CAMERA')
scene.camera=cam
cam.data.type='ORTHO'
def capture(name,pos,target,scale):
    cam.location=pos
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.ortho_scale=scale
    scene.render.filepath=str(QA/name)
    bpy.ops.render.render(write_still=True)
    print("SAVED_RENDER",name)

capture("Nevali_Face_Front_v08.png",(0,-2.5,1.65),(0,-.07,1.655),.59)
capture("Nevali_Face_ThreeQuarter_v08.png",(.8,-2.1,1.78),(0,-.07,1.65),.62)
capture("Nevali_FullBody_v08.png",(0,-3.7,.95),(0,0,.95),2.45)
capture("Nevali_Head_Top_v08.png",(0,-1,3),(0,-.03,1.82),.63)

blend=WORK/"Nevali_FaceRebuilt_Reference_v08.blend"
gltf=WORK/"Nevali_FaceRebuilt_Reference_v08.glb"
scene.render.filepath=""
cam.location=(0,-2.5,1.65)
cam.rotation_euler=(Vector((0,-.07,1.655))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.ortho_scale=.59
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)

bpy.ops.object.select_all(action='DESELECT')
body.select_set(True)
decal.select_set(True)
active=bpy.data.objects.get("NEVALI_HAIR_CROWN__EDITABLE_PATCH")
if active and not active.hide_render:active.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.gltf(filepath=str(gltf),export_format='GLB',
                          use_selection=True,export_apply=True)
report={"source":str(source),"output_blend":str(blend),"output_glb":str(gltf),
        "reference_images":[str(p.name) for p in (ROOT/"ArtSource/Characters/Nevali/References").glob("*.png")],
        "face_decal_vertices":len(facemesh.vertices),
        "original_body_vertices":len(body.data.vertices),
        "face_surface_hits":hit_count,
        "previous_brows_hidden":True,
        "original_3d_body_kept":True,
        "top_hair_still_needs_polish":True,
        "rigged":False}
(QA/"RepairReport.json").write_text(json.dumps(report,indent=2),encoding='utf8')
print("NEVALI_V08_COMPLETE",json.dumps(report))
