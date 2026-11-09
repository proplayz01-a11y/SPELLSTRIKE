"""Two separate 3D assets matching user's scholar-corner crop.

Builds a low-cost ornate antique globe and matching small dark-walnut side
table. Separate Blender object collections, GLB/LOD exports and an editable
assembled preview scene. Globe is not merged into the table.
"""
import bpy
import math
import json
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[2]
OUT=PROJECT/"Assets/Cinematics/Storyboard3D/Environment/Props/ScholarCorner"
QA=PROJECT/"Library/StoryboardScholarCornerPreview_20261022"
for p in (ROOT,OUT,QA):p.mkdir(exist_ok=True,parents=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.engine="BLENDER_EEVEE"
scene.render.resolution_x=950
scene.render.resolution_y=1050
scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"
scene.view_settings.view_transform="Standard"
scene.view_settings.look="Medium High Contrast"
scene.view_settings.exposure=-.22
scene.world=bpy.data.worlds.new("PreviewNeutral")
scene.world.color=(.12,.12,.14)

def collection(name):
    c=bpy.data.collections.new(name);scene.collection.children.link(c);return c

TABLE=collection("01_ANTIQUE_WALNUT_SIDE_TABLE")
GLOBE=collection("02_CELESTIAL_BRASS_PARCHMENT_GLOBE")
STUDIO=collection("99_PREVIEW_ONLY_NOT_EXPORTED")

def material(name,c,metal=0,rough=.6,imagefile=None):
    m=bpy.data.materials.new(name);m.diffuse_color=(*c,1)
    m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*c,1)
    p.inputs['Roughness'].default_value=rough
    p.inputs['Metallic'].default_value=metal
    if imagefile:
        img=bpy.data.images.load(str(ROOT/imagefile),check_existing=True)
        node=m.node_tree.nodes.new('ShaderNodeTexImage')
        node.image=img;node.interpolation='Linear'
        m.node_tree.links.new(node.outputs['Color'],p.inputs['Base Color'])
    return m

walnut=material("WALNUT_AgedWood",(.27,.10,.036),0,.79,
                "Walnut_AgedGrain_1024x512.png")
walnut_dark=material("WALNUT_ShadowTrim",(.092,.047,.021),0,.84)
brass=material("BRASS_AgedAntique",(.45,.24,.065),.78,.39)
gold=material("BRASS_PolishedEdge",(.73,.44,.12),.74,.33)
antique=material("BRASS_EngravedShadow",(.18,.105,.031),.72,.55)
parchment=material("GLOBE_WornParchmentCartography",(.65,.46,.27),0,.94,
                   "Globe_AntiqueParchment_1024x512.png")

def relink(obj,c):
    for old in list(obj.users_collection):old.objects.unlink(obj)
    c.objects.link(obj)
    return obj

table_root=bpy.data.objects.new("ScholarSideTable_Walnut_v01",None)
TABLE.objects.link(table_root)
table_root["reference"]="User-supplied cropped library still, Oct 22 2026"
table_root["units"]="meters, +Z up, table floor pivot at 0"
table_root["mesh_separate_from"]="AntiqueCelestialGlobe_v01"

globe_root=bpy.data.objects.new("AntiqueCelestialGlobe_Brass_v01",None)
GLOBE.objects.link(globe_root)
globe_root["reference"]="Globe at right side of wooden scholar desk in user's image"
globe_root["units"]="meters, tabletop/base pivot at 0"
globe_root["pose"]="Fixed static decor; rings separate in editable blend source"

def cube(name,at,size,m,c,root,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at)
    o=relink(bpy.context.object,c);o.name=name
    o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(m)
    o.parent=root
    if bevel:
        mod=o.modifiers.new('SoftWornEdges','BEVEL')
        mod.width=bevel;mod.segments=1
        bpy.context.view_layer.objects.active=o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def cyl(name,at,r1,r2,height,m,c,root,verts=12):
    bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,
                                    depth=height,location=at)
    ob=relink(bpy.context.object,c);ob.name=name
    ob.data.materials.append(m)
    ob.parent=root
    return ob

def sphere(name,at,r,m,c,root,seg=32,rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,
                                        radius=r,location=at)
    ob=relink(bpy.context.object,c);ob.name=name
    ob.data.materials.append(m);ob.parent=root
    for p in ob.data.polygons:p.use_smooth=True
    return ob

def path(name,coords,m,c,root,width=.012,cyclic=False,res=1):
    cr=bpy.data.curves.new(name+"Curve",'CURVE')
    cr.dimensions='3D';cr.resolution_u=1
    cr.bevel_depth=width;cr.bevel_resolution=res
    sp=cr.splines.new('POLY');sp.points.add(len(coords)-1)
    for p,xyz in zip(sp.points,coords):p.co=(*xyz,1)
    sp.use_cyclic_u=cyclic
    ob=bpy.data.objects.new(name,cr);c.objects.link(ob)
    cr.materials.append(m);ob.parent=root
    return ob

