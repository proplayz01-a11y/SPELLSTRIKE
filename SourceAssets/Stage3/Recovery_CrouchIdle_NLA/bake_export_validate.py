import bpy,pathlib,json,hashlib,math
R=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/Recovery_CrouchIdle_NLA';name='SunboundWarden_Recovery_CrouchIdle';F=O/(name+'.fbx');B=O/(name+'_Baked.blend')
assert not F.exists() and not B.exists()
bpy.ops.wm.open_mainfile(filepath=str(O/(name+'_NLA.blend')));sc=bpy.context.scene;sc.name=name;rig=bpy.data.objects['Armature'];mesh=bpy.data.objects['model']
lo,hi=sc.frame_start,sc.frame_end;rest={b.name:b.matrix_local.copy() for b in rig.data.bones};parents={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
dense={};world={};hipsteps=[];feetsteps=[];last=None;lastfeet={}
for i in range(lo*4,hi*4+1):
 f=i/4;sc.frame_set(int(f),subframe=f%1);dense[f]={b.name:b.matrix_basis.decompose() for b in rig.pose.bones};world[f]={b.name:(rig.matrix_world@b.matrix).copy() for b in rig.pose.bones}
 q=world[f]['mixamorig:Hips'].to_quaternion()
 if last:
  angle=math.degrees(last.rotation_difference(q).angle);hipsteps.append([f,min(angle,360-angle)])
 last=q
 for side in ['Left','Right']:
  p=world[f]['mixamorig:'+side+'Foot'].translation
  if side in lastfeet:feetsteps.append([f,side,(p-lastfeet[side]).length])
  lastfeet[side]=p.copy()
a=bpy.data.actions.new(name);a.use_fake_user=True;rig.animation_data.action=a
for t in rig.animation_data.nla_tracks:t.mute=True
previous={}
for f,pose in dense.items():
 for b in rig.pose.bones:
  loc,q,scale=pose[b.name]
  if b.name in previous and q.dot(previous[b.name])<0:q.negate()
  previous[b.name]=q.copy();b.rotation_mode='QUATERNION';b.location=loc;b.rotation_quaternion=q;b.scale=scale
  for prop in ['location','rotation_quaternion','scale']:b.keyframe_insert(data_path=prop,frame=f,group=b.name)
for layer in a.layers:
 for strip in layer.strips:
  for bag in strip.channelbags:
   for fc in bag.fcurves:
    for k in fc.keyframe_points:k.interpolation='LINEAR'
sc.frame_set(lo);bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(B))
bpy.ops.export_scene.fbx(filepath=str(F),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_step=.25,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE',path_mode='STRIP',use_mesh_modifiers=False)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(F),anim_offset=0);rig=bpy.data.objects['Armature'];sc=bpy.context.scene
poserr=0;matrixerr=0;angleerr=0;hip=[]
for f,pose in world.items():
 sc.frame_set(int(f),subframe=f%1)
 for b in rig.pose.bones:
  m=rig.matrix_world@b.matrix;p=pose[b.name];poserr=max(poserr,(m.translation-p.translation).length);matrixerr=max(matrixerr,max(abs(m[i][j]-p[i][j]) for i in range(4) for j in range(4)))
  angle=math.degrees(m.to_quaternion().rotation_difference(p.to_quaternion()).angle);angleerr=max(angleerr,min(angle,360-angle))
 hip.append(list((rig.matrix_world@rig.pose.bones['mixamorig:Hips'].matrix).translation))
resterr=max(abs(b.matrix_local[i][j]-rest[b.name][i][j]) for b in rig.data.bones for i in range(4) for j in range(4));same=parents=={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
manifest=json.loads((O/'preservation_manifest.json').read_text());changed=[p for p,h in manifest.items() if hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()!=h]
report={'blender':bpy.app.version_string,'action':name,'frames':[lo,hi],'fps':30,'duration':(hi-lo)/30,'quarter_frame_samples':len(world),'fbx_max_bone_position_error_m':poserr,'fbx_max_world_matrix_error':matrixerr,'fbx_max_bone_angle_error_degrees':angleerr,'bone_count':len(parents),'names_and_parents_match':same,'rest_matrix_error':resterr,'armature_scale':list(rig.scale),'armature_rotation':list(rig.rotation_euler),'hip_horizontal_range_m':[max(p[i] for p in hip)-min(p[i] for p in hip) for i in [0,1]],'largest_hip_quarter_frame_angle':max(hipsteps,key=lambda p:p[1]),'largest_foot_quarter_frame_step_m':max(feetsteps,key=lambda p:p[2]),'join_hip_steps':[p for p in hipsteps if 17<=p[0]<=23],'imported_actions':[(act.name,list(act.frame_range)) for act in bpy.data.actions],'preserved_files':len(manifest),'changed_original_files':changed,'fbx_sha256':hashlib.sha256(F.read_bytes()).hexdigest()}
(O/'export_validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
assert same and resterr<.001 and poserr<.0001 and max(x[1] for x in hipsteps)<10 and not changed
print('EXPORT_VALIDATION_PASSED')
