"""Build the single-library study-to-archive gateway from finalized Panels 16/34.

Standalone reusable structural kit in Blender 5.0.1: open Gothic masonry arch,
navy inner ribs, paired blue-gold columns, no floor, statues, torches or walls.
Renders / editable .blend / standalone GLBs with real reduced LOD meshes.
"""
import bpy
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

# Generate independent clean topology per LOD. Decimating the many separate
# stone voussoirs after joining created invalid sliver triangles.
LOD_LEVEL=int(next((arg.split('=',1)[1] for arg in sys.argv
                    if arg.startswith('lod=')), '0'))
assert LOD_LEVEL in (0,1,2)
ARCH_SEGMENTS={0:72,1:40,2:24}[LOD_LEVEL]

HERE=Path(__file__).resolve().parent
PROJ=HERE.parents[2]
EX=PROJ/"Assets/Cinematics/Storyboard3D/Environment/Architecture/GothicGateway"
QA=PROJ/"Library/StoryboardGatewayPreview_20261022"
for p in (HERE,EX,QA):p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene
s.render.engine="BLENDER_EEVEE"
s.render.resolution_x=1000;s.render.resolution_y=1000
s.render.resolution_percentage=100
s.render.image_settings.file_format='PNG'
s.view_settings.view_transform='Standard'
s.view_settings.look='Medium High Contrast'
s.world=bpy.data.worlds.new("StudioWorld");s.world.color=(.25,.25,.29)

asset=bpy.data.collections.new("STUDY_ARCHIVE_GATEWAY_ASSET")
s.collection.children.link(asset)
preview=bpy.data.collections.new("PREVIEW_ONLY_Not_Exported")
s.collection.children.link(preview)

def material(name, rgb,metal=0.0,rough=.72):
    m=bpy.data.materials.new(name);m.diffuse_color=(*rgb,1)
    m.use_nodes=True
    shader=m.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(*rgb,1)
    shader.inputs['Metallic'].default_value=metal
    shader.inputs['Roughness'].default_value=rough
    return m

stone=material("STONE_LibrarySlate_AshGray",(.095,.105,.116),0,.87)
edge=material("STONE_EdgeWarmGray",(.143,.148,.146),0,.78)
keystone=material("STONE_KeystoneDarkGray",(.100,.109,.124),0,.84)
grout=material("STONE_GroutDark",(.048,.052,.060),0,.95)
navy=material("ENAMEL_DeepNavyArch",(.012,.034,.114),.2,.43)
navy_dark=material("STONE_VaultShadow_NavyBlack",(.011,.025,.062),.08,.86)
gold=material("BRASS_AgedGold",(.40,.225,.070),.76,.42)
gold_hi=material("BRASS_LightGold",(.74,.49,.174),.72,.32)
blue=material("COLUMN_DeepAzure",(.012,.028,.132),.27,.31)

root=bpy.data.objects.new("GothicGateway_StudyToArchive_v01",None)
asset.objects.link(root)
root["storyboard"]="Finalized Panels 16 and 34"
root["origin"]="Floor center at (0,0,0); Blender +Z up; study entrance faces -Y"
root["passage"]="Always open: intended 3.5m+ clear at narrowest cross section"
root["structure"]="Separate stone voussoirs, navy inset, blue columns, gold trims, no hallway wall"
root["credits_used"]="0"

def belong(obj):
    for col in list(obj.users_collection):col.objects.unlink(obj)
    asset.objects.link(obj);obj.parent=root
    return obj

def box(name,loc,dim,mat,bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=belong(bpy.context.object);ob.name=name
    ob.dimensions=dim
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    ob.data.materials.append(mat)
    if bevel:
        mod=ob.modifiers.new('StoneEdgeWear','BEVEL')
        mod.width=bevel;mod.segments=1
        bpy.context.view_layer.objects.active=ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=ob.modifiers.new('SmoothFacets','WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return ob

def cylinder(name,loc,radius,height,mat,segments=16,rot=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments,radius=radius,depth=height,location=loc)
    ob=belong(bpy.context.object);ob.name=name
    if rot:ob.rotation_euler=rot
    ob.data.materials.append(mat)
    return ob

def ring(name,center,radius,small_radius,mat,seg=24):
    bpy.ops.mesh.primitive_torus_add(major_radius=radius,minor_radius=small_radius,
          major_segments=seg,minor_segments=5,location=center)
    ob=belong(bpy.context.object);ob.name=name
    ob.data.materials.append(mat)
    return ob

def mesh(name,verts,faces,mat):
    me=bpy.data.meshes.new(name+"Mesh");me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);asset.objects.link(ob)
    ob.parent=root;me.materials.append(mat);return ob

def tube(name,points,mat,radius=.012,cyclic=False):
    cv=bpy.data.curves.new(name+"Curve","CURVE")
    cv.dimensions='3D';cv.bevel_depth=radius;cv.bevel_resolution=1
    spl=cv.splines.new('POLY');spl.points.add(len(points)-1)
    for dest,xyz in zip(spl.points,points):dest.co=(*xyz,1)
    spl.use_cyclic_u=cyclic
    ob=bpy.data.objects.new(name,cv);asset.objects.link(ob);ob.parent=root
    cv.materials.append(mat)
    return ob

def golden_lozenge(name,x,y,z,size,mat=gold_hi):
    verts=[(x,y-.03,z+size),(x+size*.51,y,z),
           (x,y-.03,z-size),(x-size*.51,y,z),(x,y-.07,z)]
    return mesh(name,verts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],mat)

