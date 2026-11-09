import bpy,pathlib,json,math
from mathutils import Matrix,Vector
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/SunSlash_NLA'
bpy.ops.wm.open_mainfile(filepath=str(O/'SourceInspection.blend'))
sc=bpy.context.scene;rig=bpy.data.objects['Armature'];rig.animation_data_clear();rig.animation_data_create()
for tag,fn in [('Inward','Stable Sword Inward Slash'),('Slash','Sword And Shield Slash')]:
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath='C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@'+fn+'.fbx')
 src=next(o for o in set(bpy.data.objects)-before if o.type=='ARMATURE');a=bpy.data.actions.new('ADAPTED_'+tag);a.use_fake_user=True;rig.animation_data.action=a
 end=int(src.animation_data.action.frame_range[1]);prev={}
 for f in range(1,end+1):
  sc.frame_set(f)
  for pb in rig.pose.bones:
   sb=src.pose.bones[pb.name]
   if pb.parent:
    rel=pb.parent.bone.matrix_local.inverted()@pb.bone.matrix_local
    pos=pb.parent.matrix@rel.translation
   else:pos=sb.matrix.translation.copy()
   q=sb.matrix.to_quaternion();pb.matrix=Matrix.LocRotScale(pos,q,Vector((1,1,1)));bpy.context.view_layer.update()
   pb.rotation_mode='QUATERNION'
   if pb.name in prev and pb.rotation_quaternion.dot(prev[pb.name])<0:pb.rotation_quaternion.negate()
   prev[pb.name]=pb.rotation_quaternion.copy()
   for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(data_path=prop,frame=f,group=pb.name)
 bpy.data.objects.remove(src,do_unlink=True)
rig.animation_data.action=None
# Editable NLA stack: first attack only, then closing recovery from same slash source.
def strip(name,action,start,lo,hi,blend):
 t=rig.animation_data.nla_tracks.new();t.name=name;s=t.strips.new(name,int(start),bpy.data.actions[action]);s.action_slot=bpy.data.actions[action].slots[0];s.action_frame_start=lo;s.action_frame_end=hi;s.frame_start=start;s.frame_end=start+hi-lo;s.extrapolation='HOLD';s.blend_type='REPLACE';s.use_auto_blend=False;s.blend_in=blend;return s
strip('01 Inward wind-up','ADAPTED_Inward',1,1,9,0)
strip('02 First shield slash','ADAPTED_Slash',5,1,42,4)
strip('03 Natural recovery','ADAPTED_Slash',38,94,107,8)
sc.frame_start=1;sc.frame_end=51;sc.render.fps=30
# Keep source staging available; preview grid and camera are not exported.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
for a in bpy.data.screens:
 for area in a.areas:
  if area.type=='DOPESHEET_EDITOR':area.type='NLA_EDITOR'
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_perspective='CAMERA'
sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SunboundWarden_SunSlash_NLA.blend'))
for f in range(1,52):
 sc.frame_set(f);sc.render.filepath=str(O/('Merged_%03d.png'%f));bpy.ops.render.render(write_still=True)
print('NLA_PREVIEW_DONE')
