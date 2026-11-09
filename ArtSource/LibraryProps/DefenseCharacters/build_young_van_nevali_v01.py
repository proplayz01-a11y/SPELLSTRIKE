"""Two editable, rigged, storyboard-inspired low-poly character stand-ins.

Goal: usable 3D Van (black hair, clean-shaven, blue/gold robe) and child
Nevali (chestnut hair, blue/cream dress) for a quick defense prototype.
These are NOT artist-approved final sculpts or final mocap-ready Humanoid rigs.
No modifications to existing Older Van, Vander, or production prefabs.

Blender 5.0+:
 blender -b -t 3 --factory-startup --python build_young_van_nevali_v01.py
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[2]
EXPORT=PROJECT/'Assets/Cinematics/Storyboard3D/Characters/DefensePrototypes'
QA=PROJECT/'Library/StoryboardDefenseCharacters_20261023'
for d in (HERE,EXPORT,QA): d.mkdir(parents=True,exist_ok=True)

def mat(name,col,metal=0,rough=.78):
 m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF')
 p.inputs['Base Color'].default_value=(*col,1)
 p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 return m

def init_world():
 bpy.ops.wm.read_factory_settings(use_empty=True)
 s=bpy.context.scene
 s.render.engine='BLENDER_EEVEE'
 s.render.resolution_x=960;s.render.resolution_y=960
 s.render.resolution_percentage=75
 s.render.image_settings.file_format='PNG'
 s.render.film_transparent=False
 s.view_settings.view_transform='Standard'
 s.view_settings.look='Medium High Contrast'
 if s.world is None:s.world=bpy.data.worlds.new('CharacterPreviewWorld')
 s.world.color=(.27,.3,.35)
 export=bpy.data.collections.new('SKINNED_CHARACTER_EXPORT')
 s.collection.children.link(export)
 preview=bpy.data.collections.new('PREVIEW_ONLY__NO_GLTF_EXPORT')
 s.collection.children.link(preview)
 return s,export,preview

def rig_create(collection,scale,short_name):
 # Positions in metres; forearms/legs have independent articulated joints.
 p=lambda x,y,z:(x*scale,y*scale,z*scale)
 joints=[
 ('Root',p(0,0,.05),p(0,0,.92),None),
 ('Hips',p(0,0,.92),p(0,0,1.12),'Root'),
 ('Spine',p(0,0,1.12),p(0,0,1.44),'Hips'),
 ('Chest',p(0,0,1.44),p(0,0,1.66),'Spine'),
 ('Neck',p(0,0,1.66),p(0,0,1.76),'Chest'),
 ('Head',p(0,0,1.76),p(0,0,2.13),'Neck'),
 ('LeftUpperArm',p(-.29,0,1.58),p(-.44,0,1.25),'Chest'),
 ('LeftLowerArm',p(-.44,0,1.25),p(-.46,-.015,.99),'LeftUpperArm'),
 ('LeftHand',p(-.46,-.015,.99),p(-.46,-.04,.84),'LeftLowerArm'),
 ('RightUpperArm',p(.29,0,1.58),p(.44,0,1.25),'Chest'),
 ('RightLowerArm',p(.44,0,1.25),p(.46,-.015,.99),'RightUpperArm'),
 ('RightHand',p(.46,-.015,.99),p(.46,-.04,.84),'RightLowerArm'),
 ('LeftUpperLeg',p(-.135,0,.94),p(-.155,-.005,.51),'Hips'),
 ('LeftLowerLeg',p(-.155,-.005,.51),p(-.16,-.018,.11),'LeftUpperLeg'),
 ('LeftFoot',p(-.16,-.018,.11),p(-.16,-.23,.065),'LeftLowerLeg'),
 ('RightUpperLeg',p(.135,0,.94),p(.155,-.005,.51),'Hips'),
 ('RightLowerLeg',p(.155,-.005,.51),p(.16,-.018,.11),'RightUpperLeg'),
 ('RightFoot',p(.16,-.018,.11),p(.16,-.23,.065),'RightLowerLeg'),
 ]
 data=bpy.data.armatures.new(short_name+'_ArmatureData')
 rig=bpy.data.objects.new(short_name+'_ARMATURE',data);collection.objects.link(rig)
 bpy.context.view_layer.objects.active=rig;rig.select_set(True)
 bpy.ops.object.mode_set(mode='EDIT')
 for name,head,tail,parent in joints:
  bone=data.edit_bones.new(name);bone.head=head;bone.tail=tail
  if parent:bone.parent=data.edit_bones[parent]
 bpy.ops.object.mode_set(mode='OBJECT')
 rig.show_in_front=True
 rig['rig_status']='Prototype bone-weighted parts; verify Humanoid retarget before using Mixamo'
 return rig

def keep(o,collection,name,material,bone,rig):
 for cc in list(o.users_collection):cc.objects.unlink(o)
 collection.objects.link(o);o.name=name
 o.data.materials.append(material)
 if bone:
  v=o.vertex_groups.new(name=bone)
  v.add(list(range(len(o.data.vertices))),1.0,'REPLACE')
  mod=o.modifiers.new('ArmatureSkin','ARMATURE')
  mod.object=rig
  o.parent=rig
 return o

def cube(c,name,loc,dimensions,ma,bone,rig,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
 o=bpy.context.object;o.dimensions=dimensions
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('SoftenedCorner','BEVEL');mod.width=bevel
  mod.segments=1
  bpy.context.view_layer.objects.active=o
  bpy.ops.object.modifier_apply(modifier=mod.name)
  o.modifiers.new('WeightedNormals','WEIGHTED_NORMAL')
 return keep(o,c,name,ma,bone,rig)

def ellipsoid(c,name,loc,dim,ma,bone,rig,segments=16,rings=8):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc)
 o=bpy.context.object
 o.scale=(dim[0]/2,dim[1]/2,dim[2]/2)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return keep(o,c,name,ma,bone,rig)

def cone(c,name,loc,r0,r1,depth,ma,bone,rig,sides=12):
 bpy.ops.mesh.primitive_cone_add(vertices=sides,radius1=r0,radius2=r1,depth=depth,location=loc)
 return keep(bpy.context.object,c,name,ma,bone,rig)

def beam(c,name,a,b,width,ma,bone,rig):
 a=Vector(a);b=Vector(b);diff=b-a
 ob=cube(c,name,(a+b)*.5,(width,width,diff.length),ma,bone,rig)
 ob.rotation_euler=diff.to_track_quat('Z','Y').to_euler()
 return ob

def hairlock(c,name,start,end,width,ma,rig):
 return beam(c,name,start,end,width,ma,'Head',rig)

def anatomy(c,rig,s,mats,young):
 P=lambda x,y,z:(x*s,y*s,z*s)
 flesh=mats['skin'];navy=mats['navy'];gold=mats['gold']
 ink=mats['dark'];boot=mats['boots'];pants=mats['pants']
 robe=mats['robe'];hair=mats['hair'];eye=mats['eyes']
 # Sculptural rigged surfaces: single bone per part to keep joint blocks
 # independent, minimal-poly and straightforward to animate for defense.
 ellipsoid(c,'HIPS_Sculpt',P(0,0,.99),P(.47,.31,.35),robe,'Hips',rig)
 ellipsoid(c,'TORSO_Sculpt',P(0,0,1.36),P(.62,.37,.69),robe,'Spine',rig)
 ellipsoid(c,'SHOULDER_Collar',P(0,0,1.60),P(.69,.47,.17),navy,'Chest',rig)
 ellipsoid(c,'NECK',P(0,-.005,1.73),P(.16,.14,.15),flesh,'Neck',rig)
 ellipsoid(c,'FACE_CleanShaven' if young else 'FACE_Nevali',
           P(0,-.028,1.96),P(.33,.32,.40),flesh,'Head',rig)
 for side in (-1,1):
  label='Left' if side<0 else 'Right'
  ellipsoid(c,label+'_Ear',P(side*.177,-.015,1.945),
            P(.095,.07,.13),flesh,'Head',rig,12,7)
  ellipsoid(c,label+'_Eye_White',P(side*.080,-.170,1.986),
            P(.087,.027,.075),mats['white'],'Head',rig,12,6)
  ellipsoid(c,label+'_Eye_Iris',P(side*.080,-.191,1.984),
            P(.043,.019,.049),eye,'Head',rig,12,6)
  ellipsoid(c,label+'_Pupil',P(side*.080,-.204,1.985),
            P(.023,.015,.039),ink,'Head',rig,12,6)
  ellipsoid(c,label+'_Brow',P(side*.080,-.171,2.045),
            P(.097,.024,.025),hair,'Head',rig,12,6)
 ellipsoid(c,'Nose',P(0,-.193,1.924),P(.060,.077,.092),flesh,'Head',rig,12,6)
 ellipsoid(c,'Mouth',P(0,-.181,1.859),
           P(.09,.017,.014),mats['lips'],'Head',rig,12,6)
 for side in (-1,1):
  label='Left' if side<0 else 'Right'
  ellipsoid(c,label+'_UpperSleeve',P(side*.355,0,1.43),
            P(.24,.24,.41),navy,'LeftUpperArm' if side<0 else 'RightUpperArm',rig)
  ellipsoid(c,label+'_LowerSleeve',P(side*.450,-.010,1.15),
            P(.23,.23,.31),robe,'LeftLowerArm' if side<0 else 'RightLowerArm',rig)
  ellipsoid(c,label+'_Hand',P(side*.460,-.03,.92),
            P(.135,.11,.20),flesh,'LeftHand' if side<0 else 'RightHand',rig)
  ellipsoid(c,label+'_UpperLeg',P(side*.145,0,.74),
            P(.24,.24,.46),pants,'LeftUpperLeg' if side<0 else 'RightUpperLeg',rig)
  ellipsoid(c,label+'_LowerLeg',P(side*.153,-.018,.31),
            P(.19,.19,.43),pants,'LeftLowerLeg' if side<0 else 'RightLowerLeg',rig)
  ellipsoid(c,label+'_Boot',P(side*.155,-.10,.095),
            P(.20,.34,.17),boot,'LeftFoot' if side<0 else 'RightFoot',rig)
  # Separate metallic cuffs make the design read at medium distance.
  ellipsoid(c,label+'_GoldWrist',P(side*.46,-.01,1.06),
            P(.235,.23,.035),gold,'LeftLowerArm' if side<0 else 'RightLowerArm',rig,12,6)
 if young:
  # Coat tails split, so both legs retain silhouette when animated.
  for side in (-1,1):
   cside='Left' if side<0 else 'Right'
   ellipsoid(c,cside+'_NavyCoatTail',P(side*.22,.068,.77),
             P(.32,.25,.79),navy,'Hips',rig,12,6)
   beam(c,cside+'_GoldCoatEdge',P(side*.36,-.177,1.35),
        P(side*.31,-.177,.48),.031,gold,'Spine',rig)
  beam(c,'FrontGoldCoatVertical',P(-.08,-.186,1.13),P(-.08,-.186,1.57),
       .027,gold,'Spine',rig)
  ellipsoid(c,'RoundGoldChestClasp',P(0,-.222,1.588),
            P(.20,.066,.18),gold,'Chest',rig,16,8)
  ellipsoid(c,'CloakBlueShoulderDrape',P(0,.137,1.46),
            P(.54,.13,.55),navy,'Chest',rig)
  ellipsoid(c,'BlackHairCap',P(0,0,2.159),P(.35,.36,.26),hair,'Head',rig)
  for idx in range(9):
   x=(idx-4)*.037
   endx=x-.03 if idx<4 else x+.025
   hairlock(c,'ShortMessyBlackHair_%02d'%idx,P(x,-.17,2.23),
            P(endx,-.21,2.11+(idx%3)*.018),.053,hair,rig)
  for side in (-1,1):
   hairlock(c,'Sideburn_%s'%side,P(side*.17,-.058,2.19),
            P(side*.17,-.06,2.02),.043,hair,rig)
  # Attached simple staff holder mark; prop remains optional for Panel 1.
  rig['story']='Young Van Aert, black haired and clean-shaven, darker era'
 else:
  # Blue bodice and cream skirt with classic child silhouette.
  cone(c,'NEVALI_CREAM_DRESS_SKIRT',P(0,0,.75),.37*s,.205*s,.67*s,
       mats['cream'],'Hips',rig,16)
  ellipsoid(c,'NEVALI_BLUE_BODICE',P(0,-.14,1.375),
            P(.43,.12,.47),mats['blue'],'Spine',rig)
  for x in (-.20,.20):
   beam(c,'NEVALI_BLUE_SHOULDER_STRAP_'+str(x),P(x,-.165,1.34),
        P(x*.8,-.14,1.62),.07,mats['blue'],'Chest',rig)
  ellipsoid(c,'NEVALI_BRASS_STAR_BROOCH',P(0,-.205,1.53),
            P(.13,.046,.14),gold,'Chest',rig,12,6)
  ellipsoid(c,'NEVALI_CHEST_BLUE_BOW',P(0,-.22,1.51),
            P(.17,.045,.09),mats['blue'],'Chest',rig,12,6)
  # A childish full chestnut fringe and long locks tied with blue ribbons.
  ellipsoid(c,'NEVALI_CHESTNUT_HAIR_CAP',P(0,.018,2.16),
            P(.39,.38,.28),hair,'Head',rig)
  for idx in range(10):
   x=(idx-4.5)*.033
   hairlock(c,'NEVALI_BANGS_%02d'%idx,P(x,-.182,2.23),
            P(x*.82,-.215,2.09+(idx%2)*.022),.049,hair,rig)
  for side in (-1,1):
   for n in range(4):
    xi=side*(.18+n*.042)
    hairlock(c,'NEVALI_LONG_HAIR_%s_%d'%(side,n),P(xi,.11,2.14),
             P(side*(.22+n*.025),.08,1.63-(n%3)*.09),
             .058,hair,rig)
   ellipsoid(c,'NEVALI_BLUE_HAIR_RIBBON_%s'%side,
             P(side*.27,-.03,1.91),P(.14,.12,.10),
             mats['blue'],'Head',rig,12,6)
  rig['story']='Nevali, child with long chestnut hair, blue hair ribbon and cream/blue dress'

def reset_pose(rig):
 for pb in rig.pose.bones:
  pb.rotation_mode='XYZ'
  pb.rotation_euler=(0,0,0)
  pb.location=(0,0,0)

def action(rig,name,frames):
 reset_pose(rig)
 rig.animation_data_create()
 clip=bpy.data.actions.new(name)
 rig.animation_data.action=clip
 all_bones=['Head','Chest','Spine','LeftUpperArm','RightUpperArm',
            'LeftLowerArm','RightLowerArm','LeftUpperLeg','RightUpperLeg',
            'LeftLowerLeg','RightLowerLeg']
 for f,poses in frames:
  for bone in all_bones:
   pb=rig.pose.bones[bone]
   pb.rotation_euler=poses.get(bone,(0,0,0))
   pb.keyframe_insert(data_path='rotation_euler',frame=f,group=bone)
 # Put performance actions on identifiable NLA tracks; no destructive bake.
 strip_track=rig.animation_data.nla_tracks.new()
 strip_track.name=name
 strip=strip_track.strips.new(name,1,clip)
 strip.action_frame_start=1
 strip.action_frame_end=max(f for f,_ in frames)
 # Avoid unintentionally mixing every recorded clip in the Blender scene:
 strip_track.mute=True
 rig.animation_data.action=None
 reset_pose(rig)
 return clip

def add_preview(s,c,preview,short_name):
 cam=bpy.data.cameras.new('PREVIEW_CharacterCamera')
 cam.lens=45
 o=bpy.data.objects.new('PREVIEW_CharacterCamera',cam)
 preview.objects.link(o)
 o.location=(2.8,-4.6,2.0)
 o.rotation_euler=(Vector((0,0,1.15))-o.location).to_track_quat('-Z','Y').to_euler()
 s.camera=o
 def light(name,xyz,power,rgb,size):
  d=bpy.data.lights.new(name,'AREA')
  d.energy=power;d.size=size;d.color=rgb
  obj=bpy.data.objects.new(name,d);preview.objects.link(obj)
  obj.location=xyz
  obj.rotation_euler=(Vector((0,0,1.2))-obj.location).to_track_quat('-Z','Y').to_euler()
 light('PREVIEW_WarmKey',(1,-3.2,3.7),500,(1,.82,.65),4)
 light('PREVIEW_CoolRim',(-3,2.3,3.0),400,(.62,.75,1),3)
 cube(preview,'PREVIEW_FLOOR',(0,0,-.07),(3,3,.14),
      mat('PreviewFloor',(.21,.24,.31)),None,None)
 s.render.filepath=str(QA/(short_name+'_preview.png'))
 bpy.ops.render.render(write_still=True)

def build(is_van):
 s,c,u=init_world()
 name='YoungVanAert_Prototype_v01' if is_van else 'Nevali_Prototype_v01'
 scale=1 if is_van else .70
 color=dict(
  skin=mat('Skin_Warm',(.76,.54,.38) if is_van else (.91,.66,.49)),
  navy=mat('DeepNavyFabric',(.028,.079,.19) if is_van else (.91,.77,.57)),
  robe=mat('RobeFabric',(.05,.12,.23) if is_van else (.92,.82,.66)),
  gold=mat('AntiqueGold',(.76,.47,.11),.64,.32),
  dark=mat('DeepBrownEyes',(.032,.024,.026)),
  pants=mat('DarkTrouser',(.09,.073,.11) if is_van else (.82,.66,.50)),
  boots=mat('BootLeather',(.13,.069,.042) if is_van else (.38,.20,.12)),
  hair=mat('BlackNaturalHair',(.041,.029,.033) if is_van else (.29,.125,.059)),
  white=mat('EyeWhite',(.94,.86,.77)),
  eyes=mat('BlueIris',(.21,.50,.72)),
  lips=mat('LipRose',(.53,.22,.21)),
  cream=mat('CreamChildDress',(.95,.82,.64)),
  blue=mat('NevaliBlue',(.065,.26,.64))
 )
 rig=rig_create(c,scale,name)
 anatomy(c,rig,scale,color,is_van)
 if is_van:
  action(rig,'YoungVan_WatchingDaughter_Idle_v01',[
   (1,{'Head':(-.04,0,-.03),'Chest':(.018,0,0),
       'LeftUpperArm':(.19,0,-.26),'RightUpperArm':(.19,0,.26),
       'LeftLowerArm':(.5,0,-.2),'RightLowerArm':(.5,0,.2)}),
   (55,{'Head':(-.10,0,-.09),'Chest':(.031,0,0),
       'LeftUpperArm':(.22,0,-.26),'RightUpperArm':(.22,0,.26),
       'LeftLowerArm':(.52,0,-.2),'RightLowerArm':(.52,0,.2)}),
   (109,{'Head':(-.04,0,-.03),'Chest':(.018,0,0),
       'LeftUpperArm':(.19,0,-.26),'RightUpperArm':(.19,0,.26),
       'LeftLowerArm':(.5,0,-.2),'RightLowerArm':(.5,0,.2)})])
 else:
  action(rig,'Nevali_ReachingPlayingToyBlocks_v01',[
   (1,{'Head':(.17,0,.07),'Chest':(.045,0,0),
       'LeftUpperArm':(-.55,0,.10),'RightUpperArm':(-.65,0,-.10),
       'LeftLowerArm':(-.14,0,-.05),'RightLowerArm':(-.28,0,.08),
       'LeftUpperLeg':(.52,0,.02),'RightUpperLeg':(.52,0,-.02),
       'LeftLowerLeg':(-.50,0,0),'RightLowerLeg':(-.50,0,0)}),
   (44,{'Head':(.20,0,-.12),'Chest':(.08,0,0),
       'LeftUpperArm':(-.65,0,.08),'RightUpperArm':(-1.06,0,.20),
       'LeftLowerArm':(-.25,0,-.05),'RightLowerArm':(-.50,0,.20),
       'LeftUpperLeg':(.52,0,.02),'RightUpperLeg':(.52,0,-.02),
       'LeftLowerLeg':(-.50,0,0),'RightLowerLeg':(-.50,0,0)}),
   (84,{'Head':(.18,0,.09),'Chest':(.05,0,0),
       'LeftUpperArm':(-.55,0,.10),'RightUpperArm':(-.85,0,.10),
       'LeftLowerArm':(-.20,0,0),'RightLowerArm':(-.13,0,-.05),
       'LeftUpperLeg':(.52,0,.02),'RightUpperLeg':(.52,0,-.02),
       'LeftLowerLeg':(-.50,0,0),'RightLowerLeg':(-.50,0,0)}),
   (121,{'Head':(.17,0,.07),'Chest':(.045,0,0),
       'LeftUpperArm':(-.55,0,.10),'RightUpperArm':(-.65,0,-.10),
       'LeftLowerArm':(-.14,0,-.05),'RightLowerArm':(-.28,0,.08),
       'LeftUpperLeg':(.52,0,.02),'RightUpperLeg':(.52,0,-.02),
       'LeftLowerLeg':(-.50,0,0),'RightLowerLeg':(-.50,0,0)})])

 add_preview(s,c,u,name)
 bpy.context.preferences.filepaths.save_version=0
 source=HERE/(name+'.blend')
 bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True,check_existing=False)
 bpy.ops.object.select_all(action='DESELECT')
 for ob in c.objects:ob.select_set(True)
 bpy.context.view_layer.objects.active=rig
 glb=EXPORT/(name+'.glb')
 bpy.ops.export_scene.gltf(filepath=str(glb),export_format='GLB',
    use_selection=True,export_animations=True,export_animation_mode='ACTIONS',
    export_apply=False)
 meshes=[o for o in c.objects if o.type=='MESH']
 faces=sum(sum(max(0,len(p.vertices)-2) for p in o.data.polygons) for o in meshes)
 report=dict(name=name,blend=str(source),glb=str(glb),
             exported_bytes=glb.stat().st_size,meshes=len(meshes),
             triangles=faces,armature_bones=len(rig.data.bones),
             actions=[a.name for a in bpy.data.actions],
             accepted_final_sculpt=False,
             evaluated_mocap_avatar=False)
 print('CHARACTER_DEFENSE_BUILT',json.dumps(report))
 return report

results=[build(True),build(False)]
(QA/'CharacterPrototypeBuildQA.json').write_text(
 json.dumps({'status':'STORYBOARD_RECOGNIZABLE_UNPOLISHED_PROTOTYPES',
             'characters':results,'gameplay_prefabs_touched':False},indent=2),
 encoding='utf8')
