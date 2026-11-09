"""Canonical SPELLSTRIKE one-building study/connected-archive shell.

Authoring in Blender 5.0.1. Uses canonical top-down image as the SPATIAL
authority and existing modular gateway/props. Existing Unity scenes, project
gameplay and original Rodin downloads are not modified.

Run:
 blender -b -t 4 --factory-startup --python build_library_shell_v01.py
"""
import bpy
import math
import json
import random
import zipfile
from pathlib import Path
from mathutils import Vector

SRC=Path(__file__).resolve().parent
P=SRC.parents[2]
UNITY=P/"Assets/Cinematics/Storyboard3D/Environment/LibraryShell"
QA=P/"Library/StoryboardLibraryShell_20261022"
TEMP=QA/"ImportedSourceGLBs"
for f in [SRC,UNITY,QA,TEMP]:f.mkdir(parents=True,exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1600
scene.render.resolution_y=1050
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='Standard'
scene.view_settings.look='Medium High Contrast'
scene.view_settings.exposure=-.23
scene.world=bpy.data.worlds.new("StudyAmber_ArchiveNavy_Ambient")
scene.world.color=(.18,.20,.23)

def coll(name):
    c=bpy.data.collections.new(name)
    scene.collection.children.link(c)
    return c

WALL_KIT=coll("00_LIBRARY_MODULAR_STONE_WALL_KIT")
FLOOR_KIT=coll("01_LIBRARY_MODULAR_STONE_FLOOR_KIT")
BUILDING=coll("02_ONE_BUILDING_WALLS_FLOORS")
ARCHIVE=coll("03_ARCHIVE_SHELF_LAYOUT")
FURNITURE=coll("04_FRONT_STUDY_DRESSING")
LANDMARK=coll("05_GATEWAY_ARCH_AND_HERO")
PREVIEW=coll("99_PREVIEW_ONLY_CAMERAS_LIGHTS")

def rehome(obj,collection):
    for c in list(obj.users_collection):c.objects.unlink(obj)
    collection.objects.link(obj)
    return obj

def mat(name,c,rough=.75,metal=0.):
    m=bpy.data.materials.new(name)
    m.diffuse_color=(*c,1);m.use_nodes=True
    shader=m.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value=(*c,1)
    shader.inputs['Roughness'].default_value=rough
    shader.inputs['Metallic'].default_value=metal
    return m

wall_mats=[
    mat("STONE_SlateGray_A",(.087,.096,.108),.90),
    mat("STONE_SlateGray_B",(.102,.106,.111),.90),
    mat("STONE_SlateGray_C",(.112,.110,.106),.88),
    mat("STONE_MortarDark",(.044,.049,.059),.98),
]
floor_mats_study=[
    mat("FLOOR_Study_AshWarmA",(.205,.174,.133),.89),
    mat("FLOOR_Study_AshWarmB",(.229,.197,.148),.90),
    mat("FLOOR_Study_AshWarmC",(.185,.163,.135),.93),
]
floor_mats_archive=[
    mat("FLOOR_Archive_BlueSlateA",(.087,.110,.135),.88),
    mat("FLOOR_Archive_BlueSlateB",(.112,.131,.156),.90),
    mat("FLOOR_Archive_BlueSlateC",(.094,.119,.142),.94),
]
carpet=mat("FABRIC_CrimsonAged_Main",(.142,.023,.027),.96)
carpet_dark=mat("FABRIC_CrimsonDark_Pattern",(.072,.010,.014),.97)
carpet_gold=mat("FABRIC_RugOcherEmbroidery",(.35,.195,.079),.92)
wood=mat("WOOD_ArchiveAgedWalnut",(.108,.053,.025),.81)
wood_edge=mat("WOOD_ArchiveCarving",(.145,.074,.036),.77)
brass=mat("BRASS_DoorAndFrames",(.48,.285,.081),.39,.61)
iron=mat("IRON_DarkHinges",(.038,.044,.053),.43,.66)
amber=mat("GLASS_AmberStain",(.76,.301,.044),.35,.05)
amber_hi=mat("GLASS_AmberHighlight",(.98,.62,.165),.34,.03)
cyan=mat("GLASS_ArchiveBlueWindow",(.034,.17,.28),.29,.12)
navy=mat("FABRIC_DeepBlueAccent",(.017,.033,.105),.84)

def raw_mesh(name,vertices,faces,materials,indices=None,collection=BUILDING):
    me=bpy.data.meshes.new(name+"Mesh")
    me.from_pydata(vertices,[],faces);me.update()
    obj=bpy.data.objects.new(name,me)
    collection.objects.link(obj)
    for m in materials:me.materials.append(m)
    if indices:
        for face,idx in zip(me.polygons,indices):face.material_index=idx
    return obj

def box(name,loc,dims,material,collection,bevel=0,mat2=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=rehome(bpy.context.object,collection);o.name=name
    o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    if bevel:
        b=o.modifiers.new("BeveledEdges","BEVEL");b.width=bevel;b.segments=1
        bpy.context.view_layer.objects.active=o
        bpy.ops.object.modifier_apply(modifier=b.name)
    return o

def tube(name,points,material,collection,radius=.01,cyclic=False):
    cu=bpy.data.curves.new(name+"Path",'CURVE')
    cu.dimensions='3D'
    cu.bevel_depth=radius;cu.bevel_resolution=1
    sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
    for p,xyz in zip(sp.points,points):p.co=(*xyz,1)
    sp.use_cyclic_u=cyclic
    o=bpy.data.objects.new(name,cu);collection.objects.link(o)
    cu.materials.append(material)
    return o

def make_brick_module(name,length=2.,height=5.5,depth=.42):
    """Wall module: mortar core + offset courses, with shared PBR materials."""
    r=random.Random(7518)
    v=[];faces=[];ids=[]
    def cube(x0,x1,y0,y1,z0,z1,idx):
        start=len(v)
        v.extend([(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0),
                  (x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)])
        faces.extend([tuple(start+i for i in q) for q in
                [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]])
        ids.extend([idx]*6)
    # Module coordinates x=0..length; front faces -Y; origin at x0 floor.
    cube(0,length,-depth/2+.025,depth/2-.025,0,height,3)
    rows=11;row_h=height/rows
    for row in range(rows):
        low=row*row_h+.012;high=(row+1)*row_h-.012
        # Course bond staggering independent of its world location.
        start=-.45 if row%2 else 0
        x=start
        while x<length:
            a=max(0,x+.012);b=min(length,x+.97)
            if b>a+.08:
                idx=r.choices([0,1,2],[.38,.40,.22])[0]
                cube(a,b,-depth/2-.014,-depth/2+.025,low,high,idx)
                cube(a,b,depth/2-.025,depth/2+.014,low,high,idx)
            x+=1.
    obj=raw_mesh(name,v,faces,wall_mats,ids,WALL_KIT)
    obj["module_length_m"]=length
    obj["wall_height_m"]=height
    obj["wall_depth_m"]=depth
    obj["snap"]="Place at section start; local +X follows section"
    return obj

wall2=make_brick_module("Wall_SlateStraight_2m")
wall1=make_brick_module("Wall_SlateStraight_1m",1)
wallhalf=make_brick_module("Wall_SlateStraight_0p5m",.5)

def floor_module(name,mats,seed):
    """2×2 m module, 4×4 chiseled tiles with micro-depth and mortar seams."""
    rand=random.Random(seed)
    verts=[];faces=[];indices=[]
    dim=2.;n=4
    def add_block(x0,x1,y0,y1,z0,z1,idx):
        a=len(verts)
        verts.extend([(x0,y0,z0),(x1,y0,z0),(x1,y1,z0),(x0,y1,z0),
                      (x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)])
        for face in [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]:
            faces.append(tuple(a+i for i in face));indices.append(idx)
    for row in range(n):
        for col in range(n):
            jitter=rand.uniform(-.012,.008)
            x=col*.5+.010;y=row*.5+.010
            add_block(x,x+.478,y,y+.478,-.10,jitter,rand.randint(0,2))
    o=raw_mesh(name,verts,faces,mats,indices,FLOOR_KIT)
    o["snap"]="One tile module 2m x 2m; origin=SW corner; z=0 floor"
    return o

floor_study=floor_module("Floor_StoneStudy_2m",floor_mats_study,2026)
floor_archive=floor_module("Floor_StoneArchive_2m",floor_mats_archive,2028)

# Template meshes live at local origin (for standalone prefab exports).
# They must NOT appear in the assembled library at origin, where a wall
# template would otherwise physically obstruct the canonical Gothic gateway.
WALL_KIT.hide_render=True
FLOOR_KIT.hide_render=True
WALL_KIT.hide_viewport=True
FLOOR_KIT.hide_viewport=True
# A collection-level toggle is insufficiently reliable across headless
# Blender renders. Hide template OBJECTS too; linked instances elsewhere
# remain visible. Otherwise the standalone 2m wall demo blocks the doorway.
for template in list(WALL_KIT.objects)+list(FLOOR_KIT.objects):
    template.hide_render=True

# Helpers create truly linked mesh data for duplicated modular instances.
def instance_from(proto,name,collection,loc=(0,0,0),rotation=0,scale=(1,1,1)):
    o=bpy.data.objects.new(name,proto.data)
    collection.objects.link(o)
    o.location=loc;o.rotation_euler.z=rotation;o.scale=scale
    return o

def long_wall(prefix,a,b,y,height=5.5):
    """Snap repeatable 2m, 1m, 0.5m, or fractional module in +X."""
    x=a
    while x<b-.025:
        left=b-x
        size=2 if left>=2-.01 else (1 if left>=1-.01 else (.5 if left>=.5-.01 else left))
        src=wall2 if size==2 else wall1 if size==1 else wallhalf
        scalex=size/(2. if src is wall2 else 1. if src is wall1 else .5)
        if height<5.5:
            # Use exact scaled-height real masonry module only in header/sill.
            scale=(scalex,1,height/5.5)
        else:scale=(scalex,1,1)
        zbase=0
        o=instance_from(src,f'{prefix}_x{x:.2f}_w{size:.2f}',BUILDING,
                        (x,y,zbase),scale=scale)
        # Height-limited module placement handled by direct z override after.
        x+=size

def wall_section(prefix,xa,xb,y,z0=0,zh=5.5):
    x=xa
    while x<xb-.005:
        length=min(2,xb-x)
        src=wall2 if length>=1.5 else wall1 if length>=.75 else wallhalf
        default=2 if src is wall2 else 1 if src is wall1 else .5
        instance_from(src,f'WALL_{prefix}_{x:.2f}',BUILDING,
                      (x,y,z0),scale=(length/default,1,zh/5.5))
        x+=length

def side_wall(prefix,x,y_start,y_end):
    # A 2m wall's +X axis rotated to the building's +Y direction.
    y=y_start
    while y<y_end-.006:
        length=min(2,y_end-y)
        src=wall2 if length>=1.5 else wall1 if length>=.75 else wallhalf
        default=2 if src is wall2 else 1 if src is wall1 else .5
        instance_from(src,f'WALL_{prefix}_{y:.2f}',BUILDING,
            (x,y,0),rotation=math.pi/2,scale=(length/default,1,1))
        y+=length

XMIN=-7.;XMAX=7.;YFRONT=-11.5;YGATE=0.;YBACK=9.
HEIGHT=5.5

# Complete ground is one contiguous plane, with separate color-zoned modular
# flagstones, *not* a disconnected extra hallway.
for ix in range(7):
    for iy in range(6):
        # Study modules begin at -11.5 and end at +0.5. Last tiles overlap
        # the divider by half a meter; world-floor continuity is intentional.
        o=instance_from(floor_study,f"STUDY_Floor_{ix:02d}_{iy:02d}",
           BUILDING,(XMIN+ix*2,YFRONT+iy*2,0))
        if iy==5:
            # Make its top edge terminate at the physical partition y=0.
            o.scale.y=.75
for ix in range(7):
    for iy in range(5):
        o=instance_from(floor_archive,f'ARCHIVE_Floor_{ix:02d}_{iy:02d}',
                        BUILDING,(XMIN+ix*2,iy*2,0))
        if iy==4:
            o.scale.y=.5

side_wall('WEST_EntireBuilding',XMIN,YFRONT,YBACK)
side_wall('EAST_EntireBuilding',XMAX,YFRONT,YBACK)

# CENTRAL DIVIDER has one and only one gateway hole: the free-standing
# stone gateway GLB is aligned to floor origin at y=0.
wall_section('DividerWest',XMIN,-2.675,YGATE)
wall_section('DividerEast',2.675,XMAX,YGATE)

# Rear façade, with genuinely open double-door gap.
wall_section('RearWest',-7.,-1.35,YBACK)
wall_section('RearEast',1.35,7.,YBACK)
wall_section('RearDoorHeader',-1.35,1.35,YBACK,3.45,2.05)

# South façade: stained glass at x=0, entry on lower RIGHT. Keep openings,
# header and windows independent for later close-up refinements.
wall_section('SouthWest',-7.,-.90,YFRONT)
wall_section('SouthMid',.90,5.04,YFRONT)
wall_section('SouthStub',6.52,7.,YFRONT)
wall_section('SouthWindowSill',-.90,.90,YFRONT,0,1.02)
wall_section('SouthWindowHeader',-.90,.90,YFRONT,3.65,1.85)
wall_section('SouthDoorHeader',5.04,6.52,YFRONT,3.22,2.28)

# Stone lintels, sill and arch frame stay separate from the adjacent wall
# modules, allowing the southern stain-glass and entrance models to be swapped.
box('FRONT_WindowStoneSill',(0,YFRONT-.13,1.025),(2,.51,.12),
    floor_mats_study[2],BUILDING,.015)
box('REAR_DoorStoneStep',(0,YBACK-.12,.065),(2.85,.8,.14),
    wall_mats[1],BUILDING,.015)
for side in [-1,1]:
    box(f'SOUTH_WindowStoneJamb_{side}',
        (side*.915,YFRONT-.10,2.33),(.21,.68,2.65),
        wall_mats[1],BUILDING,.022)
    box(f'SOUTH_EntryDoorJamb_{side}',
        (5.78+side*.78,YFRONT-.12,1.6),(.25,.68,3.2),
        wall_mats[1],BUILDING,.024)
    box(f'REAR_DoorJamb_{side}',(side*1.34,YBACK-.10,1.75),
        (.24,.75,3.50),wall_mats[1],BUILDING,.022)

# Gothic stained-glass aperture retains distinct glass material. Amber is
# an interior lit-panel starter, not a substitute for future custom hero glass.
glass=box("SOUTH_GlassAmberPane",(0,YFRONT-.035,2.32),
        (1.52,.035,2.40),amber,BUILDING)
for i in range(5):
    x=-.72+i*.36
    box('SOUTH_GlassLeadVertical_%02d'%i,(x,YFRONT+.115,2.30),
        (.025,.065,2.45),iron,BUILDING)
for z in [1.16,2.20,3.40]:
    box('SOUTH_GlassLeadHorizontal_'+str(z),(0,YFRONT+.115,z),
        (1.63,.066,.029),iron,BUILDING)
box('SOUTH_GlassWarmCenter',(0,YFRONT+.052,2.34),(.40,.045,1.57),
    amber_hi,BUILDING)
# Interior-facing Gothic tracery: the prior lead bars were placed behind the
# opaque amber glass and vanished in the important shot from the study table.
window_outline=[
    (-.77,YFRONT+.155,1.14),(-.77,YFRONT+.155,3.03),
    (0,YFRONT+.155,3.48),(.77,YFRONT+.155,3.03),
    (.77,YFRONT+.155,1.14)
]
tube('SOUTH_GothicWindowPointedStoneFrame',window_outline,
     wall_mats[1],BUILDING,.060)
for side in (-1,1):
    # Stone corner spandrels mask the rectangular panel into a pointed silhouette.
    x0=side*.77
    verts=[
        (x0,YFRONT+.095,3.02),
        (0,YFRONT+.095,3.48),
        (0,YFRONT+.095,3.58),
        (x0,YFRONT+.095,3.58),
    ] if side==-1 else [
        (0,YFRONT+.095,3.48),
        (x0,YFRONT+.095,3.02),
        (x0,YFRONT+.095,3.58),
        (0,YFRONT+.095,3.58),
    ]
    raw_mesh(f'SOUTH_GothicGlassSpandrel_{side}',
        verts,[(0,1,2),(0,2,3)],[wall_mats[0]],collection=BUILDING)
for side in (-1,1):
    x=side*.38
    tube(f'SOUTH_GlassLancet_{side}',
       [(x-side*.17,YFRONT+.169,2.75),
        (x,YFRONT+.169,3.12),
        (x+side*.16,YFRONT+.169,2.75)],
       iron,BUILDING,.014)

# South-right wooden entry, physically located only in the correct wall.
def door_panel(name,center,dim,collection):
    o=box(name,center,dim,wood,collection,.018)
    x,y,z=center
    for dx in [-.23,0,.23]:
        box(name+'_raised_'+str(dx),(x+dx,y-.068,z),
            (.17,.029,dim[2]-.35),wood_edge,collection,.01)
    for zz in [z-.75,z+.75]:
        box(name+'_CrossBrace_'+str(zz),(x,y-.086,zz),
            (dim[0]-.10,.028,.045),iron,collection)
    box(name+'_Handle',(x+dim[0]*.28,y-.103,z),
        (.035,.05,.125),brass,collection)
    return o

door_panel("FRONT_RightEntranceDoor",(5.78,YFRONT+.10,1.58),
           (1.32,.15,3.12),BUILDING)
for side in [-1,1]:
    x=side*.685
    door_panel('REAR_ArchiveDoubleLeaf_'+str(side),
               (x,YBACK+.12,1.73),(1.28,.17,3.37),BUILDING)
    box('REAR_ArchiveLintelCarving_'+str(side),
        (side*.64,YBACK-.26,3.51),(1.32,.055,.18),
        brass,BUILDING,.009)

# Narrow pointed archive windows visible beside back double doors.
for side in [-1,1]:
    x=side*3.48
    box(f'REAR_BlueGothicWindow_{side}',(x,YBACK-.234,3.55),
        (.57,.036,1.32),cyan,BUILDING)
    for off in [-.33,.33]:
        box(f'REAR_WindowJamb_{side}_{off}',(x+off,YBACK-.265,3.55),
           (.088,.16,1.56),wall_mats[1],BUILDING,.009)
    tube(f'REAR_WindowPeak_{side}',
        [(x-.33,YBACK-.28,4.33),(x,YBACK-.28,4.58),
         (x+.33,YBACK-.28,4.33)],wall_mats[1],BUILDING,.045)

# Carpet modules. Study rug is separate from narrow carpet that continues
# through gateway to rear door. Floor stays walkable below all carpets.
def carpet_rect(name,xc,yc,width,length,collection,pattern=True):
    box(name,(xc,yc,.023),(width,length,.024),carpet,collection)
    for sx in [-1,1]:
        box(name+'_LongGoldBorder_'+str(sx),
            (xc+sx*(width/2-.084),yc,.040),(.028,length-.14,.006),
            carpet_gold,collection)
        box(name+'_LongBlackEdge_'+str(sx),
            (xc+sx*(width/2-.032),yc,.041),(.032,length-.12,.006),
            carpet_dark,collection)
    for sy in [-1,1]:
        box(name+'_GoldShort_'+str(sy),
           (xc,yc+sy*(length/2-.08),.040),
           (width-.15,.028,.006),carpet_gold,collection)
    if pattern:
        for i in range(max(0,int(length/.50))):
            y=yc-length/2+.28+i*.50
            for sx in [-1,1]:
                x=xc+sx*(width/2-.19)
                verts=[(x,y,.046),(x+.065,y+.10,.046),
                       (x,y+.20,.046),(x-.065,y+.10,.046)]
                raw_mesh(name+'_Motif_%d_%s'%(i,sx),verts,
                   [(0,1,2,3)],[carpet_dark],collection=collection)

carpet_rect('STUDY_MainRug',0,-6.76,4.45,4.90,BUILDING,True)
# A SINGLE runner physically connects the study rug to the back doors.
# Prior segmented carpets left a 2.3 m stone gap before the gateway.
carpet_rect('CONTINUOUS_CrimsonRunner',0,2.28,1.56,13.24,BUILDING,True)

# GLB references. Pack all GLB textures into saved Blender source.
def read_glb(zipbase,filename='base_basic_pbr.glb'):
    p=Path.home()/'Downloads'/zipbase
    with zipfile.ZipFile(p) as arc:
        out=TEMP/(zipbase.replace('.zip','_')+filename)
        if not out.exists() or out.stat().st_size != arc.getinfo(filename).file_size:
            out.write_bytes(arc.read(filename))
    return out

def import_mesh_asset(name,path):
    existing=set(bpy.context.scene.objects)
    bpy.ops.import_scene.gltf(filepath=str(path))
    made=[o for o in bpy.context.scene.objects if o not in existing]
    renders=[o for o in made if o.type=='MESH']
    if not renders:
        raise RuntimeError(f'{name} has no mesh in {path}')
    # Each asset has one native imported mesh object in recent Rodin exports,
    # but treat multiple as one transformed reusable parent collection.
    container=coll('SRC_'+name)
    master=bpy.data.objects.new('SRC_'+name+'_PIVOT',None)
    container.objects.link(master)
    for ob in made:
        if ob.type=='MESH':
            rehome(ob,container)
            ob.parent=master
        else:
            rehome(ob,container)
        ob.hide_render=True
    container.hide_render=True
    container.hide_viewport=True
    return master,renders

def inst_asset(src,name,dest,location,scale=(1,1,1),rot=0):
    """Copy the imported model's hierarchy with linked mesh/material data."""
    # Rodin single-mesh models have no hidden origin transforms.
    root=bpy.data.objects.new(name,None)
    dest.objects.link(root)
    root.location=location;root.rotation_euler.z=rot;root.scale=scale
    for ob in src[1]:
        child=bpy.data.objects.new(name+'_'+ob.name,ob.data)
        dest.objects.link(child)
        child.matrix_parent_inverse.identity()
        child.parent=root
        child.location=ob.location.copy()
        child.rotation_euler=ob.rotation_euler.copy()
        child.scale=ob.scale.copy()
    return root

shelf=import_mesh_asset('BOOKSHELF',read_glb('d3bf819f-3d5e-4022-8e82-64b09e93f53f.zip'))
table=import_mesh_asset('STUDY_TABLE',read_glb('5b21ce61-eced-43b4-a4ce-057dcabf5765.zip'))
chair=import_mesh_asset('CHAIR',read_glb('9202722d-e674-4191-8994-df4c6e3f128f.zip'))
statue=import_mesh_asset('GUARDIAN',read_glb('ef605499-1daa-45e0-bb67-3f6624503687.zip'))
torch=import_mesh_asset('TORCH',read_glb('97cade01-0949-4973-9b18-76fd16c3771f.zip'))

def bounds(objs):
    pt=[o.matrix_world@v.co for o in objs for v in o.data.vertices]
    return [max(p[i] for p in pt)-min(p[i] for p in pt) for i in range(3)]

def scale_for(src,target):
    b=bounds(src[1])
    return tuple(target[i]/b[i] for i in range(3))

shelf_scale=scale_for(shelf,(1.48,0.78,3.20))
table_scale=scale_for(table,(3.08,1.38,.79))
chair_scale=scale_for(chair,(.79,.83,1.36))
guardian_scale=scale_for(statue,(.79,.69,1.90))
torch_scale=scale_for(torch,(.34,.32,1.08))

# Shelf side walls: books face towards the center, not into the stone wall.
# Study: left/right tall shelves flank the huge main table and do not occlude
# lower-right door, nor central arch.
study_wall_shelves=[]
for sx in [-1,1]:
    for index,y in enumerate([-9.65,-8.01,-6.37,-4.73,-3.09]):
        o=inst_asset(shelf,f'STUDY_Shelf_{"W" if sx<0 else "E"}_{index+1:02d}',
          FURNITURE,(sx*6.22,y,0),
          shelf_scale,rot=math.pi/2 if sx<0 else -math.pi/2)
        study_wall_shelves.append(o)

# Archive wall shelves and two freestanding long rack rows, matching the top
# down map (there is NO extra intermediate hallway).
archive_shelves=[]
for sx in [-1,1]:
    for index,y in enumerate([1.70,3.36,5.02,6.68,8.33]):
        for section,x in [('Wall',6.20),('InnerRack',3.63)]:
            ob=inst_asset(shelf,
               f'ARCHIVE_{section}_{"W" if sx<0 else "E"}_{index+1:02d}',
               ARCHIVE,(sx*x,y,0),shelf_scale,
               rot=math.pi/2 if sx<0 else -math.pi/2)
            archive_shelves.append(ob)

# Specific hero-book position on the RIGHT side of left inner row; 1.65m
# retrieval height. This remains an independent Empty, NOT a fused book.
book_anchor=bpy.data.objects.new('HERO_AncientChronicle_ShelfRetrieval_POINT',None)
ARCHIVE.objects.link(book_anchor)
book_anchor.location=(-2.86,5.02,1.63)
book_anchor.empty_display_type='ARROWS';book_anchor.empty_display_size=.22
book_anchor['prop']='Place separately riggable Living Spellbook'
book_anchor['continuity']='Same volume moves to study table for Panels 18-26'

# Simple scholastic clutter: lecterns and stools are low-poly local meshes.
for sx in [-1,1]:
    box('ARCHIVE_ResearchShelfTable_'+str(sx),
        (sx*5.12,6.42,.79),(.66,.78,.12),wood,ARCHIVE,.016)
    for xx in [-.22,.22]:
        for yy in [-.27,.27]:
            box(f'ARCHIVE_ResearchTableLeg_{sx}_{xx}_{yy}',
              (sx*5.12+xx,6.42+yy,.40),(.09,.085,.77),
              wood_edge,ARCHIVE,.01)

# Correct south-facing table arrangement and seating.
main_table=inst_asset(table,'STUDY_MainStudyTable',FURNITURE,(0,-6.76,0),
                      table_scale,rot=math.pi/2)
# Source table is x-long; rotation 90 places it north-south. Match the
# target dimensions after the rotation with scale adjusted above.
for side in [-1,1]:
    inst_asset(chair,f'STUDY_CinematicChair_{side}',
               FURNITURE,(side*1.63,-6.76,0),
               chair_scale,rot=math.pi/2 if side<0 else -math.pi/2)

# Replace two crude greybox props with separate user-reference-matched
# standalone assets. Desk pivots at floor; globe pivot at tabletop.
scholar_dir='Assets/Cinematics/Storyboard3D/Environment/Props/ScholarCorner/'
scholar_table=import_mesh_asset('SCHOLAR_SIDE_TABLE',
               P/(scholar_dir+'ScholarSideTable_Walnut_v01.glb'))
scholar_globe=import_mesh_asset('ANTIQUE_CELESTIAL_GLOBE',
               P/(scholar_dir+'AntiqueCelestialGlobe_Brass_v01.glb'))
scholar_table_instance=inst_asset(scholar_table,'STUDY_ScholarSideTable',FURNITURE,
                                  (5.32,-7.80,0))
scholar_globe_instance=inst_asset(scholar_globe,'STUDY_AntiqueCelestialGlobe',FURNITURE,
                                  (5.63,-7.78,.85))
# User-approved study rearrangement: table clockwise 90deg around its own
# Z axis, together with the globe. Parent BEFORE turning so the globe orbits
# the table center instead of rotating in place or remaining left behind.
bpy.context.view_layer.update()
scholar_globe_world=scholar_globe_instance.matrix_world.copy()
scholar_globe_instance.parent=scholar_table_instance
scholar_globe_instance.matrix_world=scholar_globe_world
bpy.context.view_layer.update()
scholar_table_instance.rotation_euler.z=-math.pi/2
bpy.context.view_layer.update()

# Existing finished reusable hero models: arch, chandelier and banners.
def import_existing(name,relative,dest,location,rot=0,scale=(1,1,1)):
    src=import_mesh_asset(name,P/relative)
    return inst_asset(src,name,dest,location,scale,rot)

arch=import_existing('GOTHIC_GATEWAY',
    'Assets/Cinematics/Storyboard3D/Environment/Architecture/GothicGateway/StudyArchive_GothicGateway_v01.glb',
    LANDMARK,(0,0,0))
chandelier=import_existing('CHANDLIER',
    'Assets/Cinematics/Storyboard3D/Environment/Props/Chandeliers/StudyChandelier_BrassRing_v01.glb',
    LANDMARK,(0,-6.76,5.35))
banner_path='Assets/Cinematics/Storyboard3D/Environment/Props/Banners/LibraryBanner_NavyGold_v01.glb'
for sx in [-1,1]:
    import_existing(f'STUDY_BANNER_{sx}',banner_path,LANDMARK,
                    (sx*1.65,YFRONT+.35,2.50),rot=math.pi)

for sx in [-1,1]:
    inst_asset(statue,f'GATEWAY_GuardianStatue_{"W" if sx<0 else "E"}',
         LANDMARK,(sx*3.15,-1.01,0),guardian_scale,rot=0)
    # Lower than story spec wall torch default GLB dimensions, mounts at 2.48m.
    inst_asset(torch,f'GATEWAY_WallTorch_{"W" if sx<0 else "E"}',
         LANDMARK,(sx*3.42,-.40,2.45),torch_scale,rot=0)

# Waypoints and cameras for Unity Timeline staging, exported as empties.
MARKERS=[
 ("CINEMATIC_StudyTableFocus",(0,-6.76,1.15)),
 ("CINEMATIC_GatewayApproach",(0,-2.10,0)),
 ("CINEMATIC_GatewayCenter",(0,0,0)),
 ("CINEMATIC_ArchiveRetrieval",(0,5.05,0)),
 ("CINEMATIC_ArchiveRearDoubleDoors",(0,8.12,0)),
 ("CINEMATIC_EntranceSouthRight",(5.78,-10.40,0)),
]
for name,loc in MARKERS:
    marker=bpy.data.objects.new(name,None)
    LANDMARK.objects.link(marker);marker.location=loc
    marker.empty_display_type='CUBE';marker.empty_display_size=.15

def light(name,loc,power,size,color):
    data=bpy.data.lights.new(name,'AREA')
    data.energy=power;data.size=size;data.color=color
    obj=bpy.data.objects.new(name,data)
    PREVIEW.objects.link(obj);obj.location=loc
    obj.rotation_euler=(Vector((0,-4,0))-obj.location).to_track_quat('-Z','Y').to_euler()

# Static rendering lights for previews; no eight shadowed candle lights.
light('StudyWarm_01',(-3.0,-7.5,5.6),1050,8,(1,.78,.46))
light('StudyWarm_02',(4.5,-3.8,5.7),850,7,(1,.77,.48))
light('ArchiveCool_01',(-4,4.6,5.6),1200,9,(.50,.66,1))
light('ArchiveCool_02',(4,7.6,5.6),900,8,(.49,.66,1))
light('DoorRim',(0,10.8,5.9),300,5,(.62,.74,1))

scene.camera=None
camera_data=bpy.data.cameras.new('QA_OneBuilding_Camera')
camera=bpy.data.objects.new('QA_OneBuilding_Camera',camera_data)
PREVIEW.objects.link(camera);scene.camera=camera
camera_data.type='ORTHO'

def shoot(name,loc,target,scale,res=(1500,1000),perspective=False):
    camera.location=loc
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera_data.type='PERSP' if perspective else 'ORTHO'
    if perspective:
        camera_data.lens=scale
        camera_data.clip_start=.05
        camera_data.clip_end=140
    else:
        camera_data.ortho_scale=scale
    scene.render.resolution_x=res[0];scene.render.resolution_y=res[1]
    scene.render.filepath=str(QA/(name+'.png'))
    bpy.ops.render.render(write_still=True)

shoot('01_OneBuilding_Isometric',(19,-23,25),(0,-1.0,0),30.0)
shoot('02_StudyToArchive',(.35,-9.55,1.98),(0,2.20,2.10),30.0,perspective=True)
shoot('03_Archive_Aisle',(.54,1.12,1.94),(0,7.95,1.91),35.0,perspective=True)
shoot('05_StudyFacingSouth',(1.15,-3.05,2.05),(0,-9.3,2.2),30.0,perspective=True)
shoot('06_ScholarGlobe_InsideLibrary',(3.65,-10.10,1.88),
      (5.04,-7.79,1.18),37.0,perspective=True)
shoot('04_OneBuilding_TopDown',(0,-1.15,33),(0,-1.15,0),24.5,(1200,1500))

# Source Blender scene contains editable modular templates, the assembled
# architecture, Rodin asset instances, lighting only for preview.
for source in [o for o in scene.objects if o.name.startswith('SRC_')]:
    source.hide_render=True
bpy.ops.file.pack_all()
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        space=a.spaces.active
        space.shading.type='MATERIAL'
        space.overlay.show_overlays=False
        space.region_3d.view_location=(0,-1,0)
        space.region_3d.view_distance=34
        space.region_3d.view_rotation=(
            Vector((0,-1,0))-Vector((19,-23,25))
        ).to_track_quat('-Z','Y')
bpy.context.preferences.filepaths.save_version=0
scene.render.filepath=str(QA/'01_OneBuilding_Isometric.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'LibraryStudyArchive_OneBuilding_v01.blend'),compress=True)

# Export targets. No change to gameplay or cinematic workbench scene.
# Reference asset mesh duplicates share data within Blender and reuse materials.
def export_objects(filename,selected):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in selected:
        if ob.type not in ('CAMERA','LIGHT'):ob.select_set(True)
    meshcount=sum(ob.type=='MESH' for ob in selected)
    tris=sum(sum(max(0,len(p.vertices)-2) for p in ob.data.polygons)
             for ob in selected if ob.type=='MESH')
    dest=UNITY/filename
    bpy.ops.export_scene.gltf(filepath=str(dest),export_format='GLB',
            use_selection=True,export_apply=True)
    return {'file':str(dest),'mesh_count':meshcount,'triangles':tris,
            'bytes':dest.stat().st_size}

result={}
WALL_KIT.hide_viewport=False
FLOOR_KIT.hide_viewport=False
result['stone_straight_module']=export_objects(
    'StoneWall_2m_Modular.glb',[wall2])
result['stone_floor_study_module']=export_objects(
    'StoneFloor_Study_2m_Modular.glb',[floor_study])
result['stone_floor_archive_module']=export_objects(
    'StoneFloor_Archive_2m_Modular.glb',[floor_archive])
WALL_KIT.hide_viewport=True
FLOOR_KIT.hide_viewport=True
result['stone_walls_and_floors']=export_objects(
    'Library_StoneWallsAndFloors_v01.glb',list(BUILDING.objects))
result['archive_racks_and_props']=export_objects(
    'Library_ArchiveShelvesAndMarkers_v01.glb',list(ARCHIVE.objects))
result['study_and_landmarks']=export_objects(
    'Library_StudyFurnitureGatewayAndDecor_v01.glb',
    list(FURNITURE.objects)+list(LANDMARK.objects))

result['all_one_building']=export_objects(
    'LibraryStudyArchive_OneBuilding_v01.glb',
    list(BUILDING.objects)+list(ARCHIVE.objects)+list(FURNITURE.objects)+list(LANDMARK.objects))
result['parameters']={
   'x_extent_m':[XMIN,XMAX],'y_extent_m':[YFRONT,YBACK],
   'wall_height_m':HEIGHT,'gateway_assembly_y_m':0,
   'nominal_gateway_open_width_m':3.56,
   'archive_bookcase_instances':len(archive_shelves),
   'study_bookcase_instances':len(study_wall_shelves),
   'scholar_side_table':'at 5.32,-7.80,0; Z rotation -90 degrees; moved +0.28m X toward right bookshelf',
   'antique_globe':'independent model parented to table; final world location 5.34,-8.11,0.85',
   'origin':'world center X; y0 gateway; south study negative Y; rear archive positive Y',
   'ceiling_mode':'open roof for preview / future overhead beam module',
   'model_origin':'Blender Z up, GLB exporter converts to Unity Y up',
   'unity_scene_modified':False,
   'credits_used':0,
   'dimensions_are_draft_not_storyboard_measurements':True}
(QA/'LibraryStudyArchive_v01_BuildQA.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('LIBRARY_SHELL_BUILT',json.dumps(result))
