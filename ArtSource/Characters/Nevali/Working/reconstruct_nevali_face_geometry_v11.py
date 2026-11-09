"""Nevali face RECONSTRUCTION from real Blender geometry, not an image patch.

The original, faulty facial surface is deleted locally from the Rodin mesh.
An independently editable low-poly modeled skin/eye/nose/mouth assembly is
built in its place, inspired by Nevali's approved front/side reference images.
Neither image pixels nor new texture projection are used on the face.
All original Rodin source files and earlier v06/v08 versions remain untouched.

Run from SPELLSTRIKE root:
  blender -b -t 2 --factory-startup --python
    ArtSource/Characters/Nevali/Working/reconstruct_nevali_face_geometry_v09.py
"""
import bpy, bmesh, json, math
from mathutils import Vector
from pathlib import Path

ROOT=Path.cwd()
WORK=ROOT/"ArtSource/Characters/Nevali/Working"
QA=ROOT/"Library/NevaliModelRepair_20261023/Geometry_v11"
QA.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(WORK/"Nevali_FaceHair_Repair_v06.blend"))
scene=bpy.context.scene
body=next(o for o in bpy.data.objects if o.name.startswith("NEVALI__RODIN_ORIGINAL_BODY"))

# Delete v06 brow experiments; create a new modelled expression with all
# facial components correctly positioned on the replacement skin surface.
for o in list(bpy.data.objects):
    if o.name.startswith("NEVALI_EYEBROW_"):
        bpy.data.objects.remove(o,do_unlink=True)

# --- EXCISION: Physically delete the corrupt original facial triangles ---
# Original GLB is one material, one body mesh (hair, costume and face merged).
# Cut a symmetric oval centered on the central visage; protect upper fringe
# and ears at +/-0.165m. Do not merely hide or cover the damaged old pupils.
orig_faces=len(body.data.polygons)
bm=bmesh.new()
bm.from_mesh(body.data)
bm.faces.ensure_lookup_table()
cut_faces=[]
for face in bm.faces:
    c=face.calc_center_median()
    # ellipse with 0.168m side radius, 0.141m vertical half-radius.
    ell=(c.x/.168)**2+((c.z-1.602)/.142)**2
    if c.y < -.145 and ell < 1.0 and 1.460<c.z<1.745:
        cut_faces.append(face)
assert 200<len(cut_faces)<4000,len(cut_faces)
bmesh.ops.delete(bm,geom=cut_faces,context='FACES')
bm.to_mesh(body.data);bm.free()
body.data.update()
deleted=len(cut_faces)
print("REMOVED_ORIGINAL_FACE_TRIANGLES",deleted,"original polygons",orig_faces,"retained",len(body.data.polygons))

def mat(name,color,rough=0.84,metallic=0):
    m=bpy.data.materials.new(name)
    m.diffuse_color=(*color,1.0)
    m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=rough
    bs.inputs['Metallic'].default_value=metallic
    return m

skin=mat('NEVALI_v11_SculptedSkin_NoPhoto',(0.855,.585,.462))
skin_h=mat('NEVALI_v11_NoseBridgeSkin',(0.86,.59,.469))
socket=mat('NEVALI_v09_EyelidWarmShadow',(.48,.268,.215))
lash=mat('NEVALI_v09_EyelashesDarkChestnut',(.105,.057,.038))
brown=mat('NEVALI_v09_IrisAmberBrown',(.26,.122,.063),.27)
iris_ring=mat('NEVALI_v09_IrisOuterRing',(.118,.061,.040),.39)
pupil=mat('NEVALI_v09_EyePupilDark',(.025,.015,.012),.22)
sclera=mat('NEVALI_v10_ScleraIvory',(.93,.86,.79),.24)
shine=mat('NEVALI_v09_CatchlightWhite',(1.,.956,.88),.14)
lip=mat('NEVALI_v11_SoftRoseLips',(.68,.382,.35),.83)
lip_h=mat('NEVALI_v11_LowerLipTint',(.80,.495,.445),.82)
blush=mat('NEVALI_v09_CheekWarmBlush',(.82,.38,.31),.95)

def mesh_obj(name,verts,faces,material,smoothed=True):
    me=bpy.data.meshes.new(name+"_Topology")
    me.from_pydata(verts,[],faces)
    me.update()
    obj=bpy.data.objects.new(name,me)
    scene.collection.objects.link(obj)
    me.materials.append(material)
    if smoothed:
        for p in me.polygons:p.use_smooth=True
    return obj

def uv_orb(name,xyz,scale,material,seg=20,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,
                                         location=xyz)
    o=bpy.context.object
    o.name=name
    o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    for p in o.data.polygons:p.use_smooth=True
    return o