# START OF ARCH METRICS
# In the actual panel, the clear opening is 3.6-ish m, and the Gothic crest
# is above the main table sight line. Keep the center walkway fully open.
HALF_CLEAR=1.78
SPRING_Z=2.70
APEX_INNER_Z=4.10
ARCH_OUTER_OFFSET=.39
FRONT=-.34
BACK=.32
N=ARCH_SEGMENTS

def arch_path(u):
    """Pointed arch: quadratic half curves meeting at a sharp apex."""
    if u<=.5:
        t=u*2.0
        p0=Vector((-HALF_CLEAR,SPRING_Z))
        p1=Vector((-HALF_CLEAR,3.65))
        p2=Vector((0,APEX_INNER_Z))
    else:
        t=(u-.5)*2.0
        p0=Vector((0,APEX_INNER_Z))
        p1=Vector((HALF_CLEAR,3.65))
        p2=Vector((HALF_CLEAR,SPRING_Z))
    return (1-t)**2*p0+2*(1-t)*t*p1+t*t*p2

def arch_offset(u,d):
    p=arch_path(u)
    pa=arch_path(max(0,u-0.00005))
    pb=arch_path(min(1,u+0.00005))
    tangent=(pb-pa).normalized()
    normal=Vector((-tangent.y,tangent.x))
    return p+normal*d

def strip(name,u0,u1,d0,d1,y0,y1,mat):
    a=arch_offset(u0,d0);b=arch_offset(u0,d1)
    c=arch_offset(u1,d1);d=arch_offset(u1,d0)
    verts=[(a.x,y0,a.y),(b.x,y0,b.y),(c.x,y0,c.y),(d.x,y0,d.y),
           (a.x,y1,a.y),(b.x,y1,b.y),(c.x,y1,c.y),(d.x,y1,d.y)]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),
           (2,3,7,6),(3,0,4,7)]
    return mesh(name,verts,faces,mat)

def crown_fill(name,d0,d1,y0,y1,mat):
    """Close the upper wedge gap where both Gothic half-arcs meet at the tip."""
    eps=.000001
    il=arch_offset(.5-eps,d0);ir=arch_offset(.5+eps,d0)
    ol=arch_offset(.5-eps,d1);orr=arch_offset(.5+eps,d1)
    perimeter=[il,ir,orr,ol]
    n=len(perimeter)
    verts=[(p.x,y0,p.y) for p in perimeter]
    verts.extend((p.x,y1,p.y) for p in perimeter)
    faces=[tuple(range(n)),tuple(range(n*2-1,n-1,-1))]
    for i in range(n):
        j=(i+1)%n
        faces.append((i,n+i,n+j,j))
    return mesh(name,verts,faces,mat)

def arch_band(name,d0,d1,yfront,yback,material,segments=None):
    segments=segments or ARCH_SEGMENTS
    for i in range(segments):
        uu0=(i+.0002)/segments
        uu1=(i+.9998)/segments
        strip(f'{name}_{i:03d}',uu0,uu1,d0,d1,yfront,yback,material)

