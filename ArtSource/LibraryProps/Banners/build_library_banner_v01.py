"""SPELLSTRIKE study-library hanging banner.

Build a separate editable Blender file and a Unity-ready GLB. Designed from the
finalized Panel 08 banner: navy velvet, eight-point gold compass, brass rod,
small side stars, gold hems and V-shaped fringed bottom. No Rodin credits.

Run:
  blender -b -t 4 --factory-startup --python build_library_banner_v01.py

Does not load, modify or save any existing project/Unity scene.
"""
import bpy
import math
from array import array
from pathlib import Path
from mathutils import Vector

SRC = Path(__file__).resolve().parent
PROJECT = SRC.parents[2]
TARGET = PROJECT / "Assets/Cinematics/Storyboard3D/Environment/Props/Banners"
PREVIEW = PROJECT / "Library/StoryboardBannerPreview_20261022"
for p in [SRC, TARGET, PREVIEW]:
    p.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 900
scene.render.resolution_y = 1200
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.view_settings.view_transform = "Standard"
scene.view_settings.exposure = -0.2
scene.render.image_settings.color_mode='RGBA'
scene.world = bpy.data.worlds.new("BannerPreviewWorld")
scene.world.color = (.16, .17, .21)

asset_col = bpy.data.collections.new("BANNER_NAVYGOLD_ASSET")
scene.collection.children.link(asset_col)
stage_col = bpy.data.collections.new("PREVIEW_ONLY_NotExported")
scene.collection.children.link(stage_col)

def move_to(obj, collection=asset_col):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    collection.objects.link(obj)
    return obj

def mat(name, color, metallic=0, roughness=.75):
    m=bpy.data.materials.new(name)
    m.diffuse_color=(*color,1)
    m.use_nodes=True
    p=m.node_tree.nodes.get("Principled BSDF")
    p.inputs["Base Color"].default_value=(*color,1)
    p.inputs["Metallic"].default_value=metallic
    p.inputs["Roughness"].default_value=roughness
    return m

navy=mat("FABRIC_Navy_Deep",(.016,.039,.100),0,.92)
bronze=mat("METAL_Aged_Brass",(.57,.325,.103),.71,.33)
gold=mat("METAL_Embroidery_Gold",(.61,.31,.052),.65,.46)
gold_light=mat("METAL_Compass_Highlights",(.75,.44,.10),.55,.39)
gold_shadow=mat("METAL_Compass_Shadow",(.38,.195,.07),.68,.48)
wood=mat("WOOD_Dark_Walnut",(.075,.035,.024),0,.76)
dark=mat("METAL_Darkened_Iron",(.045,.051,.065),.76,.47)
navy_back=mat("FABRIC_Back_Navy_Unembroidered",(.014,.034,.090),0,.93)

# Tiny procedural weave in a compact shared diffuse map, embedded in GLB. The
# texture does not encode compass ornament: golden geometry stays editable.
W,H=512,1024
img=bpy.data.images.new("Banner_Navy_Woven_512x1024",width=W,height=H,alpha=False)
img.filepath_raw=str(SRC/"Banner_Navy_Woven_512x1024.png")
img.file_format="PNG"
pixels=array('f')
for j in range(H):
    v=j/(H-1)
    for i in range(W):
        u=i/(W-1)
        weave=.035*((i+j)%2)-.0175
        vertical=.045*math.sin(2*math.pi*u*5.2)
        soft=.025*math.sin(2*math.pi*v*1.65+4*u)
        edge=-.035*(abs(2*u-1)**4)
        shade=1+weave+vertical+soft+edge
        pixels.extend((.006*shade,.020*shade,.066*shade,1.0))
img.pixels.foreach_set(pixels)
img.save()
img.pack()
bsdf=navy.node_tree.nodes.get("Principled BSDF")
tex=navy.node_tree.nodes.new("ShaderNodeTexImage")
tex.image=img
navy.node_tree.links.new(tex.outputs["Color"],bsdf.inputs["Base Color"])

def mesh(name,vertices,faces,material,uvs=None):
    m=bpy.data.meshes.new(name+"_Mesh")
    m.from_pydata(vertices,[],faces);m.update()
    o=bpy.data.objects.new(name,m)
    asset_col.objects.link(o)
    m.materials.append(material)
    if uvs is not None:
        uv=m.uv_layers.new(name="UV0")
        for face in m.polygons:
            for li in face.loop_indices:
                vi=m.loops[li].vertex_index
                uv.data[li].uv=uvs[vi]
    return o

