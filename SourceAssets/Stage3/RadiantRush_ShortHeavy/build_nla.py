import bpy,pathlib,json,math
from mathutils import Vector
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/RadiantRush_ShortHeavy'
bpy.ops.wm.open_mainfile(filepath=str(O/'SourceInspection.blend'))
sc=bpy.context.scene;rig=bpy.data.objects['Armature'];ad=rig.animation_data
for t in list(ad.nla_tracks):ad.nla_tracks.remove(t)
acts={}
for tag in ['Crouch','Run','CurrentRush']:
 a=bpy.data.actions['PREVIEW_ADAPTED_'+tag].copy();a.name='ShortHeavy_'+tag+'_Canonical_InPlace';a.use_fake_user=True;acts[tag]=a
# The source has one corrupted hip quaternion at FBX frame 78 (Unity relative 76).
# Repair only the dedicated copy, retaining the raw source action for inspection.
a=acts['CurrentRush'];ad.action=a;ad.action_slot=a.slots[0];pb=rig.pose.bones['mixamorig:Hips'];qs=[]
for f in [77,79]:sc.frame_set(f);qs.append(pb.rotation_quaternion.copy())
sc.frame_set(78);pb.rotation_quaternion=qs[0].slerp(qs[1],.5);pb.keyframe_insert(data_path='rotation_quaternion',frame=78,group=pb.name)
# The same orientation has two quaternion signs. Keep all adjacent component
# keys on one hemisphere, including the repaired key, to prevent subframe flips.
for layer in a.layers:
 for ast in layer.strips:
  for bag in ast.channelbags:
   for bone in rig.pose.bones:
    path='pose.bones["'+bone.name+'"].rotation_quaternion'
    curves=sorted([fc for fc in bag.fcurves if fc.data_path==path],key=lambda fc:fc.array_index)
    if len(curves)!=4:continue
    last=None
    for keys in zip(*(fc.keyframe_points for fc in curves)):
     q=[k.co.y for k in keys]
     if last is not None and sum(x*y for x,y in zip(last,q))<0:
      for k in keys:k.co.y=-k.co.y;k.handle_left.y=-k.handle_left.y;k.handle_right.y=-k.handle_right.y
      q=[-v for v in q]
     last=q
ad.action=None
spec=[('01 Crouch anticipation - full authored timing','Crouch',1,0,18,0),('02 Prepared heavy run - one short burst','Run',17,0,20,4),('03 Existing sword release and recovery','CurrentRush',33,42,84,4)]
for name,tag,start,lo,hi,blend in spec:
 t=ad.nla_tracks.new();t.name=name;s=t.strips.new(name,start,acts[tag]);s.action_slot=acts[tag].slots[0]
 s.action_frame_start=lo;s.action_frame_end=hi;s.frame_start=start;s.scale=1;s.repeat=1;s.extrapolation='HOLD';s.blend_type='REPLACE';s.use_auto_blend=False;s.blend_in=blend;s.blend_out=0
sc.frame_start=1;sc.frame_end=75;sc.render.fps=30;sc.render.fps_base=1
for m in list(sc.timeline_markers):sc.timeline_markers.remove(m)
for label,f in [('Crouch - Source 0',1),('Push off - Source 9',10),('Run blend begins',17),('Run fully weighted',21),('Sword blend begins - Source 42',33),('Sword fully weighted',37),('Release reference - Source 56',47),('Old root issue - Source 77',68),('Repaired root - Source 78',69),('Old root issue - Source 79',70),('End one shot',75)]:sc.timeline_markers.new(label,frame=f)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='DOPESHEET_EDITOR':area.type='NLA_EDITOR'
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
sc.frame_set(1)
path=O/'SunboundWarden_RadiantRush_ShortHeavy_NLA.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(path))
samples=[];prev={};steps=[]
for f in range(1,76):
 sc.frame_set(f);row={'frame':f,'object_location':list(rig.location),'object_rotation':list(rig.rotation_euler),'bones':{}}
 for b in rig.pose.bones:
  pos=rig.matrix_world@b.head;q=(rig.matrix_world@b.matrix).to_quaternion()
  row['bones'][b.name]={'head':list(pos),'q':list(q)}
  if b.name in prev:
   p0,q0=prev[b.name];angle=math.degrees(q0.rotation_difference(q).angle);angle=min(angle,360-angle)
   steps.append({'frame':f,'bone':b.name,'angle':angle,'distance':(pos-p0).length})
  prev[b.name]=(pos.copy(),q.copy())
 samples.append(row)
 sc.render.filepath=str(O/('Final_%03d.png'%f));bpy.ops.render.render(write_still=True)
cam=sc.camera;cam.location=(4,-.4,1.6);cam.rotation_euler=(Vector((0,0,.95))-cam.location).to_track_quat('-Z','Y').to_euler()
for f in range(1,76):
 sc.frame_set(f);sc.render.filepath=str(O/('Side_%03d.png'%f));bpy.ops.render.render(write_still=True)
report={'frame_range':[1,75],'fps':30,'duration':74/30,'strips':[{'name':s.name,'action':s.action.name,'source':[s.action_frame_start,s.action_frame_end],'timeline':[s.frame_start,s.frame_end],'scale':s.scale,'repeat':s.repeat,'blend_in':s.blend_in,'blend_out':s.blend_out,'extrapolation':s.extrapolation,'blend_type':s.blend_type} for t in ad.nla_tracks for s in t.strips], 'samples':samples,'largest_angular_steps':sorted(steps,key=lambda x:x['angle'],reverse=True)[:20],'hip_steps':[s for s in steps if s['bone']=='mixamorig:Hips'],'foot_steps':[s for s in steps if s['bone'] in ['mixamorig:LeftFoot','mixamorig:RightFoot']]}
(O/'nla_validation.json').write_text(json.dumps(report,indent=2))
print('NLA_BUILD_AND_RENDER_DONE')