# Deep black/navy vault band, then royal-navy inset and gold hairlines.
# Arch masonry stays outside the actual walkable opening.
arch_band('VAULT_DarkDeep',0.008,.135,FRONT+.045,BACK+.045,navy_dark)
arch_band('INSET_CobaltBand',.139,.255,FRONT-.030,BACK+.001,navy)
arch_band('STONE_Voussoir',.270,.615,FRONT-.065,BACK+.070,stone)
crown_fill('CROWN_VaultShadow',.008,.136,FRONT+.045,BACK+.045,navy_dark)
crown_fill('CROWN_NavyKeystone',.136,.268,FRONT-.030,BACK+.001,navy)
crown_fill('CROWN_GrayKeystone',.267,.621,FRONT-.065,BACK+.070,stone)
# Subtle intermediate border blocks along the front edge.
for i in range(N):
    u=(i+.5)/N
    p=arch_offset(u,.59)
    golden=False
    if LOD_LEVEL!=2 and i%9==0:
        golden=True
    if golden:
        strip(f'ARCH_StoneTrimBlock_{i:02d}',
              (i+.12)/N,(i+.88)/N,.590,.640,
              FRONT-.093,FRONT-.038,edge)

for name,d,material,radius in [
    ('INSET_Hairline_Outer',.255,gold,.010),
    ('INSET_Hairline_Inner',.133,gold_hi,.008),
    ('STONE_OuterRidge',.617,edge,.014),
]:
    pts=[]
    for i in range(130):
        p=arch_offset(i/129,d)
        pts.append((p.x,FRONT-.10 if 'STONE' in name else FRONT-.072,p.y))
    tube(name,pts,material,radius)

# Key stone is a single dark face with a gold eight-point scholarly mark.
apex_outer=arch_offset(.5,.614)
golden_lozenge('KEYSTONE_CrownBlueGold',0,FRONT-.152,
               apex_outer.y-.06,.115,gold_hi)

# Slender navy/blue columns and masonry pier modules remain separate.
# Column x positions are outside the clear hole (3.56m at spring points).
for side in [-1,1]:
    lab='Left' if side==-1 else 'Right'
    cx=side*2.05
    outer_x=side*2.34
    # Piers: 5 courses, unobstructed center.
    for j in range(5):
        z=.27+j*.55
        mat=stone if j%2==0 else edge
        box(f'PIER_{lab}_GrayStoneCourse_{j+1:02d}',
            (outer_x,0.02,z),(.44,.70,.526),mat,bevel=.015)
        # Small projecting front stone quoin gives Panel 34's readable rhythm.
        box(f'PIER_{lab}_FrontStoneMolding_{j+1:02d}',
            (outer_x,FRONT-.13,z),(.47,.060,.48),stone,bevel=.010)
    box(f'PIER_{lab}_Plinth',(outer_x,-.015,.12),(.63,.78,.24),keystone,.019)
    box(f'PIER_{lab}_PlinthLip',(outer_x,-.02,.29),(.60,.82,.09),edge,.010)
    box(f'PIER_{lab}_SpringCap',(outer_x,-.03,2.75),(.66,.82,.18),stone,.011)
    box(f'PIER_{lab}_SpringAbacus',(outer_x,-.02,2.86),(.67,.84,.075),edge,.008)
    # Non-structural inset dark blue column with aged bronze shoulders.
    cylinder(f'COLUMN_{lab}_DeepBlueShaft',(cx,FRONT-.055,1.51),
             .132,2.11,blue,20)
    for z,r,h,ma,n in [
         (.385,.170,.10,gold,'Base'),
         (.465,.150,.085,gold_hi,'BaseRing'),
         (2.575,.164,.10,gold,'Capital'),
         (2.660,.171,.075,gold_hi,'CapitalRim')]:
        cylinder(f'COLUMN_{lab}_{n}',(cx,FRONT-.055,z),r,h,ma,20)
    box(f'COLUMN_{lab}_GoldAbacus',(cx,FRONT-.055,2.710),
        (.37,.38,.080),gold,.007)
    # Two hairline gold accents on stone pilaster visible in establishing shot.
    for xx in [-.125,.125]:
        tube(f'PIER_{lab}_ThinVerticalDetail_{xx}',
             [(outer_x+xx,FRONT-.170,.48),
              (outer_x+xx,FRONT-.170,2.51)],gold,.004)
    # At the upper spring, navy inset folds naturally into arch.
    tube(f'COLUMN_{lab}_NavyUpperEdge',
         [(cx,FRONT-.20,2.74),(cx,FRONT-.20,2.93)],
         gold_hi,.005)

# Placement hints for independently generated GuardianStatue and WallTorch.
# These are empties only, not the meshes themselves.
for side in [-1,1]:
    lab='Left' if side==-1 else 'Right'
    points=[
      ('GuardianStatue_'+lab+'_PLACEMENT_HINT',(side*3.18,-1.02,0)),
      ('WallTorch_'+lab+'_PLACEMENT_HINT',(side*3.55,-.32,3.20))
    ]
    for name,position in points:
        ob=bpy.data.objects.new(name,None)
        asset.objects.link(ob);ob.parent=root;ob.location=position
        ob.empty_display_type='SPHERE';ob.empty_display_size=.11
        ob['author_note']='Position only, actual props remain independent'