def curve_line(name,coords,radius,material,cyclic=False):
    cr=bpy.data.curves.new(name+" Geometry","CURVE")
    cr.dimensions='3D'
    cr.resolution_u=12
    cr.bevel_depth=radius
    cr.bevel_resolution=2
    path=cr.splines.new('POLY')
    path.points.add(len(coords)-1)
    for pt,p in zip(path.points,coords):pt.co=(*p,1)
    path.use_cyclic_u=cyclic
    o=bpy.data.objects.new(name,cr)
    scene.collection.objects.link(o)
    cr.materials.append(material)
    # Convert to mesh so both Blender and GLB have genuine polygon geometry.
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH')
    return bpy.context.object

def baseline(x,z):
    # Child face follows a gently rounded convex cheek/chin structure.
    # Front is NEGATIVE Y. This is new geometry, not a texture projection.
    u=abs(x)/.172
    zz=z-1.602
    return (-.262+.071*u*u+.035*((zz/.148)**2)
            - .013*math.exp(-((z-1.569)/.032)**2)*math.exp(-(x/.047)**2))

# True closed, rounded low-poly child head, replacing the cut-out original
# upper face and jaw. This is a COMPLETE 3D volume with back/skull curvature,
# never a front-facing plane or image. Original hair and ears remain outside.
bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=28,
                                     location=(0,-.137,1.625))
face=bpy.context.object
face.name="NEVALI_v11_NEW_ROUNDED_3D_HEAD"
face.scale=(.158,.146,.152)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
face.data.materials.append(skin)
for poly in face.data.polygons:poly.use_smooth=True
face["REPAIR_METHOD"]="Fully modeled 3D closed child head volume after deleting old facial polygons"

# Eyes are actual convex 3D geometry, with sclera, lens-shaped dark ring,
# brown irises, solid pupils and white catchlights. Each eyelid is a 3D rim.
for side in (-1,1):
    eye_x=.078*side
    ex=eye_x
    ez=1.636
    # Place eyeball just ahead of facial surface: not an image or a decal.
    uv_orb(f"NEVALI_v11_EYEBALL_{'L' if side<0 else 'R'}",
           (ex,-.264,ez),(.032,.011,.021),sclera)
    uv_orb(f"NEVALI_v11_IRIS_OUTER_{'L' if side<0 else 'R'}",
           (ex,-.274,ez),(.020,.0038,.018),iris_ring,24,12)
    uv_orb(f"NEVALI_v11_IRIS_BROWN_{'L' if side<0 else 'R'}",
           (ex,-.278,ez),(.017,.003,.0165),brown,24,12)
    uv_orb(f"NEVALI_v11_PUPIL_{'L' if side<0 else 'R'}",
           (ex,-.282,ez),(.008,.002,.010),pupil,20,10)
    uv_orb(f"NEVALI_v11_CATCHLIGHT_{'L' if side<0 else 'R'}",
           (ex-.005,-.285,ez+.0065),(.003,.0015,.0035),shine,12,8)
    # Eyelid contour: almond outline with outer corner lifted.
    angles=[math.pi*i/16 for i in range(17)]
    top=[]
    low=[]
    for a in angles:
        dx=.036*math.cos(a)
        dz=.023*math.sin(a)
        top.append((ex+dx,-.275+.009*abs(math.cos(a)),ez+dz+.002*(dx/.036)))
    for a in angles:
        dx=.036*math.cos(a)
        dz=.019*math.sin(a)
        low.append((ex-dx,-.272+.010*abs(math.cos(a)),ez-dz))
    curve_line(f"NEVALI_v11_UPPER_EYELID_{side}",top,.0022,lash)
    curve_line(f"NEVALI_v11_LOWER_EYELID_{side}",low,.0019,socket)
    # A few outward lash tips, real tapered 3D pieces.
    for k,dx in enumerate((-.026,-.034)):
        x=ex+dx*side
        up=1.650+.004*(2-k)/3
        curve_line(f"NEVALI_v11_LASH_{side}_{k}",
                   [(x,-.274,up),(x-.004*side,-.270,up+.004+k*.001)],
                   .0011,lash)
    brow=[]
    for i in range(13):
        t=i/12
        x=ex+(t-.5)*.083
        z=1.682+.006*math.sin(t*math.pi)
        y=-.282+.008*(abs(x)/.14)
        brow.append((x,y,z))
    curve_line(f"NEVALI_v11_BROW_SCULPT_{side}",brow,.0019,lash)

# Nose: tapered polygonal bridge, rounded tip and two wings. Modeled
# separately for tweakable front and side silhouette, without a photo.
nrings=[
    (1.590,.005,-.279),
    (1.582,.007,-.283),
    (1.575,.009,-.288),
    (1.568,.010,-.298),
    (1.563,.010,-.281),
]
nv=[];nf=[]
for z,width,front in nrings:
    nv.extend([
      (-width,front+.012,z),(-width*.55,front-.001,z),
      (width*.55,front-.001,z),(width,front+.012,z)
    ])