def tube(name,points,material,radius=.009,resolution=2,cyclic=False):
    cu=bpy.data.curves.new(name+"_Curve","CURVE")
    cu.dimensions="3D";cu.resolution_u=1
    cu.bevel_depth=radius;cu.bevel_resolution=resolution
    sp=cu.splines.new("POLY");sp.points.add(len(points)-1)
    for p,v in zip(sp.points,points):
        p.co=(*v,1)
    sp.use_cyclic_u=cyclic
    obj=bpy.data.objects.new(name,cu)
    asset_col.objects.link(obj)
    cu.materials.append(material)
    return obj

def cylinder(name, center, radius, length, material, vertices=16, along_x=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=length,location=center)
    o=bpy.context.object;o.name=name
    if along_x:o.rotation_euler.y=math.pi/2
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    move_to(o);o.data.materials.append(material)
    return o

def ico(name,center,scale,material,subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions,radius=1,location=center)
    o=bpy.context.object;o.name=name
    o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    move_to(o);o.data.materials.append(material)
    return o

# Front -Y, wall behind at +Y; units=meters. Pivot/asset origin is at
# cloth symmetry line and bottom extension z=0. Future Unity wall mounting
# only requires translating/rotating one prefab.
HALF=.52
TOP=1.86
EDGE_Z=.53
V_TIP_Z=.22
NC=28;NR=32

def bottom_z(x):
    return V_TIP_Z+(EDGE_Z-V_TIP_Z)*abs(x)/HALF

def fabric_y(x,t):
    # Fold depth is intentionally restrained so the metallic compass stays
    # readable from shots near the front.
    u=(x/HALF+1)/2
    return -.055+(.018+.017*t)*math.sin(4.1*math.pi*u+.45*t) + .007*math.sin(12*math.pi*u)

front=[];back=[];uvs=[]
for row in range(NR+1):
    t=row/NR
    for col in range(NC+1):
        x=-HALF+2*HALF*col/NC
        z=bottom_z(x)+(TOP-bottom_z(x))*t
        front.append((x,fabric_y(x,t),z))
        back.append((x,fabric_y(x,t)+.012,z))
        uvs.append((col/NC,t))
faces=[]
STRIDE=NC+1
FRONT_COUNT=len(front)
for row in range(NR):
    for col in range(NC):
        a=row*STRIDE+col;b=a+1;c=a+STRIDE;d=c+1
        faces.extend([(a,b,d),(a,d,c)])
        faces.extend([(FRONT_COUNT+d,FRONT_COUNT+b,FRONT_COUNT+a),
                      (FRONT_COUNT+c,FRONT_COUNT+d,FRONT_COUNT+a)])
for row in range(NR):
    for col in [0,NC]:
        a=row*STRIDE+col;c=a+STRIDE
        faces.append((a,c,FRONT_COUNT+c,FRONT_COUNT+a))
for col in range(NC):
    a=col;b=col+1
    faces.append((a,FRONT_COUNT+a,FRONT_COUNT+b,b))
    a=NR*STRIDE+col;b=a+1
    faces.append((a,b,FRONT_COUNT+b,FRONT_COUNT+a))
cloth=mesh("FABRIC_01_TaperedV_Navy",front+back,faces,navy,uvs+uvs)
cloth.data.materials.append(navy_back)
# Four triangles per front/back grid cell: 2 front, 2 back.
for i in range(NR*NC):
    cloth.data.polygons[4*i+2].material_index=1
    cloth.data.polygons[4*i+3].material_index=1
cloth["banner_role"]="Separate reusable cloth mesh; can be rigged for subtle sway."
for f in cloth.data.polygons:
    f.use_smooth=True

# Reinforced front gold edging. The curtain retains genuine low-poly volume.
def at(x,z,forward=-.010):
    t=max(0,min(1,(z-bottom_z(x))/(TOP-bottom_z(x))))
    return (x,fabric_y(x,t)+forward,z)

tube("EMBROIDERY_LeftEdge",[at(-HALF+.022,EDGE_Z+i*(TOP-EDGE_Z)/64,-.016) for i in range(65)],gold,.010)
tube("EMBROIDERY_RightEdge",[at(HALF-.022,EDGE_Z+i*(TOP-EDGE_Z)/64,-.016) for i in range(65)],gold,.010)
bottom_path=[at(-HALF+2*HALF*i/56,bottom_z(-HALF+2*HALF*i/56)+.024,-.014) for i in range(57)]
tube("EMBROIDERY_BottomV",bottom_path,gold_light,.014)
tube("EMBROIDERY_TopHem",[(x,-.118,TOP-.026) for x in [-.52+i*1.04/40 for i in range(41)]],gold,.012)

