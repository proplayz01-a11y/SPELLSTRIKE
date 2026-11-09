import bpy,pathlib
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/SourceAssets/Stage3/SunSlash_NLA');bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_SunSlash_NLA_Aligned.blend'));sc=bpy.context.scene;rig=bpy.data.objects['Armature'];sc.frame_set(1)
hips=rig.pose.bones['mixamorig:Hips'];initial=hips.location.copy()
# Root local X/Z are the horizontal axes; retain vertical Y breathing/compression.
for name in ['ADAPTED_Inward','ADAPTED_Slash','ADAPTED_Slash_Recovery_HemisphereAligned']:
 a=bpy.data.actions[name]
 for layer in a.layers:
  for st in layer.strips:
   for cb in st.channelbags:
    for fc in cb.fcurves:
     if fc.data_path=='pose.bones["mixamorig:Hips"].location' and fc.array_index in (0,2):
      val=initial[fc.array_index]
      for k in fc.keyframe_points:k.co.y=val;k.handle_left.y=val;k.handle_right.y=val
sc.frame_set(1);bpy.context.view_layer.update()
for side in ['Left','Right']:
 foot=rig.pose.bones['mixamorig:'+side+'Foot'];m=rig.matrix_world@foot.matrix
 target=bpy.data.objects.new('PREVIEW_'+side+'FootAnchor',None);sc.collection.objects.link(target);target.matrix_world=m;target.empty_display_size=.08
 shin=rig.pose.bones['mixamorig:'+side+'Leg'];ik=shin.constraints.new('IK');ik.name='SunSlash planted foot';ik.target=target;ik.chain_count=2;ik.use_stretch=False
 c=foot.constraints.new('COPY_ROTATION');c.name='SunSlash foot orientation';c.target=target;c.target_space='WORLD';c.owner_space='WORLD'
# Ground grid provides a fixed spatial reference for foot inspection.
for i in range(-4,5):
 for axis in [0,1]:
  bpy.ops.mesh.primitive_cube_add(size=1,location=(i*.5 if axis==0 else 0,i*.5 if axis==1 else 0,0));o=bpy.context.object;o.name='PREVIEW_Grid';o.scale=(.008,4,.004) if axis==0 else (4,.008,.004)
sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SunboundWarden_SunSlash_NLA_Final.blend'))
for f in range(1,52):
 sc.frame_set(f);sc.render.filepath=str(O/('Planted_%03d.png'%f));bpy.ops.render.render(write_still=True)
