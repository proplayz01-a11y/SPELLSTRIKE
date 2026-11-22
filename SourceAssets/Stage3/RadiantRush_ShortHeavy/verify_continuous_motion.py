import bpy,pathlib,json,math
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/RadiantRush_ShortHeavy')
bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_RadiantRush_ShortHeavy_NLA.blend'));sc=bpy.context.scene;rig=bpy.data.objects['Armature']
poses={};steps=[];last=None
for i in range(4,301):
 f=i/4;sc.frame_set(int(f),subframe=f%1);poses[f]={b.name:(rig.matrix_world@b.matrix).copy() for b in rig.pose.bones}
 q=poses[f]['mixamorig:Hips'].to_quaternion()
 if last:
  ang=math.degrees(last.rotation_difference(q).angle);steps.append((f,min(ang,360-ang)))
 last=q
for f in [17,17.5,18,18.5,19,19.5,20,20.5,21,33,33.5,34,34.5,35,35.5,36,36.5,37,67,67.5,68,68.5,69,69.5,70,70.5,71]:
 sc.frame_set(int(f),subframe=f%1);sc.render.filepath=str(O/('Fractional_%05.1f.png'%f));bpy.ops.render.render(write_still=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(O/'SunboundWarden_RadiantRush_ShortHeavy.fbx'),anim_offset=0)
rig=bpy.data.objects['Armature'];sc=bpy.context.scene;maxpos=(0,None,None);maxang=(0,None,None);hipsteps=[];last=None
for f,frame in poses.items():
 sc.frame_set(int(f),subframe=f%1)
 for b in rig.pose.bones:
  m=rig.matrix_world@b.matrix;delta=(m.translation-frame[b.name].translation).length
  ang=math.degrees(m.to_quaternion().rotation_difference(frame[b.name].to_quaternion()).angle);ang=min(ang,360-ang)
  if delta>maxpos[0]:maxpos=(delta,f,b.name)
  if ang>maxang[0]:maxang=(ang,f,b.name)
 q=(rig.matrix_world@rig.pose.bones['mixamorig:Hips'].matrix).to_quaternion()
 if last:
  ang=math.degrees(last.rotation_difference(q).angle);hipsteps.append((f,min(ang,360-ang)))
 last=q
report={'sample_interval_frames':.25,'samples':len(poses),'nla_max_hip_step_degrees':max(steps,key=lambda x:x[1]),'fbx_max_hip_step_degrees':max(hipsteps,key=lambda x:x[1]),'nla_to_fbx_max_bone_position_error_m':maxpos,'nla_to_fbx_max_bone_angle_error_degrees':maxang,'repaired_neighborhood_nla_steps':[(f,a) for f,a in steps if 67<=f<=71],'repaired_neighborhood_fbx_steps':[(f,a) for f,a in hipsteps if 67<=f<=71]}
(O/'continuous_motion_validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
assert max(a for _,a in steps)<12 and max(a for _,a in hipsteps)<12
assert maxpos[0]<.02 and maxang[0]<5
print('CONTINUOUS_VALIDATION_PASSED')
