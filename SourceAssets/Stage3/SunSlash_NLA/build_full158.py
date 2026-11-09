import bpy,pathlib,json
from mathutils import Vector
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/SunSlash_NLA';D=O/'Full158';D.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_SunSlash_NLA_Aligned.blend'));sc=bpy.context.scene;rig=bpy.data.objects['Armature'];ad=rig.animation_data
for t in list(ad.nla_tracks):ad.nla_tracks.remove(t)
ad.action=bpy.data.actions['ADAPTED_Inward'];ad.action_slot=ad.action.slots[0];sc.frame_set(1);initial=rig.pose.bones['mixamorig:Hips'].location.copy()
for tag in ['Inward','Slash']:
 a=bpy.data.actions['ADAPTED_'+tag].copy();a.name='FULL158_'+tag;a.use_fake_user=True
 for layer in a.layers:
  for st in layer.strips:
   for cb in st.channelbags:
    for fc in cb.fcurves:
     if fc.data_path=='pose.bones["mixamorig:Hips"].location' and fc.array_index in (0,2):
      for k in fc.keyframe_points:k.co.y=initial[fc.array_index];k.handle_left.y=k.co.y;k.handle_right.y=k.co.y
ad.action=None
for name,action,start,end,lo,hi,blend in [('01 FULL Inward Slash','FULL158_Inward',1,56,1,48,0),('02 FULL Sword And Shield Slash','FULL158_Slash',46,158,1,107,10)]:
 t=ad.nla_tracks.new();t.name=name;s=t.strips.new(name,start,bpy.data.actions[action]);s.action_slot=s.action.slots[0];s.action_frame_start=lo;s.action_frame_end=hi;s.frame_start=start;s.scale=(end-start)/(hi-lo);s.extrapolation='HOLD';s.blend_type='REPLACE';s.use_auto_blend=False;s.blend_in=blend
sc.frame_start=1;sc.frame_end=158;sc.render.fps=30
# Preserve source leg lifts/steps. Stabilize only grounded contact intervals.
raw={side:[] for side in ['Left','Right']}
for f in range(1,159):
 sc.frame_set(f)
 for side in raw:
  foot=rig.pose.bones['mixamorig:'+side+'Foot'];raw[side].append((rig.matrix_world@foot.matrix).translation.copy())
contacts={}
for side,points in raw.items():
 floor=min(p.z for p in points);mask=[p.z<floor+.035 for p in points];segments=[];i=0
 while i<158:
  if not mask[i]:i+=1;continue
  start=i
  while i+1<158 and mask[i+1]:i+=1
  if i-start>=3:segments.append((start,i))
  i+=1
 contacts[side]=[(a+1,b+1) for a,b in segments]
 target=bpy.data.objects.new('AUTHOR_'+side+'ContactTarget',None);sc.collection.objects.link(target);target.empty_display_size=.06
 c=rig.pose.bones['mixamorig:'+side+'Leg'].constraints.new('IK');c.name='Full158 contact stabilization';c.target=target;c.chain_count=2;c.use_stretch=False
 for idx,p in enumerate(points):
  pos=p.copy();influence=0
  for start,end in segments:
   if start<=idx<=end:
    pos=points[(start+end)//2].copy();pos.z=points[idx].z
    influence=min(1,(idx-start)/3 if start>0 else 1,(end-idx)/3 if end<157 else 1);break
  target.location=pos;target.keyframe_insert(data_path='location',frame=idx+1);c.influence=influence;c.keyframe_insert(data_path='influence',frame=idx+1)
# Fixed preview grid.
for i in range(-4,5):
 for axis in [0,1]:
  bpy.ops.mesh.primitive_cube_add(size=1,location=(i*.5 if axis==0 else 0,i*.5 if axis==1 else 0,0));o=bpy.context.object;o.name='PREVIEW_Grid';o.scale=(.008,4,.004) if axis==0 else (4,.008,.004)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
# Constraint influence keys live in a separate active action, not on bone transforms.
if ad.action:ad.action.name='AUTHOR_Full158_ContactInfluence'
sc.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(D/'SunboundWarden_SunSlash_Full158_NLA.blend'))
(D/'contact_intervals.json').write_text(json.dumps(contacts,indent=2))
for f in range(1,159):
 sc.frame_set(f);sc.render.filepath=str(D/('Frame_%03d.png'%f));bpy.ops.render.render(write_still=True)
print('FULL_158_RENDERED')