# Fringe thread strips along the entire downward-V hem, including a soft
# woven golden hem-band. Kept in a single curves object for efficient reuse.
threads=bpy.data.curves.new("GoldFringeThreadCurves","CURVE")
threads.dimensions="3D";threads.bevel_depth=.0020;threads.bevel_resolution=1
for i in range(57):
    x=-HALF+.012+i*(1.04-.024)/56
    z=bottom_z(x)+.022
    y=at(x,z,-.014)[1]
    sp=threads.splines.new('POLY');sp.points.add(1)
    sp.points[0].co=(x,y,z,1)
    sp.points[1].co=(x+.003*math.sin(i*1.27),y-.002,z-.058-.018*math.sin(i*.68)**2,1)
obj=bpy.data.objects.new("FRINGE_Gold_57Threads",threads)
asset_col.objects.link(obj);threads.materials.append(gold_light)

# Main brass-mounted dark walnut pole with blue roll and symmetric finials.
cylinder("HANGER_WalnutRod",(0,.017,1.96),.041,1.26,wood,vertices=16,along_x=True)
for x in [-.55,-.43,.43,.55]:
    cylinder("HANGER_BrassRing_"+str(x),(x,.017,1.96),.048,.028,bronze,16,along_x=True)
for s in [-1,1]:
    cylinder("HANGER_EndCollar_"+str(s),(.623*s,.017,1.96),.057,.045,gold,16,along_x=True)
    ico("HANGER_FacetedFinial_"+str(s),(.68*s,.017,1.96),(.065,.059,.059),gold_light,1)
for x in [-.31,.31]:
    tube("HANGER_ClothLoop_"+str(x),[
        (x-.043,.011,TOP+.008),(x-.043,-.061,1.97),
        (x-.043,-.008,2.016),(x-.043,.068,1.967),(x-.043,.020,TOP+.008)
    ],navy,.018)

# Compass at upper mid-panel: twin circular aged-gold rings plus 8-point
# cardinal star, exactly the dominant emblem on Panel 08.
STAR_Z=1.20
STAR_Y=-.123
ring_r=.288
for name,r,width,material in [('CompassOuterRing',ring_r,.013,bronze),
                               ('CompassInnerRing',ring_r-.024,.006,gold_light)]:
    tube(name,[(r*math.sin(t*2*math.pi/128),STAR_Y+.006,STAR_Z+r*math.cos(t*2*math.pi/128))
              for t in range(128)],material,width,cyclic=True)

def star_mesh(name,radii,widths,y,center_z,center_x=0.0,bright=gold_light,shadow=gold_shadow):
    verts=[];faces=[];mids=[]
    for k in range(8):
        a=k*math.pi/4;dx=math.sin(a);dz=math.cos(a)
        px=dz;pz=-dx
        rad=radii[k];wide=widths[k]
        # middle ridge slightly raised from fabric, metallic star facets
        c=(center_x,y+.012,center_z)
        l=(center_x+px*wide+dx*.045,y,center_z+pz*wide+dz*.045)
        r=(center_x-px*wide+dx*.045,y,center_z-pz*wide+dz*.045)
        tip=(center_x+dx*rad,y+.008,center_z+dz*rad)
        ridge=(center_x+dx*rad*.50,y-.021,center_z+dz*rad*.50)
        off=len(verts);verts.extend([c,l,r,tip,ridge])
        faces.extend([(off,off+1,off+4),(off,off+4,off+2),
                      (off+1,off+3,off+4),(off+4,off+3,off+2)])
        mids.extend([0,1,0,1])
    m=bpy.data.meshes.new(name+"Mesh")
    m.from_pydata(verts,[],faces);m.update()
    ob=bpy.data.objects.new(name,m);asset_col.objects.link(ob)
    m.materials.append(bright);m.materials.append(shadow)
    for f,mid in zip(m.polygons,mids):f.material_index=mid
    return ob

star_mesh("HERALDRY_EightPoint_CompassStar",
          [.383,.260,.383,.260,.383,.260,.383,.260],
          [.067,.035,.067,.035,.067,.035,.067,.035],
          STAR_Y-.035,STAR_Z)