# Camera + lights only inside preview collection. No ceiling/floor is modeled.
def light(name,loc,power,size,color):
    da=bpy.data.lights.new(name,'AREA')
    da.energy=power;da.size=size;da.color=color
    ob=bpy.data.objects.new(name,da)
    preview.objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector((0,0,2.22))-ob.location).to_track_quat('-Z','Y').to_euler()
light('Warm Left',(-4,-5,5),900,6,(1,.79,.56))
light('Warm Right',(4,-2.8,4),460,5,(1,.70,.42))
light('Cool Vault',(0,3.6,4.4),700,4,(.40,.56,1))
camera_data=bpy.data.cameras.new('QA_Camera')
cam=bpy.data.objects.new('QA_Camera',camera_data)
preview.objects.link(cam);s.camera=cam
camera_data.type='ORTHO'
def render(name,position,target,scale=6.25):
    cam.location=position
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    camera_data.ortho_scale=scale
    s.render.filepath=str(QA/(name+'.png'))
    bpy.ops.render.render(write_still=True)

if LOD_LEVEL==0:
    render('GothicGateway_v01_Front',(0,-12,3.18),(0,0,2.20),6.04)
    render('GothicGateway_v01_ThreeQuarter',(7.4,-11,4.25),(0,0,2.15),6.16)
    render('GothicGateway_v01_Back',(0,12,3.20),(0,0,2.16),6.04)
elif LOD_LEVEL==2:
    render('GothicGateway_v01_LOD2_ThreeQuarter',
           (7.4,-11,4.25),(0,0,2.15),6.16)

# Save independently editable geometry, not joined, so individual masonry
# stone modules can be selected and corrected.
if LOD_LEVEL==0:
    s.render.filepath=str(QA/'GothicGateway_v01_Front.png')
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.shading.type='MATERIAL'
            space.overlay.show_overlays=False
            space.region_3d.view_location=(0,0,2.15)
            space.region_3d.view_distance=9
            space.region_3d.view_rotation=(
                Vector((0,0,2.15))-Vector((7.4,-11,4.25))
            ).to_track_quat('-Z','Y')
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'StudyArchive_GothicGateway_v01.blend'),
                                compress=True)

# Compact export scene to meshes by material, keeping scene root and empty
# statue/torch placement targets. Editable construction stays in .blend.
for ob in list(asset.objects):
    if ob.type=='CURVE':
        bpy.ops.object.select_all(action='DESELECT')
        ob.select_set(True);bpy.context.view_layer.objects.active=ob
        bpy.ops.object.convert(target='MESH')
groups={}
for ob in list(asset.objects):
    if ob.type=='MESH':
        assert len(ob.data.materials)==1,ob.name
        groups.setdefault(ob.data.materials[0].name,[]).append(ob)
for matname,obs in groups.items():
    if len(obs)<2:continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs:o.select_set(True)
    bpy.context.view_layer.objects.active=obs[0]
    bpy.ops.object.join()
    obs[0].name='MESH_'+matname

def count_tris():
    return sum(sum(len(poly.vertices)-2 for poly in obj.data.polygons)
               for obj in asset.objects if obj.type=='MESH')

def export(filename):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in asset.objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    path=EX/filename
    bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',
        use_selection=True,export_apply=True)
    return path

count=count_tris()
filebase='StudyArchive_GothicGateway_v01'
glb=export(filebase+('' if LOD_LEVEL==0 else f'_LOD{LOD_LEVEL}')+'.glb')

metrics={"source_blend":str(HERE/'StudyArchive_GothicGateway_v01.blend'),
    "export":str(glb),
    "lod_level":LOD_LEVEL,
    "triangles":count,
    "arch_segments":ARCH_SEGMENTS,
    "clear_width_at_spring_m":HALF_CLEAR*2,
    "inner_arch_apex_m":APEX_INNER_Z,
    "pillar_span_m":4.1,
    "pieces_after_export":sum(ob.type=='MESH' for ob in asset.objects),
    "guardian_torch_placeholders":4,
    "credits":0,
    "unity_scene_modified":False,
    "unity_editor_tested":False}
(QA/f'GatewayQA_LOD{LOD_LEVEL}.json').write_text(json.dumps(metrics,indent=2))
print('GATEWAY_COMPLETED',json.dumps(metrics))
