"""Fast, standalone, reproducible 3D prototype environments for story panels 01-04.

Produces two variants of the SAME family home, plus a battlefield.
These are DEFENSE PROTOTYPES, not accepted final photoreal assets.
No gameplay scenes, source character rigs or existing .blend files modified.

Usage:
  blender -b --factory-startup -t 4 --python build_prologue_defense_sets_v01.py
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OUT = ROOT / 'Assets/Cinematics/Storyboard3D/Environment/DefenseSets'
QA = ROOT / 'Library/StoryboardDefenseSets_20261023'
for p in (HERE, OUT, QA):
    p.mkdir(parents=True, exist_ok=True)

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = 1152
    scene.render.resolution_y = 648
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'Medium High Contrast'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.world = bpy.data.worlds.new('DefensePrototypeWorld')
    scene.world.color = (.18,.21,.23)
    geometry = bpy.data.collections.new('EXPORT__SCENERY')
    scene.collection.children.link(geometry)
    utility = bpy.data.collections.new('PREVIEW_ONLY')
    scene.collection.children.link(utility)
    return scene, geometry, utility

def material(name, color, metallic=0, rough=.82, emit=None, emit_power=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color,1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color,1)
    bs.inputs['Metallic'].default_value = metallic
    bs.inputs['Roughness'].default_value = rough
    if emit:
        bs.inputs['Emission Color'].default_value = (*emit,1)
        bs.inputs['Emission Strength'].default_value = emit_power
    return m

def box(c, name, location, dimensions, mat, rotation=None, parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    c.objects.link(obj)
    obj.name = name
    obj.dimensions = dimensions
    if rotation:
        obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if parent: obj.parent = parent
    return obj

def cylinder(c, name, loc, rad, depth, mat, vertices=12, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=rad,depth=depth,location=loc)
    obj=bpy.context.object
    for old in list(obj.users_collection):old.objects.unlink(obj)
    c.objects.link(obj)
    obj.name=name
    if rotation:obj.rotation_euler=rotation
    obj.data.materials.append(mat)
    return obj

def ico(c,name,loc,scale,mat,subdiv=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdiv,radius=1,location=loc)
    obj=bpy.context.object
    for old in list(obj.users_collection):old.objects.unlink(obj)
    c.objects.link(obj)
    obj.name=name;obj.scale=scale;obj.data.materials.append(mat)
    return obj

def empty(c,name,position,note=''):
    obj=bpy.data.objects.new(name,None)
    c.objects.link(obj);obj.location=position
    obj['cinematic_anchor']=note
    return obj

def beam(c,name,begin,end,width,mat):
    delta=Vector(end)-Vector(begin)
    middle=(Vector(begin)+Vector(end))*.5
    ob=box(c,name,middle,(width,width,delta.length),mat)
    ob.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return ob

def preview_setup(s,utility,camera_at,camera_look,lens=39,lights=None):
    camera=bpy.data.cameras.new('PREVIEW_CAMERA')
    cam=bpy.data.objects.new('PREVIEW_CAMERA',camera)
    utility.objects.link(cam);s.camera=cam
    cam.location=camera_at
    cam.rotation_euler=(Vector(camera_look)-cam.location).to_track_quat('-Z','Y').to_euler()
    camera.type='PERSP';camera.lens=lens
    for name,loc,power,color,radius in lights or []:
        ld=bpy.data.lights.new(name,'AREA')
        ld.energy=power;ld.size=radius;ld.color=color
        ob=bpy.data.objects.new(name,ld)
        utility.objects.link(ob);ob.location=loc
        ob.rotation_euler=(Vector(camera_look)-ob.location).to_track_quat('-Z','Y').to_euler()
    s.camera.data.clip_end=300

def export(s,geometry,name,description):
    src=HERE/(name+'.blend')
    glb=OUT/(name+'.glb')
    s.render.filepath=str(QA/(name+'_preview.png'))
    s.render.resolution_percentage=75
    s.render.engine='BLENDER_EEVEE'
    s.render.image_settings.file_format='PNG'
    bpy.ops.render.render(write_still=True)
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(src),compress=True,check_existing=False)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in geometry.objects:
        ob.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',
                              use_selection=True,export_apply=True)
    tri=sum(sum(max(0,len(p.vertices)-2) for p in o.data.polygons)
            for o in geometry.objects if o.type=='MESH')
    info={'name':name,'source':str(src),'glb':str(glb),
          'bytes':glb.stat().st_size,'triangles':tri,
          'objects':len(geometry.objects),'description':description}
    assert glb.stat().st_size>1000 and tri>150
    return info

def home(ruined=False):
    s,c,u=reset()
    stone=material('Home_StonePlaster',(.37,.31,.255) if not ruined else (.19,.19,.20))
    stone2=material('Home_StoneFrame',(.27,.245,.235) if not ruined else (.16,.16,.17))
    wood=material('Home_WalnutTimber',(.29,.132,.058) if not ruined else (.10,.077,.069))
    trim=material('Home_GoldenWindowTrim',(.44,.27,.12) if not ruined else (.22,.18,.14))
    floor=material('Home_WarmOakFloor',(.27,.148,.082) if not ruined else (.18,.17,.17))
    rug=material('Home_RedGoldRug',(.42,.14,.115) if not ruined else (.25,.16,.15))
    ochre=material('Home_WarmLight',(.98,.67,.30),0,.7,(1,.53,.15),.25)
    charcoal=material('Home_CharredWood',(.07,.059,.058))
    blue=material('Home_Nevali_BlueRibbon',(.04,.25,.66))
    blockcolors=[material('Toy_Block_'+n,v) for n,v in
                 [('Red',(.65,.15,.12)),('Gold',(.86,.57,.13)),
                  ('Green',(.16,.46,.30)),('Blue',(.12,.32,.68))]]
    # Exact same footprint and toy position in BOTH timeline variants.
    box(c,'FLOOR_SHARED_HOME',(0,0,-.14),(10,12,.25),floor)
    for i in range(11):
        box(c,f'FLOOR_WOOD_PLANK_{i:02d}',(-4.5+i*.9,0,-.005),(.013,11.9,.009),trim)
    if not ruined:
        box(c,'HOUSE_LEFT_WALL',(-5,0,2.10),(.26,12,4.2),stone)
        box(c,'HOUSE_RIGHT_WALL',(5,0,2.10),(.26,12,4.2),stone)
        box(c,'HOUSE_BACK_WALL',(0,5.95,2.10),(10,.25,4.2),stone)
    else:
        # Keep SAME structural footprint, but expose daylight through
        # collapsed wall spans instead of merely painting intact walls gray.
        box(c,'RUIN_LEFT_WALL_STUMP',(-5,1.9,1.08),(.26,7.4,2.16),stone)
        box(c,'RUIN_RIGHT_WALL_STUMP',(5,-2.1,1.48),(.26,7.4,2.95),stone)
        box(c,'RUIN_BACK_WALL_LEFT',(-3.75,5.95,1.8),(2.5,.25,3.6),stone)
        box(c,'RUIN_BACK_WALL_RIGHT',(3.82,5.95,1.3),(2.25,.25,2.6),stone)
    # The entry is open at x=-2.6 so the layered doorway camera sees Nevali.
    box(c,'DOOR_ENTRY_LEFT',(-4.17,-5.95,1.9),(1.63,.28,3.8),stone)
    box(c,'DOOR_ENTRY_RIGHT',(1.25,-5.95,1.9),(7.5,.28,3.8),stone)
    box(c,'DOOR_ENTRY_HEADER',(-2.42,-5.95,3.64),(2.0,.45,.44),wood)
    for x in (-3.53,-1.30):
        box(c,'DOOR_TRIM_'+str(x),(x,-5.94,1.77),(.15,.34,3.55),trim)
    # Room's east side has a warm visible window, matching the approved panel.
    if not ruined:
        box(c,'WINDOW_AMBER_GLASS',(4.81,-.25,2.68),(.025,2.8,1.6),
            material('Home_GlassHoney',(.88,.52,.18),.1,.16,(.6,.32,.13),.9))
    for y in (-1.72,1.22):
        box(c,'WINDOW_VERTICAL_FRAME_'+str(y),(4.74,y,2.68),(.10,.11,1.9),wood)
    box(c,'WINDOW_CROSSBAR',(4.72,-.25,2.73),(.1,3.1,.11),wood)
    box(c,'WINDOW_SILL',(4.56,-.25,1.78),(.50,3.2,.13),wood)
    # Back-wall timber and safe removable roof.
    for x in (-4.75,-1.5,1.5,4.75):
        box(c,'BACK_WALL_BEAM_'+str(x),(x,5.75,3.4),(.16,.23,1.5),wood)
    roof=empty(c,'ROOF_REMOVABLE__HIDE_FOR_CINEMATIC',(0,0,0),'Hide for camera cutaways')
    if not ruined:
        for idx,x in enumerate((-3.6,-1.8,0,1.8,3.6)):
            ob=beam(c,'ROOF_CROSSBEAM_'+str(idx),(x,-5.6,4.2),(x,5.6,4.2),.15,wood)
            ob.parent=roof
        # Wooden cabinets and filled shelves, seated background.
        for z in (0.9,1.65,2.4,3.15):
            box(c,'BOOKCASE_SHELF_'+str(z),(-4.55,2.50,z),(.65,3.7,.13),wood)
        for i in range(44):
            depth=1.05+(i//11)*.75
            z=.9+.13+(i//11)*.75
            y=1.02+(i%11)*.255
            cover=blockcolors[i%4] if i%7==0 else wood
            box(c,'BOOK_SPINE_%02d'%i,(-4.53,y,z+.18),(.36,.17,.36),cover)
        box(c,'STUDY_FAMILY_RUG',(1.22,-.47,.015),(4.7,4.8,.025),rug)
        # Thin gold border and scattered simple colored toys.
        for x in (-1.11,3.53):
            box(c,'RUG_EDGE_LONG_'+str(x),(x,-.47,.035),(.04,4.7,.018),trim)
        for y in (-2.81,1.87):
            box(c,'RUG_EDGE_SHORT_'+str(y),(1.22,y,.035),(4.55,.04,.018),trim)
        for i in range(16):
            x=.24+(i%4)*.36; y=-.82+(i//4)*.41
            box(c,'TOY_BUILDING_BLOCK_%02d'%i,(x,y,.085+(i%3)*.05),
                (.29,.29,.19),blockcolors[(i+2)%4])
        # Children's block tower, deliberately separate manipulable props.
        for i in range(4):
            box(c,'TOY_TOWER_BLOCK_%02d'%i,(2.12,-.10,.16+i*.23),
                (.28,.28,.22),blockcolors[i])
        box(c,'FAMILY_TABLE',(2.63,3.11,.95),(2.5,1.35,.14),wood)
        for x in (1.6,3.65):
            for y in (2.67,3.57):
                box(c,'FAMILY_TABLE_LEG_'+str(x)+'_'+str(y),(x,y,.47),(.12,.12,.89),wood)
        for x in (2.55,3.57):
            box(c,'FAMILY_CHAIR_SEAT_'+str(x),(x,1.85,.55),(.58,.57,.10),wood)
            box(c,'FAMILY_CHAIR_BACK_'+str(x),(x,2.10,1.03),(.58,.12,.98),wood)
        cylinder(c,'WARM_WALL_SCONCE',(-3.10,5.64,2.37),.13,.10,trim)
        ico(c,'WARM_SCONCE_AMBER',(-3.10,5.59,2.55),(.13,.16,.24),ochre)
        for k in range(3):
            ico(c,'WINDOW_POTTED_GREENERY_'+str(k),(4.48,-1.1+k*.72,1.98),
                (.17,.23,.18),blockcolors[2])
    else:
        # On top of SAME foundation, visual damage is composed as a separate
        # environment variant. Do not add a new body/character stand-in.
        for i in range(11):
            x=-4.55+(i%5)*2.22;y=-4.7+(i//5)*3.7
            ob=box(c,'CHARRED_RUBBLE_BEAM_%02d'%i,(x,y,.20),
                   (.22,1.35,.22),charcoal,(.02*i,0,.24+(.23*i)))
        for i in range(56):
            x=-4.4+((i*7)%39)*.22; y=-5.2+((i*17)%44)*.24
            size=.10+(i%6)*.07
            ico(c,'HOME_RUBBLE_%02d'%i,(x,y,.03),(.4*size,.8*size,size),stone2)
        for i in range(12):
            ob=box(c,'BROKEN_WOOD_PLANK_%02d'%i,(-3.7+(i%6)*1.35,-2+(i//6)*2.7,.2),
                   (.14,1.3,.13),wood,(0,(i%3)*.10,i*.42))
        box(c,'RUIN_SINGED_RUG',(1.22,-.47,.01),(3.9,3.6,.024),rug)
        for i in range(9):
            box(c,'RUIN_SCATTERED_BLOCK_%02d'%i,(-.1+(i%5)*.38,-1+(i//5)*.8,.09),
                (.22,.22,.21),blockcolors[i%4],(0,0,.2*i))
        # Story's specific blue hair ribbon. An unlit, compact independent prop.
        box(c,'NEVALI_BLUE_RIBBON_EVIDENCE',(1.11,-1.14,.085),(.42,.07,.012),blue,(0,0,.38))
        box(c,'NEVALI_BLUE_RIBBON_LOOP',(1.18,-1.10,.10),(.15,.14,.015),blue,(0,0,.20))
        empty(c,'ANCHOR_VAN_KNEEL',(0.0,-1.7,0),'Place grief performer here')
        empty(c,'ANCHOR_WIFE_RIG_REQUIRED',(1.4,-1.9,0),'Optional non-gore approved wife pose; asset not provided')
    empty(c,'ANCHOR_VAN_YOUNG',(-2.55,-4.68,0),
          '01: Stand watching daughter from behind entrance door')
    empty(c,'ANCHOR_NEVALI_CHILD',(1.28,-.45,0),
          '01: Play with colored blocks, animation reference plate required')
    if not ruined:
        # Place reference camera INSIDE the door opening. From outside the
        # wall obscures the frame even though the room geometry is intact.
        preview_setup(s,u,(-2.45,-5.20,2.12),(1.25,-.42,1.20),39,[
            ('WarmWindowArea',(3.7,.6,3.5),1950,(1,.70,.37),5),
            ('RoomFill',(-2.2,-3.9,4),900,(.77,.82,1),6)])
        name='Prologue_Home_01_Peaceful_v01'
    else:
        # Ruined-house walls still stand enough for framing: the preview
        # camera is *inside*, and the key lights point at its actual rubble.
        preview_setup(s,u,(-2.25,-4.85,2.22),(.25,-1.30,.70),42,[
            ('OvercastWarSky',(0,-1,5.2),620,(.56,.61,.76),6),
            ('DesaturatedRubbleFill',(-3,-3,3.1),280,(.91,.61,.41),5)])
        name='Prologue_Home_04_Ruined_v01'
    return export(s,c,name,'Shared home geometry, '+('destroyed grief variant' if ruined else 'warm Nevali block-playing variant'))

def battlefield():
    s,c,u=reset()
    ground=material('War_BlackGrayRoad',(.115,.109,.125))
    stone=material('War_BrokenGrayStone',(.22,.205,.22))
    dark=material('War_CharredTimber',(.085,.079,.083))
    wall=material('War_BurnedMasonry',(.18,.155,.16))
    purple=material('War_BrokenLanguageViolet',(.22,.045,.42),.1,.29,(.42,.09,.78),1.4)
    ember=material('War_DistantEmbers',(.76,.19,.055),0,.55,(1,.19,.03),.5)
    mana=material('War_WordMagicBlue',(.11,.46,.89),.28,.21,(.08,.40,.95),1.8)
    letter=material('War_GoldLetters',(.89,.55,.16),.35,.27,(.6,.31,.06),.40)
    silhouette=material('War_EntityProxy_NotFinal',(.035,.028,.062))
    box(c,'BATTLEFIELD_COBBLED_GROUND',(0,0,-.21),(29,40,.3),ground)
    for i in range(9):
        box(c,'ROAD_STONE_RIB_%02d'%i,(0,-17.5+4.3*i,-.052),(4,.12,.06),stone)
    # Rear skyline: broken medieval buildings and towers frame a distant
    # shadow, with central clear fighting aisle for camera and Van.
    for side in (-1,1):
        for i in range(9):
            x=side*(5.2+(i%3)*3.3)
            y=-15.5+(i//3)*11.2
            h=2.4+(i*3%7)*.7
            box(c,'DESTROYED_BUILDING_%s_%02d'%(side,i),(x,y,h*.5),
                (1.9+(.3*i%1),3.5,h),wall,(0,0,.06*(i%4)))
            box(c,'COLLAPSED_PARAPET_%s_%02d'%(side,i),(x,y,h+.07),
                (2.15,3.7,.17),dark,(0,.06*i,.09*i))
            for j in range(3):
                ico(c,'BATTLE_RUBBLE_%s_%02d_%02d'%(side,i,j),
                    (x+((j-1)*.8),y+2.0+j*.4,.08),(.28,.5,.21),stone)
    # World language corruption is abstract, not a named additional monster.
    for i in range(19):
        x=-10+(i*7%21); y=-14+(i*11%30)
        if abs(x)<2 and y<9:continue
        size=1+(i%5)*.46
        obj=beam(c,'VIOLET_CORRUPTION_SPIRE_%02d'%i,
              (x,y,0),(x+.23,y+.33,size*2.7),.19,purple)
    # Colossal dark looming form, visibly a temporary silhouette.
    ico(c,'ENTITY_ABSTRACT_HEAD_PROXY',(0,14.3,7.5),(1.25,1.0,1.52),silhouette,2)
    box(c,'ENTITY_ABSTRACT_TORSO_PROXY',(0,14.3,4.8),(2.1,1.45,5.0),silhouette)
    for side in (-1,1):
        beam(c,'ENTITY_VEIL_%s'%side,(side*.9,14.2,6.1),
             (side*4.0,16.1,3.8),.65,silhouette)
        ico(c,'ENTITY_DISTANT_VIOLET_EYE_%s'%side,
            (side*.51,13.20,7.65),(.11,.06,.15),purple,2)
    # Readable miniature letter tiles A-G are STATIC proxy positions, so the
    # eventual attack can activate/move them as separate pieces.
    for i,ch in enumerate('ABCDEFG'):
        angle=math.radians(15+i*11)
        x=-1.5+3.2*math.sin(angle)
        y=1.6+i*.9
        z=1.25+i*.42
        box(c,'WORD_MAGIC_TILE_'+ch,(x,y,z),(.52,.12,.55),mana,(0,.10*i,.07*i))
        curve=bpy.data.curves.new('GLYPH_'+ch,'FONT')
        curve.body=ch;curve.size=.38;curve.align_x='CENTER';curve.align_y='CENTER'
        ob=bpy.data.objects.new('WORD_MAGIC_LETTER_'+ch,curve)
        c.objects.link(ob)
        ob.location=(x,y-.071,z-.14)
        ob.rotation_euler=(math.radians(90),0,0)
        curve.materials.append(letter)
    # Low destructive buildings can silhouette camera at 30mm from rear.
    for i in range(9):
        x=-11+(i%4)*7.6; y=7+(i//4)*5.5
        ico(c,'WAR_SMOKE_PROXY_%02d'%i,(x,y,3.4+(i%3)*1.5),
            (1.5,1.2,1.9),dark)
    empty(c,'ANCHOR_VAN_WAR_FIGHT',(-.75,-9.8,0),'Young Van with cyan staff, low rear shot 03')
    empty(c,'ANCHOR_VAN_WAR_ESTABLISH',(-2.9,-13.5,0),'Panel 02 city-wide shot')
    empty(c,'ANCHOR_ENTITY_NOT_A_NEW_CHARACTER',(0,14.2,0),'Abstract approved corruption silhouette')
    preview_setup(s,u,(-5.4,-25,7.8),(0,9,3.0),29,[
        ('BattleBlueMoon',(1,-2,16),2700,(.47,.57,.96),11),
        ('RubbleAmber',(-14,8,8),880,(1,.45,.2),9),
        ('VioletCorruption',(13,16,8),1050,(.51,.22,.85),8)])
    # glTF exports meshes, not text curves. Convert the seven glyphs but keep
    # their own meshes/transform names for later Unity attack animation.
    for ob in list(c.objects):
        if ob.type=='FONT':
            bpy.ops.object.select_all(action='DESELECT')
            ob.select_set(True)
            bpy.context.view_layer.objects.active=ob
            bpy.ops.object.convert(target='MESH')
    return export(s,c,'Prologue_GreatWar_02_03_v01',
                  'Ruined fantasy city, faceless shadow silhouette, violet corruption and static A-G spell tiles')

reports=[home(False),battlefield(),home(True)]
(QA/'PrototypeEnvironmentQA.json').write_text(json.dumps({
    'status':'DEFENSE_ONLY__PROTOTYPE',
    'source_story_panels':[1,2,3,4],
    'existing_gameplay_or_story_scenes_changed':False,
    'missing_characters':'Young Van, Nevali, wife: actor rigs not fabricated',
    'materials':'native Blender PBR exported via glTF',
    'environments':reports},indent=2),encoding='utf8')
print('PROLOGUE_DEFENSE_SETS_BUILT '+json.dumps(reports))