ico("HERALDRY_CenterFacetedGold", (0,STAR_Y-.056,STAR_Z),(.056,.018,.056),gold_light,1)
for k in range(8):
    a=k*math.pi/4
    # Gold compass ticks around ring match the panel's engraved wheel motifs.
    s=math.sin(a);c=math.cos(a)
    tube("HERALDRY_RadialTick_%02d"%k,
         [(s*.275,STAR_Y,STAR_Z+c*.275),(s*.314,STAR_Y,STAR_Z+c*.314)],
         gold,.007)
    if k%2==1:
        ico("HERALDRY_TipStud_%02d"%k,(s*.314,STAR_Y,STAR_Z+c*.314),
            (.016,.010,.016),gold_light,0)

def small_star(prefix,x,z,size):
    star_mesh(prefix,[size,size*.61]*4,[size*.20,size*.125]*4,
              STAR_Y-.018,z,center_x=x)

# Matching smaller lower stars and central lozenge from Panel 08.
small_star("HERALDRY_LowerLeftStar",-.355,.718,.047)
small_star("HERALDRY_LowerRightStar",.355,.718,.047)
verts=[(0,STAR_Y-.039,.56),(.035,STAR_Y-.015,.49),
       (0,STAR_Y-.044,.420),(-.035,STAR_Y-.015,.49),
       (0,STAR_Y-.060,.49)]
m=mesh("HERALDRY_LowerDiamond",verts,
       [(0,1,4),(1,2,4),(2,3,4),(3,0,4)],gold_light)

# Woven outline stitches, front-facing; enough detail for over-shoulder
# cinematic shots without unique 2K textures for each repeated banner.
border_orn=bpy.data.curves.new("EdgeStitches","CURVE")
border_orn.dimensions="3D";border_orn.bevel_depth=.0027
for sx in [-1,1]:
    for n in range(24):
        z=.64+n*(1.1/24)
        x=sx*(HALF-.056)
        y=at(x,z,-.021)[1]
        sp=border_orn.splines.new("POLY")
        sp.points.add(1)
        sp.points[0].co=(x-sx*.009,y,z-.011,1)
        sp.points[1].co=(x+sx*.009,y,z+.011,1)
obj=bpy.data.objects.new("EMBROIDERY_HandStitchDetails",border_orn)
asset_col.objects.link(obj);border_orn.materials.append(gold)

# A durable Empty root for easy placement and duplication in Unity.
root=bpy.data.objects.new("LibraryBanner_NavyGold_v01",None)
asset_col.objects.link(root)
root["purpose"]="Reusable cinematic library banner, shot-match to finalized Panel 08"
root["authoring_units"]="meters; +Z up; front faces -Y"
root["rodin_credits"]="0 - procedural Blender construction"
root["allowed_animation"]="Separate FABRIC_01 mesh for optional later sway"
for ob in list(asset_col.objects):
    if ob is not root:
        ob.parent=root

# Preview camera/lights are not part of export.
def light(name,location,power,color=(1,1,1),size=2):
    d=bpy.data.lights.new(name,"AREA");d.energy=power;d.shape='DISK';d.size=size
    d.color=color
    o=bpy.data.objects.new(name,d);stage_col.objects.link(o)
    o.location=location;o.rotation_euler=(Vector((0,0,1.06))-o.location).to_track_quat('-Z','Y').to_euler()
    return o
light("Warm Key",(-1.6,-2.0,3.1),340,(1,.84,.62),2.4)
light("Cool Fill",(1.4,-.9,1.65),160,(.57,.73,1),2.4)
light("Back Rim",(0.6,1.4,2.4),260,(1,.82,.50),1.7)
camera_data=bpy.data.cameras.new("PreviewCamera")
camera=bpy.data.objects.new("PreviewCamera",camera_data)
stage_col.objects.link(camera)
scene.camera=camera
camera_data.type="ORTHO"
camera_data.ortho_scale=2.55
scene.render.resolution_x=900;scene.render.resolution_y=1200

def render_view(name,loc,target,scale):
    camera.location=loc
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat("-Z","Y").to_euler()
    camera_data.ortho_scale=scale
    scene.render.filepath=str(PREVIEW/(name+".png"))
    bpy.ops.render.render(write_still=True)

render_view("LibraryBanner_NavyGold_v01_Front",(0,-6,1.23),(0,0,1.08),2.50)
render_view("LibraryBanner_NavyGold_v01_ThreeQuarter",(2.0,-5.6,1.63),(0,0,1.03),2.58)
render_view("LibraryBanner_NavyGold_v01_Back",(0,6,1.27),(0,0,1.06),2.50)