def rod(name,start,end,width,m,c,root,verts=10):
    start=Vector(start);end=Vector(end)
    vector=end-start
    o=cyl(name,(start+end)/2,width,width,vector.length,m,c,root,verts)
    o.rotation_euler=vector.to_track_quat("Z","Y").to_euler()
    return o

# Small practical dark walnut table, intentionally less grand than main study
# desk and short enough for seated/standing scholar corner shots.
W,D,TOP=1.48,.74,.85
for i,y in enumerate((-.244,0,.244)):
    # 3 real planks with separations and slightly different lengths.
    cube(f"TOP_WalnutPlank_{i+1}",(0,y,TOP-.053),
         (W,.243,.103),walnut,TABLE,table_root,.018)
for y in [-.36,.36]:
    cube(f"TOP_DarkEdgeLong_{y}",(0,y,TOP-.044),
         (W,.026,.090),walnut_dark,TABLE,table_root,.006)
for x in [-W/2+.017,W/2-.017]:
    cube(f"TOP_DarkEdgeSide_{x}",(x,0,TOP-.050),
         (.027,D,.092),walnut_dark,TABLE,table_root,.008)

# Four straight stout carved legs and functional support apron.
for xsgn in [-1,1]:
    for ysgn in [-1,1]:
        x=xsgn*.620;y=ysgn*.26
        name=f"LEG_{'W' if xsgn<0 else 'E'}_{'B' if ysgn>0 else 'F'}"
        cube(name,(x,y,.372),(.113,.117,.737),
             walnut,TABLE,table_root,.012)
        cube(name+"_Foot",(x,y,.059),(.137,.139,.090),
             walnut_dark,TABLE,table_root,.005)
        cube(name+"_ShoeTrim",(x,y,.113),(.124,.124,.014),
             brass,TABLE,table_root,.004)
        for zz in [.602,.714]:
            cube(name+"_CarvedNotch_"+str(zz),(x,ysgn*.325,zz),
                 (.087,.016,.016),walnut_dark,TABLE,table_root,.002)
for sign in [-1,1]:
    cube(f"APRON_FrontBack_{sign}",(0,sign*.273,.704),
         (1.19,.066,.158),walnut_dark,TABLE,table_root,.01)
    cube(f"APRON_CarvedLip_{sign}",(0,sign*.315,.648),
         (1.23,.022,.030),walnut,TABLE,table_root,.004)
    cube(f"APRON_End_{sign}",(sign*.630,0,.704),
         (.074,.53,.152),walnut,TABLE,table_root,.009)
for sign in [-1,1]:
    cube(f"LOWER_Stretcher_Long_{sign}",(0,sign*.265,.212),
         (1.17,.065,.070),walnut_dark,TABLE,table_root,.004)
for x in [-.62,.62]:
    cube(f"LOWER_Stretcher_Side_{x}",(x,0,.212),
         (.070,.53,.075),walnut_dark,TABLE,table_root,.004)
for side in [-1,1]:
    for x in [-.54,0,.54]:
        cyl(f"FRONT_BrassStud_{side}_{x}",(x,side*.379,.799),
            .010,.008,.011,gold,TABLE,table_root,8)

# Tiny repeating inlay corners on tabletop keep the metal identity from the
# screenshot while retaining low triangle count.
for x in [-.665,.665]:
    for y in [-.295,.295]:
        cube(f"TOP_ScholarlyBrassCorner_{x}_{y}",
             (x,y,TOP+.001),(.048,.034,.009),
             brass,TABLE,table_root,.002)

table_root["dimensions_m"]=[W,D,TOP]
table_root["decor_position_hints"]="globe at x=+0.32; separate candlestick x=-0.47"
left_socket=bpy.data.objects.new("Candlestick_LEFT_PlacementOnly",None)
TABLE.objects.link(left_socket);left_socket.parent=table_root
left_socket.location=(-.48,-.055,TOP)
left_socket.empty_display_size=.045
left_socket.empty_display_type="SPHERE"
left_socket["purpose"]="Future independent unlit tabletop candleholder"

# Globe: deep aged brass base, antiqued globe cartography, axis/meridian.
gc=Vector((0,0,.436));radius=.269
for i,(z,r0,r1,h,m) in enumerate([
    (.036,.162,.135,.065,antique),
    (.077,.132,.115,.033,brass),
    (.128,.103,.058,.094,gold),
    (.195,.046,.038,.098,brass),
    (.262,.071,.053,.049,antique),
]):
    cyl(f"GLOBE_STAND_PlinthStage_{i}",(0,0,z),
        r0,r1,h,m,GLOBE,globe_root,20)
