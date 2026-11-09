import bpy,pathlib
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/SunSlash_NLA')
bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_SunSlash_NLA.blend'));sc=bpy.context.scene;rig=bpy.data.objects['Armature'];ad=rig.animation_data
for t in ad.nla_tracks:t.mute=True
ad.action=bpy.data.actions['ADAPTED_Slash'];ad.action_slot=ad.action.slots[0];sc.frame_set(34);qs={b.name:b.rotation_quaternion.copy() for b in rig.pose.bones}
sc.frame_set(94);flip={b.name for b in rig.pose.bones if qs[b.name].dot(b.rotation_quaternion)<0}
a=ad.action.copy();a.name='ADAPTED_Slash_Recovery_HemisphereAligned';a.use_fake_user=True
for layer in a.layers:
 for st in layer.strips:
  for cb in st.channelbags:
   for fc in cb.fcurves:
    if fc.data_path.endswith('rotation_quaternion') and any('"'+n+'"' in fc.data_path for n in flip):
     for k in fc.keyframe_points:k.co.y*=-1;k.handle_left.y*=-1;k.handle_right.y*=-1
ad.action=None
ad.nla_tracks[2].strips[0].action=a;ad.nla_tracks[2].strips[0].action_slot=a.slots[0]
for t in ad.nla_tracks:t.mute=False
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SunboundWarden_SunSlash_NLA_Aligned.blend'))
for f in range(38,52):
 sc.frame_set(f);sc.render.filepath=str(O/('Merged_%03d.png'%f));bpy.ops.render.render(write_still=True)
print('Aligned bones',sorted(flip))