# BLEND remains editable, with material and geometry separation.
scene.render.filepath=str(PREVIEW/"LibraryBanner_NavyGold_v01_Front.png")
for area in bpy.context.screen.areas:
    if area.type=="VIEW_3D":
        space=area.spaces.active
        space.shading.type='MATERIAL'
        space.overlay.show_overlays=False
        space.region_3d.view_location=(0,0,1.07)
        space.region_3d.view_distance=3.0
        space.region_3d.view_rotation=(Vector((0,0,1.07))-Vector((0,-5,1.4))).to_track_quat('-Z','Y')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/"LibraryBanner_NavyGold_v01.blend"),
                            compress=True)

# Unity shipping GLB is streamlined to a handful of material-group meshes
# (the saved source .blend above retains fully editable separate ornaments).
# Curves become meshes in this disposable background Blender process only.
for ob in list(asset_col.objects):
    if ob.type=='CURVE':
        bpy.ops.object.select_all(action='DESELECT')
        ob.select_set(True)
        bpy.context.view_layer.objects.active=ob
        bpy.ops.object.convert(target='MESH')

material_groups={}
for ob in list(asset_col.objects):
    if ob.type!='MESH':continue
    # Keep fabric body separate for any future cloth animation.
    if ob.name.startswith('FABRIC_01_'):
        key='FabricBody'
    else:
        key='Other_'+(ob.data.materials[0].name if ob.data.materials else 'None')
    material_groups.setdefault(key,[]).append(ob)

for key, obs in material_groups.items():
    if len(obs)<2:continue
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs:ob.select_set(True)
    bpy.context.view_layer.objects.active=obs[0]
    bpy.ops.object.join()
    obs[0].name='PACKED_'+key

bpy.ops.object.select_all(action="DESELECT")
for ob in asset_col.objects:
    ob.select_set(True)
bpy.context.view_layer.objects.active=root
glb=TARGET/"LibraryBanner_NavyGold_v01.glb"
bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',
                          use_selection=True,export_apply=True)

tri_count=sum(sum(len(poly.vertices)-2 for poly in obj.data.polygons)
              for obj in asset_col.objects if obj.type=='MESH')

# An actual reduced-geometry counterpart, to assign as LOD1 in Unity.
# Deliberately preserve gold silhouettes more than the already dense cloth.
for obj in list(asset_col.objects):
    if obj.type!="MESH":
        continue
    if "FabricBody" in obj.name or obj.name.startswith("FABRIC_01"):
        ratio=.32
    elif "Compass_Highlights" in obj.name:
        ratio=.69
    else:
        ratio=.50
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new("LOD1_MeshReduce_Baked","DECIMATE")
    mod.ratio=ratio
    bpy.ops.object.modifier_apply(modifier=mod.name)

lod_tri_count=sum(sum(len(p.vertices)-2 for p in obj.data.polygons)
                  for obj in asset_col.objects if obj.type=="MESH")
bpy.ops.object.select_all(action="DESELECT")
for obj in asset_col.objects:obj.select_set(True)
bpy.context.view_layer.objects.active=root
lod_glb=TARGET/"LibraryBanner_NavyGold_v01_LOD1.glb"
bpy.ops.export_scene.gltf(filepath=str(lod_glb),export_format="GLB",
                          use_selection=True,export_apply=True)
render_view("LibraryBanner_NavyGold_v01_LOD1_ThreeQuarter",
            (2,-5.6,1.63),(0,0,1.03),2.58)
metrics={
    "source":str(SRC/"LibraryBanner_NavyGold_v01.blend"),
    "unity_model":str(glb),
    "unity_lod1_model":str(lod_glb),
    "front_preview":str(PREVIEW/"LibraryBanner_NavyGold_v01_Front.png"),
    "measured_lod0_triangles":tri_count,
    "measured_lod1_triangles":lod_tri_count,
    "mesh_objects":sum(obj.type=='MESH' for obj in asset_col.objects),
    "curve_objects":sum(obj.type=='CURVE' for obj in asset_col.objects),
    "materials":sorted(m.name for m in bpy.data.materials),
    "main_cloth_width_m":HALF*2,
    "top_rod_height_m":1.96,
    "lowest_fabric_m":V_TIP_Z,
    "preview_scene_only_items_not_exported":len(stage_col.objects)
}
import json
(PREVIEW/"LibraryBanner_NavyGold_v01_AUDIT.json").write_text(json.dumps(metrics,indent=2))
print("BANNER_COMPLETE",json.dumps(metrics))