for s in [-1,1]:
    for t in [-1,1]:
        # Small decorative feet are not fused to the wooden desk.
        cube(f"GLOBE_BrassClawFoot_{s}_{t}",
             (s*.122,t*.109,.033),
             (.086,.063,.032),brass,GLOBE,globe_root,.011)
sphere("GLOBE_AtlasSphere_Parchment",gc,radius,parchment,
       GLOBE,globe_root,seg=32,rings=20)

# 360-degree heavy meridian sits outside the globe. Tilted ring reads as an
# authentic old scholar's globe from the three-quarter storyboard angle.
def orbit_circle(name,r,zcenter,material,width,tilt_deg=0,
                 yoffset=0,offset_angle=0,n=100):
    angle=math.radians(tilt_deg)
    coords=[]
    for k in range(n):
        q=math.tau*k/n+offset_angle
        x=r*math.cos(q)
        zz=r*math.sin(q)
        # Rotated normal around vertical, giving oblique circles in x/y.
        xx=x*math.cos(angle)
        yy=x*math.sin(angle)+yoffset
        coords.append((xx,yy,zcenter+zz))
    return path(name,coords,material,GLOBE,globe_root,width,True,2)

orbit_circle('RING_FacetedOuterMeridian',.312,gc.z,brass,.017,
             tilt_deg=33)
orbit_circle('RING_FineGoldInlayMeridian',.306,gc.z,gold,.005,
             tilt_deg=33,yoffset=-.020)
orbit_circle('RING_CrossMeridian',.285,gc.z,antique,.009,
             tilt_deg=-50)

# Actual tilted axial brass spindle and two pole fittings.
axis=Vector((math.sin(math.radians(21)),0,
             math.cos(math.radians(21)))).normalized()
rod('AXIS_BrassSpindle',gc-axis*.353,gc+axis*.353,
    .012,brass,GLOBE,globe_root,10)
for s in [-1,1]:
    p=gc+axis*(s*.319)
    sphere(f'AXIS_GoldCap_{s}',p,.023,gold,GLOBE,globe_root,12,8)
cyl('AXIS_FinialNorth',gc+axis*.369,.026,0,.067,
    gold,GLOBE,globe_root,12)

# Ornamental equator below hemisphere's broadest silhouette; not full metal
# grid on texture, just one credible brass latitude ring.
equator_points=[]
for i in range(96):
    a=math.tau*i/96
    equator_points.append((radius*math.cos(a)*1.01,
                           radius*math.sin(a)*1.01,gc.z))
path('RING_EquatorialBrassGraduation',equator_points,
     brass,GLOBE,globe_root,.006,True,1)

# Meridian engraved marks to give miniature historic-instrument readability.
for k in range(24):
    a=math.tau*k/24
    x=.312*math.cos(a)*math.cos(math.radians(33))
    y=.312*math.cos(a)*math.sin(math.radians(33))
    z=gc.z+.312*math.sin(a)
    center=Vector((x,y,z))
    towards=(center-gc).normalized()
    path(f"GRADATION_{k:02d}",
         [center-towards*.023,center+towards*.002],
         gold,GLOBE,globe_root,.003)

globe_root["dimensions_m_approx"]=[.68,.68,.80]
globe_root["positioning"]="Use at independent table top z=0.85; origin is bottom of globe stand"

def source_objs(c):
    return list(c.objects)

def export_glb(filename,c):
    bpy.ops.object.select_all(action='DESELECT')
    for o in c.objects:o.select_set(True)
    bpy.context.view_layer.objects.active=globe_root if c==GLOBE else table_root
    dst=OUT/filename
    bpy.ops.export_scene.gltf(filepath=str(dst),export_format='GLB',
                              use_selection=True,export_apply=True)
    return dst

def triangle_count(c):
    a=0
    for obj in c.objects:
        if obj.type=='MESH':
            a+=sum(len(poly.vertices)-2 for poly in obj.data.polygons)
    return a

# Combined staged view is editable in the source .blend.
globe_root.location=(.31,.015,TOP)

def light(name,pos,power,color,size):
    ld=bpy.data.lights.new(name,'AREA')
    ld.energy=power;ld.color=color;ld.size=size
    o=bpy.data.objects.new(name,ld);STUDIO.objects.link(o)
    o.location=pos
    aim=Vector((0,0,.74))
    o.rotation_euler=(aim-o.location).to_track_quat('-Z','Y').to_euler()
light('AmberPracticalKey',(-1.9,-1.8,2.9),430,(1,.84,.58),2.6)
light('CoolLibraryFill',(2.0,1.2,2.2),220,(.55,.70,1),2.1)
light('RimOnBrass',(1.3,1.0,2.3),300,(1,.83,.61),1.8)
cd=bpy.data.cameras.new('PREVIEW_CAMERA')
camera=bpy.data.objects.new('PREVIEW_CAMERA',cd)
STUDIO.objects.link(camera);scene.camera=camera
cd.type='ORTHO'

