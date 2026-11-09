"""Create a production-ready modular SPELLSTRIKE study chandelier.

Based on the suspended warm brass/black iron candle ring in the actual
finalized storyboard Panel 05. Does NOT include flames or Unity lights.
Standalone Blender 5.0 script; no Rodin, no existing scenes edited.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[2]
ASSETS = PROJECT/"Assets/Cinematics/Storyboard3D/Environment/Props/Chandeliers"
PREVIEW = PROJECT/"Library/StoryboardChandelierPreview_20261022"
for folder in (OUT,ASSETS,PREVIEW):
    folder.mkdir(parents=True,exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.engine="BLENDER_EEVEE"
scene.render.resolution_x=900
scene.render.resolution_y=1000
scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"
scene.view_settings.view_transform="Standard"
scene.view_settings.look="Medium High Contrast"
scene.view_settings.exposure=-.20
scene.world=bpy.data.worlds.new("Chandelier_Studio_World")
scene.world.color=(.13,.13,.13)

assets=bpy.data.collections.new("CHANDLIER_STUDY_ASSET")
scene.collection.children.link(assets)
studio=bpy.data.collections.new("PREVIEW_ONLY_NOT_EXPORTED")
scene.collection.children.link(studio)

def move(obj, collection=assets):
    for c in list(obj.users_collection):c.objects.unlink(obj)
    collection.objects.link(obj)
    return obj

def material(name,albedo,metallic,roughness):
    m=bpy.data.materials.new(name)
    m.diffuse_color=(*albedo,1)
    m.use_nodes=True
    bsdf=m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value=(*albedo,1)
    bsdf.inputs["Metallic"].default_value=metallic
    bsdf.inputs["Roughness"].default_value=roughness
    return m

black_iron=material("METAL_BlackenedIron",(.075,.064,.055),.72,.37)
aged_brass=material("METAL_AgedBrass",(.47,.255,.077),.85,.34)
gold_trim=material("METAL_BrassHighlight",(.76,.47,.17),.80,.28)
wax=material("WAX_Cream_Unlit",(.78,.67,.48),0,.78)
wick=material("WICK_Charcoal_Unlit",(.038,.026,.020),0,.91)

root=bpy.data.objects.new("StudyChandelier_BrassRing_v01_CEILING_ANCHOR",None)
assets.objects.link(root)
root["Reference"]="Finalized Storyboard Panel 05, circular chandelier above study table"
root["Mount"]="Place origin on ceiling, Z = 0 in Blender; model extends down along -Z"
root["UnityVFX"]="Attach separate candle flames and optional realtime light; no fire mesh"
root["Animation"]="Static mesh; distinct flame anchors exported as empty nodes"

def parent(obj):
    obj.parent=root
    return obj

def torus(name,R,r,z,mat,major=64,minor=6,x=0,y=0):
    bpy.ops.mesh.primitive_torus_add(major_radius=R,minor_radius=r,
          major_segments=major,minor_segments=minor,location=(x,y,z))
    ob=move(bpy.context.object);ob.name=name
    ob.data.materials.append(mat);parent(ob)
    return ob

def cone(name,center,r1,r2,depth,mat,sides=12):
    bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=r1, radius2=r2,
                                    depth=depth, location=center)
    ob=move(bpy.context.object);ob.name=name
    ob.data.materials.append(mat);parent(ob)
    return ob

def ball(name,center,scale,mat):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=center)
    ob=move(bpy.context.object);ob.name=name;ob.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    ob.data.materials.append(mat);parent(ob)
    return ob

def pipe(name,points,mat,radius=.01,curve_res=1,cyclic=False):
    curve=bpy.data.curves.new(name+"Data","CURVE")
    curve.dimensions="3D"
    curve.resolution_u=1
    curve.bevel_depth=radius
    curve.bevel_resolution=curve_res
    spline=curve.splines.new("POLY")
    spline.points.add(len(points)-1)
    for point,xyz in zip(spline.points,points):point.co=(*xyz,1)
    spline.use_cyclic_u=cyclic
    ob=bpy.data.objects.new(name,curve)
    assets.objects.link(ob)
    curve.materials.append(mat)
    parent(ob)
    return ob

def rod(name,p0,p1,r,mat,sides=10):
    p0=Vector(p0);p1=Vector(p1)
    tangent=p1-p0
    ob=cone(name,(p0+p1)/2,r,r,tangent.length,mat,sides)
    ob.rotation_euler=tangent.to_track_quat('Z','Y').to_euler()
    return ob

def ring_mesh(name,cx,cy,z,profile,mat,segments=12):
    """Lathed cup/profile with open top, suitable for actual separate candles."""
    verts=[];faces=[]
    for height,radius in profile:
        for i in range(segments):
            a=2*math.pi*i/segments
            verts.append((cx+radius*math.cos(a),cy+radius*math.sin(a),z+height))
    for ring in range(len(profile)-1):
        for i in range(segments):
            a=ring*segments+i;b=ring*segments+(i+1)%segments
            c=(ring+1)*segments+(i+1)%segments;d=(ring+1)*segments+i
            faces.extend([(a,b,c),(a,c,d)])
    m=bpy.data.meshes.new(name+"Mesh");m.from_pydata(verts,[],faces);m.update()
    ob=bpy.data.objects.new(name,m);assets.objects.link(ob)
    m.materials.append(mat);parent(ob)
    return ob

R=.73
Z_RING=-1.15
N=8

# The clean ring silhouette, with a dark iron body enclosed by a few aged
# brass rims. Screenshot reference: not a gem/crystal chandelier.
torus("RING_BlackenedIron_Main",R,.043,Z_RING,black_iron,72,8)
torus("RING_GoldTrim_Top",R+.008,.011,Z_RING+.035,gold_trim,72,5)
torus("RING_GoldTrim_Bottom",R+.008,.011,Z_RING-.035,aged_brass,72,5)
torus("RING_InnerReinforcement",R-.073,.014,Z_RING-.016,aged_brass,56,5)

# Central hub and eight straight metal struts maintain a strong readable
# wheel profile when photographed from underneath.
cone("HUB_CastIron",(0,0,Z_RING-.107),.115,.115,.18,black_iron,12)
torus("HUB_GoldEdge",.120,.018,Z_RING-.12,gold_trim,20,5)
for k in range(N):
    theta=(k+.5)*math.tau/N
    c=math.cos(theta);s=math.sin(theta)
    rod(f"SPOKE_GothicIron_{k+1:02d}",
        (.10*c,.10*s,Z_RING-.035),((R-.065)*c,(R-.065)*s,Z_RING-.035),
        .013,black_iron,8)
    # Drip-like little downward brass ornaments under the ring.
    px=(R-.012)*c;py=(R-.012)*s
    ball(f"UNDER_HemStud_{k+1:02d}",(px,py,Z_RING-.068),
         (.038,.038,.036),gold_trim)
    cone(f"UNDER_Finial_{k+1:02d}",(px,py,Z_RING-.127),
         .023,0,.092,aged_brass,8)

# A subtle center drop, unlike a tall chandelier chain that blocks the table.
ball("BOTTOM_FacetedNub",(0,0,Z_RING-.228),(.075,.075,.055),gold_trim)
cone("BOTTOM_DropTaper",(0,0,Z_RING-.340),.056,0,.180,aged_brass,10)
cone("BOTTOM_GoldDropTip",(0,0,Z_RING-.455),.026,0,.092,gold_trim,8)

# Overhead rosette: orientation +Z top; place this at study ceiling level.
cone("CEILING_DarkRosette",(0,0,-.041),.128,.078,.082,black_iron,16)
torus("CEILING_Rim",.104,.013,-.068,gold_trim,24,6)
cone("CEILING_BrassPoint",(0,0,-.135),.068,0,.125,aged_brass,10)

# Four real linked suspension chains. Each 11 overlapping elliptical links
# follow a slanting line from the rosette out to the ring. Different orientation
# by alternating chain link plane for a believable construction.
CHAIN_COUNT=4
LINKS=11
chain_objs=[]
for j in range(CHAIN_COUNT):
    a=(j+.5)*math.tau/CHAIN_COUNT
    rad=Vector((math.cos(a),math.sin(a),0))
    tang=Vector((-math.sin(a),math.cos(a),0))
    top=Vector((rad.x*.055,rad.y*.055,-.155))
    foot=Vector((rad.x*(R-.025),rad.y*(R-.025),Z_RING+.044))
    segment=foot-top
    tangent=segment.normalized()
    for i in range(LINKS):
        t=(i+.45)/(LINKS)
        mid=top+segment*t
        # Align each link along the chain direction, in alternating planes.
        side=(rad if i%2==0 else tang)
        side=(side-side.dot(tangent)*tangent).normalized()
        pts=[]
        for q in range(14):
            phi=math.tau*q/14
            p=mid+tangent*(.061*math.cos(phi))+side*(.028*math.sin(phi))
            pts.append(p)
        ob=pipe(f"CHAIN_{j+1:02d}_Link_{i+1:02d}",pts,aged_brass,.008,1,True)
        chain_objs.append(ob)
    # Rods connecting actual chain to the ring and apex don't rely on VFX.
    ball(f"CHAIN_RingFastener_{j+1:02d}",foot,(.033,.033,.035),gold_trim)
    ball(f"CHAIN_CeilingFastener_{j+1:02d}",top,(.024,.024,.022),aged_brass)

# Eight upward-facing candle holders and unlit wax cylinders.
# Same candle count across real LODs to preserve layout and Unity flame anchors.
flame_markers=[]
for k in range(N):
    a=math.tau*k/N
    rad=Vector((math.cos(a),math.sin(a),0))
    sx=(R-.008)*rad.x;sy=(R-.008)*rad.y
    r_outer=R+.17
    x=r_outer*rad.x;y=r_outer*rad.y
    # Sweeping forged S-arm from the dark ring to the brass saucer.
    arm=[
        (sx,sy,Z_RING+.020),
        ((R+.035)*rad.x,(R+.035)*rad.y,Z_RING+.005),
        ((R+.120)*rad.x,(R+.120)*rad.y,Z_RING+.065),
        (x,y,Z_RING+.185)
    ]
    pipe(f"CANDLE_{k+1:02d}_ForgedCurvedArm",arm,aged_brass,.016,2)
    ball(f"CANDLE_{k+1:02d}_ArmSocket",(sx,sy,Z_RING+.020),
         (.038,.038,.037),gold_trim)

    # Gold holder platter with a raised top lip and hollow upper opening.
    base=Z_RING+.173
    ring_mesh(f"CANDLE_{k+1:02d}_Holder",x,y,base,[
        (-.025,.032),(-.013,.060),(.006,.068),
        (.026,.058),(.044,.039),(.045,.029)
    ],aged_brass,14)
    torus(f"CANDLE_{k+1:02d}_Lip",.053,.009,base+.022,
          gold_trim,20,5,x,y)
    # Cream candle, no flame baked into model.
    candle_base=base+.030
    candle_len=.205
    cone(f"CANDLE_{k+1:02d}_Wax",(x,y,candle_base+candle_len/2),
         .031,.029,candle_len,wax,12)
    torus(f"CANDLE_{k+1:02d}_TopWax",.026,.004,
          candle_base+candle_len-.002,wax,16,4,x,y)
    cone(f"CANDLE_{k+1:02d}_BlackWick",
         (x,y,candle_base+candle_len+.013),.004,.002,.026,wick,6)

    # Exported named GLB Empty node for separate Unity ParticleSystem/VFX.
    marker=bpy.data.objects.new(f"FlameSocket_{k+1:02d}_VFX_ANCHOR",None)
    assets.objects.link(marker);marker.parent=root
    marker.location=(x,y,candle_base+candle_len+.037)
    marker.empty_display_type='SPHERE';marker.empty_display_size=.031
    marker["attach"]="Unity flame particle prefab, warm light optional"
    flame_markers.append(marker)

# Lights/camera solely for preview; they are intentionally not exported.
def area_light(name,position,power,size,color):
    data=bpy.data.lights.new(name,'AREA')
    data.energy=power;data.size=size;data.color=color
    o=bpy.data.objects.new(name,data);studio.objects.link(o)
    o.location=position
    o.rotation_euler=(Vector((0,0,-.85))-o.location).to_track_quat('-Z','Y').to_euler()

area_light('Warm Amber Key',(-2.7,-2.0,1.3),610,3.0,(1.0,.82,.51))
area_light('Warm Side Bounce',(2.2,-1.7,-.7),260,2.8,(1.0,.68,.36))
area_light('Cool Library Fill',(2,2,-.5),330,3.2,(.61,.74,1.0))

camera_data=bpy.data.cameras.new('PreviewOnlyCamera')
camera=bpy.data.objects.new('PreviewOnlyCamera',camera_data)
studio.objects.link(camera);scene.camera=camera
camera_data.type='ORTHO'
def render(name,location,target,scale=2.68):
    camera.location=location
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera_data.ortho_scale=scale
    scene.render.filepath=str(PREVIEW/(name+'.png'))
    bpy.ops.render.render(write_still=True)

render('Chandelier_v01_StoryboardBelow',(2.4,-4.0,-2.4),(0,0,-.95),2.83)
render('Chandelier_v01_ThreeQuarter',(3.8,-4.1,-.38),(0,0,-.89),2.75)
render('Chandelier_v01_Top',(2.3,-2.6,3.9),(0,0,-.86),2.75)

# Source .blend is the fully editable assembly: every metal part, chain,
# candle holder, candle and VFX anchor remains individually selectable.
scene.render.filepath=str(PREVIEW/'Chandelier_v01_ThreeQuarter.png')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'StudyChandelier_BrassRing_v01.blend'),
                            compress=True)

# Shipping GLB: join all mesh by shared material, retain each named VFX anchor.
for ob in list(assets.objects):
    if ob.type=='CURVE':
        bpy.ops.object.select_all(action='DESELECT')
        ob.select_set(True);bpy.context.view_layer.objects.active=ob
        bpy.ops.object.convert(target='MESH')
for ob in list(assets.objects):
    if ob.type=='MESH':
        # Object joins below only group shapes with exactly the same material.
        assert len(ob.data.materials)==1,ob.name
groups={}
for ob in list(assets.objects):
    if ob.type=='MESH':
        m=ob.data.materials[0]
        groups.setdefault(m.name,[]).append(ob)
for name,obs in groups.items():
    if len(obs)<=1:continue
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs:ob.select_set(True)
    bpy.context.view_layer.objects.active=obs[0]
    bpy.ops.object.join()
    obs[0].name='MESH_'+name

def tris():
    return sum(sum(max(0,len(p.vertices)-2) for p in ob.data.polygons)
               for ob in assets.objects if ob.type=='MESH')

def export(name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in assets.objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    path=ASSETS/name
    bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',
                              use_selection=True,export_apply=True)
    return path

lod0=tris()
lod0_path=export('StudyChandelier_BrassRing_v01.glb')

# For the lower detail model, swap 44 small real chain links for four smooth
# cables and remove tiny trimming that cannot read at corridor distances.
for ob in list(assets.objects):
    if ob.type=='MESH' and ob.name=='MESH_METAL_AgedBrass':
        # Chain links are already merged with cups; keep their geometry and
        # simplify it conservatively, rather than destroying candle holders.
        mod=ob.modifiers.new('LOD1_MetalReduce','DECIMATE');mod.ratio=.62
        bpy.ops.object.select_all(action='DESELECT')
        ob.select_set(True);bpy.context.view_layer.objects.active=ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    elif ob.type=='MESH' and ob.name=='MESH_METAL_BlackenedIron':
        mod=ob.modifiers.new('LOD1_IronReduce','DECIMATE');mod.ratio=.70
        bpy.ops.object.select_all(action='DESELECT')
        ob.select_set(True);bpy.context.view_layer.objects.active=ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
lod1=tris()
lod1_path=export('StudyChandelier_BrassRing_v01_LOD1.glb')

# Further reduced distant-shot variant, especially useful when the library
# is rendered at handheld/Android resolutions. All eight flame marker nodes
# remain even though the physical ornament meshes become much simpler.
for ob in list(assets.objects):
    if ob.type!="MESH":continue
    if "AgedBrass" in ob.name:ratio=.41
    elif "BlackenedIron" in ob.name:ratio=.50
    elif "BrassHighlight" in ob.name:ratio=.50
    elif "Cream_Unlit" in ob.name:ratio=.63
    else:ratio=.60
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True);bpy.context.view_layer.objects.active=ob
    mod=ob.modifiers.new('LOD2_DistantReduction','DECIMATE');mod.ratio=ratio
    bpy.ops.object.modifier_apply(modifier=mod.name)
lod2=tris()
lod2_path=export('StudyChandelier_BrassRing_v01_LOD2.glb')
render('Chandelier_v01_LOD2_ThreeQuarter',(3.8,-4.1,-.38),(0,0,-.89),2.75)

audit={
 "storyboard_reference":"Finalized Prologue Panel 05",
 "source_blend":str(OUT/'StudyChandelier_BrassRing_v01.blend'),
 "source_script":str(Path(__file__).resolve()),
 "unity_lod0_glb":str(lod0_path),
 "unity_lod1_glb":str(lod1_path),
 "unity_lod2_glb":str(lod2_path),
 "rodin_credits":0,
 "approx_ring_diameter_m":2*R,
 "approx_candle_outer_diameter_m":2*(R+.17+.07),
 "ceiling_anchor_z_m":0,
 "ring_center_z_m":Z_RING,
 "candle_count":N,
 "flame_anchor_count":len(flame_markers),
 "lod0_triangle_count":lod0,
 "lod1_triangle_count":lod1,
 "lod2_triangle_count":lod2,
 "flames_geometry":False,
 "live_unity_test":False,
}
(PREVIEW/'StudyChandelier_BrassRing_v01_AUDIT.json').write_text(json.dumps(audit,indent=2))
print('CHANDLIER_COMPLETE',json.dumps(audit))
