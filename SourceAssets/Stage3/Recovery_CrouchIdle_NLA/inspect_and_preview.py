import bpy,pathlib,json,hashlib,math
from mathutils import Matrix,Vector
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/Recovery_CrouchIdle_NLA';A=R/'Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1'
paths={tag:pathlib.Path('C:/Users/David/Downloads/STAGE 3 ENEMY NO 1 FINAL@'+name+'.fbx') for tag,name in [('Crouch','Sword And Shield Crouch'),('CrouchIdle','Sword And Shield Crouch Idle'),('SlashReference','Sword And Shield Slash (1)')]}
base=R/'SourceAssets/Stage3/SunboundWarden_Rig_HELMET_FIXED.blend'
manifest={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in list(A.rglob('*'))+list(paths.values())+[base] if p.is_file()}
(O/'preservation_manifest.json').write_text(json.dumps(manifest,indent=2))
bpy.ops.wm.open_mainfile(filepath=str(base));sc=bpy.context.scene;rig=bpy.data.objects['Armature'];rig.animation_data_clear();rig.animation_data_create();report={}
for tag,path in paths.items():
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(path),anim_offset=0);src=next(o for o in set(bpy.data.objects)-before if o.type=='ARMATURE')
 a=src.animation_data.action;original_name=a.name;lo,hi=map(int,a.frame_range);a.name='SOURCE_UNMODIFIED_'+tag;a.use_fake_user=True
 assert set(b.name for b in src.data.bones)==set(b.name for b in rig.data.bones)
 report[tag]={'file':str(path),'original_imported_action':original_name,'frames':[lo,hi],'fps':sc.render.fps,'bones':len(src.data.bones),'sha256':manifest[str(path)],'samples':[]}
 dest=bpy.data.actions.new('ADAPTED_'+tag+'_Canonical_InPlace');dest.use_fake_user=True;rig.animation_data.action=dest;prev={}
 for f in range(lo,hi+1):
  sc.frame_set(f)
  for pb in rig.pose.bones:
   sb=src.pose.bones[pb.name]
   if pb.parent:pos=pb.parent.matrix@(pb.parent.bone.matrix_local.inverted()@pb.bone.matrix_local).translation
   else:pos=sb.matrix.translation.copy();pos.x=0;pos.z=0
   pb.matrix=Matrix.LocRotScale(pos,sb.matrix.to_quaternion(),Vector((1,1,1)));bpy.context.view_layer.update();pb.rotation_mode='QUATERNION'
   if pb.name in prev and pb.rotation_quaternion.dot(prev[pb.name])<0:pb.rotation_quaternion.negate()
   prev[pb.name]=pb.rotation_quaternion.copy()
   for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(data_path=prop,frame=f,group=pb.name)
  report[tag]['samples'].append({'frame':f,'hips':list(rig.matrix_world@rig.pose.bones['mixamorig:Hips'].head),'head':list(rig.matrix_world@rig.pose.bones['mixamorig:Head'].head)})
 for layer in dest.layers:
  for st in layer.strips:
   for bag in st.channelbags:
    for fc in bag.fcurves:
     for k in fc.keyframe_points:k.interpolation='LINEAR'
 bpy.data.objects.remove(src,do_unlink=True)
 print(tag,report[tag]['frames'],report[tag]['fps'],original_name,flush=True)
sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=400;sc.render.resolution_y=440;sc.render.resolution_percentage=100;sc.render.fps=30;sc.render.image_settings.file_format='PNG'
if not sc.world:sc.world=bpy.data.worlds.new('PreviewWorld')
sc.world.color=(.10,.10,.10);sh=sc.display.shading;sh.light='STUDIO';sh.color_type='SINGLE';sh.single_color=(.63,.47,.22);sh.show_shadows=True;sh.show_cavity=True;sh.background_type='WORLD'
bpy.ops.object.camera_add(location=(3,-5,2.1));cam=bpy.context.object;cam.name='PREVIEW_Camera';cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.65;sc.camera=cam
for i in range(-3,4):
 for axis in [0,1]:
  bpy.ops.mesh.primitive_cube_add(size=1,location=(i*.5 if axis==0 else 0,i*.5 if axis==1 else 0,0));o=bpy.context.object;o.name='PREVIEW_Grid';o.scale=(.006,3,.003) if axis==0 else (3,.006,.003)
for tag,data in report.items():
 a=bpy.data.actions['ADAPTED_'+tag+'_Canonical_InPlace'];rig.animation_data.action=a;rig.animation_data.action_slot=a.slots[0]
 for f in range(data['frames'][0],data['frames'][1]+1):
  sc.frame_set(f);sc.render.filepath=str(O/('Source_'+tag+'_%03d.png'%f));bpy.ops.render.render(write_still=True)
rig.animation_data.action=None
(O/'source_inspection.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SourceInspection.blend'))
print('SOURCE_INSPECTION_DONE')
