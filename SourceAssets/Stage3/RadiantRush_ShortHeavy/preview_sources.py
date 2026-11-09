import bpy, pathlib, json, math
from mathutils import Matrix,Vector
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE'); O=R/'SourceAssets/Stage3/RadiantRush_ShortHeavy'
info=json.loads((O/'source_inspection.json').read_text())
bpy.ops.wm.open_mainfile(filepath=info['ReferenceMesh']['path'])
rig=bpy.data.objects['Armature'];rig.animation_data_clear();rig.animation_data_create();sc=bpy.context.scene
report={}
for tag in ['Crouch','Run','CurrentRush']:
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=info[tag]['path'],anim_offset=0)
 src=next(o for o in set(bpy.data.objects)-before if o.type=='ARMATURE')
 original=src.animation_data.action; original.name='SOURCE_UNMODIFIED_'+tag;original.use_fake_user=True
 lo,hi=map(int,original.frame_range);action=bpy.data.actions.new('PREVIEW_ADAPTED_'+tag);action.use_fake_user=True;rig.animation_data.action=action
 previous={};report[tag]={'source_range':[lo,hi]}
 for f in range(lo,hi+1):
  sc.frame_set(f)
  for pb in rig.pose.bones:
   sb=src.pose.bones[pb.name]
   if pb.parent:pos=pb.parent.matrix@(pb.parent.bone.matrix_local.inverted()@pb.bone.matrix_local).translation
   else:
    pos=sb.matrix.translation.copy();pos.x=0;pos.z=0
   pb.matrix=Matrix.LocRotScale(pos,sb.matrix.to_quaternion(),Vector((1,1,1)))
   bpy.context.view_layer.update();pb.rotation_mode='QUATERNION'
   if pb.name in previous and pb.rotation_quaternion.dot(previous[pb.name])<0:pb.rotation_quaternion.negate()
   previous[pb.name]=pb.rotation_quaternion.copy()
   for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(data_path=prop,frame=f,group=pb.name)
 # Retain untouched source action in file, remove only this temporary imported object.
 bpy.data.objects.remove(src,do_unlink=True)
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for fc in bag.fcurves:
     for k in fc.keyframe_points:k.interpolation='LINEAR'
sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=400;sc.render.resolution_y=440;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.fps=30
if sc.world is None:sc.world=bpy.data.worlds.new('PreviewWorld')
sc.world.color=(.10,.10,.10)
sh=sc.display.shading;sh.light='STUDIO';sh.color_type='SINGLE';sh.single_color=(.63,.47,.22);sh.show_shadows=True;sh.show_cavity=True
sh.background_type='WORLD'
bpy.ops.object.camera_add(location=(3,-5,2.1));cam=bpy.context.object;cam.name='PREVIEW_Camera';cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.65;sc.camera=cam
for i in range(-3,4):
 for axis in [0,1]:
  bpy.ops.mesh.primitive_cube_add(size=1,location=(i*.5 if axis==0 else 0,i*.5 if axis==1 else 0,0));o=bpy.context.object;o.name='PREVIEW_Grid';o.scale=(.006,3,.003) if axis==0 else (3,.006,.003)
for tag in report:
 act=bpy.data.actions['PREVIEW_ADAPTED_'+tag];rig.animation_data.action=act;rig.animation_data.action_slot=act.slots[0]
 for f in range(report[tag]['source_range'][0],report[tag]['source_range'][1]+1):
  sc.frame_set(f);sc.render.filepath=str(O/('Source_'+tag+'_%03d.png'%f));bpy.ops.render.render(write_still=True)
rig.animation_data.action=None;sc.frame_start=1;sc.frame_end=84
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SourceInspection.blend'))
print('SOURCE_PREVIEWS_DONE')