for row in range(len(nrings)-1):
    for k in range(3):
        i=row*4+k
        nf.append((i,i+1,i+5,i+4))
nose=mesh_obj("NEVALI_v11_SCULPTED_LOW_POLY_NOSE_BRIDGE",nv,nf,skin_h)
uv_orb("NEVALI_v11_NOSE_ROUND_TIP",(0,-.294,1.565),
       (.009,.0035,.006),skin_h,12,8)
for side in (-1,1):
    uv_orb(f"NEVALI_v11_NOSE_WING_{side}",
           (side*.009,-.284,1.560),(.004,.002,.0035),skin,12,8)
    uv_orb(f"NEVALI_v11_SUBTLE_NOSTRIL_{side}",
           (side*.007,-.288,1.558),(.0015,.0006,.0009),socket,10,6)

# Actual slightly upturned mouth curve and physically shaded lip surfaces.
# No photo plane, no decal and no original Rodin mouth geometry survives.
def smile(x):
    t=abs(x)/.033
    return 1.532+.004*(t**1.7)
mouth=[(x,-.269+abs(x)*.12,smile(x)) for x in
       [-.031,-.027,-.020,-.011,0,.011,.020,.027,.031]]
curve_line("NEVALI_v11_MOUTH_SMILE_SEAM",mouth,.0012,socket)
upper=[(x,y+.0005,smile(x)+.0016*(1-abs(x)/.031)) for x,y,_ in mouth]
lower=[(x,y+.0007,smile(x)-.0022*(1-abs(x)/.031)) for x,y,_ in mouth]
curve_line("NEVALI_v11_UPPER_LIP",upper,.0016,lip)
curve_line("NEVALI_v11_LOWER_LIP",lower,.0019,lip_h)

# Ensure the v08 *texture-projection* object is nowhere in v09.
assert all("REPROJECT" not in o.name for o in bpy.data.objects)
assert "NEVALI_v11_NEW_ROUNDED_3D_HEAD" in bpy.data.objects
assert all(not o.name.startswith("NEVALI_FACE_EYES_NOSE_MOUTH")
           for o in bpy.data.objects)

scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1000
scene.render.resolution_y=960
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
cam=next(o for o in bpy.data.objects if o.type=='CAMERA')
scene.camera=cam
cam.data.type='ORTHO'
def capture(name,position,target,scale):
    cam.location=position
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.ortho_scale=scale
    scene.render.filepath=str(QA/name)
    bpy.ops.render.render(write_still=True)
    print("RENDERED",name)

capture("Nevali_v11_FrontFace.png",(0,-2.5,1.65),(0,-.07,1.63),.59)
capture("Nevali_v11_ThreeQuarterFace.png",(.8,-2.1,1.78),(0,-.07,1.63),.62)
capture("Nevali_v11_SideFace.png",(2.1,-.02,1.66),(0,-.05,1.63),.63)
capture("Nevali_v11_FullBody.png",(0,-3.7,.95),(0,0,.95),2.45)

blend=WORK/"Nevali_TrueMeshFace_Rebuild_v11.blend"
glb=WORK/"Nevali_TrueMeshFace_Rebuild_v11.glb"
scene.render.filepath=""
cam.location=(0,-2.5,1.65)
cam.rotation_euler=(Vector((0,-.07,1.63))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.ortho_scale=.59
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)

# In the portable GLB, every individual eye/lip/skin element is a physical
# mesh. No reference images, camera, stage lights or other QA helpers.
bpy.ops.object.select_all(action='DESELECT')
physical=[o for o in bpy.data.objects if o.type=='MESH']
for o in physical:o.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',
                          use_selection=True,export_apply=True)
report=dict(
    output_blend=str(blend),output_glb=str(glb),
    original_face_polygons=orig_faces,
    original_face_polygons_physically_deleted=deleted,
    retained_body_polygons=len(body.data.polygons),
    sculpted_face_vertices=len(face.data.vertices),
    modeled_nose_vertices=len(nose.data.vertices),
    separate_geometry_objects=len(physical),
    image_face_projection_used=False,
    is_rigged=False,
    reference_images_not_embedded_on_face=True,
    original_hair_protrusions_still_unpolished=True,
    disclaimer="Stylized defense prototype: facial expressions, lip rig, hair and eyes require animation QA"
)
(QA/"GeometryBuildQA.json").write_text(json.dumps(report,indent=2))
print("NEVALI_V11_ROUNDED_HEAD_BUILT",json.dumps(report))