def render(name,position,target,scale):
    camera.location=position
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    cd.ortho_scale=scale
    scene.render.filepath=str(QA/(name+'.png'))
    bpy.ops.render.render(write_still=True)

render('01_StudySideTable_WithGlobe',(2.1,-3.1,2.25),(0,0,.73),2.25)
render('02_SideTable_Front',(0,-4,1.75),(0,0,.48),1.93)
render('03_Globe_CloseUp',(.9,-2.25,1.75),(.31,0,1.27),1.23)
render('04_Globe_Reverse',(-1.7,1.85,1.65),(.31,0,1.23),1.30)

# Save full-editable source first. LOD reductions are applied to background
# process only after save and do not change source .blend geometry.
bpy.ops.file.pack_all()
for a in bpy.context.screen.areas:
    if a.type=="VIEW_3D":
        space=a.spaces.active
        space.shading.type="MATERIAL"
        space.overlay.show_overlays=False
        space.region_3d.view_location=(0,0,.70)
        space.region_3d.view_distance=3.5
        space.region_3d.view_rotation=(
            Vector((0,0,.70))-Vector((2.1,-3.1,2.25))
        ).to_track_quat('-Z','Y')
bpy.context.preferences.filepaths.save_version=0
scene.render.filepath=str(QA/'01_StudySideTable_WithGlobe.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ScholarCorner_TableAndGlobe_v01.blend'),
                            compress=True)

# Export optimization only happens after saving the source .blend. Group by
# material for fewer Unity renderers while preserving editable source meshes.
globe_root.location=(0,0,0)
for c in (TABLE,GLOBE):
    for o in list(c.objects):
        if o.type=='CURVE':
            bpy.ops.object.select_all(action='DESELECT')
            o.select_set(True);bpy.context.view_layer.objects.active=o
            bpy.ops.object.convert(target='MESH')
    groups={}
    for o in list(c.objects):
        if o.type!='MESH':continue
        assert len(o.data.materials)==1,o.name
        groups.setdefault(o.data.materials[0].name,[]).append(o)
    for matname,group in groups.items():
        if len(group)<2:continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in group:o.select_set(True)
        bpy.context.view_layer.objects.active=group[0]
        bpy.ops.object.join()
        group[0].name='PACKED_'+matname

table_lod0_triangles=triangle_count(TABLE)
globe_lod0_triangles=triangle_count(GLOBE)
table_main=export_glb("ScholarSideTable_Walnut_v01.glb",TABLE)
globe_main=export_glb("AntiqueCelestialGlobe_Brass_v01.glb",GLOBE)

# Lightweight LOD1 exports: conservative decimation AFTER LOD0 export.
for c in (TABLE,GLOBE):
    for o in list(c.objects):
        if o.type!='MESH':continue
        if o.name.startswith("PACKED_GLOBE_WornParchment"):continue
        if o.name=="GLOBE_AtlasSphere_Parchment":continue
        faces=len(o.data.polygons)
        if faces<50:continue
        ratio=.66 if c==GLOBE else .72
        bpy.ops.object.select_all(action='DESELECT')
        o.select_set(True);bpy.context.view_layer.objects.active=o
        dec=o.modifiers.new("LOD1_BakedSimplification","DECIMATE")
        dec.ratio=ratio
        bpy.ops.object.modifier_apply(modifier=dec.name)

table_lod1=export_glb("ScholarSideTable_Walnut_v01_LOD1.glb",TABLE)
globe_lod1=export_glb("AntiqueCelestialGlobe_Brass_v01_LOD1.glb",GLOBE)

report={
 "source_blend":str(ROOT/'ScholarCorner_TableAndGlobe_v01.blend'),
 "separate_glbs":[str(table_main),str(globe_main)],
 "lod1_glbs":[str(table_lod1),str(globe_lod1)],
 "table_dimensions_m":[W,D,TOP],
 "globe_height_m_approx":.80,
 "table_triangles_lod0":table_lod0_triangles,
 "globe_triangles_lod0":globe_lod0_triangles,
 "table_triangles_lod1":triangle_count(TABLE),
 "globe_triangles_lod1":triangle_count(GLOBE),
 "table_meshes_export":sum(o.type=='MESH' for o in TABLE.objects),
 "globe_meshes_export":sum(o.type=='MESH' for o in GLOBE.objects),
 "original_rodin_credits_used":0,
 "existing_unity_scene_modified":False,
 "candleholder":"placement Empty only; actual candle holder not merged",
 "reference":"User's uploaded side-table and antique-globe crop"
}
(QA/'ScholarCorner_v01_BuildQA.json').write_text(json.dumps(report,indent=2))
print('SCHOLAR_CORNER_BUILD_OK',json.dumps(report))
